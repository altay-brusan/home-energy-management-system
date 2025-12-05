using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
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

        _logger.LogInformation("Device Heartbeat Monitor Service stopped");
    }

    private async Task CheckDeviceHeartbeatsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDeviceHeartbeatCache>();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DeviceStatusHub>>();

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

            await ProcessStatusChangeAsync(change, cache, context, hubContext, cancellationToken);
        }

        // Save all changes to database
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessStatusChangeAsync(
        DeviceStatusChange change,
        IDeviceHeartbeatCache cache,
        IApplicationDbContext context,
        IHubContext<DeviceStatusHub> hubContext,
        CancellationToken cancellationToken)
    {
        var deviceId = change.ThingsBoardDeviceId;
        var newStatus = change.NewStatus;

        // Send SignalR notification to frontend
        await hubContext.Clients.Group($"device_{deviceId}")
            .SendAsync("DeviceStatusChanged", new
            {
                deviceId = deviceId,
                status = newStatus.ToString(),
                missedHeartbeats = change.MissedHeartbeats,
                timestamp = change.Timestamp
            }, cancellationToken);

        _logger.LogInformation("SignalR notification sent for device {DeviceId}: {Status}", 
            deviceId, newStatus);

        // Handle DISCONNECTED status - create DB log entry
        if (newStatus == DeviceStatus.DISCONNECTED && change.PreviousStatus != DeviceStatus.DISCONNECTED)
        {
            var disconnectLog = new DeviceDisconnectLog
            {
                Id = Guid.NewGuid(),
                ThingsBoardDeviceId = deviceId,
                StartTime = change.Timestamp,
                MissedHeartbeats = change.MissedHeartbeats
            };

            context.DeviceDisconnectLogs.Add(disconnectLog);

            // Store log ID in cache for later update
            var state = (cache as DeviceHeartbeatCache)?.GetDeviceState(deviceId);
            if (state != null)
            {
                state.DisconnectLogId = disconnectLog.Id;
            }

            _logger.LogWarning("Device {DeviceId} marked as DISCONNECTED. Disconnect log created: {LogId}", 
                deviceId, disconnectLog.Id);
        }

        // Handle recovery from DISCONNECTED - update DB log entry
        if (change.PreviousStatus == DeviceStatus.DISCONNECTED && newStatus == DeviceStatus.OK)
        {
            var state = (cache as DeviceHeartbeatCache)?.GetDeviceState(deviceId);
            if (state?.DisconnectLogId != null)
            {
                var disconnectLog = await context.DeviceDisconnectLogs
                    .FindAsync(new object[] { state.DisconnectLogId.Value }, cancellationToken);

                if (disconnectLog != null && disconnectLog.EndTime == null)
                {
                    disconnectLog.EndTime = change.Timestamp;
                    disconnectLog.Duration = disconnectLog.EndTime - disconnectLog.StartTime;
                    disconnectLog.RecoveryType = "Heartbeat";

                    _logger.LogInformation("Device {DeviceId} recovered. Disconnect duration: {Duration}", 
                        deviceId, disconnectLog.Duration);
                }

                state.DisconnectLogId = null;
            }
        }
    }
}

/// <summary>
/// SignalR hub for pushing device status changes to frontend
/// </summary>
public class DeviceStatusHub : Hub
{
    public async Task SubscribeToDevice(string deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }

    public async Task UnsubscribeFromDevice(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }
}
