# Shared Device Stream Manager

This document describes the reference-counted shared stream caching system for device data streaming (PowerFlow, etc.).

## Overview

The `SharedDeviceStreamManager` provides an in-memory caching layer for device data streams. It ensures that only one polling loop runs per device, regardless of how many users are viewing that device's data. This dramatically reduces RPC calls to ThingsBoard and improves overall system performance.

## Problem Statement

### Before (Without Shared Streams)

```
User 1 → StreamPowerFlow(DeviceA) → RPC Call every 5s
User 2 → StreamPowerFlow(DeviceA) → RPC Call every 5s  ← DUPLICATE!
User 3 → StreamPowerFlow(DeviceA) → RPC Call every 5s  ← DUPLICATE!
                                     ↓
                              3 RPC calls every 5s to same device!
```

**Issues:**
- N users viewing same device = N RPC calls every interval
- Wasted bandwidth and ThingsBoard resources
- Increased latency due to concurrent requests
- Higher chance of rate limiting

### After (With Shared Streams)

```
User 1 ─┐
User 2 ─┼→ SharedDeviceStream(DeviceA) → 1 RPC Call every 5s → Broadcast to all
User 3 ─┘
                                     ↓
                              Only 1 RPC call, data shared with all subscribers!
```

**Benefits:**
- N users viewing same device = 1 RPC call every interval
- Reduced ThingsBoard load
- Lower latency (shared cache)
- Better scalability

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SharedDeviceStreamManager (Singleton)                │
├─────────────────────────────────────────────────────────────────────────┤
│  ConcurrentDictionary<Guid, SharedDeviceStream>                         │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ SharedDeviceStream (per device)                                  │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │ • DeviceId: Guid                                                 │   │
│  │ • SubscriberCount: int (reference count)                         │   │
│  │ • LastData: PowerFlowDto (cached latest)                         │   │
│  │ • LastUpdated: DateTime                                          │   │
│  │ • Subscribers: ConcurrentDictionary<string, Channel<PowerFlowDto>>│  │
│  │ • PollingTask: Task (single background poller)                   │   │
│  │ • CancellationTokenSource: for cleanup                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

## Subscribe Flow

```
┌─────────────────┐
│ User subscribes │
│ to Device A     │
└────────┬────────┘
         ▼
┌─────────────────────────┐
│ Check if stream exists  │
│ in SharedDeviceStreams  │
└────────┬────────────────┘
         │
    ┌────┴────┐
    │ Exists? │
    └────┬────┘
    No   │   Yes
    ▼    │    ▼
┌────────┴───┐ ┌─────────────────┐
│ Create new │ │ Add subscriber  │
│ stream +   │ │ to existing     │
│ start poll │ │ stream          │
└────────────┘ └─────────────────┘
         │              │
         └──────┬───────┘
                ▼
        ┌───────────────┐
        │ Return Channel│
        │ for receiving │
        │ data          │
        └───────────────┘
```

## Unsubscribe Flow

```
┌───────────────────┐
│ User disconnects  │
└─────────┬─────────┘
          ▼
┌─────────────────────────┐
│ Decrement refCount      │
│ Remove from subscribers │
└─────────┬───────────────┘
          │
     ┌────┴────┐
     │refCount │
     │  == 0?  │
     └────┬────┘
     No   │   Yes
     ▼    │    ▼
  (keep   │  ┌──────────────────┐
  polling)│  │ Schedule cleanup │
          │  │ (after delay)    │
          │  └────────┬─────────┘
          │           ▼
          │  ┌──────────────────┐
          │  │ After 5s delay:  │
          │  │ If still 0 subs: │
          │  │ • Cancel polling │
          │  │ • Remove stream  │
          │  │ • Dispose        │
          │  └──────────────────┘
```

## Reference Counting Example

