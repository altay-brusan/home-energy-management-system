using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that monitors device heartbeats and handles disconnect logging
/// Checks every 1 minute for missed heartbeats
/// </summary>
public class DeviceHeartbeatMonitorService : BackgroundService
{
    private readonly ILogger<DeviceHeartbeatMonitorService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private const int CheckIntervalSeconds = 60; // Check every 1 minute

    public DeviceHeartbeatMonitorService(
        ILogger<DeviceHeartbeatMonitorService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Heartbeat Monitor Service started (heartbeat-driven mode)");

        try
        {
            // No initial cache population needed - devices auto-register on first heartbeat
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDeviceHeartbeatsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in device heartbeat monitoring");
                }

                // Wait 1 minute before next check
                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when application is shutting down
            _logger.LogInformation("Device Heartbeat Monitor Service is stopping (cancellation requested)");
        }

        _logger.LogInformation("Device Heartbeat Monitor Service stopped");
    }

    private async Task CheckDeviceHeartbeatsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDeviceHeartbeatCache>();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DeviceStatusHub>>();
        var deviceMapping = scope.ServiceProvider.GetRequiredService<IDeviceMappingService>();

        // Check all tracked devices for missed heartbeats
        var statusChanges = await cache.CheckMissedHeartbeatsAsync(cancellationToken);

        if (statusChanges.Count == 0)
        {
            _logger.LogDebug("No device status changes detected");
            return;
        }

        _logger.LogInformation("Processing {Count} device status changes", statusChanges.Count);

        foreach (var change in statusChanges)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessStatusChangeAsync(change, cache, context, hubContext, deviceMapping, cancellationToken);
        }

        // Save all changes to database
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessStatusChangeAsync(
        DeviceStatusChange change,
        IDeviceHeartbeatCache cache,
        IApplicationDbContext context,
        IHubContext<DeviceStatusHub> hubContext,
        IDeviceMappingService deviceMapping,
        CancellationToken cancellationToken)
    {
        var thingsBoardDeviceId = change.ThingsBoardDeviceId;
        var newStatus = change.NewStatus;

        // ID ADAPTER: Translate ThingsBoard ID to local deviceId for frontend
        var localDeviceId = await deviceMapping.GetDeviceIdAsync(thingsBoardDeviceId, cancellationToken);
        
        if (localDeviceId == null)
        {
            _logger.LogWarning("Could not find local deviceId for ThingsBoard device {TBDeviceId}", thingsBoardDeviceId);
            return; // Skip notification if mapping not found
        }

        // Send SignalR notification with local deviceId (frontend doesn't know about TB ID)
        await hubContext.Clients.Group($"device_{localDeviceId}")
            .SendAsync("DeviceStatusChanged", new
            {
                deviceId = localDeviceId, // ← Local ID for frontend
                status = newStatus.ToString(),
                missedHeartbeats = change.MissedHeartbeats,
                timestamp = change.Timestamp
            }, cancellationToken);

        _logger.LogInformation("SignalR notification sent for device {LocalDeviceId} (TB: {TBDeviceId}): {Status}", 
            localDeviceId, thingsBoardDeviceId, newStatus);

        // Handle DISCONNECTED status - create DB log entry
        if (newStatus == DeviceStatus.DISCONNECTED && change.PreviousStatus != DeviceStatus.DISCONNECTED)
        {
            var disconnectLog = new DeviceDisconnectLog
            {
                Id = Guid.NewGuid(),
                ThingsBoardDeviceId = thingsBoardDeviceId,
                StartTime = change.Timestamp,
                MissedHeartbeats = change.MissedHeartbeats
            };

            context.DeviceDisconnectLogs.Add(disconnectLog);

            // Store log ID in cache for later update
            var state = (cache as DeviceHeartbeatCache)?.GetDeviceState(thingsBoardDeviceId);
            if (state != null)
            {
                state.DisconnectLogId = disconnectLog.Id;
            }

                _logger.LogWarning("Device {TBDeviceId} marked as DISCONNECTED. Disconnect log created: {LogId}", 
                thingsBoardDeviceId, disconnectLog.Id);
        }

        // Handle recovery from DISCONNECTED - update DB log entry
        if (change.PreviousStatus == DeviceStatus.DISCONNECTED && newStatus == DeviceStatus.OK)
        {
            var state = (cache as DeviceHeartbeatCache)?.GetDeviceState(thingsBoardDeviceId);
            if (state?.DisconnectLogId != null)
            {
                var disconnectLog = await context.DeviceDisconnectLogs
                    .FindAsync(new object[] { state.DisconnectLogId.Value }, cancellationToken);

                if (disconnectLog != null && disconnectLog.EndTime == null)
                {
                    disconnectLog.EndTime = change.Timestamp;
                    disconnectLog.Duration = disconnectLog.EndTime - disconnectLog.StartTime;
                    disconnectLog.RecoveryType = "Heartbeat";

                    _logger.LogInformation("Device {TBDeviceId} recovered. Disconnect duration: {Duration}", 
                        thingsBoardDeviceId, disconnectLog.Duration);
                }

                state.DisconnectLogId = null;
            }
        }
    }
}
