namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service interface for device-level locking to prevent concurrent updates.
/// Used by both SetDeviceModeCommand and parameter update operations to ensure
/// only one settings-related operation can run per device at a time.
/// Also tracks device restart state to prevent operations during restart grace period.
/// </summary>
public interface IDeviceLockService
{
    /// <summary>
    /// Acquires a lock for the specified device. Blocks until the lock is available
    /// or the timeout is reached.
    /// </summary>
    /// <param name="deviceId">Device ID to lock</param>
    /// <param name="timeoutMs">Timeout in milliseconds to wait for the lock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>IDisposable handle that releases the lock when disposed, or null if lock could not be acquired</returns>
    Task<IAsyncDisposable?> AcquireLockAsync(Guid deviceId, int timeoutMs = 120000, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a device currently has an active lock
    /// </summary>
    bool IsDeviceLocked(Guid deviceId);

    /// <summary>
    /// Marks a device as restarting for the specified duration.
    /// During this grace period, operations will be blocked with DEVICE_RESTARTING error.
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="gracePeriod">Duration of the grace period (typically 2-3 minutes)</param>
    void MarkDeviceRestarting(Guid deviceId, TimeSpan gracePeriod);

    /// <summary>
    /// Checks if a device is currently in restart grace period.
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <returns>True if device is restarting</returns>
    bool IsDeviceRestarting(Guid deviceId);

    /// <summary>
    /// Gets restart information for a device.
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Restart info with estimated available time, or null if not restarting</returns>
    DeviceRestartInfo? GetRestartInfo(Guid deviceId);

    /// <summary>
    /// Clears the restart state for a device (e.g., when device comes back online).
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    void ClearRestartState(Guid deviceId);
}

/// <summary>
/// Information about a device restart state
/// </summary>
public record DeviceRestartInfo
{
    /// <summary>
    /// When the restart was initiated
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Estimated time when the device will be available again
    /// </summary>
    public DateTime EstimatedAvailableAt { get; init; }

    /// <summary>
    /// Remaining time until the device is expected to be available
    /// </summary>
    public TimeSpan RemainingTime => EstimatedAvailableAt > DateTime.UtcNow
        ? EstimatedAvailableAt - DateTime.UtcNow
        : TimeSpan.Zero;
}

/// <summary>
/// Handle returned when a device lock is successfully acquired.
/// Dispose to release the lock.
/// </summary>
public interface IDeviceLockHandle : IAsyncDisposable
{
    /// <summary>
    /// The device ID that is locked
    /// </summary>
    Guid DeviceId { get; }
}
