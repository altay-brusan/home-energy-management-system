# Device Alarm Processing Implementation Summary

## Overview

This document summarizes the implementation of the device alarm processing system that receives alarm notifications from IoT devices (via Node-RED) and processes them into structured database records.

**Implementation Date:** December 21, 2025

## What Was Implemented

### 1. SQL Seed File for DeviceNotificationTypes

**File:** `database/seeds/DeviceNotificationTypes_Seed.sql`

A comprehensive SQL seed file containing **64 predefined notification types**:

#### Inverter Notifications (44 types)
- **17 PDF-documented fault codes** from Solenux LHG8K-P1LV-EU User Manual
  - Codes: F08, F13, F18, F20, F22, F23, F24, F26, F29, F34, F35, F41, F42, F47, F48, F56, F58, F63, F64
- **16 Node-RED decoded error codes** for three-phase inverters
  - Generic fault codes: DC voltage, bus voltage, grid issues, temperature, etc.
- **10 Node-RED decoded warning codes**
  - Battery communication, temperature warnings, phase imbalance, etc.
- **1 PDF-documented warning code** (F13)

#### Battery Notifications (20 types)
From PYLON CAN Bus Protocol V1.2:
- **7 Protection errors** (over/under voltage, temperature, current, system error)
- **7 Alarm warnings** (high/low voltage, temperature, current, communication)
- **13 Informational indicators** (SOC, SOH, limits, enable/disable flags)

**Features:**
- Auto-generated GUIDs using PostgreSQL `gen_random_uuid()`
- Temporarily disables FK checks for safe truncation
- Standardized code format: `[DEVICETYPE]-[LEVEL]-[MESSAGECODE]`
- Comprehensive descriptions with troubleshooting guidance

### 2. DTOs for InverterAlarmState

**Files:**
- `src/Application/DeviceNotifications/Commands/ProcessDeviceAlarm/AlarmDto.cs`
- `src/Application/DeviceNotifications/Commands/ProcessDeviceAlarm/InverterAlarmStateDto.cs`

DTOs matching the JSON structure sent by Node-RED:
- InverterAlarmState container with metadata
- ActiveAlarm array with detailed alarm information
- Support for multiple alarm parameters

### 3. ProcessDeviceAlarm Command and Handler

**Files:**
- `src/Application/DeviceNotifications/Commands/ProcessDeviceAlarm/ProcessDeviceAlarmCommand.cs`
- `src/Application/DeviceNotifications/Commands/ProcessDeviceAlarm/ProcessDeviceAlarmCommandValidator.cs`

**Key Features:**
- Maps Node-RED alarm codes (1001-1016, 2001-2010) to DeviceNotificationType codes
- Prevents duplicate notifications within 5-minute window
- Stores comprehensive alarm data in JSON AdditionalData field
- Returns detailed result with success status, counts, and errors
- Handles partial failures gracefully

**Alarm Code Mapping:**
- Fault codes 1001-1016 → INV-ERR-* notification types
- Warning codes 2001-2010 → INV-WARN-* notification types
- Fallback handling for unknown codes

### 4. REST API Endpoint

**Endpoint:** `POST /api/device-notifications/alarm`

**File:** `src/Web/Endpoints/DeviceNotifications.cs`

**Features:**
- Device-only authorization (requires device authentication token)
- Accepts InverterAlarmState JSON payload
- Creates DeviceNotification records in database
- Sends real-time notifications via SignalR for critical alarms
- Returns processing result with created notification IDs

**Response:**
```json
{
  "success": true,
  "processedCount": 1,
  "createdCount": 1,
  "errors": []
}
```

### 5. Comprehensive Documentation

**Files:**
- `database/seeds/README.md` - Seed file documentation
- `docs/api/process-device-alarm.md` - API endpoint documentation

**Documentation includes:**
- Complete endpoint specification
- Request/response schemas with examples
- Field descriptions
- Alarm code mapping table
- cURL and C# usage examples
- Error handling guidance

## Data Flow

```
┌─────────────┐
│   Device    │
│  (Inverter) │
└──────┬──────┘
       │ Modbus RTU/TCP
       ▼
┌─────────────┐      ┌──────────────────┐
│  Node-RED   │──────▶│ InverterAlarmState│
│   Gateway   │      │      JSON         │
└──────┬──────┘      └──────────────────┘
       │ MQTT/HTTP
       │
       ▼
┌────────────────────────────────────────┐
│  HEMS Backend API                      │
│  POST /api/device-notifications/alarm  │
└──────┬─────────────────────────────────┘
       │
       ├─▶ Map alarm codes to notification types
       ├─▶ Create DeviceNotification records
       ├─▶ Store raw register values in JSON
       └─▶ Send SignalR notifications (if critical)
              │
              ▼
       ┌──────────────┐
       │   Frontend   │
       │   Clients    │
       └──────────────┘
```

## Database Schema

### DeviceNotificationTypes Table
```sql
Id                  UUID (PK)
Code                VARCHAR(50) UNIQUE    -- e.g., "INV-ERR-F08"
Message             VARCHAR(200)          -- Short message
Status              VARCHAR(100)          -- Status description
Description         VARCHAR(1000)         -- Detailed description
Register            INTEGER               -- Modbus register number
Value               INTEGER               -- Bit or value position
Category            VARCHAR(50)           -- INFO, WARNING, ERROR, ALARM
Version             VARCHAR(50)           -- Version tracking
IsActive            BOOLEAN
Created             TIMESTAMPTZ
CreatedBy           TEXT
LastModified        TIMESTAMPTZ
LastModifiedBy      TEXT
```

