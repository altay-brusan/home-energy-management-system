# Multi-Pod SignalR Improvements

## Overview

This document outlines the findings and required improvements for running the HEMS Backend in a multi-pod Kubernetes environment with SignalR real-time features.

---

## Current Status

### Frontend: READY

| Component | Status | Notes |
|-----------|--------|-------|
| `deviceStatusService.ts` | OK | `skipNegotiation: true` avoids sticky session issues |
| `powerFlowService.ts` | OK | `skipNegotiation: true` avoids sticky session issues |
| Auto-reconnect | OK | Both services have `withAutomaticReconnect()` |
| Re-subscription | OK | DeviceStatusService re-subscribes on reconnect |
| Stream restart | OK | Hook handles stream restart after reconnection |

### Backend: REQUIRES IMPROVEMENTS

| Component | Severity | Issue |
|-----------|----------|-------|
| SignalR Backplane | CRITICAL | No Redis backplane - messages won't reach clients on other pods |
| DeviceHeartbeatCache | HIGH | Singleton with in-memory state - each pod has different device status |
| SharedDeviceStreamManager | HIGH | Each pod runs duplicate polling loops |
| Hub Static Dictionaries | MEDIUM | Connection tracking is pod-local (cleanup issues) |
| ParameterUpdateBackgroundService | MEDIUM | Device locks are pod-local (race conditions possible) |
| IMemoryCache | MEDIUM | Cache misses across pods |

---

## Detailed Findings

### 1. Missing Redis Backplane (CRITICAL)

**File:** `src/Web/DependencyInjection.cs` (Line 41)

```csharp
builder.Services.AddSignalR();
```

**Issue:** SignalR uses default in-memory backplane. In multi-pod cluster, each pod maintains its own state. Clients on Pod A cannot receive messages sent by services on Pod B.

**Impact:**
- Messages sent to groups only reach clients on the same pod
- Device status change notifications may not reach all clients
- PowerFlow streaming may be disrupted during pod rollouts

---

### 2. Static In-Memory Hub State

**Files with static dictionaries:**

- `PowerFlowHub.cs` (Line 28): `_connectionStreams`
- `DeviceStatusHub.cs` (Line 27): `_connectionSubscriptions`
- `DeviceNotificationHub.cs` (Line 25): `_connectionSubscriptions`

**Issue:** These track per-connection subscription state but are pod-local only.

**Impact:** Subscription tracking and cleanup is pod-specific.

---

### 3. In-Memory Singleton Services

**DeviceHeartbeatCache** (`src/Infrastructure/Services/DeviceHeartbeatCache.cs`)

```csharp
private readonly ConcurrentDictionary<Guid, DeviceMonitoringState> _deviceStates;
```

**Issue:** Each pod maintains its own device heartbeat state.

**Impact:**
- Inconsistent device status across pods
- Possible duplicate notifications

**SharedDeviceStreamManager** (`src/Infrastructure/Services/SharedDeviceStreamManager.cs`)

```csharp
private readonly ConcurrentDictionary<Guid, SharedDeviceStream> _streams = new();
```

**Issue:** Each pod runs independent polling loops.

**Impact:**
- N pods × M devices = N×M polling loops instead of M
- Resource waste and potential API rate limiting

---

### 4. Parameter Update Locks

**ParameterUpdateBackgroundService** (`src/Infrastructure/Services/ParameterUpdateBackgroundService.cs`)

```csharp
private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _deviceLocks = new();
```

**Issue:** Device-level locking only works within a single pod.

**Impact:** Potential race conditions when multiple pods process updates to same device.

---

## Required Fixes

### 1. Add Redis Backplane (CRITICAL)

```bash
dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

```csharp
// src/Web/DependencyInjection.cs
var redisConnection = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("HEMS");
    });
```

### 2. Use Redis for DeviceHeartbeatCache

Replace in-memory `ConcurrentDictionary` with Redis-backed storage using `IDistributedCache` or direct Redis operations.

### 3. Coordinate SharedDeviceStreamManager

Options:
- Use Redis pub/sub for stream coordination (one pod polls, others receive via pub/sub)
- Use distributed lock to elect "stream leader" per device
- Accept duplicate polling (simpler, but wastes resources)

### 4. Use Distributed Cache

```csharp
// Replace AddMemoryCache:
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "HEMS:";
});
```

### 5. Distributed Locks for Parameter Updates

Use Redis-based distributed locks (e.g., RedLock pattern) for device parameter updates.

---

## Kubernetes Session Affinity (Temporary Workaround)

If Redis cannot be added immediately:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: hems-api
spec:
  sessionAffinity: ClientIP
  sessionAffinityConfig:
    clientIP:
      timeoutSeconds: 3600
```

Or in Ingress:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  annotations:
    nginx.ingress.kubernetes.io/affinity: "cookie"
    nginx.ingress.kubernetes.io/session-cookie-name: "HEMS_AFFINITY"
    nginx.ingress.kubernetes.io/session-cookie-max-age: "3600"
```

---

## Multi-Pod Behavior Analysis (Without Redis)

### Read-Only Operations (Heartbeat/PowerFlow)

These are informative features that push data to frontend without updates. In multi-pod without Redis:

| Scenario | What Happens | Severity |
|----------|--------------|----------|
| Client connects to Pod A | Works normally | OK |
| Pod A sends status update | Only clients on Pod A receive it | DATA LOSS |
| Client reconnects to Pod B | Gets different cached status | INCONSISTENT |
| Multiple pods poll same device | Duplicate API calls, wasted resources | PERFORMANCE |
| Pod restart | All in-memory state lost | DATA LOSS |

### No Crashes Expected

The system will NOT crash, but will exhibit:
- Inconsistent data between clients
- Missed real-time updates
- Resource waste from duplicate operations
- Potential API rate limiting from duplicate calls

---

## Implementation Priority

1. **Phase 1 (Critical):** Add Redis SignalR backplane
2. **Phase 2 (High):** Migrate DeviceHeartbeatCache to Redis
3. **Phase 3 (Medium):** Coordinate SharedDeviceStreamManager
4. **Phase 4 (Medium):** Add distributed locks for parameter updates
5. **Phase 5 (Low):** Migrate IMemoryCache to IDistributedCache

---

## File Locations Reference

- SignalR Configuration: `/src/Web/DependencyInjection.cs`
- Infrastructure Services: `/src/Infrastructure/DependencyInjection.cs`
- Hub Implementations:
  - `/src/Web/Hubs/PowerFlowHub.cs`
  - `/src/Web/Hubs/DeviceNotificationHub.cs`
  - `/src/Web/Hubs/DeviceSettingsHub.cs`
  - `/src/Infrastructure/Hubs/DeviceStatusHub.cs`
- Service Implementations:
  - `/src/Infrastructure/Services/DeviceHeartbeatCache.cs`
  - `/src/Infrastructure/Services/SharedDeviceStreamManager.cs`
  - `/src/Infrastructure/Services/ParameterUpdateBackgroundService.cs`
