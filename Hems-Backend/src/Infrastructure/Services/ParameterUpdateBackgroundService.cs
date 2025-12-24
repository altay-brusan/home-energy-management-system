using System.Threading.Channels;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that processes parameter updates asynchronously
/// Uses Channel for producer-consumer pattern
/// </summary>
public class ParameterUpdateBackgroundService : BackgroundService, IParameterUpdateQueueService
{
    private readonly ILogger<ParameterUpdateBackgroundService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Channel<ParameterUpdateQueueRequest> _channel;

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
        _logger.LogInformation("Parameter Update Background Service started");

        try
        {
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessParameterUpdateAsync(request, stoppingToken);
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

    private async Task ProcessParameterUpdateAsync(ParameterUpdateQueueRequest request, CancellationToken cancellationToken)
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

        int successCount = 0;
        int failedCount = 0;
        int currentIndex = 0;

        // Get all parameter values for this device
        var parameterIds = request.Parameters.Select(p => p.ParameterId).ToList();
        var parameterValues = await context.SetupDeviceParameterValues
            .Include(sdpv => sdpv.Parameter)
            .Where(sdpv =>
                sdpv.SetupDeviceId == request.SetupDeviceId &&
                parameterIds.Contains(sdpv.ParameterId))
            .ToDictionaryAsync(pv => pv.ParameterId, cancellationToken);

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
                    continue;
                }

                // Execute RPC call for single parameter
                var parameterUpdates = new Dictionary<string, string>
                {
                    [paramValue.Parameter.Name] = param.Value
                };

                var rpcResults = await rpcExecutor.ExecuteParameterUpdatesAsync(
                    request.ThingsBoardDeviceId,
                    parameterUpdates,
                    cancellationToken);

                var rpcResult = rpcResults.FirstOrDefault();

                if (rpcResult.Value?.Success == true)
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
                        "Failed to update parameter {ParameterName}: {Error}",
                        paramValue.Parameter.Name,
                        rpcResult.Value?.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                failedCount++;

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
            "Completed parameter update {CorrelationId}: {Success}/{Total} succeeded in {Duration}ms",
            request.CorrelationId,
            successCount,
            request.Parameters.Count,
            duration.TotalMilliseconds);
    }
}
