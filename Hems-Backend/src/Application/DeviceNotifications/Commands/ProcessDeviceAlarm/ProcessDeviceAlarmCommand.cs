using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using Hems_Backend.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;

public record ProcessDeviceAlarmCommand : IRequest<ProcessDeviceAlarmResult>
{
    public Guid DeviceId { get; init; }
    public InverterAlarmStateDto AlarmState { get; init; } = null!;

    /// <summary>
    /// Original raw JSON payload for storage in AdditionalData
    /// </summary>
    public string? RawJson { get; init; }
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
    /// The notification type code (e.g., W2, W31, F1, F44).
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// The human-readable message for this notification type.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// The severity level: Warning or Error.
    /// </summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description including troubleshooting steps.
    /// </summary>
    public string Description { get; init; } = string.Empty;
}

public class ProcessDeviceAlarmCommandHandler : IRequestHandler<ProcessDeviceAlarmCommand, ProcessDeviceAlarmResult>
{
    private readonly IApplicationDbContext _context;

    // Warning mappings: W1-W32 spread across 2 words (16 bits each)
    // warningWord1Raw: W1-W16 (bits 0-15) - bit N maps to W(N+1)
    // warningWord2Raw: W17-W32 (bits 0-15) - bit N maps to W(17+N)
    //
    // Fault mappings: F1-F64 spread across 4 words (16 bits each)
    // faultWord1Raw: F1-F16 (bits 0-15)
    // faultWord2Raw: F17-F32 (bits 0-15)
    // faultWord3Raw: F33-F48 (bits 0-15)
    // faultWord4Raw: F49-F64 (bits 0-15)
    //
    // CommBoard fault mappings: F65-F67
    // commBoardFaultRaw: Bit0=F65 (Flash chip error), Bit1=F66 (Time error), Bit2=F67 (EEPROM error)

    public ProcessDeviceAlarmCommandHandler(IApplicationDbContext context)
    {
        _context = context;
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

        // Determine device category from Kind field
        // TODO: In the future, this should also consider device type and version for more specific handling
        var category = DetermineCategory(request.AlarmState.Kind);

        // Extract notification codes from raw register values
        var extractedCodes = ExtractNotificationCodes(request.AlarmState);

        if (!extractedCodes.Any())
        {
            // No warnings or faults detected
            return new ProcessDeviceAlarmResult
            {
                ProcessedAlarmsCount = 0,
                CreatedNotificationIds = createdNotificationIds,
                Notifications = processedNotifications,
                Errors = errors
            };
        }

        // Load all active notification types for the determined category
        var notificationTypes = await _context.DeviceNotificationTypes
            .Where(nt => nt.IsActive && nt.Category == category)
            .ToListAsync(cancellationToken);

        // Also load generic fallback types
        var genericTypes = await _context.DeviceNotificationTypes
            .Where(nt => nt.IsActive && nt.Manufacturer == "Generic" && nt.Category == category)
            .ToListAsync(cancellationToken);

        // Process each extracted code
        foreach (var codeInfo in extractedCodes)
        {
            try
            {
                // Look up notification type by Code
                var notificationType = notificationTypes.FirstOrDefault(nt => nt.Code == codeInfo.Code);
                var isGenericFallback = false;

                // Fallback to generic type if specific code not found
                if (notificationType == null)
                {
                    notificationType = GetGenericFallbackType(genericTypes, codeInfo.Severity, category);
                    isGenericFallback = true;

                    if (notificationType == null)
                    {
                        errors.Add($"Notification type not found for code '{codeInfo.Code}' (severity: {codeInfo.Severity}, category: {category}). No generic fallback available.");
                        continue;
                    }
                }

                // Check for duplicate notifications (within last 5 minutes)
                var existingNotification = await _context.DeviceNotifications
                    .Where(dn => dn.DeviceId == request.DeviceId &&
                                 dn.DeviceNotificationTypeId == notificationType.Id &&
                                 !dn.IsProcessed &&
                                 dn.Timestamp >= timestamp.AddMinutes(-5))
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingNotification != null)
                {
                    // Update existing notification timestamp
                    existingNotification.Timestamp = timestamp;
                    continue;
                }

                // Build AdditionalData with raw JSON and extracted info
                var additionalDataDict = BuildAdditionalData(
                    request.AlarmState,
                    request.RawJson,
                    codeInfo,
                    notificationType,
                    isGenericFallback);

                // Create new notification
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

                processedNotifications.Add(new ProcessedNotificationInfo
                {
                    Id = deviceNotification.Id,
                    Key = notificationType.Code,
                    Message = notificationType.Message,
                    Severity = notificationType.Severity,
                    Description = isGenericFallback
                        ? $"{notificationType.Description} (Code: {codeInfo.Code})"
                        : notificationType.Description
                });
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing notification code {codeInfo.Code}: {ex.Message}");
            }
        }

