using System.Collections.Concurrent;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// In-memory service for tracking grid connection status per device.
/// Uses ConcurrentDictionary for thread-safe state management.
/// Notifications are only sent on state transitions.
/// </summary>
public class GridStatusTrackingService : IGridStatusTrackingService
{
    private readonly ILogger<GridStatusTrackingService> _logger;
    private readonly ConcurrentDictionary<Guid, GridStatusState> _deviceStates;

    /// <summary>
    /// Notification code for grid power outage detected (W33)
    /// </summary>
    public const string NotificationCodeOutage = "GRD-ALM-W33";

    /// <summary>
    /// Notification code for grid connection restored (W34)
    /// </summary>
    public const string NotificationCodeRestored = "GRD-ALM-W34";

    public GridStatusTrackingService(ILogger<GridStatusTrackingService> logger)
    {
        _logger = logger;
        _deviceStates = new ConcurrentDictionary<Guid, GridStatusState>();
    }

    public GridStatusUpdateResult UpdateGridStatus(
        Guid thingsBoardDeviceId,
        bool gridConnected,
        double? phaseAVoltageV = null,
        double? phaseBVoltageV = null,
        double? phaseCVoltageV = null,
        DateTime? timestampUtc = null)
    {
        var timestamp = timestampUtc ?? DateTime.UtcNow;
        var newStatus = gridConnected ? GridConnectionStatus.Connected : GridConnectionStatus.Disconnected;

        // Initialize result - will be updated by the AddOrUpdate lambdas
        var result = new GridStatusUpdateResult { Success = true, CurrentStatus = newStatus };

        _deviceStates.AddOrUpdate(
            thingsBoardDeviceId,
            // Add new device - first message sets initial state
            _ =>
            {
                var state = new GridStatusState
                {
                    ThingsBoardDeviceId = thingsBoardDeviceId,
                    Status = newStatus,
                    LastStatusMessageTime = timestamp,
                    PhaseAVoltageV = phaseAVoltageV,
                    PhaseBVoltageV = phaseBVoltageV,
                    PhaseCVoltageV = phaseCVoltageV,
                    GridConnectedFlag = gridConnected,
                    OutageStartTime = newStatus == GridConnectionStatus.Disconnected ? timestamp : null
                };

                result.PreviousStatus = GridConnectionStatus.Unknown;
                result.StatusChanged = true;
                // Only send notification if grid is disconnected on first message
                result.ShouldSendNotification = newStatus == GridConnectionStatus.Disconnected;
                result.NotificationCode = newStatus == GridConnectionStatus.Disconnected ? NotificationCodeOutage : null;

                _logger.LogInformation(
                    "Grid status initialized for device {DeviceId}: {Status} (voltages: A={PhaseA}V, B={PhaseB}V, C={PhaseC}V)",
                    thingsBoardDeviceId, newStatus, phaseAVoltageV, phaseBVoltageV, phaseCVoltageV);

                return state;
            },
            // Update existing device
            (_, existingState) =>
            {
                var previousStatus = existingState.Status;
                existingState.LastStatusMessageTime = timestamp;
                existingState.PhaseAVoltageV = phaseAVoltageV;
                existingState.PhaseBVoltageV = phaseBVoltageV;
                existingState.PhaseCVoltageV = phaseCVoltageV;
                existingState.GridConnectedFlag = gridConnected;
                existingState.Status = newStatus;

                result.PreviousStatus = previousStatus;
                result.StatusChanged = previousStatus != newStatus;

                // Handle state transitions
                if (previousStatus != newStatus)
                {
                    if (newStatus == GridConnectionStatus.Disconnected)
                    {
                        // Connected -> Disconnected: Outage started
                        existingState.OutageStartTime = timestamp;
                        result.ShouldSendNotification = true;
                        result.NotificationCode = NotificationCodeOutage;

                        _logger.LogWarning(
                            "Grid OUTAGE detected for device {DeviceId}. Voltages: A={PhaseA}V, B={PhaseB}V, C={PhaseC}V",
                            thingsBoardDeviceId, phaseAVoltageV, phaseBVoltageV, phaseCVoltageV);
                    }
                    else if (newStatus == GridConnectionStatus.Connected && previousStatus == GridConnectionStatus.Disconnected)
                    {
                        // Disconnected -> Connected: Grid restored
                        result.ShouldSendNotification = true;
                        result.NotificationCode = NotificationCodeRestored;

                        if (existingState.OutageStartTime.HasValue)
                        {
                            result.OutageDuration = timestamp - existingState.OutageStartTime.Value;
                            _logger.LogInformation(
                                "Grid RESTORED for device {DeviceId} after {Duration}. Voltages: A={PhaseA}V, B={PhaseB}V, C={PhaseC}V",
                                thingsBoardDeviceId, result.OutageDuration, phaseAVoltageV, phaseBVoltageV, phaseCVoltageV);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Grid RESTORED for device {DeviceId}. Voltages: A={PhaseA}V, B={PhaseB}V, C={PhaseC}V",
                                thingsBoardDeviceId, phaseAVoltageV, phaseBVoltageV, phaseCVoltageV);
                        }

                        existingState.OutageStartTime = null;
                    }
                    else if (newStatus == GridConnectionStatus.Connected && previousStatus == GridConnectionStatus.Unknown)
                    {
                        // Unknown -> Connected: First message shows grid connected, no notification
                        result.ShouldSendNotification = false;

                        _logger.LogInformation(
                            "Grid status confirmed CONNECTED for device {DeviceId}. Voltages: A={PhaseA}V, B={PhaseB}V, C={PhaseC}V",
                            thingsBoardDeviceId, phaseAVoltageV, phaseBVoltageV, phaseCVoltageV);
                    }
                }
                else
                {
                    // No status change - just update timestamp, no notification
                    result.ShouldSendNotification = false;

                    _logger.LogDebug(
                        "Grid status unchanged for device {DeviceId}: {Status}",
                        thingsBoardDeviceId, newStatus);
                }

                return existingState;
            });

