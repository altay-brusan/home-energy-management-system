using Hems_Backend.Domain.Common;

namespace Hems_Backend.Domain.Entities;

/// <summary>
/// Logs periods when a device was disconnected (missed heartbeats)
/// </summary>
public class DeviceDisconnectLog : BaseGuidEntity
{
    /// <summary>
    /// ThingsBoard Device ID (not local Device.Id)
    /// </summary>
    public Guid ThingsBoardDeviceId { get; set; }
    
    /// <summary>
    /// When the disconnect period started (first missed heartbeat)
    /// </summary>
    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// When the device recovered (null if still disconnected)
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// How long the device was disconnected (calculated on recovery)
    /// </summary>
    public TimeSpan? Duration { get; set; }
    
    /// <summary>
    /// Number of consecutive missed heartbeats before marking as disconnected
    /// </summary>
    public int MissedHeartbeats { get; set; }
    
    /// <summary>
    /// How the device recovered: "Heartbeat" or "PowerFlow"
    /// </summary>
    public string? RecoveryType { get; set; }
}
