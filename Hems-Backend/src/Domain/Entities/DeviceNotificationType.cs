using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class DeviceNotificationType : BaseGuidAuditableEntity
{
    public string Code { get; set; } = string.Empty; // Vendor code (e.g., W2, F1, F64)
    public string Message { get; set; } = string.Empty; // Short message from vendor documentation
    public string Description { get; set; } = string.Empty; // Detailed troubleshooting steps
    public string Category { get; set; } = string.Empty; // Device type (e.g., Inverter, Battery)
    public string Severity { get; set; } = string.Empty; // Warning, Error
    public string Version { get; set; } = string.Empty; // Notification type version (e.g., 1.0)
    public string Manufacturer { get; set; } = string.Empty; // Device manufacturer (e.g., LOFTTY)
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<DeviceNotification> DeviceNotifications { get; set; } = new List<DeviceNotification>();
}
