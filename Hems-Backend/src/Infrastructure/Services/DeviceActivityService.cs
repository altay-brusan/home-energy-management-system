using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Centralized service for recording device activity from any telemetry source.
/// Updates heartbeat cache and sends SignalR notifications when device status changes.
/// </summary>
public class DeviceActivityService : IDeviceActivityService
{
    private readonly IDeviceHeartbeatCache _heartbeatCache;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IHubContext<DeviceStatusHub> _hubContext;
    private readonly ILogger<DeviceActivityService> _logger;

    public DeviceActivityService(
        IDeviceHeartbeatCache heartbeatCache,
        IDeviceMappingService deviceMapping,
        IHubContext<DeviceStatusHub> hubContext,
        ILogger<DeviceActivityService> logger)
    {
        _heartbeatCache = heartbeatCache;
        _deviceMapping = deviceMapping;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<DeviceActivityResult> RecordActivityAsync(
        string deviceIdString,
        TelemetrySource source,
        DateTime? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(deviceIdString, out var thingsBoardDeviceId))
        {
            _logger.LogWarning(
                "Invalid device ID format in {Source} telemetry: {DeviceId}",
                source, deviceIdString);

            return new DeviceActivityResult
            {
                Success = false,
                ErrorMessage = $"Invalid device ID format: {deviceIdString}"
            };
        }

        return await RecordActivityAsync(thingsBoardDeviceId, source, timestamp, cancellationToken);
    }

    public async Task<DeviceActivityResult> RecordActivityAsync(
        Guid thingsBoardDeviceId,
        TelemetrySource source,
        DateTime? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DeviceActivityResult
        {
            ThingsBoardDeviceId = thingsBoardDeviceId
        };

        try
        {
            var activityTimestamp = timestamp ?? DateTime.UtcNow;

            // Record activity in heartbeat cache based on source type
            DeviceStatusChange? statusChange = source switch
            {
                TelemetrySource.Heartbeat => await _heartbeatCache.RecordHeartbeatAsync(
                    thingsBoardDeviceId, activityTimestamp, cancellationToken),

                TelemetrySource.PowerFlow => await _heartbeatCache.RecordPowerFlowAsync(
                    thingsBoardDeviceId, activityTimestamp, cancellationToken),

                // All other telemetry types use RecordPowerFlowAsync as it resets missed heartbeats
                _ => await _heartbeatCache.RecordPowerFlowAsync(
                    thingsBoardDeviceId, activityTimestamp, cancellationToken)
            };

            result.Success = true;
            result.StatusChange = statusChange;
            result.StatusChanged = statusChange != null;

            // If status changed (e.g., recovery from DISCONNECTED), send SignalR notification
            if (statusChange != null)
            {
                await SendStatusChangeNotificationAsync(statusChange, source, cancellationToken);
                result.NotificationSent = true;

                _logger.LogInformation(
                    "Device {DeviceId} status changed from {OldStatus} to {NewStatus} via {Source} telemetry",
                    thingsBoardDeviceId, statusChange.PreviousStatus, statusChange.NewStatus, source);
            }
            else
            {
                _logger.LogDebug(
                    "Device activity recorded for {DeviceId} via {Source} (no status change)",
                    thingsBoardDeviceId, source);
            }

            // Get local device ID for the result
            result.LocalDeviceId = await _deviceMapping.GetDeviceIdAsync(thingsBoardDeviceId, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error recording device activity for {DeviceId} via {Source}",
                thingsBoardDeviceId, source);

            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private async Task SendStatusChangeNotificationAsync(
        DeviceStatusChange statusChange,
        TelemetrySource source,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get local device ID for frontend notification
            var localDeviceId = await _deviceMapping.GetDeviceIdAsync(
                statusChange.ThingsBoardDeviceId, cancellationToken);

            if (localDeviceId == null)
            {
                _logger.LogWarning(
                    "Cannot send SignalR notification: no local device ID mapping for ThingsBoard ID {ThingsBoardDeviceId}",
                    statusChange.ThingsBoardDeviceId);
                return;
            }

            // Send SignalR notification to device group
            await _hubContext.Clients.Group($"device_{localDeviceId}")
                .SendAsync("DeviceStatusChanged", new
                {
                    deviceId = localDeviceId,
                    status = statusChange.NewStatus.ToString(),
                    missedHeartbeats = statusChange.MissedHeartbeats,
                    timestamp = statusChange.Timestamp,
                    source = source.ToString(),
                    isRecovery = statusChange.PreviousStatus == DeviceStatus.DISCONNECTED &&
                                 statusChange.NewStatus == DeviceStatus.OK
                }, cancellationToken);

            _logger.LogInformation(
                "SignalR notification sent for device {LocalDeviceId} (TB: {ThingsBoardDeviceId}): " +
                "{PreviousStatus} -> {NewStatus} via {Source}",
                localDeviceId, statusChange.ThingsBoardDeviceId,
                statusChange.PreviousStatus, statusChange.NewStatus, source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send SignalR notification for device {DeviceId}",
                statusChange.ThingsBoardDeviceId);
        }
    }
}
