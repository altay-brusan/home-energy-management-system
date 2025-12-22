using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;

public record ProcessDeviceAlarmCommand : IRequest<ProcessDeviceAlarmResult>
{
    public Guid DeviceId { get; init; }
    public InverterAlarmStateDto AlarmState { get; init; } = null!;
}

public record ProcessDeviceAlarmResult
{
    public int ProcessedAlarmsCount { get; init; }
    public List<Guid> CreatedNotificationIds { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}

public class ProcessDeviceAlarmCommandHandler : IRequestHandler<ProcessDeviceAlarmCommand, ProcessDeviceAlarmResult>
{
    private readonly IApplicationDbContext _context;

    // Mapping from Node-RED alarm codes to our DeviceNotificationType codes
    private static readonly Dictionary<int, string> AlarmCodeMapping = new()
    {
        // Fault codes (1001-1099)
        { 1001, "INV-ERR-DC-VOLTAGE-HIGH" },
        { 1002, "INV-ERR-BUS-VOLTAGE-HIGH" },
        { 1003, "INV-ERR-BUS-VOLTAGE-LOW" },
        { 1004, "INV-ERR-BUS-VOLTAGE-UNBALANCED" },
        { 1005, "INV-ERR-GRID-VOLTAGE-HIGH" },
        { 1006, "INV-ERR-GRID-VOLTAGE-LOW" },
        { 1007, "INV-ERR-GRID-FREQ-HIGH" },
        { 1008, "INV-ERR-GRID-FREQ-LOW" },
        { 1009, "INV-ERR-GRID-DISCONNECT" },
        { 1010, "INV-ERR-OUTPUT-CURRENT-HIGH" },
        { 1011, "INV-ERR-OVER-TEMP" },
        { 1012, "INV-ERR-LEAKAGE-CURRENT" },
        { 1013, "INV-ERR-HARDWARE-FAULT" },
        { 1014, "INV-ERR-EEPROM-FAULT" },
        { 1015, "INV-ERR-RELAY-FAULT" },
        { 1016, "INV-ERR-DSP-FAULT" },

        // Warning codes (2001-2099)
        { 2001, "INV-WARN-BATTERY-VOLTAGE-HIGH" },
        { 2002, "INV-WARN-BATTERY-VOLTAGE-LOW" },
        { 2003, "INV-WARN-BATTERY-COMM-FAIL" },
        { 2004, "INV-WARN-TEMP-HIGH" },
        { 2005, "INV-WARN-PV-VOLTAGE-HIGH" },
        { 2006, "INV-WARN-FAN-LOCKED" },
        { 2007, "INV-WARN-FEED-IN-POWER-LIMIT" },
        { 2008, "INV-WARN-FIRMWARE-UPDATE" },
        { 2009, "INV-WARN-PHASE-IMBALANCE" },
        { 2010, "INV-WARN-PHASE-VOLTAGE-UNBALANCED" }
    };

    public ProcessDeviceAlarmCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProcessDeviceAlarmResult> Handle(ProcessDeviceAlarmCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var createdNotificationIds = new List<Guid>();

        // Get device with related entities
        var device = await _context.Devices
            .Include(d => d.SetupDevice!)
            .ThenInclude(sd => sd.Setup!)
            .ThenInclude(s => s.Site!)
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken);

        if (device == null)
        {
            errors.Add($"Device with ID {request.DeviceId} not found");
            return new ProcessDeviceAlarmResult { Errors = errors };
        }

        if (device.SetupDevice?.Setup?.Site == null)
        {
            errors.Add($"Device {request.DeviceId} is not properly assigned to a setup and site");
            return new ProcessDeviceAlarmResult { Errors = errors };
        }

        var timestamp = DateTimeOffset.TryParse(request.AlarmState.TimestampUtc, out var parsedTime)
            ? parsedTime
            : DateTimeOffset.UtcNow;

