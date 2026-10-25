using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceDetails;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Cache service for component details with 5-second TTL.
/// Reduces RPC calls when multiple users request the same component data.
///
/// Design:
/// - Each component's details are cached for 5 seconds
/// - After 5 seconds, the cache entry expires and next request triggers fresh RPC
/// - All users requesting within the 5-second window get the same cached data
/// - No subscription management needed - just simple time-based expiration
/// </summary>
public class ComponentDetailsCacheService : IComponentDetailsCacheService
{
    private readonly ConcurrentDictionary<Guid, CacheEntry> _cache = new();
    private readonly ILogger<ComponentDetailsCacheService> _logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);

    private int _cacheHits;
    private int _cacheMisses;

    public ComponentDetailsCacheService(ILogger<ComponentDetailsCacheService> logger)
    {
        _logger = logger;
    }

    public bool TryGetCachedDetails(Guid componentId, out DeviceDetailsResponseDto? details)
    {
        if (_cache.TryGetValue(componentId, out var entry))
        {
            // Check if entry is still valid (not expired)
            if (DateTime.UtcNow - entry.CachedAt < _cacheDuration)
            {
                Interlocked.Increment(ref _cacheHits);
                details = entry.Details;

                _logger.LogDebug(
                    "ComponentDetailsCache HIT for {ComponentId}, age={AgeMs}ms",
                    componentId,
                    (DateTime.UtcNow - entry.CachedAt).TotalMilliseconds);

                return true;
            }

            // Entry expired - remove it
            _cache.TryRemove(componentId, out _);
            _logger.LogDebug(
                "ComponentDetailsCache EXPIRED for {ComponentId}, age={AgeMs}ms",
                componentId,
                (DateTime.UtcNow - entry.CachedAt).TotalMilliseconds);
        }

        Interlocked.Increment(ref _cacheMisses);
        details = null;

        _logger.LogDebug("ComponentDetailsCache MISS for {ComponentId}", componentId);

        return false;
    }

    public void CacheDetails(Guid componentId, DeviceDetailsResponseDto details)
    {
        var entry = new CacheEntry
        {
            Details = details,
            CachedAt = DateTime.UtcNow
        };

        _cache.AddOrUpdate(componentId, entry, (_, _) => entry);

        _logger.LogDebug(
            "ComponentDetailsCache STORED for {ComponentId}, total entries={EntryCount}",
            componentId,
            _cache.Count);

        // Periodic cleanup of expired entries (every ~10 cache operations)
        if (_cache.Count > 10 && (_cacheHits + _cacheMisses) % 10 == 0)
        {
            CleanupExpiredEntries();
        }
    }

    public ComponentDetailsCacheStats GetCacheStats()
    {
        return new ComponentDetailsCacheStats
        {
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            CurrentEntries = _cache.Count
        };
    }

    private void CleanupExpiredEntries()
    {
        var expiredKeys = _cache
            .Where(kvp => DateTime.UtcNow - kvp.Value.CachedAt >= _cacheDuration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug(
                "ComponentDetailsCache cleanup removed {Count} expired entries",
                expiredKeys.Count);
        }
    }

    private class CacheEntry
    {
        public required DeviceDetailsResponseDto Details { get; init; }
        public required DateTime CachedAt { get; init; }
    }
}
