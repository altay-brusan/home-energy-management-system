using Hems_Backend.Application.DeviceComponents.Queries.GetDeviceDetails;

namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Cache service for component details to reduce RPC calls when multiple users
/// request the same component data. Each cache entry expires after 5 seconds.
/// </summary>
public interface IComponentDetailsCacheService
{
    /// <summary>
    /// Try to get cached component details.
    /// </summary>
    /// <param name="componentId">The component ID to look up</param>
    /// <param name="details">The cached details if found and not expired</param>
    /// <returns>True if cached data was found and is still valid</returns>
    bool TryGetCachedDetails(Guid componentId, out DeviceDetailsResponseDto? details);

    /// <summary>
    /// Cache component details for 5 seconds.
    /// </summary>
    /// <param name="componentId">The component ID</param>
    /// <param name="details">The details to cache</param>
    void CacheDetails(Guid componentId, DeviceDetailsResponseDto details);

    /// <summary>
    /// Get cache statistics for monitoring.
    /// </summary>
    ComponentDetailsCacheStats GetCacheStats();
}

/// <summary>
/// Cache statistics for monitoring.
/// </summary>
public record ComponentDetailsCacheStats
{
    public int CacheHits { get; init; }
    public int CacheMisses { get; init; }
    public int CurrentEntries { get; init; }
    public double HitRatio => CacheHits + CacheMisses > 0
        ? (double)CacheHits / (CacheHits + CacheMisses) * 100
        : 0;
}
