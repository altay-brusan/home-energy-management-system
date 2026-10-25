using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Detects duplicate telemetry data based on timestamp.
/// If the same device sends the same data type within 4 seconds, it's considered a duplicate.
/// </summary>
public class TelemetryDeduplicationService : ITelemetryDeduplicationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TelemetryDeduplicationService> _logger;
    private const int MinimumIntervalSeconds = 4;

    public TelemetryDeduplicationService(
        IMemoryCache cache,
        ILogger<TelemetryDeduplicationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<bool> ShouldProcessTelemetryAsync(Guid deviceId, string dataType, DateTime timestamp)
    {
        var cacheKey = GetCacheKey(deviceId, dataType);
        
        // Try to get the last timestamp for this device+dataType combination
        if (_cache.TryGetValue<DateTime>(cacheKey, out var lastTimestamp))
        {
            var timeDifference = Math.Abs((timestamp - lastTimestamp).TotalSeconds);
            
            if (timeDifference < MinimumIntervalSeconds)
            {
                _logger.LogWarning(
                    "🚫 Duplicate telemetry detected - DeviceId: {DeviceId}, DataType: {DataType}, " +
                    "Current: {CurrentTime}, Last: {LastTime}, Difference: {Difference:F2}s (minimum: {Minimum}s)",
                    deviceId, dataType, timestamp, lastTimestamp, timeDifference, MinimumIntervalSeconds);
                
                return Task.FromResult(false); // Duplicate - do not process
            }
        }

        // This is new data or enough time has passed - update cache and process
        var cacheOptions = new MemoryCacheEntryOptions
        {
            // Keep the timestamp cached for 1 hour (plenty of time for deduplication)
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            // Sliding expiration of 10 minutes (if device keeps sending data, keep the entry alive)
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };

        _cache.Set(cacheKey, timestamp, cacheOptions);
        
        _logger.LogDebug(
            "✅ New telemetry accepted - DeviceId: {DeviceId}, DataType: {DataType}, Timestamp: {Timestamp}",
            deviceId, dataType, timestamp);

        return Task.FromResult(true); // New data - process it
    }

    public Task ClearCacheAsync()
    {
        // Note: IMemoryCache doesn't have a built-in Clear method
        // In production, you might want to use IDistributedCache (Redis) which supports clearing
        // For now, cache entries will expire naturally based on their expiration settings
        _logger.LogInformation("Cache clear requested - entries will expire naturally based on TTL");
        return Task.CompletedTask;
    }

    private static string GetCacheKey(Guid deviceId, string dataType)
    {
        return $"TelemetryDedup_{deviceId}_{dataType}";
    }
}
