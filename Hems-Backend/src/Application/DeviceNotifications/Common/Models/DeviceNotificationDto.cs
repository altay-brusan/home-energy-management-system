namespace Hems_Backend.Application.DeviceNotifications.Common.Models;

public class DeviceNotificationTypeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Register { get; init; }
    public int Value { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public class DeviceNotificationDto
{
    public Guid Id { get; init; }
    public Guid DeviceId { get; init; }
    public Guid SiteId { get; init; }
    public Guid SetupDeviceId { get; init; }
    public Guid DeviceNotificationTypeId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string AdditionalData { get; init; } = string.Empty;
    public bool IsProcessed { get; init; }
    public bool IsDeleted { get; init; }
    public DateTimeOffset Created { get; init; }
    
    // Navigation properties
    public DeviceNotificationTypeDto DeviceNotificationType { get; init; } = null!;
    public string DeviceManufacturer { get; init; } = string.Empty;
    public string SiteName { get; init; } = string.Empty;
}