```
Timeline:
─────────────────────────────────────────────────────────────────────────
T=0s   User 1 subscribes to Device A
       → Stream created, refCount = 1
       → Polling starts
─────────────────────────────────────────────────────────────────────────
T=10s  User 2 subscribes to Device A
       → Reuses existing stream, refCount = 2
       → Both receive same data
─────────────────────────────────────────────────────────────────────────
T=30s  User 1 disconnects
       → refCount = 1
       → Polling continues (still has subscriber)
─────────────────────────────────────────────────────────────────────────
T=45s  User 3 subscribes to Device A
       → Reuses stream, refCount = 2
       → Receives cached data immediately
─────────────────────────────────────────────────────────────────────────
T=60s  User 2 disconnects
       → refCount = 1
       → Polling continues
─────────────────────────────────────────────────────────────────────────
T=90s  User 3 disconnects
       → refCount = 0
       → Cleanup scheduled (5s delay)
─────────────────────────────────────────────────────────────────────────
T=95s  Cleanup executes
       → Still 0 subscribers
       → Polling cancelled
       → Stream removed from cache
       → Resources disposed
─────────────────────────────────────────────────────────────────────────
```

## Key Components

### ISharedDeviceStreamManager

```csharp
public interface ISharedDeviceStreamManager
{
    // Subscribe to a device's data stream
    Task<IDeviceStreamSubscription> SubscribeAsync(
        Guid deviceId,
        int intervalSeconds = 5,
        CancellationToken cancellationToken = default);

    // Unsubscribe from a device's data stream
    Task<bool> UnsubscribeAsync(string subscriptionId);

    // Get statistics for monitoring
    SharedStreamStatistics GetStatistics();
    int GetActiveStreamCount();
    int GetTotalSubscriberCount();
    int GetSubscriberCount(Guid deviceId);
    bool HasActiveSubscribers(Guid deviceId);
}
```

### IDeviceStreamSubscription

```csharp
public interface IDeviceStreamSubscription : IAsyncDisposable
{
    Guid DeviceId { get; }
    string SubscriptionId { get; }
    ChannelReader<object> DataChannel { get; }
    bool IsActive { get; }
    DateTime SubscribedAt { get; }
}
```

## Configuration

Add to `appsettings.json`:

