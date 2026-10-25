# PowerFlow Multiple Connections Analysis

**Date**: 2026-01-03
**Status**: Analysis Complete - Awaiting Implementation Decision
**Priority**: Medium-High (scalability concern)

---

## Executive Summary

The current PowerFlow implementation does **not support shared RPC results** for multiple SignalR connections viewing the same device. Each connection makes independent RPC calls to ThingsBoard, which can overwhelm devices when multiple users (e.g., User + Administrator) view the same device simultaneously.

---

## Problem Statement

**Scenario**: User logs in and views PowerFlow data for Device A. Administrator also logs in and views the same Device A.

**Expected Behavior**: Both users should see the same PowerFlow data with a single RPC call to the device.

**Actual Behavior**: Two separate RPC calls are made to the device (one per connection), every polling interval.

---

## Affected Files

| File | Location | Role |
|------|----------|------|
| `PowerFlowHub.cs` | `src/Web/Hubs/` | SignalR streaming hub - initiates data fetch per connection |
| `PowerFlowService.cs` | `src/Web/Services/` | Business logic - makes RPC calls (no caching) |
| `ThingsBoardConnectorService.cs` | `src/Infrastructure/Services/` | Centralized RPC communication |
| `DeviceMappingService.cs` | `src/Infrastructure/Services/` | Device ID mapping (correctly cached) |
| `DeviceHeartbeatCache.cs` | `src/Infrastructure/Services/` | Status tracking (thread-safe) |

---

## Current Data Flow

```
┌─────────────────┐     ┌─────────────────┐
│   User Client   │     │  Admin Client   │
└────────┬────────┘     └────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────────────────────────────┐
│           PowerFlowHub                  │
│   StreamPowerFlow(deviceId, interval)   │
└────────┬───────────────────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐     ┌─────────────────┐
│ PowerFlowService│     │ PowerFlowService│
│ GetPowerFlowAsync│     │ GetPowerFlowAsync│
└────────┬────────┘     └────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────────────────────────────┐
│      ThingsBoardConnectorService        │
│         SendTwoWayRpcAsync()            │
│                                         │
│   TWO SEPARATE RPC CALLS TO DEVICE!     │
└────────┬───────────────────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────────────────────────────┐
│        ThingsBoard Platform             │
└────────┬───────────────────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────────────────────────────┐
│              Physical Device            │
│    RECEIVES 2 RPC REQUESTS/INTERVAL     │
└─────────────────────────────────────────┘
```

---

## Code Analysis

### 1. PowerFlowHub.cs (Lines 37-91)

```csharp
public async IAsyncEnumerable<PowerFlowDto> StreamPowerFlow(
    Guid deviceId,
    int intervalSeconds,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // Clamp interval between 2-60 seconds
    if (intervalSeconds <= 0) intervalSeconds = 5;
    if (intervalSeconds < 2) intervalSeconds = 2;
    if (intervalSeconds > 60) intervalSeconds = 60;

    var interval = TimeSpan.FromSeconds(intervalSeconds);

    // ISSUE: Each connection runs its own loop
    while (!cancellationToken.IsCancellationRequested)
    {
        // ISSUE: Independent call per connection
        PowerFlowDto? dto = await _powerFlowService.GetPowerFlowAsync(deviceId, cancellationToken);

        if (dto != null)
        {
            yield return dto;
        }

        await Task.Delay(interval, cancellationToken);
    }
}
```

**Problem**: Each SignalR connection creates its own streaming loop, calling `GetPowerFlowAsync` independently.

### 2. PowerFlowService.cs (Lines 41-96)

```csharp
public async Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, CancellationToken cancellationToken = default)
{
    // Step 1: Map local deviceId to ThingsBoardDeviceId (CACHED - OK)
    var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(deviceId, cancellationToken);

    // Step 2: Call ThingsBoard RPC (NOT CACHED - PROBLEM)
    var rpcResponse = await _tbConnector.SendTwoWayRpcAsync(
        thingsBoardDeviceId.Value,
        "getPowerFlowState",
        new Dictionary<string, object>(),
        timeoutMs: 10000,
        cancellationToken: cancellationToken);

    // ... rest of method
}
```

