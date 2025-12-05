using Hems_Backend.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Hems_Backend.Infrastructure.Services;

/// <summary>
/// In-memory cache for tracking device heartbeat status (event-driven, proactive monitoring)
/// </summary>
public class DeviceHeartbeatCache : IDeviceHeartbeatCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeviceHeartbeatCache> _logger;
    
    // Concurrent dictionary for thread-safe access to device states
    private readonly ConcurrentDictionary<Guid, DeviceMonitoringState> _deviceStates;
    
    private const string CacheKeyPrefix = "DeviceMonitoring_";
    private const int HeartbeatIntervalSeconds = 60; // Expect heartbeat every 60 seconds
    private const int GracePeriodSeconds = 30; // ±30 seconds grace period
    private const int MissedHeartbeatsForPending = 1;
    private const int MissedHeartbeatsForWaiting = 2;
    private const int MissedHeartbeatsForDisconnected = 3;

    public DeviceHeartbeatCache(
        IMemoryCache cache,
        ILogger<DeviceHeartbeatCache> logger)
    {
        _cache = cache;
        _logger = logger;
        _deviceStates = new ConcurrentDictionary<Guid, DeviceMonitoringState>();
    }

    public Task RecordHeartbeatAsync(Guid thingsBoardDeviceId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        // Validate timestamp (NTP sync check - reject if too far off)
        var timeDiff = Math.Abs((timestamp - now).TotalSeconds);
        if (timeDiff > 300) // 5 minutes tolerance for clock skew
        {
            _logger.LogWarning("Device {DeviceId} heartbeat timestamp is {Diff}s off from server time. Using server time.", 
                thingsBoardDeviceId, timeDiff);
            timestamp = now;
        }

        _deviceStates.AddOrUpdate(
            thingsBoardDeviceId,
            // Add new device
            deviceId => {
                var state = new DeviceMonitoringState
                {
                    ThingsBoardDeviceId = deviceId,
                    LastHeartbeat = timestamp,
                    MissedHeartbeats = 0,
                    Status = DeviceStatus.OK
                };
                _logger.LogInformation("Device {DeviceId} started tracking (first heartbeat)", deviceId);
                return state;
            },
            // Update existing device
            (deviceId, existingState) => {
                var previousStatus = existingState.Status;
                existingState.LastHeartbeat = timestamp;
                existingState.MissedHeartbeats = 0;
                existingState.Status = DeviceStatus.OK;
                
                if (previousStatus != DeviceStatus.OK)
                {
                    _logger.LogInformation("Device {DeviceId} recovered via heartbeat (was {PreviousStatus})", 
                        deviceId, previousStatus);
                }
                
                return existingState;
            });

        return Task.CompletedTask;
    }

    public Task RecordPowerFlowAsync(Guid thingsBoardDeviceId, DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        _deviceStates.AddOrUpdate(
            thingsBoardDeviceId,
            // Add new device (PowerFlow can also start tracking)
            deviceId => {
                var state = new DeviceMonitoringState
                {
                    ThingsBoardDeviceId = deviceId,
                    LastHeartbeat = timestamp, // Use PowerFlow as heartbeat signal
                    LastPowerFlow = timestamp,
                    MissedHeartbeats = 0,
                    Status = DeviceStatus.OK
                };
                _logger.LogInformation("Device {DeviceId} started tracking (first PowerFlow)", deviceId);
                return state;
            },
            // Update existing device - PowerFlow resets missed heartbeats
            (deviceId, existingState) => {
                var previousStatus = existingState.Status;
                existingState.LastPowerFlow = timestamp;
                existingState.MissedHeartbeats = 0;
                existingState.Status = DeviceStatus.OK;
                
                if (previousStatus != DeviceStatus.OK)
                {
                    _logger.LogInformation("Device {DeviceId} recovered via PowerFlow (was {PreviousStatus})", 
                        deviceId, previousStatus);
                }
                
                return existingState;
            });

        return Task.CompletedTask;
    }

    public DeviceStatus GetDeviceStatus(Guid thingsBoardDeviceId)
    {
        if (_deviceStates.TryGetValue(thingsBoardDeviceId, out var state))
        {
            return state.Status;
        }
        
        return DeviceStatus.NOT_TRACKED;
    }

    public List<Guid> GetTrackedDevices()
    {
        return _deviceStates.Keys.ToList();
    }

    public Task<List<DeviceStatusChange>> CheckMissedHeartbeatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var statusChanges = new List<DeviceStatusChange>();
        var expectedHeartbeatWindow = TimeSpan.FromSeconds(HeartbeatIntervalSeconds + GracePeriodSeconds);

        foreach (var kvp in _deviceStates)
        {
            var deviceId = kvp.Key;
            var state = kvp.Value;
            
            var timeSinceLastHeartbeat = now - state.LastHeartbeat;
            var previousStatus = state.Status;
            
            // Check if heartbeat is overdue (with grace period)
            if (timeSinceLastHeartbeat > expectedHeartbeatWindow)
            {
                // Heartbeat is late - increment missed count
                state.MissedHeartbeats++;
                
                // Update status based on missed count
                var newStatus = state.MissedHeartbeats switch
                {
                    MissedHeartbeatsForPending => DeviceStatus.PENDING,
                    MissedHeartbeatsForWaiting => DeviceStatus.WAITING,
                    >= MissedHeartbeatsForDisconnected => DeviceStatus.DISCONNECTED,
                    _ => state.Status
                };

                if (newStatus != previousStatus)
                {
                    state.Status = newStatus;
                    
                    var change = new DeviceStatusChange
                    {
                        ThingsBoardDeviceId = deviceId,
                        PreviousStatus = previousStatus,
                        NewStatus = newStatus,
                        MissedHeartbeats = state.MissedHeartbeats,
                        Timestamp = now
                    };
                    
                    statusChanges.Add(change);
                    
                    _logger.LogWarning("Device {DeviceId} status changed: {PreviousStatus} → {NewStatus} (missed: {Count}, last seen: {TimeSince}s ago)",
                        deviceId, previousStatus, newStatus, state.MissedHeartbeats, (int)timeSinceLastHeartbeat.TotalSeconds);
                }
            }
        }

        return Task.FromResult(statusChanges);
    }

    public Task AddDeviceToCacheAsync(Guid thingsBoardDeviceId, CancellationToken cancellationToken = default)
    {
        // Optional method - devices auto-add on first heartbeat
        // Can be used when device is created to pre-populate cache
        
        _deviceStates.TryAdd(thingsBoardDeviceId, new DeviceMonitoringState
        {
            ThingsBoardDeviceId = thingsBoardDeviceId,
            LastHeartbeat = DateTime.UtcNow,
            MissedHeartbeats = 0,
            Status = DeviceStatus.OK
        });
        
        _logger.LogInformation("Device {DeviceId} pre-added to tracking", thingsBoardDeviceId);
        return Task.CompletedTask;
    }

    public Task InitializeCacheAsync(CancellationToken cancellationToken = default)
    {
        // Not needed for heartbeat-driven model
        // Devices add themselves when they send first heartbeat
        _logger.LogInformation("DeviceHeartbeatCache initialized (event-driven mode - devices will auto-register on first heartbeat)");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get internal state for a device (for disconnect logging)
    /// </summary>
    public DeviceMonitoringState? GetDeviceState(Guid thingsBoardDeviceId)
    {
        _deviceStates.TryGetValue(thingsBoardDeviceId, out var state);
        return state;
    }
}
