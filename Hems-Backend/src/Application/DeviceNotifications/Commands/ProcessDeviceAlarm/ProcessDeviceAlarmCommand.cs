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
    public List<ProcessedNotificationInfo> Notifications { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Contains detailed information about a processed notification.
/// </summary>
public record ProcessedNotificationInfo
{
    /// <summary>
    /// The notification ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The notification type code (e.g., INV-FAT-F01, BMS-WAR-A01).
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// The human-readable message for this notification type.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The severity level: ALARM, WARNING, or ERROR.
    /// </summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description including register values and diagnostic information.
    /// </summary>
    public string Description { get; init; } = string.Empty;
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
    /// Template: [INV/BMS]-[SEVERITY_CODE]-[device's code]
    /// Severity Codes: ALM (Alarm), ERR (Error), WAR (Warning)
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

        // Determine severity code based on alarm severity
        var severityCode = severity switch
        {
            "ALARM" or "FAULT" or "CRITICAL" or "PROTECTION" => "ALM",
            "ERROR" => "ERR",
            "WARNING" => "WAR",
            _ => "ALM" // Default to alarm for unknown severity
        };

        // Extract the device code (e.g., F01, P01, A01, etc.)
        // messageKey typically contains the fault/alarm code like "F01", "F58", "P01", "A01"
        var deviceCode = messageKey;

        // If messageKey doesn't look like a code, try to construct one
        if (string.IsNullOrEmpty(deviceCode) || !IsValidDeviceCode(deviceCode))
        {
            // For inverter alarms (critical), use Fxx format
            if (devicePrefix == "INV" && severityCode == "ALM")
            {
                deviceCode = $"F{alarm.VendorCode:D2}";
            }
            // For BMS alarms (protections), use Pxx format
            else if (devicePrefix == "BMS" && severityCode == "ALM")
            {
                deviceCode = $"P{alarm.VendorCode:D2}";
            }
            // For BMS warnings, use Axx format
            else if (devicePrefix == "BMS" && severityCode == "WAR")
            {
                deviceCode = $"A{alarm.VendorCode:D2}";
            }
            // For BMS errors, use Exx format
            else if (devicePrefix == "BMS" && severityCode == "ERR")
            {
                deviceCode = $"E{alarm.VendorCode:D2}";
            }
            // For inverter warnings, use Wxx format
            else if (devicePrefix == "INV" && severityCode == "WAR")
            {
                deviceCode = $"W{alarm.VendorCode:D2}";
            }
            else
            {
                deviceCode = alarm.VendorCode.ToString("D2");
            }
        }

        return $"{devicePrefix}-{severityCode}-{deviceCode}";
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
    /// Template: [INV/BMS]-[SEVERITY_CODE]-GENERIC
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

        var severityCode = severity switch
        {
            "ALARM" or "FAULT" or "CRITICAL" or "PROTECTION" => "ALM",
            "ERROR" => "ERR",
            "WARNING" => "WAR",
            _ => "ALM"
        };

        return $"{devicePrefix}-{severityCode}-GENERIC";
    }

    /// <summary>
    /// Maps severity code (ALM, WAR, ERR) to severity value (ALARM, WARNING, ERROR).
    /// </summary>
    private static string MapSeverityCodeToValue(string severityCode)
    {
        return severityCode switch
        {
            "ALM" => "ALARM",
            "WAR" => "WARNING",
            "ERR" => "ERROR",
            _ => "ALARM"
        };
    }

    /// <summary>
    /// Builds a detailed description including register values for diagnostic purposes.
    /// </summary>
    private static string BuildDetailedDescription(
        InverterAlarmStateDto alarmState,
        AlarmDto? alarm,
        string notificationTypeDescription,
        bool isGenericFallback)
    {
        var parts = new List<string>();

        // Start with the notification type description
        parts.Add(notificationTypeDescription);

        // Add register values for diagnostic purposes
        var registerInfo = new List<string>();

        if (alarmState.RunStateRaw > 0)
            registerInfo.Add($"RunState: 0x{alarmState.RunStateRaw:X4}");

        if (alarmState.WarningWord1Raw > 0)
            registerInfo.Add($"Warning1: 0x{alarmState.WarningWord1Raw:X4}");

        if (alarmState.WarningWord2Raw > 0)
            registerInfo.Add($"Warning2: 0x{alarmState.WarningWord2Raw:X4}");

        if (alarmState.FaultWord1Raw > 0)
            registerInfo.Add($"Fault1: 0x{alarmState.FaultWord1Raw:X4}");

        if (alarmState.FaultWord2Raw > 0)
            registerInfo.Add($"Fault2: 0x{alarmState.FaultWord2Raw:X4}");

        if (alarmState.FaultWord3Raw > 0)
            registerInfo.Add($"Fault3: 0x{alarmState.FaultWord3Raw:X4}");

        if (alarmState.FaultWord4Raw > 0)
            registerInfo.Add($"Fault4: 0x{alarmState.FaultWord4Raw:X4}");

        if (registerInfo.Count > 0)
        {
            parts.Add($"Registers: [{string.Join(", ", registerInfo)}]");
        }

        // Add alarm-specific info if available
        if (alarm != null)
        {
            if (!string.IsNullOrEmpty(alarm.MessageKey))
                parts.Add($"MessageKey: {alarm.MessageKey}");

            if (alarm.VendorCode > 0)
                parts.Add($"VendorCode: {alarm.VendorCode}");

            if (alarm.Code > 0)
                parts.Add($"Code: {alarm.Code}");
        }

        if (isGenericFallback)
        {
            parts.Add("(Generic fallback - specific alarm type not found)");
        }

        return string.Join(" | ", parts);
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

    /// <summary>
    /// Checks if any raw register values indicate active issues.
    /// </summary>
    private static bool HasActiveRegisterIssues(InverterAlarmStateDto alarmState)
    {
        return alarmState.WarningWord1Raw != 0 ||
               alarmState.WarningWord2Raw != 0 ||
               alarmState.FaultWord1Raw != 0 ||
               alarmState.FaultWord2Raw != 0 ||
               alarmState.FaultWord3Raw != 0 ||
               alarmState.FaultWord4Raw != 0;
    }

    /// <summary>
    /// Determines the severity based on raw register values.
    /// </summary>
    private static string DetermineRawRegisterSeverity(InverterAlarmStateDto alarmState)
    {
        // Alarms (faults) are more severe than warnings
        if (alarmState.FaultWord1Raw != 0 || alarmState.FaultWord2Raw != 0 ||
            alarmState.FaultWord3Raw != 0 || alarmState.FaultWord4Raw != 0)
        {
            return "ALARM";
        }

        if (alarmState.WarningWord1Raw != 0 || alarmState.WarningWord2Raw != 0)
        {
            return "WARNING";
        }

        return "WARNING";
    }

    /// <summary>
    /// Builds the raw register code string for display.
    /// Format: Fxx for faults, Wxx for warnings
    /// </summary>
    private static string BuildRawRegisterCode(InverterAlarmStateDto alarmState)
    {
        var parts = new List<string>();

        if (alarmState.FaultWord1Raw != 0)
            parts.Add($"FW1:0x{alarmState.FaultWord1Raw:X4}");
        if (alarmState.FaultWord2Raw != 0)
            parts.Add($"FW2:0x{alarmState.FaultWord2Raw:X4}");
        if (alarmState.FaultWord3Raw != 0)
            parts.Add($"FW3:0x{alarmState.FaultWord3Raw:X4}");
        if (alarmState.FaultWord4Raw != 0)
            parts.Add($"FW4:0x{alarmState.FaultWord4Raw:X4}");
        if (alarmState.WarningWord1Raw != 0)
            parts.Add($"WW1:0x{alarmState.WarningWord1Raw:X4}");
        if (alarmState.WarningWord2Raw != 0)
            parts.Add($"WW2:0x{alarmState.WarningWord2Raw:X4}");

        return parts.Count > 0 ? string.Join(", ", parts) : "Unknown";
    }

    public async Task<ProcessDeviceAlarmResult> Handle(ProcessDeviceAlarmCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var createdNotificationIds = new List<Guid>();
        var processedNotifications = new List<ProcessedNotificationInfo>();

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

        // Handle case when activeAlarms is empty but raw registers indicate issues
        if ((request.AlarmState.ActiveAlarms == null || !request.AlarmState.ActiveAlarms.Any()) &&
            HasActiveRegisterIssues(request.AlarmState))
        {
            try
            {
                var severity = DetermineRawRegisterSeverity(request.AlarmState);
                var rawCode = BuildRawRegisterCode(request.AlarmState);

                // Determine generic notification type based on severity
                var genericCode = severity == "ALARM" ? "INV-ALM-GENERIC" :
                                  severity == "WARNING" ? "INV-WAR-GENERIC" : "INV-ERR-GENERIC";

                var notificationType = await _context.DeviceNotificationTypes
                    .FirstOrDefaultAsync(nt => nt.Code == genericCode && nt.IsActive, cancellationToken);

                if (notificationType != null)
                {
                    // Check if this notification already exists (avoid duplicates)
                    var existingNotification = await _context.DeviceNotifications
                        .Where(dn => dn.DeviceId == request.DeviceId &&
                                     dn.DeviceNotificationTypeId == notificationType.Id &&
                                     !dn.IsProcessed &&
                                     dn.Timestamp >= timestamp.AddMinutes(-5))
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingNotification == null)
                    {
                        var additionalDataDict = new Dictionary<string, object>
                        {
                            ["resolvedCode"] = genericCode,
                            ["severity"] = severity,
                            ["sourceDevice"] = "INV",
                            ["category"] = "raw-register",
                            ["rawRegisterCode"] = rawCode,
                            ["gatewayId"] = request.AlarmState.GatewayId ?? string.Empty,
                            ["inverterSerialNumber"] = request.AlarmState.InverterSerialNumber ?? string.Empty,
                            ["runStateRaw"] = request.AlarmState.RunStateRaw,
                            ["warningWord1Raw"] = request.AlarmState.WarningWord1Raw,
                            ["warningWord2Raw"] = request.AlarmState.WarningWord2Raw,
                            ["faultWord1Raw"] = request.AlarmState.FaultWord1Raw,
                            ["faultWord2Raw"] = request.AlarmState.FaultWord2Raw,
                            ["faultWord3Raw"] = request.AlarmState.FaultWord3Raw,
                            ["faultWord4Raw"] = request.AlarmState.FaultWord4Raw,
                            ["isGenericFallback"] = true,
                            ["displayMessage"] = $"Device issue detected (Code: {rawCode}). Please contact technical support for assistance."
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

                        // Build detailed description with register values
                        var detailedDescription = BuildDetailedDescription(
                            request.AlarmState,
                            null,
                            notificationType.Description,
                            isGenericFallback: true);

                        processedNotifications.Add(new ProcessedNotificationInfo
                        {
                            Id = deviceNotification.Id,
                            Key = notificationType.Code,
                            Message = notificationType.Message,
                            Severity = notificationType.Severity,
                            Description = detailedDescription
                        });

                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
                else
                {
                    errors.Add($"Generic notification type '{genericCode}' not found for raw register alarm");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing raw register alarm: {ex.Message}");
            }

            return new ProcessDeviceAlarmResult
            {
                ProcessedAlarmsCount = 0,
                CreatedNotificationIds = createdNotificationIds,
                Notifications = processedNotifications,
                Errors = errors
            };
        }

        // If activeAlarms is null or empty and no raw register issues, return early
        if (request.AlarmState.ActiveAlarms == null || !request.AlarmState.ActiveAlarms.Any())
        {
            return new ProcessDeviceAlarmResult
            {
                ProcessedAlarmsCount = 0,
                CreatedNotificationIds = createdNotificationIds,
                Notifications = processedNotifications,
                Errors = errors
            };
        }

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

                    additionalData["lastOccurredUtc"] = alarm.LastOccurredUtc ?? string.Empty;

                    existingNotification.AdditionalData = JsonSerializer.Serialize(additionalData);
                    existingNotification.Timestamp = timestamp;

                    continue;
                }

                // Create new notification
                var additionalDataDict = new Dictionary<string, object>
                {
                    ["resolvedCode"] = notificationTypeCode,
                    ["severity"] = alarm.Severity ?? string.Empty,
                    ["sourceDevice"] = alarm.SourceDevice ?? string.Empty,
                    ["category"] = alarm.Category ?? string.Empty,
                    ["messageKey"] = alarm.MessageKey ?? string.Empty,
                    ["vendorCode"] = alarm.VendorCode,
                    ["alarmCode"] = alarm.Code,
                    ["messageParams"] = alarm.MessageParams ?? new Dictionary<string, object>(),
                    ["firstOccurredUtc"] = alarm.FirstOccurredUtc ?? string.Empty,
                    ["lastOccurredUtc"] = alarm.LastOccurredUtc ?? string.Empty,
                    ["occurrenceCount"] = alarm.OccurrenceCount,
                    ["gatewayId"] = request.AlarmState.GatewayId ?? string.Empty,
                    ["inverterSerialNumber"] = request.AlarmState.InverterSerialNumber ?? string.Empty,
                    ["runStateRaw"] = request.AlarmState.RunStateRaw,
                    ["warningWord1Raw"] = request.AlarmState.WarningWord1Raw,
                    ["warningWord2Raw"] = request.AlarmState.WarningWord2Raw,
                    ["faultWord1Raw"] = request.AlarmState.FaultWord1Raw,
                    ["faultWord2Raw"] = request.AlarmState.FaultWord2Raw,
                    ["faultWord3Raw"] = request.AlarmState.FaultWord3Raw,
                    ["faultWord4Raw"] = request.AlarmState.FaultWord4Raw,
                    ["isGenericFallback"] = isGenericFallback
                };

                // Add display message - use notification type message for known alarms,
                // or build a generic message for unknown alarms
                if (isGenericFallback)
                {
                    var rawRegisterCode = BuildRawRegisterCode(request.AlarmState);
                    additionalDataDict["displayMessage"] = $"Device issue detected (Code: {alarm.MessageKey ?? rawRegisterCode}). Please contact technical support for assistance.";
                    if (!string.IsNullOrEmpty(rawRegisterMessage))
                    {
                        additionalDataDict["rawRegisterMessage"] = rawRegisterMessage;
                    }
                }
                else
                {
                    // Use the notification type's description for known alarms
                    additionalDataDict["displayMessage"] = notificationType.Description;
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

                // Build detailed description with register values
                var detailedDescription = BuildDetailedDescription(
                    request.AlarmState,
                    alarm,
                    notificationType.Description,
                    isGenericFallback);

                processedNotifications.Add(new ProcessedNotificationInfo
                {
                    Id = deviceNotification.Id,
                    Key = notificationType.Code,
                    Message = notificationType.Message,
                    Severity = notificationType.Severity,
                    Description = detailedDescription
                });
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
            Notifications = processedNotifications,
            Errors = errors
        };
    }
}
