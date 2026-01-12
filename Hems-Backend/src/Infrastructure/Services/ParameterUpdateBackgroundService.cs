using System.Collections.Concurrent;
using System.Threading.Channels;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that processes parameter updates asynchronously.
/// Features:
/// - Device-level locking to prevent concurrent updates to the same device
/// - Rollback/compensation mechanism for atomic batch updates
/// - Exponential backoff retry via RPC executor
/// </summary>
public class ParameterUpdateBackgroundService : BackgroundService, IParameterUpdateQueueService
{
    private readonly ILogger<ParameterUpdateBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Channel<ParameterUpdateQueueRequest> _channel;

    /// <summary>
    /// Device-level locks to prevent concurrent updates to the same device.
    /// Each device gets its own SemaphoreSlim to ensure sequential processing.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _deviceLocks = new();

    /// <summary>
    /// Lock timeout in milliseconds - how long to wait for a device lock
    /// </summary>
    private const int DeviceLockTimeoutMs = 120000; // 2 minutes

    public ParameterUpdateBackgroundService(
        ILogger<ParameterUpdateBackgroundService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        // Bounded channel to prevent memory issues with too many queued requests
        _channel = Channel.CreateBounded<ParameterUpdateQueueRequest>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>
    /// Queue a parameter update request for background processing
    /// </summary>
    public async ValueTask QueueParameterUpdateAsync(ParameterUpdateQueueRequest request, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(request, cancellationToken);
        _logger.LogInformation(
            "Queued parameter update request {CorrelationId} for device {DeviceId} with {Count} parameters",
            request.CorrelationId,
            request.DeviceId,
            request.Parameters.Count);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Parameter Update Background Service started (with device locking and rollback support)");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessParameterUpdateWithLockAsync(request, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing parameter update request {CorrelationId}",
                        request.CorrelationId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Parameter Update Background Service stopping");
        }

        _logger.LogInformation("Parameter Update Background Service stopped");
    }

