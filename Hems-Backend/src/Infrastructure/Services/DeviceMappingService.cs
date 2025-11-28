using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Device mapping service with in-memory caching
/// Maps between local DeviceId and ThingsBoardDeviceId
/// </summary>
public class DeviceMappingService : IDeviceMappingService
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "DeviceMapping_";
    private const string ReverseCacheKeyPrefix = "DeviceMappingReverse_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public DeviceMappingService(IApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Guid?> GetThingsBoardDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{deviceId}";
        
        // Try to get from cache
        if (_cache.TryGetValue<Guid?>(cacheKey, out var cachedThingsBoardId))
        {
            return cachedThingsBoardId;
        }

        // Query from database
        var device = await _context.Devices
            .Where(d => d.Id == deviceId && !d.IsDeleted)
            .Select(d => new { d.Id, d.ThingsBoardDeviceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (device?.ThingsBoardDeviceId == null)
        {
            return null;
        }

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration,
            SlidingExpiration = TimeSpan.FromHours(6)
        };

        _cache.Set(cacheKey, device.ThingsBoardDeviceId, cacheOptions);
        
        // Also cache the reverse mapping
        var reverseCacheKey = $"{ReverseCacheKeyPrefix}{device.ThingsBoardDeviceId}";
        _cache.Set(reverseCacheKey, device.Id, cacheOptions);

        return device.ThingsBoardDeviceId;
    }

    public async Task<Guid?> GetDeviceIdAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ReverseCacheKeyPrefix}{thingsBoardDeviceId}";
        
        // Try to get from cache
        if (_cache.TryGetValue<Guid?>(cacheKey, out var cachedDeviceId))
        {
            return cachedDeviceId;
        }

        // Query from database
        var device = await _context.Devices
            .Where(d => d.ThingsBoardDeviceId == thingsBoardDeviceId && !d.IsDeleted)
            .Select(d => new { d.Id, d.ThingsBoardDeviceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (device == null)
        {
            return null;
        }

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration,
            SlidingExpiration = TimeSpan.FromHours(6)
        };

        _cache.Set(cacheKey, device.Id, cacheOptions);
        
        // Also cache the forward mapping
        var forwardCacheKey = $"{CacheKeyPrefix}{device.Id}";
        _cache.Set(forwardCacheKey, device.ThingsBoardDeviceId, cacheOptions);

        return device.Id;
    }

    public Task InvalidateCacheAsync(Guid deviceId)
    {
        var cacheKey = $"{CacheKeyPrefix}{deviceId}";
        _cache.Remove(cacheKey);
        
        // Note: We can't easily remove the reverse mapping without knowing the ThingsBoardDeviceId
        // But that's okay - cache will expire naturally
        
        return Task.CompletedTask;
    }

    public Task ClearCacheAsync()
    {
        // Note: IMemoryCache doesn't have a clear all method
        // In production, consider using IDistributedCache (Redis) which supports pattern-based removal
        // For now, cache entries will expire based on their expiration settings
        
        return Task.CompletedTask;
    }
}
