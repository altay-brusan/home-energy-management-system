namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service interface for device-level locking to prevent concurrent updates.
/// Used by both SetDeviceModeCommand and parameter update operations to ensure
/// only one settings-related operation can run per device at a time.
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
