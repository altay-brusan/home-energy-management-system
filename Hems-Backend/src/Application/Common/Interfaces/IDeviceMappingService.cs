namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for mapping between local DeviceId and ThingsBoardDeviceId
/// Uses caching to reduce database load
/// </summary>
public interface IDeviceMappingService
{
    /// <summary>
    /// Get ThingsBoardDeviceId from local DeviceId
    /// </summary>
    Task<Guid?> GetThingsBoardDeviceIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get local DeviceId from ThingsBoardDeviceId
    /// </summary>
    Task<Guid?> GetDeviceIdAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidate cache for a specific device
    /// </summary>
    Task InvalidateCacheAsync(Guid deviceId);
    
    /// <summary>
    /// Clear all device mapping cache
    /// </summary>
    Task ClearCacheAsync();
}
