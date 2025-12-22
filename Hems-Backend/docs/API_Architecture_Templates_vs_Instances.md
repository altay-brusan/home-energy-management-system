# API Architecture: Template Management vs Device Instance Management

## Overview

Your HEMS backend has a **two-tier architecture** for managing device settings:

1. **Template Tier** - Define what parameters exist and their default values (Admin only)
2. **Instance Tier** - Manage actual device settings (User facing)

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   TEMPLATE TIER (Admin Only)                    │
│                   Configuration & Definitions                    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ defines structure
                              │ and defaults
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  INSTANCE TIER (User Facing)                    │
│                  Actual Device Settings                         │
└─────────────────────────────────────────────────────────────────┘
```

## Tier 1: Template Management (Admin Only)

### Purpose
Configure the **system-wide** parameter definitions and default values for each device model.

### Entities & Endpoints

#### 1. **ParameterGroups** - Categories/Organization
**Entity**: `ParameterGroup`
**Table**: `ParameterGroups`
**Endpoint**: `/api/ParameterGroups`

**What it manages:**
- Hierarchical grouping of parameters
- Example: "BatterySettings", "SystemWorkMode", "TimeOfUse"
- Tree structure with parent-child relationships

**Operations:**
```http
GET    /api/ParameterGroups           # List all groups
GET    /api/ParameterGroups/{id}      # Get specific group
POST   /api/ParameterGroups           # Create new group
PUT    /api/ParameterGroups/{id}      # Update group
DELETE /api/ParameterGroups/{id}      # Delete group
```

**Example Data:**
```json
{
  "id": "guid",
  "name": "BatterySettings",
  "description": "Battery Settings - Charge sources, thresholds, protection",
  "parentId": null,
  "sortOrder": 1,
  "children": [
    {
      "id": "guid-2",
      "name": "ChargeSources",
      "parentId": "guid"
    }
  ]
}
```

---

#### 2. **Parameters** - Parameter Definitions
**Entity**: `Parameter`
**Table**: `Parameters`
**Endpoint**: `/api/Parameters`

**What it manages:**
- Definition of each parameter (metadata)
- Example: "SolarSell", "MaxSellPower", "ShutdownSOC"
- Data type, unit, description

**Operations:**
```http
GET    /api/Parameters                # List all parameters
GET    /api/Parameters/{id}           # Get specific parameter
POST   /api/Parameters                # Create new parameter
PUT    /api/Parameters/{id}           # Update parameter definition
DELETE /api/Parameters/{id}           # Delete parameter
```

**Example Data:**
```json
{
  "id": "00c97bc6-e3a6-4b07-9fd9-2f5812ca9801",
  "parameterGroupId": "1fbf312f-940e-47fa-831c-76f4f5edfc7e",
  "name": "SolarSell",
  "description": "Solar Sell Enable/Disable",
  "elementType": "Boolean",
  "roleType": "User",
  "unit": "",
  "defaultValue": "false",
  "sortOrder": 1
}
```

**Key Fields:**
- `name` - Parameter name (used in RPC mapping)
- `elementType` - Data type (Boolean, Decimal, Text)
- `unit` - Measurement unit (W, %, V, A, etc.)
- `defaultValue` - Global default across all models

---

#### 3. **ParameterValueSettingSchemes** - Model-Specific Templates
**Entity**: `ParameterValueSettingScheme`
**Table**: `ParameterValueSettingSchemes`
**Endpoint**: `/api/ParameterValueSettingSchemes`

**What it manages:**
- Default parameter VALUES for each device model
- Example: "single-phase-loftty" has SolarSell="false", but "three-phase-ec100" might have "true"
- Model-specific configuration templates

**Operations:**
```http
GET    /api/ParameterValueSettingSchemes              # List all schemes
GET    /api/ParameterValueSettingSchemes/tree         # Get tree view
POST   /api/ParameterValueSettingSchemes              # Create scheme value
PUT    /api/ParameterValueSettingSchemes/{id}         # Update scheme value
DELETE /api/ParameterValueSettingSchemes/{id}         # Delete scheme value
```

**Query Parameters:**
- `modelNumber` - Filter by device model

**Example Data:**
```json
{
  "id": "guid",
  "parameterId": "00c97bc6-e3a6-4b07-9fd9-2f5812ca9801",
  "parameterGroupId": "1fbf312f-940e-47fa-831c-76f4f5edfc7e",
  "value": "false",
  "sortOrder": 1,
  "modelNumber": "single-phase-loftty"
}
```

**Tree View Response:**
```json
{
  "modelNumber": "single-phase-loftty",
  "parameterGroups": [
    {
      "id": "guid",
      "name": "BatterySettings",
      "parameters": [
        {
          "id": "param-guid",
          "name": "SolarSell",
          "value": "false",
          "elementType": "Boolean"
        }
      ]
    }
  ]
}
```

---

## Tier 2: Device Instance Management (User Facing)

### Purpose
Manage **actual device settings** for specific device installations. These are the values users modify for their devices.

### Entities & Endpoints

#### 4. **SetupDeviceParameterValues** - Device Instance Settings
**Entity**: `SetupDeviceParameterValue`
**Table**: `SetupDeviceParameterValues`
**Endpoint**: `/api/SetupDeviceParameterValues`

**What it manages:**
- Actual parameter values for each installed device
- **Copied from templates** during device registration
- **User-modifiable** settings

**Operations:**
```http
GET    /api/SetupDeviceParameterValues                          # List values
GET    /api/SetupDeviceParameterValues/tree                     # Get tree view
GET    /api/SetupDeviceParameterValues/device/{id}/tree         # Get by device
POST   /api/SetupDeviceParameterValues                          # Create value
POST   /api/SetupDeviceParameterValues/device/{id}/initialize   # Initialize device
PUT    /api/SetupDeviceParameterValues/{id}                     # Update value (DB only)
PUT    /api/SetupDeviceParameterValues/device/{deviceId}/parameter/{parameterId}  # Update value (DB only)
DELETE /api/SetupDeviceParameterValues/{id}                     # Delete value
```

**Query Parameters:**
- `setupDeviceId` - Required for most operations

**Example Data:**
```json
{
  "id": "guid",
  "setupDeviceId": "a089891e-ad8c-42ef-8ce0-6516fcddbb51",
  "parameterId": "00c97bc6-e3a6-4b07-9fd9-2f5812ca9801",
  "parameterGroupId": "1fbf312f-940e-47fa-831c-76f4f5edfc7e",
  "value": "true",  // User changed from "false" to "true"
  "sortOrder": 1,
  "modelNumber": "single-phase-loftty",
  "lastModified": "2025-12-19T14:30:00Z",
  "lastModifiedBy": "user-guid"
}
```

---

#### 5. **DeviceSettings** - New RPC-Integrated API (Your Implementation)
**Endpoint**: `/api/DeviceSettings`

**What it manages:**
- Device parameter updates **WITH device synchronization**
- Updates device via RPC FIRST, then database
- Device mode management (preset configurations)

**Operations:**
```http
GET    /api/DeviceSettings/{setupDeviceId}/parameters           # Get all parameters
PUT    /api/DeviceSettings/{setupDeviceId}/parameters/{id}      # Update single (RPC + DB)
PUT    /api/DeviceSettings/{setupDeviceId}/parameters           # Update batch (RPC + DB)
GET    /api/DeviceSettings/modes                                # Get available modes
POST   /api/DeviceSettings/{setupDeviceId}/mode                 # Apply device mode
```

---

## Comparison: Old vs New Device Settings API

### SetupDeviceParameterValues (Old - Database Only)
```http
PUT /api/SetupDeviceParameterValues/{id}
{
  "value": "true"
}
```

**Behavior:**
- ✅ Updates database
- ❌ Does NOT update physical device
- ❌ No RPC communication
- ⚠️ Device and database OUT OF SYNC

**Use Case:**
- Admin operations
- Bulk data corrections
- Testing

---

### DeviceSettings (New - RPC + Database)
```http
PUT /api/DeviceSettings/{setupDeviceId}/parameters/{parameterId}
{
  "value": "true"
}
```

**Behavior:**
- ✅ Updates device via RPC FIRST
- ✅ If successful, updates database
- ✅ Ensures synchronization
- ✅ Returns detailed results

**Use Case:**
- User-facing operations
- Production device management
- Mobile app integration

---

## Complete Data Flow

### Template Definition Flow (Admin)

```
1. Admin creates ParameterGroup
   POST /api/ParameterGroups
   {
     "name": "BatterySettings",
     "description": "Battery configuration"
   }

   → Creates group structure