    /// <summary>
    /// Gets or creates a semaphore lock for a specific device
    /// </summary>
    private SemaphoreSlim GetDeviceLock(Guid deviceId)
    {
        return _deviceLocks.GetOrAdd(deviceId, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// Processes a parameter update request with device-level locking
    /// </summary>
    private async Task ProcessParameterUpdateWithLockAsync(ParameterUpdateQueueRequest request, CancellationToken cancellationToken)
    {
        var deviceLock = GetDeviceLock(request.DeviceId);

        _logger.LogDebug(
            "Attempting to acquire lock for device {DeviceId} (CorrelationId: {CorrelationId})",
            request.DeviceId, request.CorrelationId);

        // Try to acquire the device lock with timeout
        if (!await deviceLock.WaitAsync(DeviceLockTimeoutMs, cancellationToken))
        {
            _logger.LogWarning(
                "Failed to acquire device lock for {DeviceId} within {Timeout}ms - another update is in progress",
                request.DeviceId, DeviceLockTimeoutMs);

            // Notify failure via SignalR
            using var scope = _serviceScopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IDeviceSettingsNotificationService>();

            await notificationService.NotifyParameterUpdateCompletedAsync(new ParameterUpdateCompletedMessage
            {
                CorrelationId = request.CorrelationId,
                DeviceId = request.DeviceId,
                TotalParameters = request.Parameters.Count,
                SuccessCount = 0,
                FailedCount = request.Parameters.Count,
                CompletedAt = DateTime.UtcNow,
                Duration = TimeSpan.Zero
            }, cancellationToken);

            return;
        }

        try
        {
            _logger.LogDebug(
                "Acquired lock for device {DeviceId} (CorrelationId: {CorrelationId})",
                request.DeviceId, request.CorrelationId);

            await ProcessParameterUpdateWithRollbackAsync(request, cancellationToken);
        }
        finally
        {
            deviceLock.Release();
            _logger.LogDebug(
                "Released lock for device {DeviceId} (CorrelationId: {CorrelationId})",
                request.DeviceId, request.CorrelationId);
        }
    }

    /// <summary>
    /// Processes parameter updates with rollback support.
    /// If any parameter fails, all previously successful updates are reverted.
    /// </summary>
    private async Task ProcessParameterUpdateWithRollbackAsync(ParameterUpdateQueueRequest request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var rpcExecutor = scope.ServiceProvider.GetRequiredService<IDeviceRpcExecutorService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IDeviceSettingsNotificationService>();

        // Notify start
        await notificationService.NotifyParameterUpdateStartedAsync(new ParameterUpdateStartedMessage
        {
            CorrelationId = request.CorrelationId,
            DeviceId = request.DeviceId,
            TotalParameters = request.Parameters.Count,
            StartedAt = startTime
        }, cancellationToken);

        // Get all parameter values for this device
        var parameterIds = request.Parameters.Select(p => p.ParameterId).ToList();
        var parameterValues = await context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Where(sdpv =>
                sdpv.SetupDeviceId == request.SetupDeviceId &&
                parameterIds.Contains(sdpv.ParameterId))
            .ToDictionaryAsync(pv => pv.ParameterId, cancellationToken);

        // Track original values for rollback
        var originalValues = new Dictionary<Guid, OriginalParameterValue>();
        var successfulUpdates = new List<SuccessfulUpdate>();

        int successCount = 0;
        int failedCount = 0;
        int currentIndex = 0;
        bool needsRollback = false;
        string? rollbackReason = null;

        // Process each parameter one by one
        foreach (var param in request.Parameters)
        {
            currentIndex++;

            try
            {
                if (!parameterValues.TryGetValue(param.ParameterId, out var paramValue))
                {
                    _logger.LogWarning("Parameter {ParameterId} not found for device {DeviceId}",
                        param.ParameterId, request.DeviceId);

                    failedCount++;
                    needsRollback = true;
                    rollbackReason = $"Parameter '{param.ParameterName}' not found";

                    await notificationService.NotifyParameterUpdateFailedAsync(new ParameterUpdateFailedMessage
                    {
                        CorrelationId = request.CorrelationId,
                        DeviceId = request.DeviceId,
                        ParameterId = param.ParameterId,
                        ParameterName = param.ParameterName,
                        ErrorMessage = "Parameter not found",
                        CurrentIndex = currentIndex,
                        TotalParameters = request.Parameters.Count,
                        FailedAt = DateTime.UtcNow
                    }, cancellationToken);

                    break; // Stop processing on first failure
                }

                // Store original value for rollback
                originalValues[param.ParameterId] = new OriginalParameterValue
                {
                    ParameterId = param.ParameterId,
                    ParameterName = paramValue.Parameter.Name,
                    OriginalValue = paramValue.Value
                };

                // Execute RPC call with retry support
                var parameterUpdates = new Dictionary<string, string>
                {
                    [paramValue.Parameter.Name] = param.Value
                };

                var rpcResults = await rpcExecutor.ExecuteParameterUpdatesAsync(
                    request.ThingsBoardDeviceId,
                    parameterUpdates,
                    RpcRetryOptions.Default,
                    cancellationToken);

                var rpcResult = rpcResults.FirstOrDefault();

                if (rpcResult.Value?.Success == true)
                {
                    // Update database
                    paramValue.Value = param.Value;
                    await context.SaveChangesAsync(cancellationToken);

                    successCount++;

                    // Track successful update for potential rollback
                    successfulUpdates.Add(new SuccessfulUpdate
                    {
                        ParameterId = param.ParameterId,
                        ParameterName = paramValue.Parameter.Name,
                        NewValue = param.Value,
                        OriginalValue = originalValues[param.ParameterId].OriginalValue
                    });

                    await notificationService.NotifyParameterUpdatedAsync(new ParameterUpdatedMessage
                    {
                        CorrelationId = request.CorrelationId,
                        DeviceId = request.DeviceId,
                        ParameterId = param.ParameterId,
                        ParameterName = paramValue.Parameter.Name,
                        NewValue = param.Value,
                        RpcMethod = rpcResult.Value?.RpcMethod,
                        CurrentIndex = currentIndex,
                        TotalParameters = request.Parameters.Count,
                        UpdatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    _logger.LogDebug(
                        "Successfully updated parameter {ParameterName} ({Index}/{Total})",
                        paramValue.Parameter.Name,
                        currentIndex,
                        request.Parameters.Count);
                }
                else
                {
                    failedCount++;
                    needsRollback = true;
                    rollbackReason = rpcResult.Value?.ErrorMessage ?? "RPC call failed";

                    await notificationService.NotifyParameterUpdateFailedAsync(new ParameterUpdateFailedMessage
                    {
                        CorrelationId = request.CorrelationId,
                        DeviceId = request.DeviceId,
                        ParameterId = param.ParameterId,
                        ParameterName = paramValue.Parameter.Name,
                        ErrorMessage = rpcResult.Value?.ErrorMessage ?? "RPC call failed",
                        RpcMethod = rpcResult.Value?.RpcMethod,
                        CurrentIndex = currentIndex,
                        TotalParameters = request.Parameters.Count,
                        FailedAt = DateTime.UtcNow
                    }, cancellationToken);

                    _logger.LogWarning(
                        "Failed to update parameter {ParameterName} (ErrorCategory: {ErrorCategory}): {Error}",
                        paramValue.Parameter.Name,
                        rpcResult.Value?.ErrorCategory,
                        rpcResult.Value?.ErrorMessage);

                    break; // Stop processing on first failure
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                needsRollback = true;
                rollbackReason = ex.Message;

                _logger.LogError(ex, "Exception updating parameter {ParameterId}", param.ParameterId);

                await notificationService.NotifyParameterUpdateFailedAsync(new ParameterUpdateFailedMessage
                {
                    CorrelationId = request.CorrelationId,
                    DeviceId = request.DeviceId,
                    ParameterId = param.ParameterId,
                    ParameterName = param.ParameterName,
                    ErrorMessage = ex.Message,
                    CurrentIndex = currentIndex,
                    TotalParameters = request.Parameters.Count,
                    FailedAt = DateTime.UtcNow
                }, cancellationToken);

                break; // Stop processing on exception
            }
        }

        // Perform rollback if needed
        if (needsRollback && successfulUpdates.Count > 0)
        {
            _logger.LogWarning(
                "Rolling back {Count} successful updates due to failure: {Reason}",
                successfulUpdates.Count, rollbackReason);

            var rollbackSuccess = await PerformRollbackAsync(
                request,
                successfulUpdates,
                parameterValues,
                rpcExecutor,
                context,
                notificationService,
                cancellationToken);

            if (rollbackSuccess)
            {
                _logger.LogInformation(
                    "Successfully rolled back {Count} parameter updates for device {DeviceId}",
                    successfulUpdates.Count, request.DeviceId);

                // Reset success count since everything was rolled back
                successCount = 0;
                failedCount = request.Parameters.Count;
            }
            else
            {
                _logger.LogError(
                    "CRITICAL: Partial rollback failure for device {DeviceId} - database and device may be inconsistent!",
                    request.DeviceId);
            }
        }

        var completedAt = DateTime.UtcNow;
        var duration = completedAt - startTime;

        // Notify completion
        await notificationService.NotifyParameterUpdateCompletedAsync(new ParameterUpdateCompletedMessage
        {
            CorrelationId = request.CorrelationId,
            DeviceId = request.DeviceId,
            TotalParameters = request.Parameters.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            CompletedAt = completedAt,
            Duration = duration
        }, cancellationToken);

        _logger.LogInformation(
            "Completed parameter update {CorrelationId}: {Success}/{Total} succeeded in {Duration}ms (Rollback: {RollbackPerformed})",
            request.CorrelationId,
            successCount,
            request.Parameters.Count,
            duration.TotalMilliseconds,
            needsRollback && successfulUpdates.Count > 0);
    }

    /// <summary>
    /// Performs rollback of successful updates by restoring original values
    /// </summary>
    private async Task<bool> PerformRollbackAsync(
        ParameterUpdateQueueRequest request,
        List<SuccessfulUpdate> successfulUpdates,
        Dictionary<Guid, Domain.Entities.SetupDeviceParameterValue> parameterValues,
        IDeviceRpcExecutorService rpcExecutor,
        IApplicationDbContext context,
        IDeviceSettingsNotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var allRollbacksSuccessful = true;

        // Rollback in reverse order
        foreach (var update in successfulUpdates.AsEnumerable().Reverse())
        {
            try
            {
                _logger.LogDebug(
                    "Rolling back parameter {ParameterName} from {NewValue} to {OriginalValue}",
                    update.ParameterName, update.NewValue, update.OriginalValue);

                // Revert device via RPC (with aggressive retry for rollback)
                var rollbackParams = new Dictionary<string, string>
                {
                    [update.ParameterName] = update.OriginalValue
                };

                var rpcResult = await rpcExecutor.ExecuteParameterUpdatesAsync(
                    request.ThingsBoardDeviceId,
                    rollbackParams,
                    RpcRetryOptions.Aggressive, // Use aggressive retry for rollback
                    cancellationToken);

                var result = rpcResult.FirstOrDefault();

                if (result.Value?.Success == true)
                {
                    // Revert database
                    if (parameterValues.TryGetValue(update.ParameterId, out var paramValue))
                    {
                        paramValue.Value = update.OriginalValue;
                        await context.SaveChangesAsync(cancellationToken);
                    }

                    _logger.LogInformation(
                        "Successfully rolled back parameter {ParameterName}",
                        update.ParameterName);
                }
                else
                {
                    allRollbacksSuccessful = false;
                    _logger.LogError(
                        "Failed to rollback parameter {ParameterName}: {Error}",
                        update.ParameterName, result.Value?.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                allRollbacksSuccessful = false;
                _logger.LogError(ex,
                    "Exception during rollback of parameter {ParameterName}",
                    update.ParameterName);
            }
        }

        return allRollbacksSuccessful;
    }

    /// <summary>
    /// Stores original parameter value for rollback
    /// </summary>
    private record OriginalParameterValue
    {
        public Guid ParameterId { get; init; }
        public string ParameterName { get; init; } = string.Empty;
        public string OriginalValue { get; init; } = string.Empty;
    }

    /// <summary>
    /// Tracks a successful update for potential rollback
    /// </summary>
    private record SuccessfulUpdate
    {
        public Guid ParameterId { get; init; }
        public string ParameterName { get; init; } = string.Empty;
        public string NewValue { get; init; } = string.Empty;
        public string OriginalValue { get; init; } = string.Empty;
    }
}
