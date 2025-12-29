using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Tracks the history of device operating modes.
/// Each record represents a mode change for a device.
/// Only one record should be active (IsActive = true) per device at any time.
/// </summary>
public class DeviceModeHistory : BaseGuidAuditableEntity
{
    /// <summary>
    /// The device ID this mode record belongs to
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// The device mode value (matches DeviceMode enum values)
    /// 1 = SellToGrid, 2 = SelfConsumption, 3 = SmartZeroExport, 99 = Custom
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Human-readable description of the mode
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this mode record is currently active.
    /// Only one record per device should be active at any time.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation property
    public Device Device { get; set; } = null!;
}
