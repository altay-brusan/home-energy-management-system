namespace Hems_Backend.Application.Common.Interfaces;

/// <summary>
/// Grid connection status from phase voltage monitoring
/// </summary>
public enum GridConnectionStatus
{
    /// <summary>
    /// Grid is connected (any phase voltage > 150V)
    /// </summary>
    Connected,

    /// <summary>
    /// Grid is disconnected (all phase voltages <= 150V)
    /// </summary>
    Disconnected,

    /// <summary>
    /// Status is unknown (no data received yet)
    /// </summary>
    Unknown
}

/// <summary>
/// Tracks the current grid status state for a device
/// </summary>
public class GridStatusState
{
    /// <summary>
    /// ThingsBoard device ID
    /// </summary>
    public Guid ThingsBoardDeviceId { get; set; }

    /// <summary>
    /// Current grid connection status
    /// </summary>
    public GridConnectionStatus Status { get; set; } = GridConnectionStatus.Unknown;

    /// <summary>
    /// Timestamp of the last status message received
    /// </summary>
    public DateTime LastStatusMessageTime { get; set; }

    /// <summary>
    /// Timestamp when the current outage started (null if not in outage)
    /// </summary>
    public DateTime? OutageStartTime { get; set; }

    /// <summary>
    /// Last known Phase A voltage (V)
    /// </summary>
    public double? PhaseAVoltageV { get; set; }

    /// <summary>
    /// Last known Phase B voltage (V)
    /// </summary>
    public double? PhaseBVoltageV { get; set; }

    /// <summary>
    /// Last known Phase C voltage (V)
    /// </summary>
    public double? PhaseCVoltageV { get; set; }

    /// <summary>
    /// Last known gridConnected flag from device
    /// </summary>
    public bool? GridConnectedFlag { get; set; }
}

/// <summary>
/// Result of updating grid status for a device
/// </summary>
public class GridStatusUpdateResult
{
    /// <summary>
    /// Whether the update was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Previous grid connection status before this update
    /// </summary>
    public GridConnectionStatus PreviousStatus { get; set; }

    /// <summary>
    /// Current grid connection status after this update
    /// </summary>
    public GridConnectionStatus CurrentStatus { get; set; }

    /// <summary>
    /// Whether the status changed with this update
    /// </summary>
    public bool StatusChanged { get; set; }

    /// <summary>
    /// Whether a notification should be sent (only on state transitions)
    /// </summary>
    public bool ShouldSendNotification { get; set; }

    /// <summary>
    /// Notification code to use if ShouldSendNotification is true
    /// GRD-WAR-W33 for outage, GRD-WAR-W34 for restoration
    /// </summary>
    public string? NotificationCode { get; set; }

    /// <summary>
    /// Duration of the outage if grid was just restored
    /// </summary>
    public TimeSpan? OutageDuration { get; set; }

    /// <summary>
    /// Error message if Success is false
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Service for tracking grid connection status per device.
/// Uses in-memory state tracking with ConcurrentDictionary for thread-safety.
/// Notifications are only sent on state transitions (Connected -> Disconnected or vice versa).
/// </summary>
public interface IGridStatusTrackingService
{
    /// <summary>
    /// Updates the grid status for a device based on telemetry data.
    /// Returns whether a notification should be sent (only on state transitions).
    /// </summary>
    /// <param name="thingsBoardDeviceId">ThingsBoard device ID</param>
    /// <param name="gridConnected">Grid connected flag from device</param>
    /// <param name="phaseAVoltageV">Phase A voltage in volts (optional)</param>
    /// <param name="phaseBVoltageV">Phase B voltage in volts (optional)</param>
    /// <param name="phaseCVoltageV">Phase C voltage in volts (optional)</param>
    /// <param name="timestampUtc">Timestamp of the telemetry message</param>
    /// <returns>Result indicating whether a notification should be sent</returns>
    GridStatusUpdateResult UpdateGridStatus(
        Guid thingsBoardDeviceId,
        bool gridConnected,
        double? phaseAVoltageV = null,
        double? phaseBVoltageV = null,
        double? phaseCVoltageV = null,
        DateTime? timestampUtc = null);

    /// <summary>
    /// Gets the current grid status state for a device
    /// </summary>
    /// <param name="thingsBoardDeviceId">ThingsBoard device ID</param>
    /// <returns>Current state or null if device not tracked</returns>
    GridStatusState? GetGridStatus(Guid thingsBoardDeviceId);

    /// <summary>
    /// Gets all devices currently in outage mode
    /// </summary>
    /// <returns>List of device IDs in outage</returns>
    List<Guid> GetDevicesInOutageMode();

    /// <summary>
    /// Gets devices that need RPC fallback check (no message received within timeout)
    /// </summary>
    /// <param name="timeout">Timeout duration since last message</param>
    /// <returns>List of device IDs needing RPC check</returns>
    List<Guid> GetDevicesNeedingRpcFallback(TimeSpan timeout);

    /// <summary>
    /// Initializes the grid status for a new device (e.g., during setup)
    /// </summary>
    /// <param name="thingsBoardDeviceId">ThingsBoard device ID</param>
    /// <param name="initialStatus">Initial status (typically from RPC query)</param>
    void InitializeDeviceStatus(Guid thingsBoardDeviceId, GridConnectionStatus initialStatus);

    /// <summary>
    /// Removes a device from tracking (e.g., when device is deleted)
    /// </summary>
    /// <param name="thingsBoardDeviceId">ThingsBoard device ID</param>
    /// <returns>True if device was removed, false if not found</returns>
    bool RemoveDevice(Guid thingsBoardDeviceId);

    /// <summary>
    /// Gets the count of devices currently being tracked
    /// </summary>
    int GetTrackedDeviceCount();
}
