# Grid Outage Detection System

This document provides comprehensive documentation for the Grid Outage Detection system, which monitors grid connection status via the EC-100 gateway and sends notifications to users on state transitions.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [State Machine](#state-machine)
4. [API Endpoint](#api-endpoint)
5. [SignalR Notifications](#signalr-notifications)
6. [Notification Types](#notification-types)
7. [Configuration](#configuration)
8. [Testing](#testing)

---

## Overview

The Grid Outage Detection system monitors the electrical grid connection status for each device and notifies users when:

1. **Grid power is lost** (outage detected) - GRD-WAR-W33 notification
2. **Grid power is restored** - GRD-WAR-W34 notification

### Key Features

- **In-memory state tracking** using `ConcurrentDictionary` for thread-safe, low-latency updates
- **Event-driven notifications** - only sent on state transitions (not repeated during ongoing outage)
- **RPC fallback monitoring** - checks devices via RPC if no telemetry received for 90 seconds
- **Demo device support** - demo devices are skipped for notifications

### Architecture Diagram

```
+------------------------------------------------------------------+
|                    GRID STATUS TELEMETRY FLOW                     |
+------------------------------------------------------------------+
|                                                                   |
|  EC-100 Gateway (monitors phase voltages)                         |
|       |                                                           |
|       v                                                           |
|  POST /api/DeviceData/GridStatus                                  |
|       |                                                           |
|       v                                                           |
|  +-------------------------------------------------------+        |
|  | ProcessGridStatusTelemetryCommand                     |        |
|  |   - Parse JSON payload (deviceId, voltages, flag)     |        |
|  |   - Record device activity                            |        |
|  |   - Skip demo devices                                 |        |
|  +-------------------------------------------------------+        |
|       |                                                           |
|       v                                                           |
|  +-------------------------------------------------------+        |
|  | GridStatusTrackingService (Singleton)                 |        |
|  |   - ConcurrentDictionary<Guid, GridStatusState>       |        |
|  |   - UpdateGridStatus() - determines if notification   |        |
|  |     needed based on state transition                  |        |
|  +-------------------------------------------------------+        |
|       |                                                           |
|       v (if state changed)                                        |
|  +-------------------------------------------------------+        |
|  | AlarmNotificationSender                               |        |
|  |   - Create DeviceNotification record                  |        |
|  |   - Send SignalR to Site_{siteId} group               |        |
|  |   - Send SignalR to Device_{deviceId} group           |        |
|  +-------------------------------------------------------+        |
|       |                                                           |
|       v                                                           |
|  Frontend App (React Native)                                      |
|                                                                   |
+------------------------------------------------------------------+

+------------------------------------------------------------------+
|                    RPC FALLBACK MONITORING                        |
+------------------------------------------------------------------+
|                                                                   |
|  GridStatusMonitorService (BackgroundService)                     |
|       | (runs every 60 seconds)                                   |
|       v                                                           |
|  GetDevicesNeedingRpcFallback(90 seconds)                         |
|       | (devices in Disconnected state with no message for 90s)   |
|       v                                                           |
|  SendTwoWayRpcAsync("getGridConnectionStatusState")               |
|       |                                                           |
|       v                                                           |
|  UpdateGridStatus() -> Send notification if grid restored         |
|                                                                   |
+------------------------------------------------------------------+
```

---

## Architecture

### Components

| Component | Type | Description |
|-----------|------|-------------|
| `IGridStatusTrackingService` | Interface | Defines grid status tracking contract |
| `GridStatusTrackingService` | Singleton | In-memory state tracking with ConcurrentDictionary |
| `ProcessGridStatusTelemetryCommand` | MediatR Command | Handles incoming telemetry from gateway |
| `GridStatusMonitorService` | BackgroundService | RPC fallback checker (every 60 seconds) |

### Data Classes

#### GridConnectionStatus (Enum)

```csharp
public enum GridConnectionStatus
{
    Connected,      // Grid is connected (any phase voltage > 150V)
    Disconnected,   // Grid is disconnected (all phase voltages <= 150V)
    Unknown         // Status is unknown (no data received yet)
}
```

#### GridStatusState

```csharp
public class GridStatusState
{
    public Guid ThingsBoardDeviceId { get; set; }
    public GridConnectionStatus Status { get; set; }
    public DateTime LastStatusMessageTime { get; set; }
    public DateTime? OutageStartTime { get; set; }
    public double? PhaseAVoltageV { get; set; }
    public double? PhaseBVoltageV { get; set; }
    public double? PhaseCVoltageV { get; set; }
    public bool? GridConnectedFlag { get; set; }
}
```

#### GridStatusUpdateResult

```csharp
public class GridStatusUpdateResult
{
    public bool Success { get; set; }
    public GridConnectionStatus PreviousStatus { get; set; }
    public GridConnectionStatus CurrentStatus { get; set; }
    public bool StatusChanged { get; set; }
    public bool ShouldSendNotification { get; set; }
    public string? NotificationCode { get; set; }  // "GRD-WAR-W33" or "GRD-WAR-W34"
    public TimeSpan? OutageDuration { get; set; }  // Set when grid is restored
}
```

---

## State Machine

### State Transitions

```
                         First message
                         gridConnected=true
    +----------+         (no notification)        +------------+
    |          | ------------------------------>  |            |
    | Unknown  |                                  | Connected  |
    |          | ------------------------------>  |            |
    +----------+         First message            +------------+
         |               gridConnected=false           |    ^
         |               (send GRD-WAR-W33)            |    |
         |                                             |    |
         |                    +-------------+          |    |
         +------------------> |             | <--------+    |
           First message      | Disconnected|  gridConnected=false
           gridConnected=false|             |  (send GRD-WAR-W33)
           (send GRD-WAR-W33) +-------------+
                                    |
                                    | gridConnected=true
                                    | (send GRD-WAR-W34)
                                    v
                              +------------+
                              | Connected  |
                              +------------+
```

### Transition Table

| Previous State | Grid Connected | New State | Notification |
|----------------|----------------|-----------|--------------|
| Unknown | `false` | Disconnected | GRD-WAR-W33 (Outage) |
| Unknown | `true` | Connected | None |
| Connected | `false` | Disconnected | GRD-WAR-W33 (Outage) |
| Connected | `true` | Connected | None |
| Disconnected | `true` | Connected | GRD-WAR-W34 (Restored) |
| Disconnected | `false` | Disconnected | None |

### Key Design Decisions

1. **Default state is Unknown** - Devices are not tracked until first telemetry message
2. **No notification on first Connected** - If grid is fine on first message, no alert needed
3. **Notification on first Disconnected** - User should know about outage even on first message
4. **RPC fallback only for Disconnected** - Only check devices that are in outage mode

---

## API Endpoint

### POST /api/DeviceData/GridStatus

Receives grid status telemetry from the EC-100 gateway.

#### Request Format

```json
{
  "deviceId": "70f09130-ed61-11f0-a84b-2f34a5f564b4",
  "data": {
    "version": 1,
    "phaseAVoltageV": 230.5,
    "phaseBVoltageV": 231.0,
    "phaseCVoltageV": 229.8,
    "gridConnected": true,
    "timestampUtc": "2024-01-15T10:30:00Z"
  }
}
```

#### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `deviceId` | string (UUID) | Yes | ThingsBoard device ID |
| `data.version` | int | No | Message version (currently 1) |
| `data.phaseAVoltageV` | double | No | Phase A voltage in volts |
| `data.phaseBVoltageV` | double | No | Phase B voltage in volts |
| `data.phaseCVoltageV` | double | No | Phase C voltage in volts |
| `data.gridConnected` | boolean | Yes | Grid connection status flag |
| `data.timestampUtc` | string (ISO 8601) | No | Timestamp (defaults to server UTC time) |

#### Response Format

```json
{
  "message": "Grid status changed to Disconnected",
  "deviceId": "70f09130-ed61-11f0-a84b-2f34a5f564b4",
  "gridConnected": false,
  "statusChanged": true,
  "notificationSent": true
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `message` | string | Human-readable status message |
| `deviceId` | UUID | ThingsBoard device ID |
| `gridConnected` | boolean | Current grid connection status |
| `statusChanged` | boolean | Whether status changed with this message |
| `notificationSent` | boolean | Whether a notification was created and sent |

#### Error Responses

| Status Code | Error | Description |
|-------------|-------|-------------|
| 400 | Missing 'deviceId' | deviceId field not present in request |
| 400 | Invalid 'deviceId' format | deviceId is not a valid UUID |
| 400 | Missing 'data' object | data field not present or not an object |

---

## SignalR Notifications

Grid status notifications are sent via the existing `DeviceNotificationHub` to:

- `Site_{siteId}` - All devices for the site
- `Device_{deviceId}` - Specific device

### Notification Payload

```json
{
  "id": "notification-uuid",
  "deviceId": "device-uuid",
  "siteId": "site-uuid",
  "setupDeviceId": "setup-device-uuid",
  "deviceNotificationTypeId": "type-uuid",
  "timestamp": "2024-01-15T10:30:00Z",
  "additionalData": "{...}",
  "isProcessed": false,
  "created": "2024-01-15T10:30:00Z",
  "deviceNotificationType": {
    "id": "type-uuid",
    "code": "GRD-WAR-W33",
    "message": "Grid Power Outage Detected",
    "description": "The system has detected a loss of electricity from the grid...",
    "category": "grid",
    "severity": "WARNING",
    "version": "1.0",
    "manufacturer": "LOFTTY",
    "isActive": true
  },
  "key": "GRD-WAR-W33",
  "displayMessage": "Grid Power Outage Detected",
  "severity": "WARNING",
  "detailedDescription": "The system has detected a loss of electricity from the grid..."
}
```

### AdditionalData JSON Schema

```json
{
  "gridStatus": "Disconnected",
  "phaseAVoltageV": 0,
  "phaseBVoltageV": 0,
  "phaseCVoltageV": 0,
  "timestamp": "2024-01-15T10:30:00.000Z",
  "outageDurationSeconds": 3600,
  "outageDurationFormatted": "1h 0m 0s",
  "source": "TELEMETRY"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `gridStatus` | string | "Connected" or "Disconnected" |
| `phaseAVoltageV` | double | Phase A voltage at time of notification |
| `phaseBVoltageV` | double | Phase B voltage at time of notification |
| `phaseCVoltageV` | double | Phase C voltage at time of notification |
| `timestamp` | string | ISO 8601 timestamp |
| `outageDurationSeconds` | double | (Restored only) Duration in seconds |
| `outageDurationFormatted` | string | (Restored only) Human-readable duration |
| `source` | string | "TELEMETRY" or "RPC_FALLBACK" |

---

## Notification Types

### W33: Grid Power Outage Detected

| Property | Value |
|----------|-------|
| Code | `GRD-WAR-W33` |
| Message | Grid Power Outage Detected |
| Description | The system has detected a loss of electricity from the grid. Your home is now operating on battery/solar power. |
| Category | grid |
| Severity | WARNING |
| Manufacturer | LOFTTY |

**Triggered when:** Grid connection status changes from Connected to Disconnected, OR first message shows Disconnected.

### W34: Grid Connection Restored

| Property | Value |
|----------|-------|
| Code | `GRD-WAR-W34` |
| Message | Grid Connection Restored |
| Description | The electricity supply from the grid has been restored. Normal operation resumed. |
| Category | grid |
| Severity | WARNING |
| Manufacturer | LOFTTY |

**Triggered when:** Grid connection status changes from Disconnected to Connected.

---

## Configuration

### Service Registration (DependencyInjection.cs)

```csharp
// Grid Status Tracking Services
builder.Services.AddSingleton<IGridStatusTrackingService, GridStatusTrackingService>();
builder.Services.AddHostedService<GridStatusMonitorService>();
```

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| RPC Fallback Timeout | 90 seconds | Time before RPC check for devices in outage |
| Monitor Interval | 60 seconds | How often GridStatusMonitorService runs |
| RPC Timeout | 10,000 ms | Timeout for RPC calls to device |

### TelemetrySource Enum

A new enum value was added:

```csharp
public enum TelemetrySource
{
    Heartbeat,
    PowerFlow,
    BatterySoc,
    BatteryEnergyFlow,
    DailyEnergyFlow,
    TotalEnergyFlow,
    Alarm,
    GridStatus,  // NEW
    Unknown
}
```

---

## Testing

### Manual Testing

#### Test 1: Outage Detection (First Message)

```bash
curl -X POST http://localhost:5000/api/DeviceData/GridStatus \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "your-device-id",
    "data": {
      "gridConnected": false,
      "phaseAVoltageV": 0,
      "phaseBVoltageV": 0,
      "phaseCVoltageV": 0
    }
  }'
```

**Expected:** GRD-WAR-W33 notification created and sent via SignalR.

#### Test 2: No Duplicate Notification

Send the same request again:

**Expected:** No new notification (status unchanged).

#### Test 3: Grid Restored

```bash
curl -X POST http://localhost:5000/api/DeviceData/GridStatus \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "your-device-id",
    "data": {
      "gridConnected": true,
      "phaseAVoltageV": 230.5,
      "phaseBVoltageV": 231.0,
      "phaseCVoltageV": 229.8
    }
  }'
```

**Expected:** GRD-WAR-W34 notification created with outage duration in AdditionalData.

#### Test 4: First Message Connected

Use a new device ID:

```bash
curl -X POST http://localhost:5000/api/DeviceData/GridStatus \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "new-device-id",
    "data": {
      "gridConnected": true,
      "phaseAVoltageV": 230.0,
      "phaseBVoltageV": 230.0,
      "phaseCVoltageV": 230.0
    }
  }'
```

**Expected:** No notification (grid is fine on first message).

### SQL Script

Run the seed script to add notification types:

```bash
psql -U username -d database -f scripts/seed_device_notification_types.sql
```

---

## Related Documentation

- [Device Notifications System](./DeviceNotifications_System.md) - Core notification system
- [Device Status and Notifications](./DeviceStatus_And_Notifications.md) - Device online/offline tracking
- [SignalR Hubs](./DeviceSettingsSignalR.md) - SignalR configuration

---

## Changelog

| Date | Version | Changes |
|------|---------|---------|
| 2026-02-02 | 1.0 | Initial implementation with telemetry endpoint and RPC fallback |
