using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for device-level locking to prevent concurrent updates.
/// Ensures only one settings-related operation (mode change or parameter update)
/// can run per device at a time.
/// </summary>
public class DeviceLockService : IDeviceLockService
{
    private readonly ILogger<DeviceLockService> _logger;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _deviceLocks = new();

    public DeviceLockService(ILogger<DeviceLockService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates a semaphore for a specific device
    /// </summary>
    private SemaphoreSlim GetDeviceLock(Guid deviceId)
    {
        return _deviceLocks.GetOrAdd(deviceId, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// Acquires a lock for the specified device
    /// </summary>
    public async Task<IAsyncDisposable?> AcquireLockAsync(Guid deviceId, int timeoutMs = 120000, CancellationToken cancellationToken = default)
    {
        var semaphore = GetDeviceLock(deviceId);

        _logger.LogDebug("Attempting to acquire lock for device {DeviceId}", deviceId);

        if (await semaphore.WaitAsync(timeoutMs, cancellationToken))
        {
            _logger.LogDebug("Acquired lock for device {DeviceId}", deviceId);
            return new DeviceLockHandle(deviceId, semaphore, _logger);
        }

        _logger.LogWarning(
            "Failed to acquire lock for device {DeviceId} within {Timeout}ms - another operation is in progress",
            deviceId, timeoutMs);

        return null;
    }

    /// <summary>
    /// Checks if a device currently has an active lock
    /// </summary>
    public bool IsDeviceLocked(Guid deviceId)
    {
        if (_deviceLocks.TryGetValue(deviceId, out var semaphore))
        {
            return semaphore.CurrentCount == 0;
        }
        return false;
    }

    /// <summary>
    /// Lock handle that releases the semaphore when disposed
    /// </summary>
    private class DeviceLockHandle : IDeviceLockHandle
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger _logger;
        private bool _disposed;

        public Guid DeviceId { get; }

        public DeviceLockHandle(Guid deviceId, SemaphoreSlim semaphore, ILogger logger)
        {
            DeviceId = deviceId;
            _semaphore = semaphore;
            _logger = logger;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _semaphore.Release();
                _logger.LogDebug("Released lock for device {DeviceId}", DeviceId);
            }
            return ValueTask.CompletedTask;
        }
    }
}
