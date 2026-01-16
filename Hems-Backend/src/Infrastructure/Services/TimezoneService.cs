using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// Service for handling timezone conversions with caching
/// Caches device timezone information for 24 hours to improve performance
/// </summary>
public class TimezoneService : ITimezoneService
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TimezoneService> _logger;
    private const string CacheKeyPrefix = "DeviceTimezone_";
    private const string SiteCacheKeyPrefix = "SiteTimezone_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public TimezoneService(
        IApplicationDbContext context,
        IMemoryCache cache,
        ILogger<TimezoneService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TimeZoneInfo> GetDeviceTimezoneAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{deviceId}";

        // Try to get from cache
        if (_cache.TryGetValue<TimeZoneInfo>(cacheKey, out var cachedTimezone) && cachedTimezone != null)
        {
            _logger.LogDebug("Timezone cache hit for device {DeviceId}", deviceId);
            return cachedTimezone;
        }

        _logger.LogDebug("Timezone cache miss for device {DeviceId}, querying database", deviceId);

        // Query from database: Device -> SetupDevice -> Setup -> Site -> Timezone
        var timezoneId = await _context.Devices
            .Where(d => d.Id == deviceId && !d.IsDeleted)
            .Select(d => d.SetupDevice!.Setup.Site.Timezone)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(timezoneId))
        {
            _logger.LogWarning("No timezone found for device {DeviceId}, using UTC", deviceId);
            return TimeZoneInfo.Utc;
        }

        TimeZoneInfo timezone;
        try
        {
            // .NET 6+ supports IANA timezone IDs on all platforms
            timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            _logger.LogInformation("Found timezone {TimezoneId} for device {DeviceId}", timezoneId, deviceId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogError("Timezone {TimezoneId} not found for device {DeviceId}, falling back to UTC", timezoneId, deviceId);
            timezone = TimeZoneInfo.Utc;
        }

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration,
            SlidingExpiration = TimeSpan.FromHours(6)
        };

        _cache.Set(cacheKey, timezone, cacheOptions);

        return timezone;
    }

    public async Task<TimeZoneInfo> GetSiteTimezoneAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{SiteCacheKeyPrefix}{siteId}";

        // Try to get from cache
        if (_cache.TryGetValue<TimeZoneInfo>(cacheKey, out var cachedTimezone) && cachedTimezone != null)
        {
            _logger.LogDebug("Timezone cache hit for site {SiteId}", siteId);
            return cachedTimezone;
        }

        _logger.LogDebug("Timezone cache miss for site {SiteId}, querying database", siteId);

        // Query from database: Site -> Timezone
        var timezoneId = await _context.Sites
            .Where(s => s.Id == siteId && !s.IsDeleted)
            .Select(s => s.Timezone)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(timezoneId))
        {
            _logger.LogWarning("No timezone found for site {SiteId}, using UTC", siteId);
            return TimeZoneInfo.Utc;
        }

        TimeZoneInfo timezone;
        try
        {
            // .NET 6+ supports IANA timezone IDs on all platforms
            timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            _logger.LogInformation("Found timezone {TimezoneId} for site {SiteId}", timezoneId, siteId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogError("Timezone {TimezoneId} not found for site {SiteId}, falling back to UTC", timezoneId, siteId);
            timezone = TimeZoneInfo.Utc;
        }

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration,
            SlidingExpiration = TimeSpan.FromHours(6)
        };

        _cache.Set(cacheKey, timezone, cacheOptions);

        return timezone;
    }

    public async Task<DateTime> ConvertUtcToDeviceTimeAsync(Guid deviceId, DateTime utcDateTime, CancellationToken cancellationToken = default)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            _logger.LogWarning("ConvertUtcToDeviceTimeAsync called with non-UTC datetime for device {DeviceId}", deviceId);
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        var timezone = await GetDeviceTimezoneAsync(deviceId, cancellationToken);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timezone);

        _logger.LogDebug(
            "Converted UTC {UtcTime} to device local time {LocalTime} for device {DeviceId} in timezone {TimezoneId}",
            utcDateTime, localTime, deviceId, timezone.Id);

        return localTime;
    }

    public async Task<DateTime> ConvertDeviceTimeToUtcAsync(Guid deviceId, DateTime localDateTime, CancellationToken cancellationToken = default)
    {
        var timezone = await GetDeviceTimezoneAsync(deviceId, cancellationToken);

        // Specify that the datetime is unspecified (local to the device timezone)
        if (localDateTime.Kind != DateTimeKind.Unspecified)
        {
            localDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        }

        var utcTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timezone);

        _logger.LogDebug(
            "Converted device local time {LocalTime} to UTC {UtcTime} for device {DeviceId} in timezone {TimezoneId}",
            localDateTime, utcTime, deviceId, timezone.Id);

        return utcTime;
    }

    public Task InvalidateCacheAsync(Guid deviceId)
    {
        var cacheKey = $"{CacheKeyPrefix}{deviceId}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Invalidated timezone cache for device {DeviceId}", deviceId);
        return Task.CompletedTask;
    }
}
