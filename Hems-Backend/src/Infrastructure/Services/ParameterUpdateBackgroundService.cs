using System.Collections.Concurrent;
using System.Threading.Channels;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that processes parameter updates asynchronously.
/// Features:
/// - Device-level locking to prevent concurrent updates to the same device
/// - Write-Verify-Retry pattern: write value, read back to verify, retry up to 3 times
/// - Continue on failure (no rollback) - reports failed parameters at end
/// - Cancellation support via correlation ID
/// </summary>
public class ParameterUpdateBackgroundService : BackgroundService, IParameterUpdateQueueService
{
    private readonly ILogger<ParameterUpdateBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDeviceLockService _deviceLockService;
    private readonly Channel<ParameterUpdateQueueRequest> _channel;

    /// <summary>
    /// Active cancellation tokens by correlation ID for cancellation support.
    /// </summary>
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeCancellations = new();

    /// <summary>
    /// Operation state cache for late-subscribing clients (keeps last 5 minutes of completed operations)
    /// </summary>
    private readonly ConcurrentDictionary<string, BatchOperationStateSnapshot> _operationStates = new();

    /// <summary>
    /// How long to keep completed operation states for late-subscribing clients
    /// </summary>
    private static readonly TimeSpan StateRetentionDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Lock timeout in milliseconds - how long to wait for a device lock
    /// </summary>
    private const int DeviceLockTimeoutMs = 120000; // 2 minutes

    /// <summary>
    /// Maximum retry attempts for each parameter write-verify cycle
    /// </summary>
    private const int MaxWriteVerifyRetries = 3;

    /// <summary>
    /// Delay between write and verify read in milliseconds
    /// </summary>
    private const int WriteVerifyDelayMs = 150;

    public ParameterUpdateBackgroundService(
        ILogger<ParameterUpdateBackgroundService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IDeviceLockService deviceLockService)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _deviceLockService = deviceLockService;

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