**Problem**: No caching mechanism for RPC results. Every call triggers a new RPC request.

### 3. What IS Correctly Cached

**DeviceMappingService.cs** - Device ID mappings are cached for 24 hours:
```csharp
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
    SlidingExpiration = TimeSpan.FromHours(6)
};
_cache.Set(cacheKey, device?.ThingsBoardDeviceId, cacheOptions);
```

**DeviceHeartbeatCache.cs** - Status tracking uses thread-safe `ConcurrentDictionary`:
```csharp
private readonly ConcurrentDictionary<Guid, DeviceMonitoringState> _deviceStates;
```

---

## Impact Assessment

### Scalability Impact

| Viewers per Device | RPC Calls/5 sec | RPC Calls/minute | RPC Calls/hour |
|--------------------|-----------------|------------------|----------------|
| 1                  | 1               | 12               | 720            |
| 2                  | 2               | 24               | 1,440          |
| 5                  | 5               | 60               | 3,600          |
| 10                 | 10              | 120              | 7,200          |
| 50                 | 50              | 600              | 36,000         |

### Device Impact

- Embedded devices may have limited processing power
- MQTT RPC calls consume device CPU and memory
- High RPC frequency can cause device instability
- Battery-powered devices drain faster

### ThingsBoard Impact

- API rate limits may be exceeded
- Increased server load
- Higher bandwidth consumption
- Potential for throttling

---

## Proposed Solutions

### Option A: Time-Based RPC Result Cache (Recommended)

**Concept**: Cache RPC results for a short duration (2-5 seconds). Multiple viewers share cached data.

**Implementation**:

```csharp
// New interface: src/Application/Common/Interfaces/IPowerFlowCache.cs
public interface IPowerFlowCache
{
    Task<PowerFlowDto?> GetOrFetchAsync(
        Guid deviceId,
        Func<Task<PowerFlowDto?>> fetchFunc,
        CancellationToken cancellationToken = default);
}

// Implementation: src/Infrastructure/Services/PowerFlowCache.cs
public class PowerFlowCache : IPowerFlowCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    private const int CacheSeconds = 3; // Cache for 3 seconds

    public async Task<PowerFlowDto?> GetOrFetchAsync(
        Guid deviceId,
        Func<Task<PowerFlowDto?>> fetchFunc,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"powerflow_{deviceId}";

        // Check cache first
        if (_cache.TryGetValue(cacheKey, out PowerFlowDto? cached))
        {
            return cached;
        }

        // Use lock to prevent duplicate RPC calls
        var deviceLock = _locks.GetOrAdd(deviceId, _ => new SemaphoreSlim(1, 1));
        await deviceLock.WaitAsync(cancellationToken);

        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(cacheKey, out cached))
            {
                return cached;
            }

            // Fetch from device (single RPC call)
            var result = await fetchFunc();

            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(CacheSeconds));
            }

            return result;
        }
        finally
        {
            deviceLock.Release();
        }
    }
}
```

**Modified PowerFlowService.cs**:

```csharp
public async Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, CancellationToken cancellationToken = default)
{
    return await _powerFlowCache.GetOrFetchAsync(
        deviceId,
        async () => await FetchFromDeviceAsync(deviceId, cancellationToken),
        cancellationToken);
}

private async Task<PowerFlowDto?> FetchFromDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
{
    // Existing RPC logic here
    var thingsBoardDeviceId = await _deviceMapping.GetThingsBoardDeviceIdAsync(deviceId, cancellationToken);
    var rpcResponse = await _tbConnector.SendTwoWayRpcAsync(...);
    // ... rest of existing logic
}
```

