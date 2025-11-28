namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Service to detect and prevent duplicate telemetry data based on timestamp and device ID.
/// Telemetry with the same data type from the same device within 4 seconds is considered a duplicate.
/// </summary>
public interface ITelemetryDeduplicationService
{
    /// <summary>
    /// Checks if the telemetry should be processed or is a duplicate.
    /// </summary>
    /// <param name="deviceId">The ThingsBoard device ID</param>
    /// <param name="dataType">The telemetry data type (e.g., "BatterySoc", "PowerFlow")</param>
    /// <param name="timestamp">The timestamp of the telemetry data</param>
    /// <returns>True if this is new data and should be processed, False if it's a duplicate</returns>
    Task<bool> ShouldProcessTelemetryAsync(Guid deviceId, string dataType, DateTime timestamp);

    /// <summary>
    /// Clears all cached telemetry timestamps (for testing/maintenance)
    /// </summary>
    Task ClearCacheAsync();
}