    /// <summary>
    /// Cancel an in-progress parameter update operation by correlation ID
    /// </summary>
    public bool CancelOperation(string correlationId)
    {
        if (_activeCancellations.TryGetValue(correlationId, out var cts))
        {
            _logger.LogInformation("Cancelling operation {CorrelationId}", correlationId);
            cts.Cancel();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if an operation is currently active
    /// </summary>
    public bool IsOperationActive(string correlationId)
    {
        return _activeCancellations.ContainsKey(correlationId);
    }

    /// <summary>
    /// Get the current state of a batch operation (for late-subscribing clients)
    /// </summary>
    public BatchOperationStateSnapshot? GetOperationState(string correlationId)
    {
        if (_operationStates.TryGetValue(correlationId, out var state))
        {
            _logger.LogDebug("Retrieved operation state for {CorrelationId}: Status={Status}", correlationId, state.Status);
            return state;
        }
        return null;
    }

    /// <summary>
    /// Initialize operation state as "pending" before queuing
    /// Called by command handler to ensure state exists when API returns
    /// </summary>
    public void InitializeOperationState(string correlationId, Guid deviceId, int totalParameters)
    {
        var state = new BatchOperationStateSnapshot
        {
            CorrelationId = correlationId,
            DeviceId = deviceId,
            Status = "pending",
            TotalParameters = totalParameters,
            CurrentIndex = 0,
            SuccessCount = 0,
            FailedCount = 0,
            SkippedCount = 0,
            WasCancelled = false,
            StartedAt = null,
            CompletedAt = null,
            Duration = null,
            FailedParameters = new List<FailedParameterSnapshot>()
        };
        _operationStates[correlationId] = state;
        _logger.LogDebug("Initialized pending operation state for {CorrelationId}", correlationId);
    }

    /// <summary>
    /// Update and store the current operation state
    /// </summary>
    private void UpdateOperationState(BatchOperationStateSnapshot state)
    {
        _operationStates[state.CorrelationId] = state;
        _logger.LogDebug("Updated operation state for {CorrelationId}: Status={Status}, Progress={Current}/{Total}",
            state.CorrelationId, state.Status, state.CurrentIndex, state.TotalParameters);
    }

    /// <summary>
    /// Cleanup old completed operation states
    /// </summary>
    private void CleanupOldStates()
    {
        var cutoff = DateTime.UtcNow - StateRetentionDuration;
        var keysToRemove = _operationStates
            .Where(kvp => kvp.Value.CompletedAt.HasValue && kvp.Value.CompletedAt.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _operationStates.TryRemove(key, out _);
            _logger.LogDebug("Removed expired operation state: {CorrelationId}", key);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Parameter Update Background Service started (with write-verify-retry pattern)");

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
    /// Processes a parameter update request with device-level locking
    /// </summary>
    private async Task ProcessParameterUpdateWithLockAsync(ParameterUpdateQueueRequest request, CancellationToken stoppingToken)
    {
        _logger.LogDebug(
            "Attempting to acquire lock for device {DeviceId} (CorrelationId: {CorrelationId})",
            request.DeviceId, request.CorrelationId);

        // Try to acquire the device lock with timeout using shared lock service
        var lockHandle = await _deviceLockService.AcquireLockAsync(request.DeviceId, DeviceLockTimeoutMs, stoppingToken);

        if (lockHandle == null)
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
                SkippedCount = 0,
                CompletedAt = DateTime.UtcNow,
                Duration = TimeSpan.Zero,
                WasCancelled = false,
                FailedParameters = request.Parameters.Select(p => new FailedParameterInfo
                {
                    ParameterId = p.ParameterId,
                    ParameterName = p.ParameterName,
                    ErrorMessage = "Device is busy - another update is in progress",
                    RetryAttempts = 0
                }).ToList()
            }, stoppingToken);

            return;
        }

        // Create cancellation token for this operation
        using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _activeCancellations[request.CorrelationId] = operationCts;

        try
        {
            _logger.LogDebug(
                "Acquired lock for device {DeviceId} (CorrelationId: {CorrelationId})",
                request.DeviceId, request.CorrelationId);

            await ProcessParameterUpdatesAsync(request, operationCts.Token);
        }
        finally
        {
            await lockHandle.DisposeAsync();
            _activeCancellations.TryRemove(request.CorrelationId, out _);

            _logger.LogDebug(
                "Released lock for device {DeviceId} (CorrelationId: {CorrelationId})",
                request.DeviceId, request.CorrelationId);
        }
    }

    /// <summary>
    /// Processes parameter updates with write-verify-retry pattern.
    /// Continues on failure (no rollback) and reports failed parameters at end.
    /// </summary>
    private async Task ProcessParameterUpdatesAsync(ParameterUpdateQueueRequest request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var rpcExecutor = scope.ServiceProvider.GetRequiredService<IDeviceRpcExecutorService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IDeviceSettingsNotificationService>();

        // IMPORTANT: Cache state BEFORE sending notification
        // This ensures late-subscribing clients can get state even if they subscribe
        // after we send the notification but before any parameters are processed
        UpdateOperationState(new BatchOperationStateSnapshot
        {
            CorrelationId = request.CorrelationId,
            DeviceId = request.DeviceId,
            Status = "in_progress",
            TotalParameters = request.Parameters.Count,
            CurrentIndex = 0,
            SuccessCount = 0,
            FailedCount = 0,
            SkippedCount = 0,
            WasCancelled = false,
            StartedAt = startTime,
            CompletedAt = null,
            Duration = null,
            FailedParameters = new List<FailedParameterSnapshot>()
        });

        // Notify start (clients in group will receive this; late subscribers will get cached state)
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

        int successCount = 0;
        int failedCount = 0;
        int skippedCount = 0;
        int currentIndex = 0;
        bool wasCancelled = false;
        var failedParameters = new List<FailedParameterInfo>();

        // Process each parameter one by one
        foreach (var param in request.Parameters)
        {
            currentIndex++;

            // Check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Operation {CorrelationId} was cancelled at parameter {Index}/{Total}",
                    request.CorrelationId, currentIndex, request.Parameters.Count);

                wasCancelled = true;
                skippedCount = request.Parameters.Count - currentIndex + 1;
                break;
            }

            try
            {
                if (!parameterValues.TryGetValue(param.ParameterId, out var paramValue) || paramValue?.Parameter == null)
                {
                    _logger.LogWarning("Parameter {ParameterId} not found for device {DeviceId}",
                        param.ParameterId, request.DeviceId);

                    failedCount++;
                    failedParameters.Add(new FailedParameterInfo
                    {
                        ParameterId = param.ParameterId,
                        ParameterName = param.ParameterName,
                        ErrorMessage = "Parameter not found",
                        RetryAttempts = 0
                    });

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

                    continue; // Skip to next parameter
                }

                // Execute write-verify-retry pattern
                var (success, errorMessage, retryAttempts) = await WriteVerifyRetryAsync(
                    rpcExecutor,
                    request.ThingsBoardDeviceId,
                    paramValue.Parameter.Name,
                    param.Value,
                    cancellationToken);

                if (success)
                {
                    // Update database
                    paramValue.Value = param.Value;
                    await context.SaveChangesAsync(cancellationToken);

                    successCount++;

                    await notificationService.NotifyParameterUpdatedAsync(new ParameterUpdatedMessage
                    {
                        CorrelationId = request.CorrelationId,
                        DeviceId = request.DeviceId,
                        ParameterId = param.ParameterId,
                        ParameterName = paramValue.Parameter.Name,
                        NewValue = param.Value,
                        CurrentIndex = currentIndex,
                        TotalParameters = request.Parameters.Count,
                        UpdatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    // Update cached state for late-subscribing clients
                    UpdateOperationState(new BatchOperationStateSnapshot
                    {
                        CorrelationId = request.CorrelationId,
                        DeviceId = request.DeviceId,
                        Status = "in_progress",
                        TotalParameters = request.Parameters.Count,
                        CurrentIndex = currentIndex,
                        SuccessCount = successCount,
                        FailedCount = failedCount,
                        SkippedCount = skippedCount,
                        WasCancelled = false,
                        StartedAt = startTime,
                        CompletedAt = null,
                        Duration = null,
                        FailedParameters = failedParameters.Select(fp => new FailedParameterSnapshot
                        {
                            ParameterId = fp.ParameterId,
                            ParameterName = fp.ParameterName,
                            ErrorMessage = fp.ErrorMessage,
                            RetryAttempts = fp.RetryAttempts
                        }).ToList()
                    });

                    _logger.LogDebug(
                        "Successfully updated parameter {ParameterName} ({Index}/{Total}) after {Retries} retries",
                        paramValue.Parameter.Name,
                        currentIndex,
                        request.Parameters.Count,
                        retryAttempts);
                }
                else
                {
                    failedCount++;
                    failedParameters.Add(new FailedParameterInfo
                    {
                        ParameterId = param.ParameterId,
                        ParameterName = paramValue.Parameter.Name,
                        ErrorMessage = errorMessage ?? "Write-verify failed after retries",
                        RetryAttempts = retryAttempts
                    });

                    await notificationService.NotifyParameterUpdateFailedAsync(new ParameterUpdateFailedMessage
                    {
                        CorrelationId = request.CorrelationId,
                        DeviceId = request.DeviceId,
                        ParameterId = param.ParameterId,
                        ParameterName = paramValue.Parameter.Name,
                        ErrorMessage = errorMessage ?? "Write-verify failed after retries",
                        CurrentIndex = currentIndex,
                        TotalParameters = request.Parameters.Count,
                        FailedAt = DateTime.UtcNow
                    }, cancellationToken);

                    // Update cached state for late-subscribing clients
                    UpdateOperationState(new BatchOperationStateSnapshot
                    {
                        CorrelationId = request.CorrelationId,
                        DeviceId = request.DeviceId,
                        Status = "in_progress",
                        TotalParameters = request.Parameters.Count,
                        CurrentIndex = currentIndex,
                        SuccessCount = successCount,
                        FailedCount = failedCount,
                        SkippedCount = skippedCount,
                        WasCancelled = false,
                        StartedAt = startTime,
                        CompletedAt = null,
                        Duration = null,
                        FailedParameters = failedParameters.Select(fp => new FailedParameterSnapshot
                        {
                            ParameterId = fp.ParameterId,
                            ParameterName = fp.ParameterName,
                            ErrorMessage = fp.ErrorMessage,
                            RetryAttempts = fp.RetryAttempts
                        }).ToList()
                    });

                    _logger.LogWarning(
                        "Failed to update parameter {ParameterName} after {Retries} retries: {Error}",
                        paramValue.Parameter.Name,
                        retryAttempts,
                        errorMessage);

                    // Continue to next parameter (no rollback)
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                skippedCount = request.Parameters.Count - currentIndex + 1;
                break;
            }
            catch (Exception ex)
            {
                failedCount++;
                failedParameters.Add(new FailedParameterInfo
                {
                    ParameterId = param.ParameterId,
                    ParameterName = param.ParameterName,
                    ErrorMessage = ex.Message,
                    RetryAttempts = 0
                });

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

                // Continue to next parameter (no rollback)
            }
        }

        var completedAt = DateTime.UtcNow;
        var duration = completedAt - startTime;

        // If any parameters were successfully updated, set device mode to Custom
        if (successCount > 0)
        {
            await SetDeviceModeToCustomAsync(context, request.DeviceId, CancellationToken.None);
        }

        // Notify completion with list of failed parameters
        await notificationService.NotifyParameterUpdateCompletedAsync(new ParameterUpdateCompletedMessage
        {
            CorrelationId = request.CorrelationId,
            DeviceId = request.DeviceId,
            TotalParameters = request.Parameters.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount,
            CompletedAt = completedAt,
            Duration = duration,
            WasCancelled = wasCancelled,
            FailedParameters = failedParameters
        }, CancellationToken.None); // Use None to ensure completion message is sent even if cancelled

        // Cache final state for late-subscribing clients
        var finalStatus = wasCancelled ? "cancelled" : (failedCount > 0 ? "failed" : "completed");
        UpdateOperationState(new BatchOperationStateSnapshot
        {
            CorrelationId = request.CorrelationId,
            DeviceId = request.DeviceId,
            Status = finalStatus,
            TotalParameters = request.Parameters.Count,
            CurrentIndex = request.Parameters.Count - skippedCount,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount,
            WasCancelled = wasCancelled,
            StartedAt = startTime,
            CompletedAt = completedAt,
            Duration = duration,
            FailedParameters = failedParameters.Select(fp => new FailedParameterSnapshot
            {
                ParameterId = fp.ParameterId,
                ParameterName = fp.ParameterName,
                ErrorMessage = fp.ErrorMessage,
                RetryAttempts = fp.RetryAttempts
            }).ToList()
        });

        // Cleanup old completed operations periodically
        CleanupOldStates();

        _logger.LogInformation(
            "Completed parameter update {CorrelationId}: {Success}/{Total} succeeded, {Failed} failed, {Skipped} skipped in {Duration}ms (Cancelled: {Cancelled})",
            request.CorrelationId,
            successCount,
            request.Parameters.Count,
            failedCount,
            skippedCount,
            duration.TotalMilliseconds,
            wasCancelled);
    }

    /// <summary>
    /// Writes a parameter value and verifies it was written correctly.
    /// Retries up to MaxWriteVerifyRetries times if verification fails.
    /// </summary>
    private async Task<(bool Success, string? ErrorMessage, int RetryAttempts)> WriteVerifyRetryAsync(
        IDeviceRpcExecutorService rpcExecutor,
        Guid tbDeviceId,
        string parameterName,
        string expectedValue,
        CancellationToken cancellationToken)
    {
        string? lastError = null;

        for (int attempt = 0; attempt <= MaxWriteVerifyRetries; attempt++)
        {
            try
            {
                // Step 1: Write the value
                var writeResult = await rpcExecutor.ExecuteParameterUpdateAsync(
                    tbDeviceId,
                    parameterName,
                    expectedValue,
                    RpcRetryOptions.Default,
                    cancellationToken);

                if (!writeResult.Success)
                {
                    lastError = writeResult.ErrorMessage ?? "Write failed";
                    _logger.LogDebug(
                        "Write attempt {Attempt} failed for parameter {ParameterName}: {Error}",
                        attempt + 1, parameterName, lastError);

                    if (attempt < MaxWriteVerifyRetries)
                    {
                        await Task.Delay(WriteVerifyDelayMs * (attempt + 1), cancellationToken);
                    }
                    continue;
                }

                // Step 2: Wait briefly for device to process
                await Task.Delay(WriteVerifyDelayMs, cancellationToken);

                // Step 3: Read back to verify
                var readValue = await rpcExecutor.GetParameterValueAsync(
                    tbDeviceId,
                    parameterName,
                    cancellationToken);

                if (readValue == null)
                {
                    lastError = "Failed to read back parameter value for verification";
                    _logger.LogDebug(
                        "Verify attempt {Attempt} failed for parameter {ParameterName}: could not read back",
                        attempt + 1, parameterName);

                    if (attempt < MaxWriteVerifyRetries)
                    {
                        await Task.Delay(WriteVerifyDelayMs * (attempt + 1), cancellationToken);
                    }
                    continue;
                }

                // Step 4: Compare values (normalize for comparison)
                if (ValuesMatch(expectedValue, readValue))
                {
                    _logger.LogDebug(
                        "Parameter {ParameterName} verified successfully on attempt {Attempt}",
                        parameterName, attempt + 1);
                    return (true, null, attempt);
                }

                lastError = $"Verification failed: expected '{expectedValue}', got '{readValue}'";
                _logger.LogDebug(
                    "Verify mismatch for parameter {ParameterName} on attempt {Attempt}: expected '{Expected}', got '{Actual}'",
                    parameterName, attempt + 1, expectedValue, readValue);

                if (attempt < MaxWriteVerifyRetries)
                {
                    await Task.Delay(WriteVerifyDelayMs * (attempt + 1), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogDebug(ex,
                    "Exception on attempt {Attempt} for parameter {ParameterName}",
                    attempt + 1, parameterName);

                if (attempt < MaxWriteVerifyRetries)
                {
                    await Task.Delay(WriteVerifyDelayMs * (attempt + 1), cancellationToken);
                }
            }
        }

        return (false, lastError, MaxWriteVerifyRetries);
    }

    /// <summary>
    /// Compares two values for equality, handling numeric and string comparisons
    /// </summary>
    private static bool ValuesMatch(string expected, string actual)
    {
        // Exact string match
        if (string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            return true;

        // Try numeric comparison (handles "1" vs "1.0" vs "1.00")
        if (double.TryParse(expected, out var expectedNum) && double.TryParse(actual, out var actualNum))
        {
            return Math.Abs(expectedNum - actualNum) < 0.0001;
        }

        // Try boolean comparison (handles "true" vs "1" vs "True")
        if (bool.TryParse(expected, out var expectedBool))
        {
            if (bool.TryParse(actual, out var actualBool))
                return expectedBool == actualBool;

            // Handle "1"/"0" as boolean
            if (actual == "1" || actual == "0")
                return expectedBool == (actual == "1");
        }

        return false;
    }

    /// <summary>
    /// Sets device mode to Custom when parameters are manually updated.
    /// This ensures DeviceModeHistory correctly reflects that the device is now in custom mode.
    /// </summary>
    private async Task SetDeviceModeToCustomAsync(IApplicationDbContext context, Guid deviceId, CancellationToken cancellationToken)
    {
        const int CustomModeValue = 99; // DeviceMode.Custom = 99

        try
        {
            // Check if there's already an active Custom mode entry
            var existingCustomMode = await context.DeviceModeHistory
                .FirstOrDefaultAsync(m => m.DeviceId == deviceId && m.Mode == CustomModeValue && m.IsActive, cancellationToken);

            if (existingCustomMode != null)
            {
                _logger.LogDebug("Device {DeviceId} is already in Custom mode, skipping mode update", deviceId);
                return;
            }

            // Deactivate all existing active modes for this device
            var existingActiveModes = await context.DeviceModeHistory
                .Where(m => m.DeviceId == deviceId && m.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existingMode in existingActiveModes)
            {
                existingMode.IsActive = false;
            }

            // Create new Custom mode history record
            var modeHistory = new DeviceModeHistory
            {
                DeviceId = deviceId,
                Mode = CustomModeValue,
                Description = "Custom Mode",
                IsActive = true
            };

            context.DeviceModeHistory.Add(modeHistory);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Device {DeviceId} mode changed to Custom due to parameter updates",
                deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to update device mode to Custom for device {DeviceId}. Parameter updates were still successful.",
                deviceId);
            // Don't throw - parameter updates were successful, mode update is a side effect
        }
    }
}
