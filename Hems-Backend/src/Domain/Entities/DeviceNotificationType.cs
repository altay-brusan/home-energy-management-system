using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class DeviceNotificationType : BaseGuidAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Register { get; set; }
    public int Value { get; set; }
    public string Category { get; set; } = string.Empty; // Device region/component (e.g., fan, thermal, battery, grid)
    public string Severity { get; set; } = string.Empty; // ALARM, WARNING, ERROR
    public string Version { get; set; } = string.Empty; // Notification type version
    public string Manufacturer { get; set; } = string.Empty; // Device manufacturer (e.g., LOFTTY)
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<DeviceNotification> DeviceNotifications { get; set; } = new List<DeviceNotification>();
}
