namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Cache service for tracking device heartbeat status
/// </summary>
public interface IDeviceHeartbeatCache
{
    /// <summary>
    /// Mark device as alive when powerflow data is received
    /// </summary>
    Task MarkDeviceAliveAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active (non-deleted) device IDs from database and populate cache
    /// </summary>
    Task InitializeCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get devices that haven't sent powerflow data in the last 5+ minutes
    /// </summary>
    Task<List<Guid>> GetInactiveDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update heartbeat attempt count for a device
    /// </summary>
    Task UpdateHeartbeatAttemptAsync(Guid thingsBoardDeviceId, int attemptCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark device as offline after failed heartbeat attempts
    /// </summary>
    Task MarkDeviceOfflineAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get device heartbeat status
    /// </summary>
    DeviceHeartbeatStatus? GetDeviceStatus(Guid thingsBoardDeviceId);

    /// <summary>
    /// Add a newly created device to the heartbeat cache
    /// </summary>
    Task AddDeviceToCacheAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Device heartbeat status information
/// </summary>
public class DeviceHeartbeatStatus
{
    public Guid ThingsBoardDeviceId { get; set; }
    public DateTime LastSeen { get; set; }
    public bool IsOnline { get; set; }
    public int HeartbeatAttempts { get; set; }
    public DateTime? LastHeartbeatAttempt { get; set; }
}
