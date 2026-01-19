# SignalR Frontend Updates Documentation

This document describes the SignalR implementation in the HEMS mobile app, recent updates made, and how the backend SignalR hubs work.

---

## Table of Contents

1. [Overview](#overview)
2. [Backend SignalR Hubs](#backend-signalr-hubs)
3. [Frontend SignalR Services](#frontend-signalr-services)
4. [Recent Updates](#recent-updates)
5. [Connection Flow](#connection-flow)
6. [Reconnection Handling](#reconnection-handling)
7. [Usage Examples](#usage-examples)

---

## Overview

The HEMS app uses SignalR for real-time communication with the backend for two main features:

| Feature | Hub | Service | Hook |
|---------|-----|---------|------|
| Device Status | `/deviceStatusHub` | `deviceStatusService.ts` | `useDeviceStatus.ts` |
| Power Flow | `/powerFlowHub` | `powerFlowService.ts` | `usePowerFlowStream.ts` |

**Backend URL:** `https://hems-api-dev.oklidis.com.tr`

---

## Backend SignalR Hubs

### 1. DeviceStatusHub (`/deviceStatusHub`)

Provides real-time device online/offline status updates.

#### Methods (Client → Server)

| Method | Parameters | Description |
|--------|------------|-------------|
| `SubscribeToDevice` | `deviceId: string` | Subscribe to status updates for a device |
| `UnsubscribeFromDevice` | `deviceId: string` | Unsubscribe from device status updates |

#### Events (Server → Client)

| Event | Payload | Description |
|-------|---------|-------------|
| `DeviceStatusChanged` | `DeviceStatusChangedDto` | Fired when device status changes |
| `DeviceStatusError` | `{ errorCode, message, timestamp }` | Fired on subscription errors |

#### DeviceStatusChangedDto

```typescript
interface DeviceStatusChangedDto {
  deviceId: string;
  status: 'OK' | 'PENDING' | 'WAITING' | 'DISCONNECTED' | 'NOT_TRACKED';
  missedHeartbeats: number;
  timestamp: string;
  isInitialStatus?: boolean;
}
```

#### Status Values

| Status | Meaning |
|--------|---------|
| `OK` | Device is online and sending heartbeats |
| `PENDING` | Device missed 1-2 heartbeats |
| `WAITING` | Device missed 3-4 heartbeats |
| `DISCONNECTED` | Device missed 5+ heartbeats |
| `NOT_TRACKED` | Device not being monitored |

---

### 2. PowerFlowHub (`/powerFlowHub`)

Provides real-time power flow data streaming from devices.

#### Methods (Client → Server)

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `StreamPowerFlow` | `deviceId: string, intervalSeconds: number` | `IAsyncEnumerable<PowerFlowDto>` | Start streaming power flow data |

#### Events (Server → Client)

| Event | Payload | Description |
|-------|---------|-------------|
| `PowerFlowError` | `{ errorCode, message, deviceId, timestamp }` | Fired on stream errors |

#### PowerFlowDto

```typescript
interface PowerFlowDto {
  deviceId: string;
  solarPowerKw: number;
  batteryPowerKw: number;
  gridPowerKw: number;
  loadPowerKw: number;
  batterySoc: number;
  timestamp: string;
}
```

---

## Frontend SignalR Services

### deviceStatusService.ts

Singleton service managing DeviceStatusHub connection.

```typescript
import { deviceStatusService } from '../services/deviceStatusService';

// Connect and subscribe
await deviceStatusService.connect();
await deviceStatusService.subscribeToDevice(deviceId);

// Listen for status changes
const unsubscribe = deviceStatusService.addStatusListener((data) => {
  console.log('Status:', data.status);
});

// Listen for connection state changes
const unsubscribe2 = deviceStatusService.addConnectionStateListener((state) => {
  console.log('Connection:', state); // 'Connected' | 'Connecting' | 'Reconnecting' | 'Disconnected' | 'Failed'
});

// Cleanup
await deviceStatusService.unsubscribeFromDevice();
await deviceStatusService.disconnect();
```

### powerFlowService.ts

Singleton service managing PowerFlowHub connection and streaming.

```typescript
import { powerFlowService } from '../services/powerFlowService';

// Start streaming
const stopStream = await powerFlowService.startStream(
  deviceId,
  5, // interval in seconds
  (data) => console.log('Power:', data), // onData
  (error) => console.error('Error:', error), // onError
  () => console.log('Stream completed') // onComplete
);

// Stop streaming
stopStream();
// or
powerFlowService.stopStream(deviceId);

// Check status
console.log(powerFlowService.isConnected);
console.log(powerFlowService.connectionState);
console.log(powerFlowService.activeStreamCount);
```

---

## Recent Updates

### 1. Skip Negotiation for Multi-Pod Compatibility

**Files Changed:**
- `src/services/deviceStatusService.ts`
- `src/services/powerFlowService.ts`

**Change:**
```typescript
// BEFORE (caused 404 errors in multi-pod environment)
this.connection = new HubConnectionBuilder()
    .withUrl(this.HUB_URL, {
        accessTokenFactory: () => getToken(),
    })

// AFTER (works in multi-pod environment)
this.connection = new HubConnectionBuilder()
    .withUrl(this.HUB_URL, {
        accessTokenFactory: () => getToken(),
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
    })
```

**Why:** In a multi-pod Kubernetes environment, the negotiate request might go to Pod A while the WebSocket connection goes to Pod B. Pod B doesn't know the connectionId from Pod A, causing 404 errors. `skipNegotiation: true` bypasses this by connecting directly via WebSocket.

---

### 2. Reconnection Handling in usePowerFlowStream Hook

**File Changed:** `src/hooks/usePowerFlowStream.ts`

**Change:** Added automatic stream restart after SignalR reconnection.

```typescript
// Track previous connection state for reconnection detection
const prevConnectionStateRef = useRef<SignalRConnectionState>('Disconnected');

useEffect(() => {
    const unsubscribe = powerFlowService.addConnectionStateListener((state) => {
        const prevState = prevConnectionStateRef.current;
        setConnectionState(state);
        prevConnectionStateRef.current = state;

        // Handle reconnection: restart stream if we were reconnecting and now connected
        if (prevState === 'Reconnecting' && state === 'Connected') {
            console.log('[usePowerFlowStream] Reconnection detected, restarting stream');
            isStreamingRef.current = false;
            if (deviceId && enabled && isAppActive.current) {
                startStream();
            }
        }

        // Handle disconnection: reset streaming state
        if (state === 'Disconnected') {
            isStreamingRef.current = false;
            setStreamState('idle');
        }
    });
    return unsubscribe;
}, [deviceId, enabled, startStream]);
```

**Why:** When SignalR reconnects, the server-side stream subscription is lost. The hook now detects reconnection and automatically restarts the stream.

---

### 3. Stale Connection Cleanup in PowerFlowService

**File Changed:** `src/services/powerFlowService.ts`

**Change:** Added cleanup of stale connections before creating new ones.

```typescript
// Clean up any existing stale connection before creating a new one
if (this.connection) {
    try {
        await this.connection.stop();
    } catch (e) {
        // Ignore cleanup errors
    }
    this.connection = null;
}
```

**Why:** Prevents connection leaks and ensures fresh state on reconnection attempts.

---

### 4. Fixed Error Handling in PowerFlowService

**File Changed:** `src/services/powerFlowService.ts`

**Change:** Set `connection = null` on connection failure.

```typescript
} catch (error: any) {
    this.isConnecting = false;
    // Clean up failed connection to ensure fresh state on retry
    this.connection = null;  // <-- Added this line
    this.notifyConnectionState('Failed');
    throw error;
}
```

**Why:** Ensures the service can retry connection properly after a failure.

---

## Connection Flow

### DeviceStatusHub Connection Flow

```
1. App starts
   └── useDeviceStatus hook mounts

2. Hook calls deviceStatusService.connect()
   └── Service creates HubConnection with skipNegotiation
   └── Service calls connection.start()
   └── WebSocket connects directly to /deviceStatusHub

3. Hook calls deviceStatusService.subscribeToDevice(deviceId)
   └── Service invokes 'SubscribeToDevice' on hub
   └── Server adds connection to device group
   └── Server sends initial status via 'DeviceStatusChanged' event

4. Server detects status change
   └── Server broadcasts to device group
   └── Client receives 'DeviceStatusChanged' event
   └── Service notifies all status listeners
   └── Hook updates state, component re-renders
```

### PowerFlowHub Connection Flow

```
1. Component using usePowerFlowStream mounts

2. Hook calls powerFlowService.startStream(deviceId, interval, callbacks)
   └── Service calls ensureConnected()
   └── Service creates HubConnection with skipNegotiation
   └── WebSocket connects directly to /powerFlowHub

3. Service calls connection.stream('StreamPowerFlow', deviceId, interval)
   └── Server starts polling device for power data
   └── Server yields PowerFlowDto every {interval} seconds

4. Client receives stream data
   └── Service calls onData callback
   └── Hook updates powerFlow state
   └── Component re-renders with new data

5. Component unmounts or stream stops
   └── Hook calls stopStream()
   └── Service disposes stream subscription
   └── If no active streams, service disconnects
```

---

## Reconnection Handling

### Automatic Reconnection (Built-in)

Both services use `withAutomaticReconnect()`:

```typescript
.withAutomaticReconnect({
    nextRetryDelayInMilliseconds: (retryContext) => {
        // Exponential backoff: 1s, 2s, 4s, 8s, ... max 30s
        const delay = Math.min(
            1000 * Math.pow(2, retryContext.previousRetryCount),
            30000
        );
        return delay;
    }
})
```

### Re-subscription After Reconnect

**DeviceStatusService:** Automatically re-subscribes to the device after reconnection.

```typescript
this.connection.onreconnected(async (_connectionId) => {
    if (this.currentDeviceId) {
        const deviceId = this.currentDeviceId;
        this.currentDeviceId = null; // Reset to allow re-subscribe
        await this.subscribeToDevice(deviceId);
    }
});
```

**usePowerFlowStream Hook:** Automatically restarts stream after reconnection.

```typescript
if (prevState === 'Reconnecting' && state === 'Connected') {
    isStreamingRef.current = false;
    if (deviceId && enabled && isAppActive.current) {
        startStream();
    }
}
```

---

## Usage Examples

### Using useDeviceStatus Hook

```tsx
import { useDeviceStatus } from '../hooks/useDeviceStatus';

const DeviceStatusBadge = ({ deviceId }: { deviceId: string }) => {
    const {
        deviceStatus,
        connectionState,
        isConnected,
        missedHeartbeats,
    } = useDeviceStatus(deviceId);

    if (!isConnected) {
        return <Text>Connecting...</Text>;
    }

    return (
        <View>
            <Text>Status: {deviceStatus}</Text>
            <Text>Missed Heartbeats: {missedHeartbeats}</Text>
        </View>
    );
};
```

### Using usePowerFlowStream Hook

```tsx
import { usePowerFlowStream } from '../hooks/usePowerFlowStream';

const PowerFlowDisplay = ({ deviceId }: { deviceId: string }) => {
    const {
        powerFlow,
        isStreaming,
        connectionState,
        error,
    } = usePowerFlowStream({
        deviceId,
        intervalSeconds: 5,
        enabled: true,
    });

    if (error) {
        return <Text>Error: {error.message}</Text>;
    }

    if (!powerFlow) {
        return <Text>Loading power data...</Text>;
    }

    return (
        <View>
            <Text>Solar: {powerFlow.solarPowerKw} kW</Text>
            <Text>Battery: {powerFlow.batteryPowerKw} kW</Text>
            <Text>Grid: {powerFlow.gridPowerKw} kW</Text>
            <Text>Load: {powerFlow.loadPowerKw} kW</Text>
            <Text>SOC: {powerFlow.batterySoc}%</Text>
        </View>
    );
};
```

---

## Troubleshooting

### Connection Fails with 404

**Cause:** Multi-pod environment without sticky sessions.

**Solution:** Ensure `skipNegotiation: true` and `transport: HttpTransportType.WebSockets` are set.

### Stream Stops After Reconnection

**Cause:** Hook not detecting reconnection.

**Solution:** The `usePowerFlowStream` hook now handles this automatically.

### Status Shows Wrong Value

**Cause:** Backend sends status as string enum.

**Solution:** Ensure frontend expects: `'OK' | 'PENDING' | 'WAITING' | 'DISCONNECTED' | 'NOT_TRACKED'`

### Connection Drops Frequently

**Cause:** Server timeout or network issues.

**Solution:**
- Check `withServerTimeout()` value (currently 120000ms = 2 minutes)
- Check `withKeepAliveInterval()` value (currently 15000ms = 15 seconds)

---

## File Reference

| File | Purpose |
|------|---------|
| `src/services/deviceStatusService.ts` | DeviceStatusHub connection management |
| `src/services/powerFlowService.ts` | PowerFlowHub connection and streaming |
| `src/hooks/useDeviceStatus.ts` | React hook for device status |
| `src/hooks/usePowerFlowStream.ts` | React hook for power flow streaming |
| `src/api/types/signalR.types.ts` | TypeScript type definitions |

---

## Summary of Changes Made

| Change | File | Reason |
|--------|------|--------|
| Added `skipNegotiation: true` | Both services | Multi-pod compatibility |
| Added `transport: WebSockets` | Both services | Required for skipNegotiation |
| Added reconnection detection | `usePowerFlowStream.ts` | Auto-restart streams |
| Added stale connection cleanup | `powerFlowService.ts` | Prevent connection leaks |
| Fixed error handling | `powerFlowService.ts` | Clean state on failure |
| Removed forced LongPolling | `deviceStatusService.ts` | Was causing 404 errors |
