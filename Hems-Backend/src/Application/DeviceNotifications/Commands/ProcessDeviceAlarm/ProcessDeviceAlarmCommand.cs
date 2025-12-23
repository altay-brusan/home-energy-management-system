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

    public ProcessDeviceAlarmCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Resolves the notification type code based on the alarm's source device, severity, and message key.
    /// Template: [INV/BMS]-[CATEGORY]-[device's code]
    /// Categories: INF (Info), DBG (Debug), ERR (Error), FAT (Fault), WAR (Warning)
    /// </summary>
    private static string ResolveNotificationTypeCode(AlarmDto alarm)
    {
        var sourceDevice = alarm.SourceDevice?.ToUpperInvariant() ?? "INV";
        var severity = alarm.Severity?.ToUpperInvariant() ?? "FAULT";
        var messageKey = alarm.MessageKey?.ToUpperInvariant() ?? "";

        // Determine device prefix
        var devicePrefix = sourceDevice switch
        {
            "BMS" or "BATTERY" => "BMS",
            _ => "INV"
        };

        // Determine category based on severity
        var category = severity switch
        {
            "FAULT" or "CRITICAL" or "PROTECTION" => "FAT",
            "ERROR" => "ERR",
            "WARNING" or "ALARM" => "WAR",
            "INFO" or "INFORMATION" => "INF",
            "DEBUG" => "DBG",
            _ => "FAT" // Default to fault for unknown severity
        };

        // Extract the device code (e.g., F01, P01, A01, etc.)
        // messageKey typically contains the fault/alarm code like "F01", "F58", "P01", "A01"
        var deviceCode = messageKey;

        // If messageKey doesn't look like a code, try to construct one
        if (string.IsNullOrEmpty(deviceCode) || !IsValidDeviceCode(deviceCode))
        {
            // For inverter faults, use Fxx format
            if (devicePrefix == "INV" && category == "FAT")
            {
                deviceCode = $"F{alarm.VendorCode:D2}";
            }
            // For BMS protections, use Pxx format
            else if (devicePrefix == "BMS" && category == "FAT")
            {
                deviceCode = $"P{alarm.VendorCode:D2}";
            }
            // For BMS alarms/warnings, use Axx format
            else if (devicePrefix == "BMS" && category == "WAR")
            {
                deviceCode = $"A{alarm.VendorCode:D2}";
            }
            // For BMS errors, use Exx format
            else if (devicePrefix == "BMS" && category == "ERR")
            {
                deviceCode = $"E{alarm.VendorCode:D2}";
            }
            // For inverter warnings, use Wxx format
            else if (devicePrefix == "INV" && category == "WAR")
            {
                deviceCode = $"W{alarm.VendorCode:D2}";
            }
            // For info messages, use Ixx format
            else if (category == "INF")
            {
                deviceCode = $"I{alarm.VendorCode:D2}";
            }
            else
            {
                deviceCode = alarm.VendorCode.ToString("D2");
            }
        }

        return $"{devicePrefix}-{category}-{deviceCode}";
    }

    /// <summary>
    /// Checks if a string looks like a valid device code (e.g., F01, P02, A03)
    /// </summary>
    private static bool IsValidDeviceCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length < 2)
            return false;

        // Valid codes start with a letter followed by digits (F01, P02, A03, W01, E01, I01)
        return char.IsLetter(code[0]) && code.Skip(1).All(char.IsDigit);
    }

    /// <summary>
    /// Resolves the generic notification type code based on the alarm's source device and severity.
    /// Template: [INV/BMS]-[CATEGORY]-GENERIC
    /// </summary>
    private static string ResolveGenericNotificationTypeCode(AlarmDto alarm)
    {
        var sourceDevice = alarm.SourceDevice?.ToUpperInvariant() ?? "INV";
        var severity = alarm.Severity?.ToUpperInvariant() ?? "FAULT";

        var devicePrefix = sourceDevice switch
        {
            "BMS" or "BATTERY" => "BMS",
            _ => "INV"
        };

        var category = severity switch
        {
            "FAULT" or "CRITICAL" or "PROTECTION" => "FAT",
            "ERROR" => "ERR",
            "WARNING" or "ALARM" => "WAR",
            _ => "FAT"
        };

        return $"{devicePrefix}-{category}-GENERIC";
    }

    /// <summary>
    /// Builds a human-readable message from raw register values for unknown alarms.
    /// </summary>
    private static string BuildRawRegisterMessage(InverterAlarmStateDto alarmState, AlarmDto alarm)
    {
        var parts = new List<string>();

        if (alarmState.RunStateRaw > 0)
            parts.Add($"RunState: 0x{alarmState.RunStateRaw:X4}");

        if (alarmState.WarningWord1Raw > 0)
            parts.Add($"Warn1: 0x{alarmState.WarningWord1Raw:X4}");

        if (alarmState.WarningWord2Raw > 0)
            parts.Add($"Warn2: 0x{alarmState.WarningWord2Raw:X4}");

        if (alarmState.FaultWord1Raw > 0)
            parts.Add($"Fault1: 0x{alarmState.FaultWord1Raw:X4}");

        if (alarmState.FaultWord2Raw > 0)
            parts.Add($"Fault2: 0x{alarmState.FaultWord2Raw:X4}");

        if (alarmState.FaultWord3Raw > 0)
            parts.Add($"Fault3: 0x{alarmState.FaultWord3Raw:X4}");

        if (alarmState.FaultWord4Raw > 0)
            parts.Add($"Fault4: 0x{alarmState.FaultWord4Raw:X4}");

        // Add alarm-specific info
        if (!string.IsNullOrEmpty(alarm.MessageKey))
            parts.Add($"Key: {alarm.MessageKey}");

        if (alarm.VendorCode > 0)
            parts.Add($"VendorCode: {alarm.VendorCode}");

        if (alarm.Code > 0)
            parts.Add($"Code: {alarm.Code}");

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Unknown alarm - no register data available";
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
                // Resolve the notification type code using the new template format
                // Template: [INV/BMS]-[CATEGORY]-[device's code]
                var notificationTypeCode = ResolveNotificationTypeCode(alarm);
                var isGenericFallback = false;
                string? rawRegisterMessage = null;

                var notificationType = await _context.DeviceNotificationTypes
                    .FirstOrDefaultAsync(nt => nt.Code == notificationTypeCode && nt.IsActive, cancellationToken);

                if (notificationType == null)
                {
                    // Try generic fallback
                    var genericCode = ResolveGenericNotificationTypeCode(alarm);
                    notificationType = await _context.DeviceNotificationTypes
                        .FirstOrDefaultAsync(nt => nt.Code == genericCode && nt.IsActive, cancellationToken);

                    if (notificationType == null)
                    {
                        // Log detailed info for debugging - neither specific nor generic found
                        errors.Add($"Notification type '{notificationTypeCode}' and generic '{genericCode}' not found for alarm " +
                                  $"(code: {alarm.Code}, vendorCode: {alarm.VendorCode}, severity: {alarm.Severity}, " +
                                  $"sourceDevice: {alarm.SourceDevice}, messageKey: {alarm.MessageKey}). Skipping.");
                        continue;
                    }

                    // Using generic fallback - build raw register message
                    isGenericFallback = true;
                    rawRegisterMessage = BuildRawRegisterMessage(request.AlarmState, alarm);
                    notificationTypeCode = genericCode;
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
                    ["resolvedCode"] = notificationTypeCode,
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
                    ["faultWord4Raw"] = request.AlarmState.FaultWord4Raw,
                    ["isGenericFallback"] = isGenericFallback
                };

                // Add raw register message for generic fallback notifications
                if (isGenericFallback && !string.IsNullOrEmpty(rawRegisterMessage))
                {
                    additionalDataDict["rawRegisterMessage"] = rawRegisterMessage;
                }

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
