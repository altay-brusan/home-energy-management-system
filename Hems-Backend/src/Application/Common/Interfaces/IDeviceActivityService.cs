namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Telemetry source types for device activity tracking
/// </summary>
public enum TelemetrySource
{
    Heartbeat,
    PowerFlow,
    BatterySoc,
    BatteryEnergyFlow,
    DailyEnergyFlow,
    TotalEnergyFlow,
    Alarm,
    Unknown
}

/// <summary>
/// Result of recording device activity
/// </summary>
public class DeviceActivityResult
{
    /// <summary>
    /// Whether the activity was successfully recorded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The ThingsBoard device ID
    /// </summary>
    public Guid ThingsBoardDeviceId { get; set; }

    /// <summary>
    /// The local device ID (for frontend)
    /// </summary>
    public Guid? LocalDeviceId { get; set; }

    /// <summary>
    /// Whether the device status changed (e.g., recovery from DISCONNECTED)
    /// </summary>
    public bool StatusChanged { get; set; }

    /// <summary>
    /// The status change details (if any)
    /// </summary>
    public DeviceStatusChange? StatusChange { get; set; }

    /// <summary>
    /// Whether SignalR notification was sent
    /// </summary>
    public bool NotificationSent { get; set; }

    /// <summary>
    /// Error message if recording failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Centralized service for recording device activity from any telemetry source.
/// Updates heartbeat cache and sends SignalR notifications when device status changes.
/// </summary>
public interface IDeviceActivityService
{
    /// <summary>
    /// Records device activity from any telemetry source.
    /// This method:
    /// 1. Updates the heartbeat cache with the latest activity timestamp
    /// 2. Resets missed heartbeat counter (device is proven online)
    /// 3. Sends SignalR notification if device recovered from offline state
    /// </summary>
    /// <param name="thingsBoardDeviceId">The ThingsBoard device ID</param>
    /// <param name="source">The telemetry source type</param>
    /// <param name="timestamp">Optional timestamp from device (defaults to UTC now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing status change info and whether notification was sent</returns>
    Task<DeviceActivityResult> RecordActivityAsync(
        Guid thingsBoardDeviceId,
        TelemetrySource source,
        DateTime? timestamp = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records device activity and parses the device ID from string format.
    /// Convenience method for endpoints that receive device ID as string.
    /// </summary>
    /// <param name="deviceIdString">The ThingsBoard device ID as string</param>
    /// <param name="source">The telemetry source type</param>
    /// <param name="timestamp">Optional timestamp from device</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing status change info, or error if ID parsing failed</returns>
    Task<DeviceActivityResult> RecordActivityAsync(
        string deviceIdString,
        TelemetrySource source,
        DateTime? timestamp = null,
        CancellationToken cancellationToken = default);
}