```json
{
  "SharedStream": {
    "DefaultPollingIntervalSeconds": 5,
    "MinPollingIntervalSeconds": 2,
    "MaxPollingIntervalSeconds": 60,
    "CleanupDelaySeconds": 5,
    "StaleDataThresholdSeconds": 30
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `DefaultPollingIntervalSeconds` | 5 | Default interval if not specified |
| `MinPollingIntervalSeconds` | 2 | Minimum allowed interval |
| `MaxPollingIntervalSeconds` | 60 | Maximum allowed interval |
| `CleanupDelaySeconds` | 5 | Grace period before cleanup after last subscriber leaves |
| `StaleDataThresholdSeconds` | 30 | Max age of cached data before it's considered stale |

## Usage in PowerFlowHub

```csharp
public async IAsyncEnumerable<PowerFlowDto> StreamPowerFlow(
    Guid deviceId,
    int intervalSeconds,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    // Subscribe to shared stream
    await using var subscription = await _streamManager.SubscribeAsync(
        deviceId, intervalSeconds, cancellationToken);

    // Read from the shared channel
    await foreach (var data in subscription.DataChannel.ReadAllAsync(cancellationToken))
    {
        if (data is PowerFlowDto powerFlowDto)
        {
            yield return powerFlowDto;
        }
    }
    // Automatic unsubscribe on dispose
}
```

## Thread Safety

The implementation is fully thread-safe:

- `ConcurrentDictionary` for stream and subscription storage
- `Channel<T>` for thread-safe data broadcasting
- Lock-free operations where possible
- Bounded channels prevent memory issues with slow consumers

## Monitoring

### Get Statistics

```csharp
// From PowerFlowHub
public SharedStreamStatistics GetStreamStatistics()
{
    return _streamManager.GetStatistics();
}
```

### Statistics Response

```json
{
  "activeStreamCount": 5,
  "totalSubscriberCount": 12,
  "subscribersPerDevice": {
    "device-a-guid": 3,
    "device-b-guid": 5,
    "device-c-guid": 4
  }
}
```

## Performance Characteristics

| Metric | Before | After |
|--------|--------|-------|
| RPC calls (10 users, 1 device) | 10/interval | 1/interval |
| RPC calls (10 users, 5 devices) | 50/interval | 5/interval |
| Memory usage | N channels | 1 stream + N lightweight subscriptions |
| Cleanup on disconnect | Immediate | Delayed (configurable grace period) |

## Error Handling

- **RPC failures**: Logged, polling continues with next interval
- **Slow consumers**: Old data dropped (bounded channel with `DropOldest`)
- **Dead subscriptions**: Automatically detected and removed during broadcast
- **Cancellation**: Graceful cleanup of all resources

## Device Offline Detection

When a device becomes unreachable (power loss, network issues, etc.), the stream manager detects this and sends zero values to subscribers instead of showing stale cached data.

### Offline Detection Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEVICE OFFLINE DETECTION                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  RPC Poll #1 ──► FAIL ──► consecutiveErrors = 1                 │
│       │                                                         │
│       ▼ (5 seconds)                                             │
│  RPC Poll #2 ──► FAIL ──► consecutiveErrors = 2                 │
│       │                                                         │
│       ▼ (5 seconds)                                             │
│  RPC Poll #3 ──► FAIL ──► consecutiveErrors = 3 ⚠️              │
│       │                   │                                     │
│       │                   ▼                                     │
│       │         ┌─────────────────────────────────┐             │
│       │         │ DeviceOfflineThreshold reached! │             │
│       │         │                                 │             │
│       │         │ 1. Set IsDeviceOffline = true   │             │
│       │         │ 2. Create zero PowerFlowDto     │             │
│       │         │ 3. Broadcast to all subscribers │             │
│       │         └─────────────────────────────────┘             │
│       │                                                         │
│       ▼                                                         │
│  Frontend receives zero data ──► Shows gray flow lines          │
│                                  (no active power flow)         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Recovery Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEVICE RECOVERY DETECTION                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Device comes back online                                       │
│       │                                                         │
│       ▼                                                         │
│  RPC Poll ──► SUCCESS ──► Real PowerFlowDto received            │
│       │                   │                                     │
│       │                   ▼                                     │
│       │         ┌─────────────────────────────────┐             │
│       │         │ Device recovered!               │             │
│       │         │                                 │             │
│       │         │ 1. Set IsDeviceOffline = false  │             │
│       │         │ 2. Reset consecutiveErrors = 0  │             │
│       │         │ 3. Broadcast real data          │             │
│       │         └─────────────────────────────────┘             │
│       │                                                         │
│       ▼                                                         │
│  Frontend receives real data ──► Shows active flow lines        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Configuration

| Constant | Value | Description |
|----------|-------|-------------|
| `DeviceOfflineThreshold` | 3 | Consecutive RPC failures before sending zero data |
| `maxConsecutiveErrors` | 10 | Consecutive failures before adding extended delay |

### Timing

With default 5-second polling interval:
- **Device offline detected**: 15 seconds (3 × 5s)
- **Extended delay triggered**: 50 seconds (10 × 5s)

### Zero PowerFlowDto

When device is detected as offline, the following data is sent:

```json
{
  "deviceId": "device-guid",
  "timestampUtc": "2025-01-16T14:00:00Z",
  "solarPowerKw": 0,
  "gridPowerKw": 0,
  "loadPowerKw": 0,
  "batteryPowerKw": 0,
  "generatorPowerKw": 0
}
```

This causes the frontend to display gray flow lines (no active power flow) instead of stale cached data from when the device was last online.

### Integration with Device Status

The PowerFlow offline detection works alongside the heartbeat-based device status system:

| System | Detection Time | Purpose |
|--------|---------------|---------|
| **DeviceHeartbeatCache** | ~210 seconds | Official device online/offline status |
| **PowerFlow Offline** | ~15 seconds | Clear stale PowerFlow visualization |

The PowerFlow offline detection is faster because:
1. It polls every 5 seconds (vs 60-second heartbeat interval)
2. It only needs 3 failures (vs 3+ missed heartbeats)

This ensures users see accurate PowerFlow data quickly when a device disconnects, while the heartbeat system provides authoritative status tracking with database logging.

## Backwards Compatibility

The legacy direct polling method is still available:

```csharp
// Legacy method - bypasses shared stream
public async IAsyncEnumerable<PowerFlowDto> StreamPowerFlowDirect(...)
```

Use `StreamPowerFlow` for new implementations.

## File Locations

| Component | Path |
|-----------|------|
| Interface | `src/Application/Common/Interfaces/ISharedDeviceStreamManager.cs` |
| Implementation | `src/Infrastructure/Services/SharedDeviceStreamManager.cs` |
| DI Registration | `src/Infrastructure/DependencyInjection.cs` |
| Hub Usage | `src/Web/Hubs/PowerFlowHub.cs` |