        return result;
    }

    public GridStatusState? GetGridStatus(Guid thingsBoardDeviceId)
    {
        _deviceStates.TryGetValue(thingsBoardDeviceId, out var state);
        return state;
    }

    public List<Guid> GetDevicesInOutageMode()
    {
        return _deviceStates
            .Where(kvp => kvp.Value.Status == GridConnectionStatus.Disconnected)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public List<Guid> GetDevicesNeedingRpcFallback(TimeSpan timeout)
    {
        var cutoffTime = DateTime.UtcNow - timeout;

        return _deviceStates
            .Where(kvp =>
                kvp.Value.Status == GridConnectionStatus.Disconnected &&
                kvp.Value.LastStatusMessageTime < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public void InitializeDeviceStatus(Guid thingsBoardDeviceId, GridConnectionStatus initialStatus)
    {
        var now = DateTime.UtcNow;

        _deviceStates.AddOrUpdate(
            thingsBoardDeviceId,
            _ =>
            {
                _logger.LogInformation(
                    "Grid status initialized for device {DeviceId}: {Status} (via RPC query)",
                    thingsBoardDeviceId, initialStatus);

                return new GridStatusState
                {
                    ThingsBoardDeviceId = thingsBoardDeviceId,
                    Status = initialStatus,
                    LastStatusMessageTime = now,
                    OutageStartTime = initialStatus == GridConnectionStatus.Disconnected ? now : null
                };
            },
            (_, existingState) =>
            {
                // Only update if current status is Unknown
                if (existingState.Status == GridConnectionStatus.Unknown)
                {
                    existingState.Status = initialStatus;
                    existingState.LastStatusMessageTime = now;
                    existingState.OutageStartTime = initialStatus == GridConnectionStatus.Disconnected ? now : null;

                    _logger.LogInformation(
                        "Grid status updated for device {DeviceId}: Unknown -> {Status} (via RPC query)",
                        thingsBoardDeviceId, initialStatus);
                }

                return existingState;
            });
    }

    public bool RemoveDevice(Guid thingsBoardDeviceId)
    {
        var removed = _deviceStates.TryRemove(thingsBoardDeviceId, out _);

        if (removed)
        {
            _logger.LogInformation("Device {DeviceId} removed from grid status tracking", thingsBoardDeviceId);
        }

        return removed;
    }

    public int GetTrackedDeviceCount() => _deviceStates.Count;
}
