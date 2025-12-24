namespace Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;

public class InverterAlarmStateDto
{
    public string TimestampUtc { get; set; } = string.Empty;
    public string GatewayId { get; set; } = string.Empty;
    public string InverterSerialNumber { get; set; } = string.Empty;

    public int RunStateRaw { get; set; }
    public int WarningWord1Raw { get; set; }
    public int WarningWord2Raw { get; set; }
    public int FaultWord1Raw { get; set; }
    public int FaultWord2Raw { get; set; }
    public int FaultWord3Raw { get; set; }
    public int FaultWord4Raw { get; set; }

    public List<AlarmDto> ActiveAlarms { get; set; } = new();
}