2. Admin creates Parameter definition
   POST /api/Parameters
   {
     "parameterGroupId": "group-guid",
     "name": "SolarSell",
     "elementType": "Boolean",
     "defaultValue": "false"
   }

   → Defines what the parameter is

3. Admin creates ParameterValueSettingScheme
   POST /api/ParameterValueSettingSchemes
   {
     "parameterId": "param-guid",
     "value": "false",
     "modelNumber": "single-phase-loftty"
   }

   → Sets default value for this model
```

### Device Instance Flow (User)

```
1. User registers device
   POST /api/Setups
   {
     "siteId": "site-guid",
     "deviceSerialNumber": "123"
   }

   → Backend automatically copies all ParameterValueSettingSchemes
      to SetupDeviceParameterValues for this device

2. User views device settings
   GET /api/DeviceSettings/{setupDeviceId}/parameters

   → Returns all parameters with current values

3. User updates device setting
   PUT /api/DeviceSettings/{setupDeviceId}/parameters/{id}
   {
     "value": "true"
   }

   → Updates device via RPC
   → If successful, updates SetupDeviceParameterValue in DB

4. User applies device mode
   POST /api/DeviceSettings/{setupDeviceId}/mode
   {
     "mode": 1
   }

   → Updates multiple SetupDeviceParameterValues at once
   → All synchronized with device