        if (createdNotificationIds.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ProcessDeviceAlarmResult
        {
            ProcessedAlarmsCount = extractedCodes.Count,
            CreatedNotificationIds = createdNotificationIds,
            Notifications = processedNotifications,
            Errors = errors
        };
    }

    /// <summary>
    /// Determines the notification category based on the Kind field.
    /// Maps "InverterAlarmState" to "Inverter" and "BatteryAlarmState" to "Battery".
    /// </summary>
    private static string DetermineCategory(string kind)
    {
        return kind?.ToUpperInvariant() switch
        {
            "INVERTERALARMSTATE" => nameof(DeviceType.Invertor).Replace("Invertor", "Inverter"),
            "BATTERYALARMSTATE" => nameof(DeviceType.Battery),
            "BMSALARMSTATE" => nameof(DeviceType.Battery),
            _ => "Inverter" // Default to Inverter for backward compatibility
        };
    }

    /// <summary>
    /// Extracts warning and fault codes from raw register values.
    /// </summary>
    private static List<ExtractedCodeInfo> ExtractNotificationCodes(DeviceAlarmStateDto alarmState)
    {
        var codes = new List<ExtractedCodeInfo>();

        // Extract warnings from warningWord1Raw (W1-W16)
        // Bit N maps to W(N+1): bit0=W1, bit1=W2, ..., bit15=W16
        ExtractWarningCodes(alarmState.WarningWord1Raw, 1, codes);

        // Extract warnings from warningWord2Raw (W17-W32)
        // Bit N maps to W(17+N): bit0=W17, bit1=W18, ..., bit15=W32
        ExtractWarningCodes(alarmState.WarningWord2Raw, 17, codes);

        // Extract faults from faultWord1Raw (F1-F16)
        ExtractFaultCodes(alarmState.FaultWord1Raw, 1, codes);

        // Extract faults from faultWord2Raw (F17-F32)
        ExtractFaultCodes(alarmState.FaultWord2Raw, 17, codes);

        // Extract faults from faultWord3Raw (F33-F48)
        ExtractFaultCodes(alarmState.FaultWord3Raw, 33, codes);

        // Extract faults from faultWord4Raw (F49-F64)
        ExtractFaultCodes(alarmState.FaultWord4Raw, 49, codes);

        // Extract comm board faults (F65-F67)
        // Bit0=F65 (Flash chip error), Bit1=F66 (Time error), Bit2=F67 (EEPROM error)
        ExtractCommBoardFaultCodes(alarmState.CommBoardFaultRaw, codes);

        return codes;
    }

    /// <summary>
    /// Extracts warning codes from a warning register.
    /// Each bit position maps to a warning code (W1-W32).
    /// </summary>
    private static void ExtractWarningCodes(int registerValue, int baseCode, List<ExtractedCodeInfo> codes)
    {
        if (registerValue == 0) return;

        for (int bit = 0; bit < 16; bit++)
        {
            var bitMask = 1 << bit;
            if ((registerValue & bitMask) != 0)
            {
                var warningNumber = baseCode + bit;
                codes.Add(new ExtractedCodeInfo
                {
                    Code = $"W{warningNumber}",
                    Severity = "Warning",
                    BitPosition = bit,
                    RegisterValue = registerValue
                });
            }
        }
    }

    /// <summary>
    /// Extracts fault codes from a fault register.
    /// Each bit position maps to a fault code (F1-F64).
    /// </summary>
    private static void ExtractFaultCodes(int registerValue, int baseCode, List<ExtractedCodeInfo> codes)
    {
        if (registerValue == 0) return;

        for (int bit = 0; bit < 16; bit++)
        {
            var bitMask = 1 << bit;
            if ((registerValue & bitMask) != 0)
            {
                var faultNumber = baseCode + bit;
                codes.Add(new ExtractedCodeInfo
                {
                    Code = $"F{faultNumber}",
                    Severity = "Error", // All faults map to Error severity
                    BitPosition = bit,
                    RegisterValue = registerValue
                });
            }
        }
    }

