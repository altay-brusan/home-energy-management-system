using System.Text.Json;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;
using Hems_Backend.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Application.DeviceData.Commands.ProcessAlarmTelemetry;

/// <summary>
/// Result of processing alarm telemetry
/// </summary>
public record ProcessAlarmTelemetryResult
{
    public bool Success { get; init; }
    public int ProcessedCount { get; init; }
    public int CreatedCount { get; init; }
    public List<ProcessedNotificationInfo> Notifications { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Command to process raw alarm telemetry from ThingsBoard
/// Handles JSON parsing, device mapping, alarm processing, and SignalR notifications
/// </summary>
public record ProcessAlarmTelemetryCommand : IRequest<ProcessAlarmTelemetryResult>
{
    /// <summary>
    /// Raw JSON body from the telemetry endpoint
    /// Expected format: { deviceId: string, data: InverterAlarmState }
    /// </summary>
    public JsonElement RawBody { get; init; }
}

public class ProcessAlarmTelemetryCommandHandler : IRequestHandler<ProcessAlarmTelemetryCommand, ProcessAlarmTelemetryResult>
{
    private readonly ISender _sender;
    private readonly IDeviceMappingService _deviceMapping;
    private readonly IDeviceActivityService _activityService;
    private readonly IDemoDataService _demoDataService;
    private readonly IAlarmNotificationSender _notificationSender;
    private readonly ILogger<ProcessAlarmTelemetryCommandHandler> _logger;

    public ProcessAlarmTelemetryCommandHandler(
        ISender sender,
        IDeviceMappingService deviceMapping,
        IDeviceActivityService activityService,
        IDemoDataService demoDataService,
        IAlarmNotificationSender notificationSender,
        ILogger<ProcessAlarmTelemetryCommandHandler> logger)
    {
        _sender = sender;
        _deviceMapping = deviceMapping;
        _activityService = activityService;
        _demoDataService = demoDataService;
        _notificationSender = notificationSender;
        _logger = logger;
    }

    public async Task<ProcessAlarmTelemetryResult> Handle(ProcessAlarmTelemetryCommand request, CancellationToken cancellationToken)
    {
        var body = request.RawBody;

        _logger.LogInformation("Alarm telemetry received at {UtcNow}", DateTime.UtcNow);

        // 1) Parse ThingsBoardDeviceId from root level "deviceId" field
        Guid thingsBoardDeviceId = Guid.Empty;
        if (body.TryGetProperty("deviceId", out var deviceIdElement))
        {
            var deviceIdStr = deviceIdElement.GetString();
            if (!string.IsNullOrWhiteSpace(deviceIdStr) && Guid.TryParse(deviceIdStr, out var parsedDeviceId))
            {
                thingsBoardDeviceId = parsedDeviceId;
            }
            else
            {
                _logger.LogWarning("Invalid deviceId format: {DeviceId}", deviceIdStr);
                return new ProcessAlarmTelemetryResult
                {
                    Success = false,
                    Errors = new List<string> { "Invalid 'deviceId' format" }
                };
            }
        }
        else
        {
            _logger.LogWarning("Missing deviceId in root level");
            return new ProcessAlarmTelemetryResult
            {
                Success = false,
                Errors = new List<string> { "Missing 'deviceId' in request" }
            };
        }

        // Record device activity
        await _activityService.RecordActivityAsync(thingsBoardDeviceId, TelemetrySource.Alarm);

        // Check if this is a demo device - skip database storage for demo devices
        var isDemoDevice = await _demoDataService.IsDemoDeviceByThingsBoardIdAsync(thingsBoardDeviceId, cancellationToken);
        if (isDemoDevice)
        {
            _logger.LogDebug(
                "Skipping Alarm database storage for demo device {DeviceId}",
                thingsBoardDeviceId);
            return new ProcessAlarmTelemetryResult
            {
                Success = true,
                ProcessedCount = 0,
                CreatedCount = 0,
                Notifications = new List<ProcessedNotificationInfo>(),
                Errors = new List<string>()
            };
        }

        // 2) Convert ThingsBoard ID to internal device ID
        var internalDeviceId = await _deviceMapping.GetDeviceIdAsync(thingsBoardDeviceId, cancellationToken);
        if (internalDeviceId == null)
        {
            _logger.LogWarning("No device found for ThingsBoardDeviceId: {ThingsBoardDeviceId}", thingsBoardDeviceId);
            return new ProcessAlarmTelemetryResult
            {
                Success = false,
                Errors = new List<string> { $"Device not found for ThingsBoardDeviceId: {thingsBoardDeviceId}" }
            };
        }

        // 3) Get the inner "data" object containing InverterAlarmState
        if (!body.TryGetProperty("data", out var dataElement) ||
            dataElement.ValueKind != JsonValueKind.Object)
        {
            _logger.LogWarning("Received alarm telemetry without 'data' object");
            return new ProcessAlarmTelemetryResult
            {
                Success = false,
                Errors = new List<string> { "Missing 'data' object" }
            };
        }

        // 4) Parse InverterAlarmState from data
        var alarmState = ParseAlarmState(dataElement);

        // 5) Process alarms via ProcessDeviceAlarmCommand
        var command = new ProcessDeviceAlarmCommand
        {
            DeviceId = internalDeviceId.Value,
            AlarmState = alarmState,
            RawJson = dataElement.GetRawText() // Store original JSON for AdditionalData
        };

        var result = await _sender.Send(command, cancellationToken);

        // 6) Send SignalR notifications for created notifications
        if (result.CreatedNotificationIds.Any())
        {
            await _notificationSender.SendNotificationsAsync(
                result.CreatedNotificationIds,
                result.Notifications,
                cancellationToken);
        }

        _logger.LogInformation(
            "Alarm telemetry processed for device {DeviceId} (TB: {ThingsBoardDeviceId}). " +
            "Processed: {ProcessedCount}, Created: {CreatedCount}, Errors: {ErrorCount}",
            internalDeviceId, thingsBoardDeviceId,
            result.ProcessedAlarmsCount, result.CreatedNotificationIds.Count, result.Errors.Count);

        return new ProcessAlarmTelemetryResult
        {
            Success = result.Errors.Count == 0,
            ProcessedCount = result.ProcessedAlarmsCount,
            CreatedCount = result.CreatedNotificationIds.Count,
            Notifications = result.Notifications,
            Errors = result.Errors
        };
    }

    private static InverterAlarmStateDto ParseAlarmState(JsonElement dataElement)
    {
        var alarmState = new InverterAlarmStateDto();

        // Parse Kind to determine device type (InverterAlarmState or BatteryAlarmState)
        if (dataElement.TryGetProperty("kind", out var kindEl))
            alarmState.Kind = kindEl.GetString() ?? string.Empty;

        if (dataElement.TryGetProperty("timestampUtc", out var tsEl))
            alarmState.TimestampUtc = tsEl.GetString() ?? string.Empty;

        if (dataElement.TryGetProperty("version", out var verEl))
            alarmState.Version = verEl.GetInt32();

        if (dataElement.TryGetProperty("gatewayId", out var gwEl))
            alarmState.GatewayId = gwEl.GetString() ?? string.Empty;

        // Support both inverterSerialNumber and generic serialNumber
        if (dataElement.TryGetProperty("inverterSerialNumber", out var snEl))
            alarmState.SerialNumber = snEl.GetString() ?? string.Empty;
        else if (dataElement.TryGetProperty("serialNumber", out var sn2El))
            alarmState.SerialNumber = sn2El.GetString() ?? string.Empty;

        if (dataElement.TryGetProperty("runStateRaw", out var rsEl))
            alarmState.RunStateRaw = rsEl.GetInt32();

        if (dataElement.TryGetProperty("commBoardFaultRaw", out var cbfEl))
            alarmState.CommBoardFaultRaw = cbfEl.GetInt32();

        if (dataElement.TryGetProperty("warningWord1Raw", out var ww1El))
            alarmState.WarningWord1Raw = ww1El.GetInt32();

        if (dataElement.TryGetProperty("warningWord2Raw", out var ww2El))
            alarmState.WarningWord2Raw = ww2El.GetInt32();

        if (dataElement.TryGetProperty("faultWord1Raw", out var fw1El))
            alarmState.FaultWord1Raw = fw1El.GetInt32();

        if (dataElement.TryGetProperty("faultWord2Raw", out var fw2El))
            alarmState.FaultWord2Raw = fw2El.GetInt32();

        if (dataElement.TryGetProperty("faultWord3Raw", out var fw3El))
            alarmState.FaultWord3Raw = fw3El.GetInt32();

        if (dataElement.TryGetProperty("faultWord4Raw", out var fw4El))
            alarmState.FaultWord4Raw = fw4El.GetInt32();

        if (dataElement.TryGetProperty("workFlagTripData", out var wftEl))
            alarmState.WorkFlagTripData = wftEl.GetInt32();

        // Parse activeAlarms array
        if (dataElement.TryGetProperty("activeAlarms", out var alarmsEl) &&
            alarmsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var alarmEl in alarmsEl.EnumerateArray())
            {
                var alarm = ParseAlarm(alarmEl);
                alarmState.ActiveAlarms.Add(alarm);
            }
        }

        return alarmState;
    }

    private static AlarmDto ParseAlarm(JsonElement alarmEl)
    {
        var alarm = new AlarmDto();

        if (alarmEl.TryGetProperty("code", out var codeEl))
            alarm.Code = codeEl.GetInt32();

        if (alarmEl.TryGetProperty("vendorCode", out var vcEl))
            alarm.VendorCode = vcEl.GetInt32();

        if (alarmEl.TryGetProperty("severity", out var sevEl))
            alarm.Severity = sevEl.GetString() ?? string.Empty;

        if (alarmEl.TryGetProperty("sourceDevice", out var sdEl))
            alarm.SourceDevice = sdEl.GetString() ?? string.Empty;

        if (alarmEl.TryGetProperty("category", out var catEl))
            alarm.Category = catEl.GetString() ?? string.Empty;

        if (alarmEl.TryGetProperty("messageKey", out var mkEl))
            alarm.MessageKey = mkEl.GetString() ?? string.Empty;

        if (alarmEl.TryGetProperty("messageParams", out var mpEl) &&
            mpEl.ValueKind == JsonValueKind.Object)
        {
            alarm.MessageParams = JsonSerializer.Deserialize<Dictionary<string, object>>(mpEl.GetRawText())
                ?? new Dictionary<string, object>();
        }

        if (alarmEl.TryGetProperty("firstOccurredUtc", out var foEl))
            alarm.FirstOccurredUtc = foEl.GetString() ?? string.Empty;

        if (alarmEl.TryGetProperty("lastOccurredUtc", out var loEl))
            alarm.LastOccurredUtc = loEl.GetString() ?? string.Empty;

        if (alarmEl.TryGetProperty("occurrenceCount", out var ocEl))
            alarm.OccurrenceCount = ocEl.GetInt32();

        return alarm;
    }
}
