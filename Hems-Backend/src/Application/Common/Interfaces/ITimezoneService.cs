namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service for handling timezone conversions for devices
/// Caches device timezone information for performance
/// </summary>
public interface ITimezoneService
{
    /// <summary>
    /// Gets the timezone for a device by its ID
    /// Returns the IANA timezone identifier (e.g., "Europe/Istanbul")
    /// </summary>
    /// <param name="deviceId">Local device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TimeZoneInfo for the device's site timezone, or UTC if not found</returns>
    Task<TimeZoneInfo> GetDeviceTimezoneAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a UTC DateTime to the device's local timezone
    /// </summary>
    /// <param name="deviceId">Local device ID</param>
    /// <param name="utcDateTime">UTC DateTime to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DateTime in device's local timezone</returns>
    Task<DateTime> ConvertUtcToDeviceTimeAsync(Guid deviceId, DateTime utcDateTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a device's local DateTime to UTC
    /// </summary>
    /// <param name="deviceId">Local device ID</param>
    /// <param name="localDateTime">Local DateTime to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DateTime in UTC</returns>
    Task<DateTime> ConvertDeviceTimeToUtcAsync(Guid deviceId, DateTime localDateTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the timezone cache for a specific device
    /// </summary>
    /// <param name="deviceId">Local device ID</param>
    Task InvalidateCacheAsync(Guid deviceId);
}
