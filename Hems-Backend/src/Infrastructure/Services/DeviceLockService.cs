using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for device-level locking to prevent concurrent updates.
/// Ensures only one settings-related operation (mode change or parameter update)
/// can run per device at a time.
/// Also tracks device restart state to prevent operations during restart grace period.
/// </summary>
public class DeviceLockService : IDeviceLockService
{
    private readonly ILogger<DeviceLockService> _logger;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _deviceLocks = new();
    private readonly ConcurrentDictionary<Guid, RestartState> _restartingDevices = new();

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
    /// Marks a device as restarting for the specified duration.
    /// During this grace period, operations will be blocked with DEVICE_RESTARTING error.
    /// </summary>
    public void MarkDeviceRestarting(Guid deviceId, TimeSpan gracePeriod)
    {
        var restartState = new RestartState
        {
            StartedAt = DateTime.UtcNow,
            EstimatedAvailableAt = DateTime.UtcNow.Add(gracePeriod)
        };

        _restartingDevices.AddOrUpdate(deviceId, restartState, (_, _) => restartState);

        _logger.LogInformation(
            "🔄 Device {DeviceId} marked as restarting. Grace period: {GracePeriod}, Expected available at: {EstimatedAvailableAt}",
            deviceId, gracePeriod, restartState.EstimatedAvailableAt);
    }

    /// <summary>
    /// Checks if a device is currently in restart grace period.
    /// Automatically clears expired restart states.
    /// </summary>
    public bool IsDeviceRestarting(Guid deviceId)
    {
        if (_restartingDevices.TryGetValue(deviceId, out var state))
        {
            if (DateTime.UtcNow < state.EstimatedAvailableAt)
            {
                return true;
            }

            // Grace period expired, clean up
            _restartingDevices.TryRemove(deviceId, out _);
            _logger.LogDebug(
                "Restart grace period expired for device {DeviceId}, cleared restart state",
                deviceId);
        }
        return false;
    }

    /// <summary>
    /// Gets restart information for a device.
    /// </summary>
    public DeviceRestartInfo? GetRestartInfo(Guid deviceId)
    {
        if (_restartingDevices.TryGetValue(deviceId, out var state))
        {
            if (DateTime.UtcNow < state.EstimatedAvailableAt)
            {
                return new DeviceRestartInfo
                {
                    StartedAt = state.StartedAt,
                    EstimatedAvailableAt = state.EstimatedAvailableAt
                };
            }

            // Grace period expired, clean up
            _restartingDevices.TryRemove(deviceId, out _);
        }
        return null;
    }

    /// <summary>
    /// Clears the restart state for a device (e.g., when device comes back online).
    /// </summary>
    public void ClearRestartState(Guid deviceId)
    {
        if (_restartingDevices.TryRemove(deviceId, out _))
        {
            _logger.LogInformation(
                "✅ Restart state cleared for device {DeviceId}",
                deviceId);
        }
    }

    /// <summary>
    /// Internal class to track restart state
    /// </summary>
    private class RestartState
    {
        public DateTime StartedAt { get; init; }
        public DateTime EstimatedAvailableAt { get; init; }
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
