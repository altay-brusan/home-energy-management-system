namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Device connection status based on heartbeat monitoring
/// </summary>
public enum DeviceStatus
{
    /// <summary>
    /// Device is sending heartbeats on time
    /// </summary>
    OK,
    
    /// <summary>
    /// Missed 1 heartbeat - potential issue
    /// </summary>
    PENDING,
    
    /// <summary>
    /// Missed 2 heartbeats - likely problem
    /// </summary>
    WAITING,
    
    /// <summary>
    /// Missed 3+ heartbeats - disconnected
    /// </summary>
    DISCONNECTED,
    
    /// <summary>
    /// Device never sent heartbeat, not being tracked
    /// </summary>
    NOT_TRACKED
}

/// <summary>
/// Cache service for tracking device heartbeat status (in-memory, event-driven)
/// </summary>
public interface IDeviceHeartbeatCache
{
    /// <summary>
    /// Record heartbeat from device. Starts tracking if first heartbeat.
    /// Returns DeviceStatusChange if status changed (e.g., recovery from DISCONNECTED)
    /// </summary>
    Task<DeviceStatusChange?> RecordHeartbeatAsync(Guid thingsBoardDeviceId, DateTime timestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record PowerFlow data received. Resets missed heartbeat count.
    /// Returns DeviceStatusChange if status changed (e.g., recovery from DISCONNECTED)
    /// </summary>
    Task<DeviceStatusChange?> RecordPowerFlowAsync(Guid thingsBoardDeviceId, DateTime timestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current status of a device
    /// </summary>
    DeviceStatus GetDeviceStatus(Guid thingsBoardDeviceId);

    /// <summary>
    /// Get full monitoring state of a device (status, missedHeartbeats, timestamps)
    /// Returns null if device is not tracked
    /// </summary>
    DeviceMonitoringState? GetDeviceState(Guid thingsBoardDeviceId);

    /// <summary>
    /// Get all devices currently being tracked
    /// </summary>
    List<Guid> GetTrackedDevices();

    /// <summary>
    /// Check all tracked devices for missed heartbeats and update statuses
    /// Called by background service every minute
    /// </summary>
    Task<List<DeviceStatusChange>> CheckMissedHeartbeatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a newly created device to tracking (optional - device will auto-add on first heartbeat)
    /// </summary>
    Task AddDeviceToCacheAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize cache on startup - not needed for heartbeat-driven model
    /// </summary>
    Task InitializeCacheAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a device status change event
/// </summary>
public class DeviceStatusChange
{
    public Guid ThingsBoardDeviceId { get; set; }
    public DeviceStatus PreviousStatus { get; set; }
    public DeviceStatus NewStatus { get; set; }
    public int MissedHeartbeats { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Device monitoring state (in-memory only)
/// </summary>
public class DeviceMonitoringState
{
    public Guid ThingsBoardDeviceId { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime? LastPowerFlow { get; set; }
    public int MissedHeartbeats { get; set; }
    public DeviceStatus Status { get; set; }
    public Guid? DisconnectLogId { get; set; } // Reference to active disconnect log in DB
}