    /// <summary>
    /// Extracts comm board fault codes from the commBoardFaultRaw register.
    /// Bit0=F65 (Flash chip error), Bit1=F66 (Time error), Bit2=F67 (EEPROM error)
    /// </summary>
    private static void ExtractCommBoardFaultCodes(int registerValue, List<ExtractedCodeInfo> codes)
    {
        if (registerValue == 0) return;

        // F65: Bit0 - Flash chip error
        if ((registerValue & 0x01) != 0)
        {
            codes.Add(new ExtractedCodeInfo
            {
                Code = "F65",
                Severity = "Error",
                BitPosition = 0,
                RegisterValue = registerValue
            });
        }

        // F66: Bit1 - Time error
        if ((registerValue & 0x02) != 0)
        {
            codes.Add(new ExtractedCodeInfo
            {
                Code = "F66",
                Severity = "Error",
                BitPosition = 1,
                RegisterValue = registerValue
            });
        }

        // F67: Bit2 - EEPROM error
        if ((registerValue & 0x04) != 0)
        {
            codes.Add(new ExtractedCodeInfo
            {
                Code = "F67",
                Severity = "Error",
                BitPosition = 2,
                RegisterValue = registerValue
            });
        }
    }

    /// <summary>
    /// Gets the appropriate generic fallback notification type.
    /// </summary>
    private static DeviceNotificationType? GetGenericFallbackType(
        List<DeviceNotificationType> genericTypes,
        string severity,
        string category)
    {
        var prefix = category switch
        {
            "Inverter" => "INV",
            "Battery" => "BMS",
            _ => "INV"
        };

        var severityCode = severity?.ToUpperInvariant() switch
        {
            "WARNING" => "WAR",
            "ERROR" => "ERR",
            "ALARM" => "ALM",
            _ => "ERR"
        };

        var genericCode = $"{prefix}-{severityCode}-GENERIC";
        return genericTypes.FirstOrDefault(nt => nt.Code == genericCode);
    }

    /// <summary>
    /// Builds the AdditionalData dictionary for storage.
    /// </summary>
    private static Dictionary<string, object> BuildAdditionalData(
        DeviceAlarmStateDto alarmState,
        string? rawJson,
        ExtractedCodeInfo codeInfo,
        DeviceNotificationType notificationType,
        bool isGenericFallback)
    {
        var additionalData = new Dictionary<string, object>
        {
            ["kind"] = alarmState.Kind,
            ["extractedCode"] = codeInfo.Code,
            ["severity"] = codeInfo.Severity,
            ["bitPosition"] = codeInfo.BitPosition,
            ["registerValue"] = codeInfo.RegisterValue,
            ["isGenericFallback"] = isGenericFallback,
            ["notificationTypeCode"] = notificationType.Code,
            ["gatewayId"] = alarmState.GatewayId ?? string.Empty,
            ["serialNumber"] = alarmState.SerialNumber ?? string.Empty,
            ["version"] = alarmState.Version,
            // Raw register values for diagnostic purposes
            ["registers"] = new Dictionary<string, int>
            {
                ["runStateRaw"] = alarmState.RunStateRaw,
                ["commBoardFaultRaw"] = alarmState.CommBoardFaultRaw,
                ["warningWord1Raw"] = alarmState.WarningWord1Raw,
                ["warningWord2Raw"] = alarmState.WarningWord2Raw,
                ["faultWord1Raw"] = alarmState.FaultWord1Raw,
                ["faultWord2Raw"] = alarmState.FaultWord2Raw,
                ["faultWord3Raw"] = alarmState.FaultWord3Raw,
                ["faultWord4Raw"] = alarmState.FaultWord4Raw,
                ["workFlagTripData"] = alarmState.WorkFlagTripData
            }
        };

        // Include raw JSON payload if provided
        if (!string.IsNullOrEmpty(rawJson))
        {
            additionalData["rawPayload"] = rawJson;
        }

        return additionalData;
    }
}

/// <summary>
/// Internal record for tracking extracted notification codes.
/// </summary>
internal record ExtractedCodeInfo
{
    public string Code { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public int BitPosition { get; init; }
    public int RegisterValue { get; init; }
}
