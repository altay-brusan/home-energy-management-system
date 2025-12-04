using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// In-memory cache service for tracking device heartbeat status
/// </summary>
public class DeviceHeartbeatCache : IDeviceHeartbeatCache
{
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DeviceHeartbeatCache> _logger;
    private const string CacheKeyPrefix = "DeviceHeartbeat_";
    private const string AllDevicesKey = "AllActiveDevices";
    private const int InactiveThresholdMinutes = 5;

    public DeviceHeartbeatCache(
        IMemoryCache cache,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DeviceHeartbeatCache> logger)
    {
        _cache = cache;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task InitializeCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            // Get all active (non-deleted) devices from database
            var activeDevices = await context.Devices
                .Where(d => !d.IsDeleted)
                .Select(d => d.ThingsBoardDeviceId)
                .ToListAsync(cancellationToken);

            // Store the list of active devices (filter out any nulls just in case)
            var validDevices = activeDevices.Where(id => id != Guid.Empty).ToList();
            _cache.Set(AllDevicesKey, validDevices, TimeSpan.FromHours(24));

            // Initialize status for each device
            foreach (var deviceId in validDevices)
            {
                var cacheKey = $"{CacheKeyPrefix}{deviceId}";
                if (!_cache.TryGetValue<DeviceHeartbeatStatus>(cacheKey, out _))
                {
                    var status = new DeviceHeartbeatStatus
                    {
                        ThingsBoardDeviceId = deviceId,
                        LastSeen = DateTime.UtcNow,
                        IsOnline = true,
                        HeartbeatAttempts = 0
                    };
                    _cache.Set(cacheKey, status, TimeSpan.FromDays(1));
                }
            }

            _logger.LogInformation("Device heartbeat cache initialized with {Count} devices", activeDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing device heartbeat cache");
            throw;
        }
    }

    public Task MarkDeviceAliveAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{thingsBoardDeviceId}";

        var status = _cache.Get<DeviceHeartbeatStatus>(cacheKey);
        if (status == null)
        {
            status = new DeviceHeartbeatStatus
            {
                ThingsBoardDeviceId = thingsBoardDeviceId,
                LastSeen = DateTime.UtcNow,
                IsOnline = true,
                HeartbeatAttempts = 0
            };
        }
        else
        {
            status.LastSeen = DateTime.UtcNow;
            status.IsOnline = true;
            status.HeartbeatAttempts = 0;
            status.LastHeartbeatAttempt = null;
        }

        _cache.Set(cacheKey, status, TimeSpan.FromDays(1));
        _logger.LogDebug("Device {DeviceId} marked as alive", thingsBoardDeviceId);

        return Task.CompletedTask;
    }

    public Task<List<Guid>> GetInactiveDevicesAsync(CancellationToken cancellationToken = default)
    {
        var inactiveDevices = new List<Guid>();
        var activeDevices = _cache.Get<List<Guid>>(AllDevicesKey);

        if (activeDevices == null)
        {
            _logger.LogWarning("Active devices list not found in cache");
            return Task.FromResult(inactiveDevices);
        }

        var now = DateTime.UtcNow;
        var threshold = now.AddMinutes(-InactiveThresholdMinutes);

        foreach (var deviceId in activeDevices)
        {
            var cacheKey = $"{CacheKeyPrefix}{deviceId}";
            var status = _cache.Get<DeviceHeartbeatStatus>(cacheKey);

            if (status != null && status.LastSeen < threshold && status.IsOnline)
            {
                inactiveDevices.Add(deviceId);
            }
        }

        _logger.LogInformation("Found {Count} inactive devices", inactiveDevices.Count);
        return Task.FromResult(inactiveDevices);
    }

    public Task UpdateHeartbeatAttemptAsync(Guid thingsBoardDeviceId, int attemptCount, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{thingsBoardDeviceId}";
        var status = _cache.Get<DeviceHeartbeatStatus>(cacheKey);

        if (status != null)
        {
            status.HeartbeatAttempts = attemptCount;
            status.LastHeartbeatAttempt = DateTime.UtcNow;
            _cache.Set(cacheKey, status, TimeSpan.FromDays(1));

            _logger.LogInformation("Device {DeviceId} heartbeat attempt {Attempt}", thingsBoardDeviceId, attemptCount);
        }

        return Task.CompletedTask;
    }

    public Task MarkDeviceOfflineAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{thingsBoardDeviceId}";
        var status = _cache.Get<DeviceHeartbeatStatus>(cacheKey);

        if (status != null)
        {
            status.IsOnline = false;
            _cache.Set(cacheKey, status, TimeSpan.FromDays(1));

            _logger.LogWarning("Device {DeviceId} marked as offline", thingsBoardDeviceId);
        }

        return Task.CompletedTask;
    }

    public DeviceHeartbeatStatus? GetDeviceStatus(Guid thingsBoardDeviceId)
    {
        var cacheKey = $"{CacheKeyPrefix}{thingsBoardDeviceId}";
        return _cache.Get<DeviceHeartbeatStatus>(cacheKey);
    }

    public Task AddDeviceToCacheAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default)
    {
        if (thingsBoardDeviceId == Guid.Empty)
        {
            _logger.LogWarning("Attempted to add empty device ID to cache");
            return Task.CompletedTask;
        }

        // Add to the list of all active devices
        var activeDevices = _cache.Get<List<Guid>>(AllDevicesKey) ?? new List<Guid>();
        if (!activeDevices.Contains(thingsBoardDeviceId))
        {
            activeDevices.Add(thingsBoardDeviceId);
            _cache.Set(AllDevicesKey, activeDevices, TimeSpan.FromHours(24));
        }

        // Initialize heartbeat status for the new device
        var cacheKey = $"{CacheKeyPrefix}{thingsBoardDeviceId}";
        var status = new DeviceHeartbeatStatus
        {
            ThingsBoardDeviceId = thingsBoardDeviceId,
            LastSeen = DateTime.UtcNow,
            IsOnline = true,
            HeartbeatAttempts = 0
        };
        _cache.Set(cacheKey, status, TimeSpan.FromDays(1));

        _logger.LogInformation("Added new device {DeviceId} to heartbeat cache", thingsBoardDeviceId);
        return Task.CompletedTask;
    }
}