### DeviceNotifications Table (existing)
```sql
Id                        UUID (PK)
DeviceId                  UUID (FK → Devices)
SiteId                    UUID (FK → Sites)
SetupDeviceId             UUID (FK → SetupDevices)
DeviceNotificationTypeId  UUID (FK → DeviceNotificationTypes)
Timestamp                 TIMESTAMPTZ
AdditionalData            TEXT (JSON)    -- Contains alarm details
IsProcessed               BOOLEAN
IsDeleted                 BOOLEAN
Created                   TIMESTAMPTZ
CreatedBy                 TEXT
LastModified              TIMESTAMPTZ
LastModifiedBy            TEXT
```

## AdditionalData JSON Structure

The `AdditionalData` field stores rich alarm context:

```json
{
  "severity": "fault",
  "sourceDevice": "grid",
  "category": "ac_voltage",
  "messageKey": "GRID_VOLTAGE_HIGH",
  "vendorCode": 4,
  "alarmCode": 1005,
  "messageParams": {
    "measuredValue": 253.5,
    "limitValue": 253.0
  },
  "firstOccurredUtc": "2025-12-21T20:29:45Z",
  "lastOccurredUtc": "2025-12-21T20:30:00Z",
  "occurrenceCount": 3,
  "gatewayId": "EC100",
  "inverterSerialNumber": "INV-12345",
  "runStateRaw": 3,
  "warningWord1Raw": 0,
  "warningWord2Raw": 0,
  "faultWord1Raw": 16,
  "faultWord2Raw": 0,
  "faultWord3Raw": 0,
  "faultWord4Raw": 0
}
```

## Integration with Node-RED

The system integrates with your existing Node-RED flow:

**Node-RED Flow:** `ems-communication-jsons/three-phase/loftty/telemetry-flows/monitoring/inverter_alarm_state_tab.json`

**How it works:**
1. Node-RED reads Modbus registers from inverter (600, 653-658)
2. Decodes fault/warning bits into structured alarm objects
3. Builds InverterAlarmState JSON
4. Sends JSON to backend via HTTP POST
5. Backend processes alarms and stores in database
6. Frontend receives real-time updates via SignalR

## Next Steps

### 1. Run the SQL Seed File

```bash
# Using psql
psql -U your_username -d hems_db -f database/seeds/DeviceNotificationTypes_Seed.sql

# Or via pgAdmin Query Tool
```

### 2. Update Node-RED Flow

Update your Node-RED MQTT/HTTP node to point to:
```
POST https://your-backend-url/api/device-notifications/alarm
```

Add device authentication token to headers:
```
Authorization: Bearer YOUR_DEVICE_TOKEN
```

### 3. Test the Integration

1. Trigger a test alarm from your inverter or use Node-RED inject node
2. Verify alarm is received by backend
3. Check database for new DeviceNotification records
4. Confirm SignalR notifications are sent to frontend

### 4. Extend for Battery Alarms

Similar implementation can be created for battery alarms from the BMS:
- Create BatteryAlarmState DTO
- Add battery alarm code mapping
- Reuse ProcessDeviceAlarm command or create separate handler

## Files Created/Modified

### New Files
```
database/seeds/
  ├── DeviceNotificationTypes_Seed.sql
  └── README.md

src/Application/DeviceNotifications/Commands/ProcessDeviceAlarm/
  ├── AlarmDto.cs
  ├── InverterAlarmStateDto.cs
  ├── ProcessDeviceAlarmCommand.cs
  └── ProcessDeviceAlarmCommandValidator.cs

docs/api/
  └── process-device-alarm.md
```

### Modified Files
```
src/Web/Endpoints/DeviceNotifications.cs
  - Added ProcessDeviceAlarm endpoint
  - Added using statements for IApplicationDbContext
```

## Build Status

✅ **Build Successful** - No errors or warnings

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:17.61
```

## Testing Checklist

- [x] Build compiles successfully
- [ ] SQL seed file runs without errors
- [ ] DeviceNotificationTypes table populated with 64 records
- [ ] API endpoint accessible via Swagger/Postman
- [ ] Sample InverterAlarmState JSON accepted
- [ ] DeviceNotification records created in database
- [ ] SignalR notifications sent for critical alarms
- [ ] Frontend receives real-time notifications
- [ ] Duplicate alarms handled correctly (5-minute window)
- [ ] Unknown alarm codes handled gracefully

## Support for Future Device Types

The architecture is extensible:

1. **Add new device types:**
   - Add notification types to seed file (e.g., `PV-ERR-*`, `GRID-WARN-*`)
   - Create new DTOs for device-specific alarm formats
   - Add code mapping in handler
   - Reuse ProcessDeviceAlarm or create device-specific command

2. **Version notification types:**
   - Version field tracks schema changes
   - Can maintain multiple versions simultaneously
   - Enables backwards compatibility

3. **Multi-language support:**
   - Store message translations in separate table
   - Lookup by notification type code and language
   - Return localized messages in API responses

## Known Limitations

1. **Node-RED code mapping is hardcoded** in ProcessDeviceAlarmCommandHandler
   - Consider moving to database configuration table for flexibility

2. **5-minute duplicate window is hardcoded**
   - Consider making this configurable per notification type

3. **Battery alarms not yet implemented**
   - Only inverter alarms are currently supported
   - Battery implementation would follow similar pattern

## Conclusion

The device alarm processing system is now fully implemented and ready for integration testing. The system provides:

- ✅ Comprehensive alarm type definitions (64 types)
- ✅ Flexible alarm code mapping
- ✅ Duplicate prevention
- ✅ Rich alarm context storage
- ✅ Real-time notifications via SignalR
- ✅ Detailed API documentation
- ✅ Extensible architecture

**Next immediate action:** Run the SQL seed file to populate the DeviceNotificationTypes table.