```

## Data Relationships

```
ParameterGroup (1)
    ↓ has many
Parameter (N) - Definition
    ↓ has many
ParameterValueSettingScheme (N) - Template per model
    ↓ copied to during initialization
SetupDeviceParameterValue (N) - Instance per device
    ↓ belongs to
SetupDevice (1) - Device installation
    ↓ belongs to
Device (1) - Physical device
```

## When to Use Each API

### Use Template APIs (`/api/ParameterGroups`, `/api/Parameters`, `/api/ParameterValueSettingSchemes`)
**When:**
- Adding new parameter types to the system
- Defining defaults for new device models
- Reorganizing parameter structure
- System configuration

**Who:**
- Administrators only
- System configurators
- During initial system setup

**Example Scenarios:**
- "Add support for new device model 'EC200'"
- "Create new parameter 'MaxGridImport'"
- "Change default SolarSell to 'true' for all new EC100 devices"

---

### Use Old Instance API (`/api/SetupDeviceParameterValues`)
**When:**
- Bulk data operations (admin tools)
- Database corrections
- Testing/debugging
- When device communication is unavailable

**Who:**
- Administrators
- Support team
- Developers

**Example Scenarios:**
- "Fix incorrect values in database"
- "Initialize settings for legacy devices"
- "Bulk update for testing"

---

### Use New Device Settings API (`/api/DeviceSettings`)
**When:**
- Normal user operations
- Mobile app features
- Web dashboard controls
- Production device management

**Who:**
- End users
- Mobile app
- Web frontend
- Automation systems

**Example Scenarios:**
- "User wants to enable solar selling on their inverter"
- "User switches device to 'Energy Save' mode"
- "App updates grid charging settings"
- "Dashboard shows current device configuration"

---

## Best Practices

### For Template Management

1. **Version Control**
   - Keep track of template changes
   - Document why defaults changed
   - Consider migration strategy for existing devices

2. **Model Naming**
   - Use consistent model numbers
   - Match manufacturer specifications
   - Document model capabilities

3. **Parameter Naming**
   - Use clear, descriptive names
   - Match RPC field names when possible
   - Keep consistent with documentation

### For Instance Management

1. **Use DeviceSettings API for Users**
   - Always use `/api/DeviceSettings` for user-facing features
   - Ensures device synchronization
   - Provides better error handling

2. **Reserve SetupDeviceParameterValues for Admin**
   - Only use for bulk operations
   - Document when and why used
   - Consider device sync implications

3. **Handle Offline Devices**
   - Check device status before updates
   - Queue updates for offline devices
   - Notify users of sync status

## Migration Path

If you have existing frontend code using old API:

```javascript
// OLD (Database only)
PUT /api/SetupDeviceParameterValues/{id}

// NEW (RPC + Database)
PUT /api/DeviceSettings/{setupDeviceId}/parameters/{parameterId}
```

**Steps:**
1. Identify all uses of SetupDeviceParameterValues update endpoints
2. Replace with DeviceSettings API
3. Update error handling (now includes RPC failures)
4. Add device status checks
5. Test with actual devices

## Summary Table

| Feature | Template APIs | Old Instance API | New DeviceSettings API |
|---------|---------------|------------------|------------------------|
| **Scope** | System-wide | Database only | Device + Database |
| **Who** | Admins | Admins/Support | End Users |
| **RPC** | No | No | Yes ✅ |
| **Sync** | N/A | No ❌ | Yes ✅ |
| **Use Case** | Configuration | Bulk ops | Production |
| **Authorization** | AdminOnly | AdminOnly | All authenticated |
| **Batch Updates** | Manual | Yes | Yes (optimized) |
| **Device Modes** | No | No | Yes ✅ |

## Conclusion

Your system has a well-architected separation:

1. **Templates** define WHAT parameters exist and their defaults
2. **Instances** are ACTUAL values for specific devices
3. **Old API** manages database without device sync
4. **New API** manages both device and database with full sync

The new DeviceSettings API I implemented bridges the gap between database management and physical device control, making it the **recommended API for all user-facing operations**.
