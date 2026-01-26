# Device Notifications System

This document provides comprehensive documentation for the Device Notifications system, including database schema, business logic, API endpoints, and real-time SignalR integration.

## Table of Contents

1. [Overview](#overview)
2. [Database Schema](#database-schema)
3. [Business Logic](#business-logic)
4. [API Endpoints](#api-endpoints)
5. [SignalR Integration](#signalr-integration)
6. [Notification Types Reference](#notification-types-reference)

---

## Overview

The Device Notifications system processes alarm and warning telemetry from IoT devices (inverters, batteries) and:

1. **Parses raw register values** to extract warning/fault codes
2. **Maps codes to notification types** from the database
3. **Stores notifications** with full diagnostic data
4. **Sends real-time alerts** via SignalR to connected clients

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         DEVICE ALARM FLOW                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  IoT Device (Inverter/Battery)                                          │
│       │                                                                 │
│       ▼                                                                 │
│  Node-RED / Gateway                                                     │
│       │                                                                 │
│       ▼                                                                 │
│  POST /api/DeviceData/Alarm                                             │
│       │                                                                 │
│       ▼                                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ ProcessAlarmTelemetryCommand                                    │    │
│  │   - Parse JSON payload                                          │    │
│  │   - Map ThingsBoard ID to internal Device ID                    │    │
│  │   - Extract "kind" field (InverterAlarmState/BatteryAlarmState) │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│       │                                                                 │
│       ▼                                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ ProcessDeviceAlarmCommand                                       │    │
│  │   - Extract codes from raw registers (bit mapping)              │    │
│  │   - Lookup notification types by Code + Category                │    │
│  │   - Create DeviceNotification records                           │    │
│  │   - Store raw JSON in AdditionalData                            │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│       │                                                                 │
│       ▼                                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ AlarmNotificationSender                                         │    │
│  │   - Send SignalR notifications to Site_{siteId} group           │    │
│  │   - Send SignalR notifications to Device_{deviceId} group       │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│       │                                                                 │
│       ▼                                                                 │
│  Frontend App (React Native)                                            │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Database Schema

### DeviceNotificationTypes Table

Stores the catalog of all possible notification types (warnings, errors, faults).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | PK | Primary key |
| `Code` | VARCHAR(50) | UNIQUE, NOT NULL | Vendor code (e.g., "W2", "F1", "F64") |
| `Message` | VARCHAR(200) | NOT NULL | Short human-readable message |
| `Description` | VARCHAR(1000) | NOT NULL | Detailed troubleshooting steps |
| `Category` | VARCHAR(50) | NOT NULL, INDEX | Device category: "Inverter" or "Battery" |
| `Severity` | VARCHAR(20) | NOT NULL, INDEX | "Warning", "Error", "ALARM", "WARNING", "ERROR" |
| `Version` | VARCHAR(20) | NOT NULL | Schema version (e.g., "1.0") |
| `Manufacturer` | VARCHAR(100) | NOT NULL | Device manufacturer (e.g., "LOFTTY", "Generic") |
| `IsActive` | BOOLEAN | NOT NULL, DEFAULT true, INDEX | Whether this type is actively used |
| `Created` | TIMESTAMP | NOT NULL | Created timestamp |
| `CreatedBy` | VARCHAR | NOT NULL | Created by user |
| `LastModified` | TIMESTAMP | NOT NULL | Last modified timestamp |
| `LastModifiedBy` | VARCHAR | NOT NULL | Last modified by user |

**Indexes:**
- `IX_DeviceNotificationTypes_Code` (UNIQUE)
- `IX_DeviceNotificationTypes_Category`
- `IX_DeviceNotificationTypes_Severity`
- `IX_DeviceNotificationTypes_IsActive`

### DeviceNotifications Table

Stores individual notification instances received from devices.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | PK | Primary key |
| `DeviceId` | UUID | FK, NOT NULL | Reference to Device |
| `SiteId` | UUID | FK, NOT NULL | Reference to Site |
| `SetupDeviceId` | UUID | FK, NOT NULL | Reference to SetupDevice |
| `DeviceNotificationTypeId` | UUID | FK, NOT NULL | Reference to DeviceNotificationType |
| `Timestamp` | TIMESTAMPTZ | NOT NULL | When the notification occurred |
| `AdditionalData` | TEXT | NOT NULL | JSON payload with raw data |
| `IsProcessed` | BOOLEAN | NOT NULL, DEFAULT false | Whether user has acknowledged |
| `IsDeleted` | BOOLEAN | NOT NULL, DEFAULT false | Soft delete flag |
| `Created` | TIMESTAMP | NOT NULL | Created timestamp |
| `CreatedBy` | VARCHAR | | Created by user |
| `LastModified` | TIMESTAMP | | Last modified timestamp |
| `LastModifiedBy` | VARCHAR | | Last modified by user |

**Foreign Keys:**
- `DeviceId` → `Devices(Id)`
- `SiteId` → `Sites(Id)`
- `SetupDeviceId` → `SetupDevices(Id)`
- `DeviceNotificationTypeId` → `DeviceNotificationTypes(Id)`

### AdditionalData JSON Schema

The `AdditionalData` field stores the complete diagnostic information:

```json
{
  "kind": "InverterAlarmState",
  "extractedCode": "W31",
  "severity": "Warning",
  "bitPosition": 14,
  "registerValue": 16384,
  "isGenericFallback": false,
  "notificationTypeCode": "W31",
  "gatewayId": "EC100-GW-001",
  "serialNumber": "INV-12345",
  "version": 1,
  "registers": {
    "runStateRaw": 3,
    "commBoardFaultRaw": 0,
    "warningWord1Raw": 0,
    "warningWord2Raw": 16384,
    "faultWord1Raw": 0,
    "faultWord2Raw": 0,
    "faultWord3Raw": 0,
    "faultWord4Raw": 0,
    "workFlagTripData": 0
  },
  "rawPayload": "{...original JSON...}"
}
```

---

## Business Logic

### Telemetry Input Format

The system receives alarm telemetry via `POST /api/DeviceData/Alarm`:

```json
{
  "deviceId": "70f09130-ed61-11f0-a84b-2f34a5f564b4",
  "deviceName": "DEMO-NL0RS3P12S0000",
  "deviceType": "default",
  "deviceProfile": "default",
  "ts": 1768466361077,
  "kind": "InverterAlarmState",
  "data": {
    "kind": "InverterAlarmState",
    "timestampUtc": "2026-01-15T08:39:21.077Z",
    "version": 1,
    "gatewayId": "EC100-GW-001",
    "inverterSerialNumber": "INV-12345",
    "runStateRaw": 3,
    "commBoardFaultRaw": 0,
    "warningWord1Raw": 0,
    "warningWord2Raw": 16384,
    "faultWord1Raw": 0,
    "faultWord2Raw": 0,
    "faultWord3Raw": 0,
    "faultWord4Raw": 0,
    "workFlagTripData": 0,
    "activeAlarms": [...]
  }
}
```

### Kind Field - Category Determination

The `kind` field determines the device category for notification type lookup:

| Kind Value | Category | Description |
|------------|----------|-------------|
| `InverterAlarmState` | Inverter | Solar inverter warnings/faults |
| `BatteryAlarmState` | Battery | Battery/BMS warnings/faults |
| `BMSAlarmState` | Battery | Battery Management System |
| (default) | Inverter | Backward compatibility |

**TODO:** In the future, this should also consider device type and firmware version for more specific handling.

### Register Bit Mapping

#### Warning Registers (W1-W32)

| Register | Bits | Warning Codes | Formula |
|----------|------|---------------|---------|
| `warningWord1Raw` | 0-15 | W1-W16 | W{bit+1} |
| `warningWord2Raw` | 0-15 | W17-W32 | W{17+bit} |

**Example:** `warningWord2Raw = 16384` (binary: 0100000000000000)
- Bit 14 is set (2^14 = 16384)
- Code = W{17+14} = W31 (Battery communication warning)

#### Fault Registers (F1-F64)

| Register | Bits | Fault Codes | Formula |
|----------|------|-------------|---------|
| `faultWord1Raw` | 0-15 | F1-F16 | F{1+bit} |
| `faultWord2Raw` | 0-15 | F17-F32 | F{17+bit} |
| `faultWord3Raw` | 0-15 | F33-F48 | F{33+bit} |
| `faultWord4Raw` | 0-15 | F49-F64 | F{49+bit} |

**Example:** `faultWord3Raw = 2048` (binary: 0000100000000000)
- Bit 11 is set (2^11 = 2048)
- Code = F{33+11} = F44 (AC VW under Voltage fault)

#### Communication Board Faults (F65-F67)

| Register | Bit | Fault Code | Description |
|----------|-----|------------|-------------|
| `commBoardFaultRaw` | 0 | F65 | Flash chip error |
| `commBoardFaultRaw` | 1 | F66 | Time error |
| `commBoardFaultRaw` | 2 | F67 | EEPROM error |

### Notification Type Lookup

1. Extract codes from raw registers using bit mapping
2. Determine category from `kind` field
3. Query `DeviceNotificationTypes` where:
   - `Code` = extracted code (e.g., "W31", "F44")
   - `Category` = determined category (e.g., "Inverter")
   - `IsActive` = true

### Generic Fallback Types

If a specific code is not found, the system falls back to generic types:

| Category | Warning Fallback | Error Fallback | Alarm Fallback |
|----------|-----------------|----------------|----------------|
| Inverter | INV-WAR-GENERIC | INV-ERR-GENERIC | INV-ALM-GENERIC |
| Battery | BMS-WAR-GENERIC | BMS-ERR-GENERIC | BMS-ALM-GENERIC |

### Duplicate Prevention

Notifications are deduplicated within a 5-minute window:
- Same `DeviceId`
- Same `DeviceNotificationTypeId`
- Not yet processed (`IsProcessed = false`)
- `Timestamp` within last 5 minutes

If duplicate found, only update the existing notification's timestamp.

---

## API Endpoints

### User Endpoints (Require User Authorization)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/DeviceNotifications` | Get paginated notifications |
| GET | `/api/DeviceNotifications/Deleted` | Get soft-deleted notifications |
| PUT | `/api/DeviceNotifications/{id}/MarkRead` | Mark notification as read |
| PUT | `/api/DeviceNotifications/MarkAllRead/{siteId}` | Mark all site notifications as read |
| PUT | `/api/DeviceNotifications/{id}/Restore` | Restore deleted notification |
| DELETE | `/api/DeviceNotifications/{id}` | Soft delete notification |
| DELETE | `/api/DeviceNotifications/{id}/Purge` | Permanently delete notification |

### Device Endpoints (Require Device Authorization)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/DeviceNotifications` | Create notification (device-initiated) |

### Admin Tool Endpoints (Require Admin Authorization)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/DeviceNotifications/Tool/Types` | Get all notification types |

### Telemetry Endpoint (Processes Alarms)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/DeviceData/Alarm` | Process alarm telemetry from gateway |

### Query Parameters for GET /api/DeviceNotifications

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `siteId` | UUID | Yes | Filter by site |
| `deviceId` | UUID | No | Filter by device |
| `severity` | string | No | Filter by severity |
| `isProcessed` | bool | No | Filter by processed status |
| `fromDate` | DateTime | No | Filter from date |
| `toDate` | DateTime | No | Filter to date |
| `pageNumber` | int | No | Page number (default: 1) |
| `pageSize` | int | No | Page size (default: 10) |

### Response DTOs

#### DeviceNotificationDto

```json
{
  "id": "uuid",
  "deviceId": "uuid",
  "siteId": "uuid",
  "setupDeviceId": "uuid",
  "deviceNotificationTypeId": "uuid",
  "timestamp": "2026-01-15T08:39:21.077Z",
  "additionalData": "{...}",
  "isProcessed": false,
  "created": "2026-01-15T08:39:21.077Z",
  "deviceNotificationType": {
    "id": "uuid",
    "code": "W31",
    "message": "Battery communication warning",
    "description": "Abnormal battery communication...",
    "category": "Inverter",
    "severity": "Warning",
    "version": "1.0",
    "manufacturer": "LOFTTY",
    "isActive": true
  },
  "deviceManufacturer": "LOFTTY",
  "siteName": "My Home",
  "deviceName": "Inverter 1",
  "key": "W31",
  "displayMessage": "Battery communication warning",
  "severity": "Warning",
  "detailedDescription": "Abnormal battery communication..."
}
```

#### DeviceNotificationTypeDto

```json
{
  "id": "uuid",
  "code": "W31",
  "message": "Battery communication warning",
  "description": "Abnormal battery communication. 1) Check whether the BMS connection is stable...",
  "category": "Inverter",
  "severity": "Warning",
  "version": "1.0",
  "manufacturer": "LOFTTY",
  "isActive": true
}
```

---

## SignalR Integration

### DeviceNotificationHub

**URL:** `/deviceNotificationHub`

**Client Methods (Frontend receives):**

| Method | Payload | Description |
|--------|---------|-------------|
| `ReceiveDeviceNotification` | DeviceNotificationDto | New notification received |

**Hub Methods (Frontend calls):**

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinSiteGroup` | siteId (string) | Subscribe to site notifications |
| `LeaveSiteGroup` | siteId (string) | Unsubscribe from site |
| `JoinDeviceGroup` | deviceId (string) | Subscribe to device notifications |
| `LeaveDeviceGroup` | deviceId (string) | Unsubscribe from device |

### Notification Groups

- `Site_{siteId}` - All notifications for a site
- `Device_{deviceId}` - Notifications for specific device

### Critical Severities

Only notifications with these severities trigger SignalR push:
- `ALARM`
- `WARNING`
- `ERROR`

### Frontend Integration Example

```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/deviceNotificationHub', {
    accessTokenFactory: () => authToken
  })
  .withAutomaticReconnect()
  .build();

// Listen for notifications
connection.on('ReceiveDeviceNotification', (notification) => {
  console.log('New notification:', notification);
  // Update UI, show toast, etc.
});

// Connect and subscribe
await connection.start();
await connection.invoke('JoinSiteGroup', siteId);
```

---

## Notification Types Reference

### Active Warning Codes (Severity: Warning)

| Code | Message | Description |
|------|---------|-------------|
| W2 | Fan warning | Fan abnormal alarm |
| W3 | Grid phase wrong | Grid phase sequence error |
| W4 | Meter communication failure | Meter communication lost |
| W11 | Battery low SOC warning | Battery state of charge is low |
| W31 | Battery communication warning | BMS communication abnormal |
| W32 | Parallel communication warning | Parallel inverter communication unstable |

### Placeholder Warning Codes (IsActive: false)

Reserved for future use when manufacturer provides documentation:
- W1, W5-W10, W12-W30

### Fault Codes (Severity: Error)

| Code | Message | Description |
|------|---------|-------------|
| F1 | DC inversed failure | PV input polarity reversed |
| F2 | DC insulation failure | PV insulation resistance low |
| F3 | GFDI failure | Ground fault detection issue |
| F4 | GFDI ground failure | PV grounding issue |
| F5 | EEPROM read failure | Internal memory read error |
| F6 | EEPROM write failure | Internal memory write error |
| F7 | DC/DC soft-start fault | BUS voltage build failure |
| F8 | GFDI relay failure | Ground fault relay malfunction |
| F9 | IGBT failure | Power transistor failure |
| F10 | AuxPowerBoard failure | Auxiliary power board issue |
| F11 | AC main contactor failure | Main contactor malfunction |
| F12 | AC slave contactor failure | Slave contactor malfunction |
| F13 | Grid-Mode changed | Grid type/frequency changed |
| F14 | DC over current fault | DC side overcurrent |
| F15 | SW_AC over current fault | AC software overcurrent |
| F16 | GFCI failure | Leakage current detected |
| F17 | Tz_PV over current fault | PV overcurrent protection |
| F18 | HW_Ac Over current fault | AC hardware overcurrent |
| F19 | Tz_Integ fault | Integration fault |
| F20 | Tz_Dc over Current fault | DC overcurrent protection |
| F21 | Tz_HV over current fault | BUS overcurrent |
| F22 | Tz_Emergency stop fault | Remote shutdown active |
| F23 | Tz_GFCI_OC_Fault | Leakage current fault |
| F24 | DC insulation fault | PV isolation low |
| F25 | DC feedback fault | DC feedback issue |
| F26 | Bus unbalance fault | BUS voltage unbalanced |
| F27 | DC insulation fault | DC insulation issue |
| F28 | DCI Over M1 fault | DCI overcurrent M1 |
| F29 | Parallel CANBus fault | Parallel communication fault |
| F30 | AC main contactor fault | Main contactor fault |
| F31 | AC slave contactor fault | Slave contactor fault |
| F32 | DCIOver M2 fault | DCI overcurrent M2 |
| F33 | AC over current fault | Grid current too large |
| F34 | AC over load fault | Backup load exceeded |
| F35 | AC no utility fault | Grid disconnected |
| F36 | AC Grid phase sequence fault | Grid phase error |
| F37 | AC volt unbalance fault | AC voltage unbalanced |
| F38 | Parallel system Stop1 | Parallel system stopped |
| F39 | Inverter AC over current fault | Inverter AC overcurrent |
| F40 | Inverter DC over current fault | Inverter DC overcurrent |
| F41 | parallel system stop | Parallel system shutdown |
| F42 | parallel version fault | Firmware version mismatch |
| F43 | AC VW over voltage fault | Grid voltage too high |
| F44 | AC VW under Voltage fault | Grid voltage too low |
| F45 | AC UV over voltage fault | AC UV overvoltage |
| F46 | AC uv under voltage fault | AC UV undervoltage |
| F47 | AC over frequency fault | Grid frequency too high |
| F48 | AC under frequency fault | Grid frequency too low |
| F49 | AC U grid Current DC high fault | AC U DC component high |
| F50 | AC V grid Current DC high fault | AC V DC component high |
| F51 | AC W grid Current DC high fault | AC W DC component high |
| F52 | AC A inductor current DC high fault | Inductor A DC high |
| F53 | AC B inductor current DC high fault | Inductor B DC high |
| F54 | Battery voltage high fault | Battery overvoltage |
| F55 | DC voltage high fault | BUS voltage too high |
| F56 | DC voltage Low fault | Battery voltage low |
| F57 | AC back feed fault | AC backfeed detected |
| F58 | BMS communication fault | BMS communication lost |
| F59 | AC V grid current high fault | AC V current too high |
| F60 | Generator voltage or frequency fault | Generator issue |
| F61 | Button manual OFF | Inverter manually switched off |
| F62 | DRMs0 stop | DRM function active (Australia) |
| F63 | Arc fault | Arc fault detected (US only) |
| F64 | Heatsink high temperature fault | Overheating |

### Communication Board Faults

| Code | Message | Description |
|------|---------|-------------|
| F65 | Communication board flash chip error | Internal circuit board flash memory error |
| F66 | Communication board time error | Internal circuit board time sync error |
| F67 | Communication board EEPROM error | Internal circuit board EEPROM error |

### Generic Fallback Types

| Code | Category | Severity | Description |
|------|----------|----------|-------------|
| INV-ALM-GENERIC | Inverter | ALARM | Generic inverter alarm |
| INV-WAR-GENERIC | Inverter | WARNING | Generic inverter warning |
| INV-ERR-GENERIC | Inverter | ERROR | Generic inverter error |
| BMS-ALM-GENERIC | Battery | ALARM | Generic BMS alarm |
| BMS-WAR-GENERIC | Battery | WARNING | Generic BMS warning |
| BMS-ERR-GENERIC | Battery | ERROR | Generic BMS error |

---

## SQL Script

The notification types are seeded using: `scripts/add_deye_notification_types.sql`

This script:
1. Enables `uuid-ossp` extension for PostgreSQL
2. Truncates existing notification types
3. Inserts all active warning codes (W2, W3, W4, W11, W31, W32)
4. Inserts placeholder warnings (W1, W5-W30) with `IsActive = false`
5. Inserts all fault codes (F1-F64)
6. Inserts communication board faults (F65-F67)
7. Inserts generic fallback types

**Run after migration:**
```bash
psql -U username -d database -f scripts/add_deye_notification_types.sql
```

---

## Related Documentation

- [Device Status and Notifications](./DeviceStatus_And_Notifications.md) - Device online/offline tracking
- [SignalR Hubs](./DeviceSettingsSignalR.md) - SignalR configuration

---

## Changelog

| Date | Version | Changes |
|------|---------|---------|
| 2026-01-15 | 2.0 | Complete rewrite with Deye/LOFTTY fault codes, dynamic bit mapping, Kind field support |
| 2025-12-21 | 1.0 | Initial implementation with Node-RED alarm codes |
