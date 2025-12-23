# Device Notification Types Seed Data

## Overview

This directory contains SQL seed files for populating the `DeviceNotificationTypes` table with predefined alarm and notification definitions for inverters and batteries.

## Files

### DeviceNotificationTypes_Seed.sql

Comprehensive seed file containing all device notification type definitions.

**Contents:**
- **Inverter Notifications (44 types)**:
  - 17 PDF-documented fault codes (F08-F64) from Solenux LHG8K-P1LV-EU
  - 16 Node-RED decoded error codes (ERR)
  - 10 Node-RED decoded warning codes (WARN)
  - 1 PDF-documented warning code (F13)

- **Battery Notifications (20 types)**:
  - 7 Protection errors (ERR) from PYLON CAN Bus Protocol
  - 7 Alarm warnings (ALARM)
  - 13 Informational status indicators (INFO)

**Total: 64 notification types**

## Running the Seed File

### Using psql

```bash
psql -U your_username -d your_database -f DeviceNotificationTypes_Seed.sql
```

### Using pgAdmin

1. Open pgAdmin and connect to your database
2. Open Query Tool (Tools → Query Tool)
3. Open the file: File → Open → Select `DeviceNotificationTypes_Seed.sql`
4. Execute the script: F5 or click the Execute button

### Using .NET EF Migrations

You can also execute this SQL file from a C# migration or database initializer:

```csharp
var sqlScript = File.ReadAllText("database/seeds/DeviceNotificationTypes_Seed.sql");
await context.Database.ExecuteSqlRawAsync(sqlScript);
```

## Notification Code Format

All notification type codes follow the standardized format:

```
[DEVICETYPE]-[LEVEL]-[MESSAGECODE]
```

**Device Types:**
- `INV` - Inverter
- `BAT` - Battery

**Levels:**
- `INFO` - Informational status
- `WARN` - Warning (non-critical)
- `ALARM` - Alarm (approaching critical threshold)
- `ERR` - Error (critical fault/protection triggered)

**Examples:**
- `INV-ERR-F08` - Inverter Error F08 (Grid Relay Failure)
- `INV-WARN-BATTERY-COMM-FAIL` - Inverter Warning (Battery Communication Failure)
- `BAT-ERR-P00` - Battery Error P00 (Cell Over Voltage Protection)
- `BAT-ALARM-A02` - Battery Alarm A02 (Cell High Temperature Alarm)
- `BAT-INFO-I04` - Battery Info I04 (State of Charge)

## Data Sources

### Inverter Alarms

**Source 1: Solenux LHG8K-P1LV-EU User Manual**
- Location: `C:\Trash\inverter-alarms.pdf`
- Section: Fault Information and Processing
- Codes: F08, F13, F18, F20, F22, F23, F24, F26, F29, F34, F35, F41, F42, F47, F48, F56, F58, F63, F64

**Source 2: Node-RED Alarm Decoding**
- Location: `C:\Users\altay\OneDrive\Projects\oklidis\HEMS\Hems-Gateway\ems-communication-jsons\three-phase\loftty\telemetry-flows\monitoring\inverter_alarm_state_tab.json`
- Generic alarm codes for three-phase inverters
- Fault codes: 1001-1016
- Warning codes: 2001-2010

### Battery Alarms

**Source: PYLON CAN Bus Protocol V1.2**
- Location: `C:\Users\altay\OneDrive\Companies\Oklidis\Projects\HEMS\BMS documents\General Data\BMS\55998=9079-CAN-Bus-protocol-PYLON-low-voltage-V1.2-20180408.pdf`
- CAN ID: 0x359
- Protection bits (Table 1 & 2): Byte 0-1
- Alarm bits (Table 3 & 4): Byte 2-3

## Important Notes

1. **FK Checks**: The script temporarily disables foreign key checks using PostgreSQL's `session_replication_role` to allow safe truncation.

2. **Auto-Generated GUIDs**: All IDs use PostgreSQL's `gen_random_uuid()` function to avoid hardcoded values.

3. **Idempotent**: Running this script multiple times will clear and repopulate the table with the same data.

4. **Version Control**: All notification types include a version field (currently "1.0") for future schema evolution.

5. **Active Status**: All seeded notification types are marked as `IsActive = true` by default.

## Maintenance

When adding new notification types:

1. Follow the code format convention
2. Use appropriate category (INFO, WARNING, ALARM, ERROR)
3. Provide clear, actionable descriptions
4. Document the source (register, value, vendor documentation)
5. Update this README with the new count

## Related Documentation

- [Device Notifications API](../../docs/api/device-notifications.md)
- [ProcessDeviceAlarm Endpoint](../../docs/api/process-device-alarm.md)
- [Node-RED Integration](../../docs/integration/node-red.md)
