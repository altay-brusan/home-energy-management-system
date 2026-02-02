using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;
using Hems_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Background service that monitors devices in outage mode and checks their status via RPC.
/// Runs every 60 seconds to check if devices that haven't sent a message in 90 seconds need RPC fallback.
/// </summary>
public class GridStatusMonitorService : BackgroundService
{
    private readonly ILogger<GridStatusMonitorService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IGridStatusTrackingService _gridStatusTracking;

    private const int CheckIntervalSeconds = 60;
    private static readonly TimeSpan RpcFallbackTimeout = TimeSpan.FromSeconds(90);

    public GridStatusMonitorService(
        ILogger<GridStatusMonitorService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IGridStatusTrackingService gridStatusTracking)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _gridStatusTracking = gridStatusTracking;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Grid Status Monitor Service started");

        try
        {
            // Wait a bit before first check to let the app initialize
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDevicesInOutageModeAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in grid status monitoring");
                }

                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Grid Status Monitor Service is stopping (cancellation requested)");
        }

        _logger.LogInformation("Grid Status Monitor Service stopped");
    }

    private async Task CheckDevicesInOutageModeAsync(CancellationToken cancellationToken)
    {
        // Get devices that need RPC fallback (in outage mode and no message for 90s)
        var devicesNeedingRpc = _gridStatusTracking.GetDevicesNeedingRpcFallback(RpcFallbackTimeout);

        if (devicesNeedingRpc.Count == 0)
        {
            _logger.LogDebug("No devices need RPC fallback check");
            return;
        }

        _logger.LogInformation("Checking {Count} devices via RPC for grid status", devicesNeedingRpc.Count);

        using var scope = _serviceScopeFactory.CreateScope();
        var tbConnector = scope.ServiceProvider.GetRequiredService<IThingsBoardConnectorService>();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var deviceMapping = scope.ServiceProvider.GetRequiredService<IDeviceMappingService>();
        var demoDataService = scope.ServiceProvider.GetRequiredService<IDemoDataService>();
        var notificationSender = scope.ServiceProvider.GetRequiredService<IAlarmNotificationSender>();

        foreach (var thingsBoardDeviceId in devicesNeedingRpc)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await CheckDeviceGridStatusViaRpcAsync(
                    thingsBoardDeviceId,
                    tbConnector,
                    context,
                    deviceMapping,
                    demoDataService,
                    notificationSender,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking grid status via RPC for device {DeviceId}", thingsBoardDeviceId);
            }
        }
    }

    private async Task CheckDeviceGridStatusViaRpcAsync(
        Guid thingsBoardDeviceId,
        IThingsBoardConnectorService tbConnector,
        IApplicationDbContext context,
        IDeviceMappingService deviceMapping,
        IDemoDataService demoDataService,
        IAlarmNotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending RPC to check grid status for device {DeviceId}", thingsBoardDeviceId);

        // Send RPC to get grid connection status
        var rpcResponse = await tbConnector.SendTwoWayRpcAsync(
            thingsBoardDeviceId,
            "getGridConnectionStatusState",
            new Dictionary<string, object>(),
            timeoutMs: 10000,
            cancellationToken: cancellationToken);

        if (rpcResponse?.Success != true || rpcResponse.Response == null)
        {
            _logger.LogWarning(
                "RPC failed for device {DeviceId}: {Error}",
                thingsBoardDeviceId, rpcResponse?.Error ?? "No response");
            return;
        }

        // Parse response
        bool gridConnected = true;
        double? phaseAVoltageV = null;
        double? phaseBVoltageV = null;
        double? phaseCVoltageV = null;

        if (rpcResponse.Response.TryGetValue("gridConnected", out var gcObj))
        {
            gridConnected = Convert.ToBoolean(gcObj);
        }

        if (rpcResponse.Response.TryGetValue("phaseAVoltageV", out var paObj))
        {
            phaseAVoltageV = Convert.ToDouble(paObj);
        }

        if (rpcResponse.Response.TryGetValue("phaseBVoltageV", out var pbObj))
        {
            phaseBVoltageV = Convert.ToDouble(pbObj);
        }

        if (rpcResponse.Response.TryGetValue("phaseCVoltageV", out var pcObj))
        {
            phaseCVoltageV = Convert.ToDouble(pcObj);
        }

        _logger.LogDebug(
            "RPC response for device {DeviceId}: gridConnected={GridConnected}, A={PhaseA}V, B={PhaseB}V, C={PhaseC}V",
            thingsBoardDeviceId, gridConnected, phaseAVoltageV, phaseBVoltageV, phaseCVoltageV);

        // Update grid status
        var updateResult = _gridStatusTracking.UpdateGridStatus(
            thingsBoardDeviceId,
            gridConnected,
            phaseAVoltageV,
            phaseBVoltageV,
            phaseCVoltageV);

        // If notification needed (grid restored), create and send it
        if (updateResult.ShouldSendNotification && !string.IsNullOrEmpty(updateResult.NotificationCode))
        {
            await CreateAndSendNotificationAsync(
                thingsBoardDeviceId,
                updateResult.NotificationCode,
                updateResult.CurrentStatus,
                updateResult.OutageDuration,
                phaseAVoltageV,
                phaseBVoltageV,
                phaseCVoltageV,
                context,
                deviceMapping,
                notificationSender,
                cancellationToken);
        }
    }

    private async Task CreateAndSendNotificationAsync(
        Guid thingsBoardDeviceId,
        string notificationCode,
        GridConnectionStatus currentStatus,
        TimeSpan? outageDuration,
        double? phaseAVoltageV,
        double? phaseBVoltageV,
        double? phaseCVoltageV,
        IApplicationDbContext context,
        IDeviceMappingService deviceMapping,
        IAlarmNotificationSender notificationSender,
        CancellationToken cancellationToken)
    {
        try
        {
            var internalDeviceId = await deviceMapping.GetDeviceIdAsync(thingsBoardDeviceId, cancellationToken);
            if (internalDeviceId == null)
            {
                _logger.LogWarning("No device found for ThingsBoardDeviceId: {ThingsBoardDeviceId}", thingsBoardDeviceId);
                return;
            }

            var device = await context.Devices
                .Include(d => d.SetupDevice!)
                .ThenInclude(sd => sd.Setup!)
                .ThenInclude(s => s.Site!)
                .FirstOrDefaultAsync(d => d.Id == internalDeviceId, cancellationToken);

            if (device?.SetupDevice?.Setup?.Site == null)
            {
                _logger.LogWarning("Device {DeviceId} is not properly assigned to a setup and site", internalDeviceId);
                return;
            }

            var notificationType = await context.DeviceNotificationTypes
                .FirstOrDefaultAsync(nt => nt.Code == notificationCode && nt.IsActive, cancellationToken);

            if (notificationType == null)
            {
                _logger.LogWarning("Notification type not found for code: {Code}", notificationCode);
                return;
            }

            var timestamp = DateTime.UtcNow;
            var additionalData = new Dictionary<string, object>
            {
                ["gridStatus"] = currentStatus.ToString(),
                ["phaseAVoltageV"] = phaseAVoltageV ?? 0,
                ["phaseBVoltageV"] = phaseBVoltageV ?? 0,
                ["phaseCVoltageV"] = phaseCVoltageV ?? 0,
                ["timestamp"] = timestamp.ToString("O"),
                ["source"] = "RPC_FALLBACK"
            };

            if (outageDuration.HasValue)
            {
                additionalData["outageDurationSeconds"] = outageDuration.Value.TotalSeconds;
                additionalData["outageDurationFormatted"] = FormatDuration(outageDuration.Value);
            }

            var notification = new DeviceNotification
            {
                DeviceId = internalDeviceId.Value,
                SiteId = device.SetupDevice.Setup.SiteId,
                SetupDeviceId = device.SetupDevice.Id,
                DeviceNotificationTypeId = notificationType.Id,
                Timestamp = new DateTimeOffset(timestamp, TimeSpan.Zero),
                AdditionalData = JsonSerializer.Serialize(additionalData),
                IsProcessed = false
            };

            context.DeviceNotifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);

            var processedNotification = new ProcessedNotificationInfo
            {
                Id = notification.Id,
                Key = notificationType.Code,
                Message = notificationType.Message,
                Severity = notificationType.Severity,
                Description = notificationType.Description
            };

            await notificationSender.SendNotificationsAsync(
                new List<Guid> { notification.Id },
                new List<ProcessedNotificationInfo> { processedNotification },
                cancellationToken);

            _logger.LogInformation(
                "Grid status notification (via RPC) created and sent: {NotificationId} ({Code}) for device {DeviceId}",
                notification.Id, notificationCode, internalDeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating grid status notification (via RPC) for device {DeviceId}", thingsBoardDeviceId);
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
        }
        if (duration.TotalMinutes >= 1)
        {
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        }
        return $"{duration.Seconds}s";
    }
}
