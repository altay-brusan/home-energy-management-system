using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceData.Commands.ProcessGridStatusTelemetry;

/// <summary>
/// Result of processing grid status telemetry
/// </summary>
public record ProcessGridStatusTelemetryResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public Guid? DeviceId { get; init; }
    public bool? GridConnected { get; init; }
    public bool StatusChanged { get; init; }
    public bool NotificationSent { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Command to process raw grid status telemetry from device (EC-100 gateway)
/// Expected format: { deviceId: string, data: { version, phaseAVoltageV, phaseBVoltageV, phaseCVoltageV, gridConnected, timestampUtc } }
/// </summary>
public record ProcessGridStatusTelemetryCommand : IRequest<ProcessGridStatusTelemetryResult>
{
    /// <summary>
    /// Raw JSON body from the telemetry endpoint
    /// </summary>
    public JsonElement RawBody { get; init; }
}

public class ProcessGridStatusTelemetryCommandHandler : IRequestHandler<ProcessGridStatusTelemetryCommand, ProcessGridStatusTelemetryResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDeviceActivityService _activityService;
    private readonly IDemoDataService _demoDataService;
    private readonly IGridStatusTrackingService _gridStatusTracking;
    private readonly IAlarmNotificationSender _notificationSender;
    private readonly ILogger<ProcessGridStatusTelemetryCommandHandler> _logger;

    public ProcessGridStatusTelemetryCommandHandler(
        IApplicationDbContext context,
        IDeviceMappingService deviceMapping,
        IDeviceActivityService activityService,
        IDemoDataService demoDataService,
        IGridStatusTrackingService gridStatusTracking,
        IAlarmNotificationSender notificationSender,
        ILogger<ProcessGridStatusTelemetryCommandHandler> logger)
    {
        _context = context;
        _deviceMapping = deviceMapping;
        _activityService = activityService;
        _demoDataService = demoDataService;
        _gridStatusTracking = gridStatusTracking;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public async Task<ProcessGridStatusTelemetryResult> Handle(ProcessGridStatusTelemetryCommand request, CancellationToken cancellationToken)
    {
        var body = request.RawBody;

        _logger.LogDebug("Grid status telemetry received at {UtcNow}. Raw data: {RawBody}",
            DateTime.UtcNow, body.GetRawText());

        // 1) Parse ThingsBoardDeviceId from root level "deviceId" field
        if (!body.TryGetProperty("deviceId", out var deviceIdElement))
        {
            _logger.LogWarning("Missing deviceId in grid status telemetry");
            return new ProcessGridStatusTelemetryResult
            {
                Success = false,
                Error = "Missing 'deviceId' in request"
            };
        }

        var deviceIdStr = deviceIdElement.GetString();
        if (string.IsNullOrWhiteSpace(deviceIdStr) || !Guid.TryParse(deviceIdStr, out var thingsBoardDeviceId))
        {
            _logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceIdStr);
            return new ProcessGridStatusTelemetryResult
            {
                Success = false,
                Error = "Invalid 'deviceId' format"
            };
        }

        // 2) Get the inner "data" object
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("Received grid status telemetry without 'data' object");
            return new ProcessGridStatusTelemetryResult
            {
                Success = false,
                Error = "Missing 'data' object"
            };
        }

        // 3) Parse timestamp (or use UTC now)
        DateTime? timestampUtc = null;
        if (dataElement.TryGetProperty("timestampUtc", out var tsElement))
        {
            var tsString = tsElement.GetString();
            if (!string.IsNullOrEmpty(tsString) && DateTime.TryParse(tsString, out var parsedTs))
            {
                timestampUtc = DateTime.SpecifyKind(parsedTs, DateTimeKind.Utc);
            }
        }

        // 4) Record device activity
        await _activityService.RecordActivityAsync(thingsBoardDeviceId, TelemetrySource.GridStatus, timestampUtc);

        // 5) Check if demo device - skip for demo devices
        var isDemoDevice = await _demoDataService.IsDemoDeviceByThingsBoardIdAsync(thingsBoardDeviceId, cancellationToken);
        if (isDemoDevice)
        {
            _logger.LogDebug("Skipping grid status processing for demo device {DeviceId}", thingsBoardDeviceId);
            return new ProcessGridStatusTelemetryResult
            {
                Success = true,
                Message = "Demo device - skipped",
                DeviceId = thingsBoardDeviceId,
                StatusChanged = false,
                NotificationSent = false
            };
        }

        // 6) Parse voltage values and gridConnected flag
        double? phaseAVoltageV = null;
        double? phaseBVoltageV = null;
        double? phaseCVoltageV = null;
        bool gridConnected = true; // Default to connected if not specified

        if (dataElement.TryGetProperty("phaseAVoltageV", out var phaseAEl) &&
            phaseAEl.TryGetDouble(out var phaseA))
        {
            phaseAVoltageV = phaseA;
        }

        if (dataElement.TryGetProperty("phaseBVoltageV", out var phaseBEl) &&
            phaseBEl.TryGetDouble(out var phaseB))
        {
            phaseBVoltageV = phaseB;
        }

        if (dataElement.TryGetProperty("phaseCVoltageV", out var phaseCEl) &&
            phaseCEl.TryGetDouble(out var phaseC))
        {
            phaseCVoltageV = phaseC;
        }

        if (TryGetGridConnectedValue(dataElement, out var parsedGridConnected))
        {
            gridConnected = parsedGridConnected;
        }
        else
        {
            _logger.LogWarning(
                "Could not find or parse 'gridConnected' in grid status telemetry for device {DeviceId}. " +
                "Defaulting to connected. Available data keys: [{DataKeys}]",
                thingsBoardDeviceId,
                string.Join(", ", dataElement.EnumerateObject().Select(p => p.Name)));
        }

        // 7) Update grid status tracking
        var updateResult = _gridStatusTracking.UpdateGridStatus(
            thingsBoardDeviceId,
            gridConnected,
            phaseAVoltageV,
            phaseBVoltageV,
            phaseCVoltageV,
            timestampUtc);

        // 8) If notification needed, create DeviceNotification and send via SignalR
        bool notificationSent = false;
        if (updateResult.ShouldSendNotification && !string.IsNullOrEmpty(updateResult.NotificationCode))
        {
            notificationSent = await CreateAndSendNotificationAsync(
                thingsBoardDeviceId,
                updateResult.NotificationCode,
                updateResult.CurrentStatus,
                updateResult.OutageDuration,
                phaseAVoltageV,
                phaseBVoltageV,
                phaseCVoltageV,
                timestampUtc ?? DateTime.UtcNow,
                cancellationToken);
        }

        _logger.LogInformation(
            "Grid status processed for device {DeviceId}: gridConnected={GridConnected}, statusChanged={StatusChanged}, notificationSent={NotificationSent}",
            thingsBoardDeviceId, gridConnected, updateResult.StatusChanged, notificationSent);

        return new ProcessGridStatusTelemetryResult
        {
            Success = true,
            Message = updateResult.StatusChanged
                ? $"Grid status changed to {updateResult.CurrentStatus}"
                : "Grid status unchanged",
            DeviceId = thingsBoardDeviceId,
            GridConnected = gridConnected,
            StatusChanged = updateResult.StatusChanged,
            NotificationSent = notificationSent
        };
    }

    private async Task<bool> CreateAndSendNotificationAsync(
        Guid thingsBoardDeviceId,
        string notificationCode,
        GridConnectionStatus currentStatus,
        TimeSpan? outageDuration,
        double? phaseAVoltageV,
        double? phaseBVoltageV,
        double? phaseCVoltageV,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get internal device ID
            var internalDeviceId = await _deviceMapping.GetDeviceIdAsync(thingsBoardDeviceId, cancellationToken);
            if (internalDeviceId == null)
            {
                _logger.LogWarning("No device found for ThingsBoardDeviceId: {ThingsBoardDeviceId}", thingsBoardDeviceId);
                return false;
            }

            // Get device with related entities
            var device = await _context.Devices
                .Include(d => d.SetupDevice!)
                .ThenInclude(sd => sd.Setup!)
                .ThenInclude(s => s.Site!)
                .FirstOrDefaultAsync(d => d.Id == internalDeviceId, cancellationToken);

            if (device?.SetupDevice?.Setup?.Site == null)
            {
                _logger.LogWarning("Device {DeviceId} is not properly assigned to a setup and site", internalDeviceId);
                return false;
            }

            // Find notification type by code
            var notificationType = await _context.DeviceNotificationTypes
                .FirstOrDefaultAsync(nt => nt.Code == notificationCode && nt.IsActive, cancellationToken);

            if (notificationType == null)
            {
                _logger.LogWarning("Notification type not found for code: {Code}", notificationCode);
                return false;
            }

            // Build additional data
            var additionalData = new Dictionary<string, object>
            {
                ["gridStatus"] = currentStatus.ToString(),
                ["phaseAVoltageV"] = phaseAVoltageV ?? 0,
                ["phaseBVoltageV"] = phaseBVoltageV ?? 0,
                ["phaseCVoltageV"] = phaseCVoltageV ?? 0,
                ["timestamp"] = timestamp.ToString("O")
            };

            if (outageDuration.HasValue)
            {
                additionalData["outageDurationSeconds"] = outageDuration.Value.TotalSeconds;
                additionalData["outageDurationFormatted"] = FormatDuration(outageDuration.Value);
            }

            // Create notification
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

            _context.DeviceNotifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            // Send SignalR notification
            var processedNotification = new ProcessedNotificationInfo
            {
                Id = notification.Id,
                Key = notificationType.Code,
                Message = notificationType.Message,
                Severity = notificationType.Severity,
                Description = notificationType.Description
            };

            await _notificationSender.SendNotificationsAsync(
                new List<Guid> { notification.Id },
                new List<ProcessedNotificationInfo> { processedNotification },
                cancellationToken);

            _logger.LogInformation(
                "Grid status notification created and sent: {NotificationId} ({Code}) for device {DeviceId}",
                notification.Id, notificationCode, internalDeviceId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating grid status notification for device {DeviceId}", thingsBoardDeviceId);
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse the gridConnected value from the data element.
    /// Handles multiple property name casings and value types (boolean, integer 0/1, string "true"/"false").
    /// </summary>
    public static bool TryGetGridConnectedValue(JsonElement dataElement, out bool gridConnected)
    {
        gridConnected = true;

        // Try common property name variations (case-sensitive JSON property lookup)
        JsonElement gridConnectedEl = default;
        bool found = dataElement.TryGetProperty("gridConnected", out gridConnectedEl)
                  || dataElement.TryGetProperty("grid_connected", out gridConnectedEl)
                  || dataElement.TryGetProperty("GridConnected", out gridConnectedEl)
                  || dataElement.TryGetProperty("GRID_CONNECTED", out gridConnectedEl);

        if (!found)
        {
            // Fallback: case-insensitive search through all properties
            foreach (var prop in dataElement.EnumerateObject())
            {
                if (prop.Name.Equals("gridConnected", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("grid_connected", StringComparison.OrdinalIgnoreCase))
                {
                    gridConnectedEl = prop.Value;
                    found = true;
                    break;
                }
            }
        }

        if (!found)
            return false;

        // Parse the value based on its JSON type
        switch (gridConnectedEl.ValueKind)
        {
            case JsonValueKind.True:
                gridConnected = true;
                return true;

            case JsonValueKind.False:
                gridConnected = false;
                return true;

            case JsonValueKind.Number:
                // 0 = disconnected, any non-zero = connected
                gridConnected = gridConnectedEl.TryGetInt32(out var intVal) ? intVal != 0 : gridConnectedEl.GetDouble() != 0;
                return true;

            case JsonValueKind.String:
                var strVal = gridConnectedEl.GetString();
                if (bool.TryParse(strVal, out var boolVal))
                {
                    gridConnected = boolVal;
                    return true;
                }
                // Handle "0"/"1" as string
                if (strVal == "0") { gridConnected = false; return true; }
                if (strVal == "1") { gridConnected = true; return true; }
                return false;

            default:
                return false;
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