        // Process each active alarm
        foreach (var alarm in request.AlarmState.ActiveAlarms)
        {
            try
            {
                // Try to find notification type by mapped code
                string? notificationTypeCode = null;

                if (AlarmCodeMapping.TryGetValue(alarm.Code, out var mappedCode))
                {
                    notificationTypeCode = mappedCode;
                }
                else
                {
                    // Fallback: Try to construct code from severity and messageKey
                    var level = alarm.Severity.ToUpper() switch
                    {
                        "FAULT" => "ERR",
                        "WARNING" => "WARN",
                        _ => "INFO"
                    };

                    notificationTypeCode = $"INV-{level}-{alarm.MessageKey}";
                }

                var notificationType = await _context.DeviceNotificationTypes
                    .FirstOrDefaultAsync(nt => nt.Code == notificationTypeCode && nt.IsActive, cancellationToken);

                if (notificationType == null)
                {
                    // Create a generic notification type code for unknown alarms
                    notificationTypeCode = $"INV-{(alarm.Severity.ToUpper() == "FAULT" ? "ERR" : "WARN")}-UNKNOWN-{alarm.Code}";

                    notificationType = await _context.DeviceNotificationTypes
                        .FirstOrDefaultAsync(nt => nt.Code == notificationTypeCode, cancellationToken);

                    if (notificationType == null)
                    {
                        errors.Add($"Notification type not found for alarm code {alarm.Code} ({alarm.MessageKey}). Skipping.");
                        continue;
                    }
                }

                // Check if this notification already exists (avoid duplicates)
                var existingNotification = await _context.DeviceNotifications
                    .Where(dn => dn.DeviceId == request.DeviceId &&
                                 dn.DeviceNotificationTypeId == notificationType.Id &&
                                 !dn.IsProcessed &&
                                 dn.Timestamp >= timestamp.AddMinutes(-5)) // Within last 5 minutes
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingNotification != null)
                {
                    // Update existing notification with new occurrence
                    var additionalData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        existingNotification.AdditionalData) ?? new Dictionary<string, object>();

                    if (additionalData.ContainsKey("occurrenceCount"))
                    {
                        additionalData["occurrenceCount"] = alarm.OccurrenceCount;
                    }

                    additionalData["lastOccurredUtc"] = alarm.LastOccurredUtc;

                    existingNotification.AdditionalData = JsonSerializer.Serialize(additionalData);
                    existingNotification.Timestamp = timestamp;

                    continue;
                }

                // Create new notification
                var additionalDataDict = new Dictionary<string, object>
                {
                    ["severity"] = alarm.Severity,
                    ["sourceDevice"] = alarm.SourceDevice,
                    ["category"] = alarm.Category,
                    ["messageKey"] = alarm.MessageKey,
                    ["vendorCode"] = alarm.VendorCode,
                    ["alarmCode"] = alarm.Code,
                    ["messageParams"] = alarm.MessageParams,
                    ["firstOccurredUtc"] = alarm.FirstOccurredUtc,
                    ["lastOccurredUtc"] = alarm.LastOccurredUtc,
                    ["occurrenceCount"] = alarm.OccurrenceCount,
                    ["gatewayId"] = request.AlarmState.GatewayId,
                    ["inverterSerialNumber"] = request.AlarmState.InverterSerialNumber,
                    ["runStateRaw"] = request.AlarmState.RunStateRaw,
                    ["warningWord1Raw"] = request.AlarmState.WarningWord1Raw,
                    ["warningWord2Raw"] = request.AlarmState.WarningWord2Raw,
                    ["faultWord1Raw"] = request.AlarmState.FaultWord1Raw,
                    ["faultWord2Raw"] = request.AlarmState.FaultWord2Raw,
                    ["faultWord3Raw"] = request.AlarmState.FaultWord3Raw,
                    ["faultWord4Raw"] = request.AlarmState.FaultWord4Raw
                };

                var deviceNotification = new DeviceNotification
                {
                    DeviceId = request.DeviceId,
                    SiteId = device.SetupDevice.Setup.SiteId,
                    SetupDeviceId = device.SetupDevice.Id,
                    DeviceNotificationTypeId = notificationType.Id,
                    Timestamp = timestamp,
                    AdditionalData = JsonSerializer.Serialize(additionalDataDict),
                    IsProcessed = false
                };

                _context.DeviceNotifications.Add(deviceNotification);
                createdNotificationIds.Add(deviceNotification.Id);
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing alarm {alarm.Code}: {ex.Message}");
            }
        }

        if (createdNotificationIds.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ProcessDeviceAlarmResult
        {
            ProcessedAlarmsCount = request.AlarmState.ActiveAlarms.Count,
            CreatedNotificationIds = createdNotificationIds,
            Errors = errors
        };
    }
}
