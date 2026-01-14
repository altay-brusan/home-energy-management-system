# Device Status and Notifications System

This document describes how device online/offline status is tracked and how notifications are delivered to the frontend via SignalR.

## Overview

The HEMS backend uses a hybrid approach to track device connectivity:

1. **Immediate Detection**: Any telemetry data received from a device proves it's online
2. **Scheduled Detection**: A background service monitors for missing signals to detect disconnection

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEVICE SENDS TELEMETRY                       │
├─────────────────────────────────────────────────────────────────┤
│  POST /api/DeviceData/PowerFlow ────┐                           │
│  POST /api/DeviceData/BatterySoc ───┤                           │
│  POST /api/DeviceData/DailyEnergyFlow─┤  IDeviceActivityService │
│  POST /api/DeviceData/TotalEnergyFlow─┼──► RecordActivityAsync()│
│  POST /api/DeviceData/Heartbeat ────┤           │               │
│  POST /api/DeviceData/Alarm ────────┘           ▼               │
│                               ┌─────────────────────────────┐   │
│                               │ IDeviceHeartbeatCache       │   │
│                               │ (In-memory status tracking) │   │
│                               └──────────────┬──────────────┘   │
│                                              ▼                  │
│                               ┌─────────────────────────────┐   │
│                               │ SignalR DeviceStatusHub     │   │
│                               │ (Push to frontend)          │   │
│                               └─────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    BACKGROUND MONITORING                        │
├─────────────────────────────────────────────────────────────────┤
│  DeviceHeartbeatMonitorService (runs every 60 seconds)          │
│       │                                                         │
│       ▼                                                         │
│  Check all tracked devices for elapsed time since last activity │
│       │                                                         │
│       ├── 0-90s: Status = OK                                    │
│       ├── 90-150s: Status = PENDING (missed 1 window)           │
│       ├── 150-210s: Status = WAITING (missed 2 windows)         │
│       └── 210s+: Status = DISCONNECTED (missed 3+ windows)      │
│                    │                                            │
│                    ▼                                            │
│            SignalR notification to frontend                     │
│            + Create DeviceDisconnectLog in database             │
└─────────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. IDeviceActivityService

**Location**: `src/Application/Common/Interfaces/IDeviceActivityService.cs`

Centralized service for recording device activity from any telemetry source.

```csharp
public interface IDeviceActivityService
{
    Task<DeviceActivityResult> RecordActivityAsync(
        Guid thingsBoardDeviceId,
        TelemetrySource source,
        DateTime? timestamp = null,
        CancellationToken cancellationToken = default);
}
```

**Telemetry Sources**:
- `Heartbeat` - Dedicated heartbeat signal
- `PowerFlow` - Power flow telemetry
- `BatterySoc` - Battery state of charge
- `DailyEnergyFlow` - Daily cumulative energy
- `TotalEnergyFlow` - Lifetime energy
- `Alarm` - Alarm/warning telemetry

### 2. IDeviceHeartbeatCache

**Location**: `src/Application/Common/Interfaces/IDeviceHeartbeatCache.cs`

In-memory cache tracking device status using ThingsBoard device IDs.

**Device Status States**:
| Status | Elapsed Time | Description |
|--------|-------------|-------------|
| `OK` | 0-90s | Device is sending data on time |
| `PENDING` | 90-150s | Missed 1 heartbeat window |
| `WAITING` | 150-210s | Missed 2 heartbeat windows |
| `DISCONNECTED` | 210s+ | Device is offline |
| `NOT_TRACKED` | N/A | Device never sent any data |

### 3. DeviceStatusHub

**Location**: `src/Infrastructure/Hubs/DeviceStatusHub.cs`

SignalR hub for frontend subscriptions.

**Hub Methods**:
- `SubscribeToDevice(deviceId)` - Subscribe to device status updates
- `UnsubscribeFromDevice(deviceId)` - Unsubscribe from updates

**Client Events**:
- `DeviceStatusChanged` - Fired when device status changes

```json
{
  "deviceId": "local-device-uuid",
  "status": "OK|PENDING|WAITING|DISCONNECTED|NOT_TRACKED",
  "missedHeartbeats": 0,
  "timestamp": "2025-01-09T12:00:00Z",
  "source": "PowerFlow",
  "isRecovery": true
}
```

### 4. DeviceHeartbeatMonitorService

**Location**: `src/Infrastructure/Services/DeviceHeartbeatMonitorService.cs`

