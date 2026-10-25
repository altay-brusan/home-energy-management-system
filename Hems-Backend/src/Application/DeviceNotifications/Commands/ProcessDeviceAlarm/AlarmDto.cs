namespace Hems_Backend.Application.DeviceNotifications.Commands.ProcessDeviceAlarm;

public class AlarmDto
{
    public int Code { get; set; }
    public int VendorCode { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string SourceDevice { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string MessageKey { get; set; } = string.Empty;
    public Dictionary<string, object> MessageParams { get; set; } = new();
    public string FirstOccurredUtc { get; set; } = string.Empty;
    public string LastOccurredUtc { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
}
