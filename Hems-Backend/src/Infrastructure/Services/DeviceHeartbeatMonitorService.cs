using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that monitors device heartbeats and handles offline detection
/// </summary>
public class DeviceHeartbeatMonitorService : BackgroundService
{
    private readonly ILogger<DeviceHeartbeatMonitorService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private const int CheckIntervalMinutes = 5;
    private const int RetryDelayMinutes = 2;
    private const int MaxRetryAttempts = 3;

    public DeviceHeartbeatMonitorService(
        ILogger<DeviceHeartbeatMonitorService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Heartbeat Monitor Service started");

        // Initial delay to let application start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        // Initialize cache on startup
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IDeviceHeartbeatCache>();
            await cache.InitializeCacheAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorDevicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in device heartbeat monitoring");
            }

            // Wait for next check interval
            await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Device Heartbeat Monitor Service stopped");
    }

    private async Task MonitorDevicesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDeviceHeartbeatCache>();
        var heartbeatSender = scope.ServiceProvider.GetRequiredService<IHeartbeatSender>();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Get devices that haven't sent powerflow data in 5+ minutes
        var inactiveDevices = await cache.GetInactiveDevicesAsync(cancellationToken);

        _logger.LogInformation("Checking {Count} inactive devices", inactiveDevices.Count);

        foreach (var deviceId in inactiveDevices)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessInactiveDeviceAsync(deviceId, cache, heartbeatSender, context, cancellationToken);
        }
    }

    private async Task ProcessInactiveDeviceAsync(
        Guid thingsBoardDeviceId,
        IDeviceHeartbeatCache cache,
        IHeartbeatSender heartbeatSender,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var deviceStatus = cache.GetDeviceStatus(thingsBoardDeviceId);
        if (deviceStatus == null)
        {
            _logger.LogWarning("Device status not found in cache for {DeviceId}", thingsBoardDeviceId);
            return;
        }

        _logger.LogInformation("Processing inactive device {DeviceId}, attempts: {Attempts}",
            thingsBoardDeviceId, deviceStatus.HeartbeatAttempts);

        // Retry logic: 3 attempts with 2-minute delays
        for (int attempt = deviceStatus.HeartbeatAttempts + 1; attempt <= MaxRetryAttempts; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            _logger.LogInformation("Heartbeat attempt {Attempt}/{MaxAttempts} for device {DeviceId}",
                attempt, MaxRetryAttempts, thingsBoardDeviceId);

            // Update attempt count in cache
            await cache.UpdateHeartbeatAttemptAsync(thingsBoardDeviceId, attempt, cancellationToken);

            // Record heartbeat attempt in database
            var heartbeatRecord = new DeviceHeartbeat
            {
                Id = Guid.NewGuid(),
                ThingsBoardDeviceId = thingsBoardDeviceId,
                Timestamp = DateTime.UtcNow,
                Status = HeartbeatStatus.Sent,
                AttemptNumber = attempt
            };

            context.DeviceHeartbeats.Add(heartbeatRecord);
            await context.SaveChangesAsync(cancellationToken);

            // Send heartbeat request
            var responseReceived = await heartbeatSender.SendHeartbeatAsync(thingsBoardDeviceId, cancellationToken);

            if (responseReceived)
            {
                _logger.LogInformation("Device {DeviceId} responded to heartbeat", thingsBoardDeviceId);

                // Update heartbeat record with response
                heartbeatRecord.Status = HeartbeatStatus.ResponseReceived;
                heartbeatRecord.ResponseReceivedAt = DateTime.UtcNow;
                heartbeatRecord.ResponseMessage = "Device responded successfully";
                await context.SaveChangesAsync(cancellationToken);

                // Mark device as alive in cache
                await cache.MarkDeviceAliveAsync(thingsBoardDeviceId, cancellationToken);
                return; // Device is back online
            }
            else
            {
                _logger.LogWarning("Device {DeviceId} did not respond to heartbeat attempt {Attempt}",
                    thingsBoardDeviceId, attempt);

                // Update heartbeat record
                heartbeatRecord.Status = HeartbeatStatus.NoResponse;
                heartbeatRecord.ResponseMessage = $"No response on attempt {attempt}";
                await context.SaveChangesAsync(cancellationToken);

                // If not the last attempt, wait before retrying
                if (attempt < MaxRetryAttempts)
                {
                    _logger.LogInformation("Waiting {Minutes} minutes before next retry for device {DeviceId}",
                        RetryDelayMinutes, thingsBoardDeviceId);
                    await Task.Delay(TimeSpan.FromMinutes(RetryDelayMinutes), cancellationToken);
                }
            }
        }

        // All retry attempts failed - mark device as offline
        _logger.LogError("Device {DeviceId} failed all {MaxAttempts} heartbeat attempts - marking as offline",
            thingsBoardDeviceId, MaxRetryAttempts);

        await cache.MarkDeviceOfflineAsync(thingsBoardDeviceId, cancellationToken);

        // Record device offline status
        var offlineRecord = new DeviceHeartbeat
        {
            Id = Guid.NewGuid(),
            ThingsBoardDeviceId = thingsBoardDeviceId,
            Timestamp = DateTime.UtcNow,
            Status = HeartbeatStatus.DeviceOffline,
            AttemptNumber = MaxRetryAttempts,
            ResponseMessage = "Device marked as offline after max retry attempts"
        };

        context.DeviceHeartbeats.Add(offlineRecord);
        await context.SaveChangesAsync(cancellationToken);

        // Send alarm to user
        await SendDeviceOfflineAlarmAsync(thingsBoardDeviceId, context, cancellationToken);
    }

    private async Task SendDeviceOfflineAlarmAsync(
        Guid thingsBoardDeviceId,
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get device information
            var device = await context.Devices
                .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == thingsBoardDeviceId, cancellationToken);

            if (device == null)
            {
                _logger.LogWarning("Device not found for ThingsBoard ID {DeviceId}", thingsBoardDeviceId);
                return;
            }

            // Get users who should be notified (device owners/admins)
            var userSites = await context.UserSites
                .Include(us => us.Site)
                .ThenInclude(s => s.Setup)
                .ThenInclude(setup => setup!.SetupDevice)
                .Where(us => us.Site.Setup != null &&
                    us.Site.Setup.SetupDevice != null &&
                    us.Site.Setup.SetupDevice.DeviceId == device.Id)
                .ToListAsync(cancellationToken);

            var userIds = userSites.Select(us => us.UserId).Distinct().ToList();

            // Create notifications for each user
            foreach (var userId in userIds)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Email = string.Empty, // Will be populated by notification service
                    InvitationId = Guid.Empty, // Not related to invitation
                    Message = $"Device '{device.Name}' is offline and not responding to heartbeat requests.",
                    IsRead = false
                };

                context.Notifications.Add(notification);
            }

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Sent offline alarm for device {DeviceId} to {UserCount} users",
                thingsBoardDeviceId, userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending device offline alarm for {DeviceId}", thingsBoardDeviceId);
        }
    }
}