Background hosted service that runs every 60 seconds to:
- Check for devices that haven't sent data
- Transition device status based on elapsed time
- Send SignalR notifications for status changes
- Create `DeviceDisconnectLog` entries in database

## Data Flow

### Device Comes Online (Recovery)

```
1. Device sends any telemetry (PowerFlow, Heartbeat, etc.)
          │
          ▼
2. Endpoint receives telemetry, calls IDeviceActivityService
          │
          ▼
3. DeviceActivityService updates IDeviceHeartbeatCache
   - Sets LastActivity = now
   - Resets MissedHeartbeats = 0
   - Sets Status = OK
          │
          ▼
4. If status changed (was DISCONNECTED, now OK):
   - Send SignalR "DeviceStatusChanged" event
   - Update DeviceDisconnectLog with EndTime
          │
          ▼
5. Frontend receives real-time notification
```

### Device Goes Offline

```
1. Device stops sending data (WiFi lost, power off, etc.)
          │
          ▼
2. DeviceHeartbeatMonitorService runs (every 60s)
          │
          ▼
3. Checks elapsed time since LastActivity
          │
          ▼
4. Transitions status: OK → PENDING → WAITING → DISCONNECTED
          │
          ▼
5. For each transition:
   - Send SignalR "DeviceStatusChanged" event
   - On DISCONNECTED: Create DeviceDisconnectLog entry
          │
          ▼
6. Frontend receives status update notifications
```

## ID Mapping

The system uses two ID types:
- **Local Device ID**: `Device.Id` - Shown to frontend
- **ThingsBoard Device ID**: `Device.ThingsBoardDeviceId` - Internal for TB integration

`IDeviceMappingService` handles translation between these IDs with caching.

## Demo Devices

Demo devices (for app store assessment) bypass heartbeat monitoring:
- Always return status = "ONLINE"
- Use simulated data
- Checked via `IDemoDataService.IsDemoDeviceAsync()`

## Database Entities

### DeviceDisconnectLog

Records historical disconnect events:
- `ThingsBoardDeviceId` - Which device
- `StartTime` - When disconnection detected
- `EndTime` - When device recovered (null if still offline)
- `Duration` - Calculated disconnect duration
- `RecoveryType` - "Heartbeat", "PowerFlow", etc.

## API Endpoints

### Device Telemetry (update status on receive)

| Endpoint | Updates Status |
|----------|---------------|
| `POST /api/DeviceData/Heartbeat` | Yes |
| `POST /api/DeviceData/PowerFlow` | Yes |
| `POST /api/DeviceData/BatterySoc` | Yes |
| `POST /api/DeviceData/DailyEnergyFlow` | Yes |
| `POST /api/DeviceData/TotalEnergyFlow` | Yes |
| `POST /api/DeviceData/Alarm` | Yes |

### Status Query

| Endpoint | Description |
|----------|-------------|
| `GET /api/DeviceData/Status/{deviceId}` | Get current device status |

## Configuration

**Timing Configuration** (in `DeviceHeartbeatCache`):
- `HeartbeatIntervalSeconds = 60` - Expected heartbeat frequency
- `GracePeriodSeconds = 30` - Grace period for timing
- `StaleDeviceHours = 24` - Auto-remove disconnected devices after 24h

## Frontend Integration

### Subscribing to Device Status

```javascript
// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/deviceStatusHub")
    .build();

// Listen for status changes
connection.on("DeviceStatusChanged", (data) => {
    console.log(`Device ${data.deviceId}: ${data.status}`);
    if (data.isRecovery) {
        console.log("Device recovered!");
    }
});

// Subscribe to a device
await connection.invoke("SubscribeToDevice", deviceId);
```

### Status Display

| Status | UI Indicator | User Message |
|--------|-------------|--------------|
| `OK` | Green | "Online" |
| `PENDING` | Yellow | "Checking connection..." |
| `WAITING` | Orange | "Connection unstable" |
| `DISCONNECTED` | Red | "Offline" |
| `NOT_TRACKED` | Gray | "Never connected" |

## Troubleshooting

### Device shows offline but sending data

1. Check logs for activity recording errors
2. Verify device ID mapping exists in database
3. Check if device is marked as demo device

### SignalR notifications not received

1. Verify frontend is subscribed to correct device ID (local ID, not ThingsBoard ID)
2. Check SignalR connection is established
3. Verify user has access to the device's site

### Status transitions too slow

The background monitor runs every 60 seconds. Worst case:
- First missed detection: ~120 seconds (heartbeat interval + check interval)
- DISCONNECTED transition: ~210 seconds from last data
