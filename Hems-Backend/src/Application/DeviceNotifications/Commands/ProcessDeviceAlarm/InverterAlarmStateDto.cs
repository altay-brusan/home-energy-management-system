namespace Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;

/// <summary>
/// DTO for device alarm state telemetry.
/// Supports both InverterAlarmState and BatteryAlarmState via the Kind property.
/// </summary>
public class DeviceAlarmStateDto
{
    /// <summary>
    /// The kind/type of alarm state: "InverterAlarmState" or "BatteryAlarmState".
    /// Used to determine the device category (Inverter vs Battery) when looking up notification types.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    public string TimestampUtc { get; set; } = string.Empty;
    public int Version { get; set; }
    public string GatewayId { get; set; } = string.Empty;

    /// <summary>
    /// Serial number - can be inverter or battery depending on Kind
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    // Inverter-specific raw registers
    public int RunStateRaw { get; set; }
    public int CommBoardFaultRaw { get; set; }
    public int WarningWord1Raw { get; set; }
    public int WarningWord2Raw { get; set; }
    public int FaultWord1Raw { get; set; }
    public int FaultWord2Raw { get; set; }
    public int FaultWord3Raw { get; set; }
    public int FaultWord4Raw { get; set; }
    public int WorkFlagTripData { get; set; }

    // Battery-specific raw registers (TODO: Add when battery alarm format is defined)
    // public int BatteryWarningRaw { get; set; }
    // public int BatteryFaultRaw { get; set; }

    public List<AlarmDto> ActiveAlarms { get; set; } = new();
}

// Keep backward compatibility alias
public class InverterAlarmStateDto : DeviceAlarmStateDto
{
    public string InverterSerialNumber
    {
        get => SerialNumber;
        set => SerialNumber = value;
    }
}