**Pros**:
- Minimal code changes
- Backward compatible
- Single RPC per cache window
- Independent viewer intervals still work

**Cons**:
- Slight data staleness (3 seconds max)
- Memory usage per active device

---

### Option B: Pub/Sub Broadcast Model

**Concept**: Background service polls devices, broadcasts to all subscribers via SignalR groups.

**Implementation**:

```csharp
// New service: src/Web/Services/PowerFlowBroadcastService.cs
public class PowerFlowBroadcastService : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, DeviceSubscription> _subscriptions = new();
    private readonly IHubContext<PowerFlowHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public void Subscribe(Guid deviceId, string connectionId, int intervalSeconds)
    {
        var subscription = _subscriptions.GetOrAdd(deviceId, id => new DeviceSubscription
        {
            DeviceId = id,
            IntervalSeconds = intervalSeconds,
            Connections = new ConcurrentBag<string>()
        });

        subscription.Connections.Add(connectionId);

        // Start polling if first subscriber
        if (subscription.Connections.Count == 1)
        {
            _ = StartPollingAsync(deviceId, intervalSeconds);
        }
    }

    public void Unsubscribe(Guid deviceId, string connectionId)
    {
        if (_subscriptions.TryGetValue(deviceId, out var subscription))
        {
            // Remove connection from bag
            // Stop polling if no subscribers remain
        }
    }

    private async Task StartPollingAsync(Guid deviceId, int intervalSeconds)
    {
        while (_subscriptions.TryGetValue(deviceId, out var subscription) &&
               subscription.Connections.Any())
        {
            using var scope = _scopeFactory.CreateScope();
            var powerFlowService = scope.ServiceProvider.GetRequiredService<IPowerFlowService>();

            var dto = await powerFlowService.GetPowerFlowAsync(deviceId);

            if (dto != null)
            {
                // Broadcast to ALL subscribers of this device
                await _hubContext.Clients.Group($"powerflow_{deviceId}")
                    .SendAsync("ReceivePowerFlow", dto);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
        }
    }
}
```

**Modified PowerFlowHub.cs**:

```csharp
public override async Task OnConnectedAsync()
{
    await base.OnConnectedAsync();
}

public async Task SubscribeToPowerFlow(Guid deviceId, int intervalSeconds)
{
    // Join SignalR group
    await Groups.AddToGroupAsync(Context.ConnectionId, $"powerflow_{deviceId}");

    // Register with broadcast service
    _broadcastService.Subscribe(deviceId, Context.ConnectionId, intervalSeconds);
}

public async Task UnsubscribeFromPowerFlow(Guid deviceId)
{
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"powerflow_{deviceId}");
    _broadcastService.Unsubscribe(deviceId, Context.ConnectionId);
}

public override async Task OnDisconnectedAsync(Exception? exception)
{
    // Clean up all subscriptions for this connection
    _broadcastService.UnsubscribeAll(Context.ConnectionId);
    await base.OnDisconnectedAsync(exception);
}
```

**Pros**:
- True single RPC per device regardless of viewers
- Consistent data for all viewers
- Easy to add rate limiting

**Cons**:
- More significant code changes
- Breaking change for frontend (different subscription model)
- More complex state management

---

### Option C: Keep Current Implementation

**When This Is Acceptable**:
- Low number of expected simultaneous viewers per device
- Devices can handle multiple RPC calls without issues
- ThingsBoard rate limits are not a concern

**Mitigations If Keeping Current**:
- Add rate limiting per device
- Increase minimum interval from 2s to 5s
- Add documentation warning

---

## Recommended Action

**Recommendation**: Implement **Option A (Time-Based RPC Result Cache)** because:

1. Minimal code changes required
2. No breaking changes to frontend
3. Significant reduction in RPC calls
4. Backward compatible with existing flow
5. Can be implemented incrementally

---

## Files to Create/Modify

### New Files

