using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

public class DeviceNotification : BaseGuidAuditableEntity
{
    public Guid DeviceId { get; set; }
    public Guid SiteId { get; set; }
    public Guid SetupDeviceId { get; set; }
    public Guid DeviceNotificationTypeId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string AdditionalData { get; set; } = string.Empty;
    public bool IsProcessed { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public Device Device { get; set; } = null!;
    public Site Site { get; set; } = null!;
    public SetupDevice SetupDevice { get; set; } = null!;
    public DeviceNotificationType DeviceNotificationType { get; set; } = null!;
}