1. `src/Application/Common/Interfaces/IPowerFlowCache.cs`
2. `src/Infrastructure/Services/PowerFlowCache.cs`

### Modified Files

1. `src/Web/Services/PowerFlowService.cs` - Add cache dependency
2. `src/Infrastructure/DependencyInjection.cs` - Register cache service
3. `src/Web/DependencyInjection.cs` - (if needed)

---

## Testing Checklist

- [ ] Single viewer receives PowerFlow data correctly
- [ ] Two viewers of same device share cached data
- [ ] Different devices have separate caches
- [ ] Cache expires after configured duration
- [ ] No race conditions with concurrent access
- [ ] Memory is cleaned up when cache expires
- [ ] Device status updates still broadcast to all viewers
- [ ] RPC errors are handled properly (don't cache errors)

---

## Related Components (Already Working Correctly)

### DeviceStatusHub.cs - Group-based broadcasting

```csharp
// Users subscribe to device status
await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{localDeviceId}");

// Status changes broadcast to all subscribers
await _hubContext.Clients.Group($"device_{deviceId}")
    .SendAsync("DeviceStatusChanged", statusData);
```

This pattern should be referenced when implementing Option B.

### DeviceMappingService.cs - Correct caching pattern

```csharp
// Check cache first
if (_cache.TryGetValue<Guid?>(cacheKey, out var cachedThingsBoardId))
{
    return cachedThingsBoardId;
}

// Query database only if not cached
var device = await _context.Devices...

// Cache with options
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
    SlidingExpiration = TimeSpan.FromHours(6)
};
_cache.Set(cacheKey, device?.ThingsBoardDeviceId, cacheOptions);
```

This pattern should be adapted for PowerFlow caching.

---

## Notes for Future Implementation

1. **Cache Duration**: 3 seconds recommended. Adjust based on:
   - Device data change frequency
   - Acceptable staleness for users
   - Memory constraints

2. **Lock Strategy**: Use `SemaphoreSlim` per device to prevent thundering herd problem when cache expires.

3. **Error Handling**: Do NOT cache failed RPC responses. Let each caller retry.

4. **Metrics**: Consider adding telemetry to track:
   - Cache hit rate
   - RPC calls saved
   - Active viewers per device

5. **Frontend Impact**: None for Option A. Frontend continues calling `StreamPowerFlow` as before.

---

## Questions to Resolve Before Implementation

1. What cache duration is acceptable? (2, 3, or 5 seconds?)
2. Should cache be per-device or configurable per-user?
3. Should we add monitoring/logging for cache hits?
4. Are there any devices that require real-time data without caching?

---

## Appendix: ThingsBoard RPC Details

### RPC Call Structure

```http
POST /api/plugins/rpc/twoway/{deviceId}?timeout=10000
Authorization: Bearer {token}
Content-Type: application/json

{
    "method": "getPowerFlowState",
    "params": {}
}
```

### RPC Response Structure

```json
{
    "kind": "powerFlowState",
    "timestampUtc": "2026-01-03T10:30:00Z",
    "version": 1,
    "deviceId": "device-123",
    "pv": {
        "totalPowerW": 5000
    },
    "grid": {
        "totalPowerW": -1500
    },
    "load": {
        "totalPowerW": 3500
    },
    "battery": {
        "powerW": 0,
        "socPercent": 80
    },
    "generator": {
        "totalPowerW": 0
    }
}
```

### MQTT RPC Flow (for reference)

```
Backend                    ThingsBoard                    Device
   |                           |                            |
   |-- POST /rpc/twoway ------>|                            |
   |                           |-- MQTT publish ----------->|
   |                           |   v1/devices/me/rpc/request|
   |                           |                            |
   |                           |<-- MQTT response ----------|
   |                           |   v1/devices/me/rpc/response
   |<-- HTTP response ---------|                            |
   |                           |                            |
```

---

*Document created: 2026-01-03*
*Last updated: 2026-01-03*
*Author: Claude Code Analysis*
