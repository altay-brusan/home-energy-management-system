# Device Settings Initialization Flow

## Overview

Device settings are automatically initialized when a device is registered and added to a Setup. The system uses a **template-based approach** where predefined parameter schemes are copied to create device-specific parameter instances.

## Key Entities

### 1. **ParameterValueSettingScheme** (Template)
- **Purpose**: Template/blueprint for device settings
- **Scope**: One template per device model (e.g., "single-phase-loftty")
- **Location**: `ParameterValueSettingSchemes` table
- **Contains**:
  - `ParameterId` - Which parameter (e.g., "SolarSell")
  - `Value` - Default value for this model (e.g., "false")
  - `ModelNumber` - Device model (e.g., "single-phase-loftty")
  - `ParameterGroupId` - Grouping (e.g., "InverterMode")
  - `SortOrder` - Display order

### 2. **SetupDeviceParameterValue** (Instance)
- **Purpose**: Actual parameter values for a specific device
- **Scope**: One set per SetupDevice (each registered device)
- **Location**: `SetupDeviceParameterValues` table
- **Contains**:
  - `SetupDeviceId` - Which device instance
  - `ParameterId` - Which parameter
  - `Value` - **Current** value (can be modified)
  - `ModelNumber` - Device model
  - All other fields from template

## Complete Initialization Flow

### Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEVICE REGISTRATION FLOW                      │
└─────────────────────────────────────────────────────────────────┘

Step 1: Create Device
─────────────────────
  User creates device via API
         │
         ▼
  POST /api/devices
  {
    "manufacturer": "Manufacturer",
    "modelNumber": "single-phase-loftty",
    "serialNumber": "2509080139",
    "qrCode": "QR-123"
  }
         │
         ▼
  CreateDeviceCommand
         │
         ├─> Creates device in ThingsBoard
         │   (for RPC communication)
         │
         ├─> Creates Device in local database
         │   - Stores ThingsBoardDeviceId
         │   - Stores ModelNumber (important!)
         │   - Status: Created but NOT configured
         │
         └─> Adds to heartbeat monitoring

  Result: Device exists but has NO SETTINGS yet


Step 2: Create Setup (Links Device to Site)
────────────────────────────────────────────
  User creates setup for a site
         │
         ▼
  POST /api/setups
  {
    "siteId": "site-guid",
    "name": "My Home Setup",
    "deviceSerialNumber": "2509080139"
  }
         │
         ▼
  CreateSetupCommand.Handle()
         │
         ├─> Validates site exists and user has access
         │
         ├─> Finds device by serial number/QR code
         │   - Checks ThingsBoard
         │   - Validates in local database
         │   - Gets device.ModelNumber
         │
         ├─> Creates Setup entity
         │   (container for this installation)
         │
         ├─> Creates SetupDevice entity
         │   - Links Setup to Device
         │   - This is the "installation instance"
         │
         └─> **INITIALIZE DEVICE SETTINGS** ⚡
             (THIS IS WHERE MAGIC HAPPENS!)

             Code (CreateSetup.cs, lines 130-144):
             ┌──────────────────────────────────────┐
             │ var schemes = await _context         │
             │   .ParameterValueSettingSchemes      │
             │   .Where(f => f.ModelNumber ==       │
             │          device.ModelNumber)         │
             │   .ToListAsync();                    │
             │                                      │
             │ foreach (var scheme in schemes)      │
             │ {                                    │
             │   var parameter = new                │
             │     SetupDeviceParameterValue        │
             │   {                                  │
             │     ParameterId = scheme.ParameterId │
             │     Value = scheme.Value             │
             │     SetupDeviceId = setupDevice.Id   │
             │     ModelNumber = scheme.ModelNumber │
             │     // ... copy all fields          │
             │   };                                 │
             │   _context.SetupDeviceParameterValues│
             │     .Add(parameter);                 │
             │ }                                    │
             └──────────────────────────────────────┘

  Result: Device now has complete settings copied from template!


Alternative Flow: CreateSetupDevice Command
────────────────────────────────────────────
  (Used when adding device to existing setup)
         │
         ▼
  POST /api/setupdevices
  {
    "setupId": "setup-guid",
    "deviceId": "device-guid"
  }
         │
         ▼
  CreateSetupDeviceCommand.Handle()
         │
         ├─> Creates SetupDevice entity
         │
         └─> Calls InitializeDeviceSettingsCommand
             (Separate command for reusability)

             Code (CreateSetupDevice.cs, lines 54-58):
             ┌──────────────────────────────────────┐
             │ await _mediator.Send(new             │
             │   InitializeDeviceSettingsCommand    │
             │   {                                  │
             │     SetupDeviceId = entity.Id,       │
             │     ModelNumber = device.ModelNumber │
             │   }, cancellationToken);             │
             └──────────────────────────────────────┘
```

## Detailed Initialization Logic

### InitializeDeviceSettingsCommand

**File**: `src/Application/SetupDeviceParameterValues/Commands/InitializeDeviceSettings/InitializeDeviceSettings.cs`

**What it does**:

```csharp
public async Task<int> Handle(InitializeDeviceSettingsCommand request, CancellationToken ct)
{
    // 1. GET TEMPLATE: Fetch all parameter schemes for this device model
    var schemes = await _context.ParameterValueSettingSchemes
        .Where(pvss => pvss.ModelNumber == request.ModelNumber)
        .ToListAsync(ct);

    // Example: For "single-phase-loftty" model, this fetches ~50 parameter templates:
    // - SolarSell: "false"
    // - MaxSellPower: "12000"
    // - GridChargingEnabled: "true"
    // - etc.

    if (schemes.Count == 0)
    {
        return 0; // No template found for this model
    }

    // 2. CHECK EXISTING: Prevent duplicate initialization
    var existingSettings = await _context.SetupDeviceParameterValues
        .Where(sdpv => sdpv.SetupDeviceId == request.SetupDeviceId)
        .ToListAsync(ct);

    if (existingSettings.Any())
    {
        return 0; // Already initialized, don't overwrite
    }

    // 3. COPY TEMPLATE TO INSTANCE: Create device-specific parameter values
    var deviceParameterValues = schemes.Select(scheme =>
        new SetupDeviceParameterValue
        {
            SetupDeviceId = request.SetupDeviceId,    // Link to this device
            ParameterId = scheme.ParameterId,          // Same parameter
            ParameterGroupId = scheme.ParameterGroupId,// Same group
            Value = scheme.Value,                      // COPY default value
            SortOrder = scheme.SortOrder,              // Same order
            ModelNumber = scheme.ModelNumber           // Same model
        }).ToList();

    // 4. SAVE: Persist all parameter values at once
    _context.SetupDeviceParameterValues.AddRange(deviceParameterValues);
    await _context.SaveChangesAsync(ct);

    return deviceParameterValues.Count; // Return count of initialized parameters
}
```

## Template Structure Example

### ParameterValueSettingScheme (Template)

**For model: "single-phase-loftty"**

| Parameter ID | Parameter Name | Value | Group | Sort Order |
|--------------|----------------|-------|-------|------------|
| guid-1 | SolarSell | "false" | InverterMode | 1 |
| guid-2 | MaxSellPower | "12000" | InverterMode | 2 |
| guid-3 | GridChargingEnabled | "true" | ChargeSources | 1 |
| guid-4 | ShutdownSOC | "20" | ProtectionThresholds | 1 |
| ... | ... | ... | ... | ... |

**Total**: ~50 parameters per device model

### SetupDeviceParameterValue (Instance)

**After initialization for SetupDevice "setup-device-guid-123"**

| ID | Setup Device ID | Parameter ID | Parameter Name | Value | Last Modified |
|----|-----------------|--------------|----------------|-------|---------------|
| guid-a | setup-device-guid-123 | guid-1 | SolarSell | "false" | 2025-12-19 |
| guid-b | setup-device-guid-123 | guid-2 | MaxSellPower | "12000" | 2025-12-19 |
| guid-c | setup-device-guid-123 | guid-3 | GridChargingEnabled | "true" | 2025-12-19 |
| guid-d | setup-device-guid-123 | guid-4 | ShutdownSOC | "20" | 2025-12-19 |
| ... | ... | ... | ... | ... | ... |

**Total**: ~50 parameter instances for this specific device

## Data Flow After Initialization

```
┌──────────────────────────────────────────────────────────────┐
│                  AFTER INITIALIZATION                        │
└──────────────────────────────────────────────────────────────┘

Database State:
──────────────

ParameterValueSettingSchemes (Templates - READ ONLY)
├─ Model: "single-phase-loftty"
│  ├─ SolarSell: "false"
│  ├─ MaxSellPower: "12000"
│  └─ ... (50 parameters)
└─ Model: "three-phase-ec100"
   └─ ... (50 parameters)

SetupDeviceParameterValues (Device Instances - MODIFIABLE)
├─ SetupDevice A (Model: "single-phase-loftty")
│  ├─ SolarSell: "false" → can be changed to "true"
│  ├─ MaxSellPower: "12000" → can be changed to "10000"
│  └─ ... (50 parameters)
├─ SetupDevice B (Model: "single-phase-loftty")
│  ├─ SolarSell: "true"  → independent from A
│  ├─ MaxSellPower: "8000" → independent from A
│  └─ ... (50 parameters)
└─ SetupDevice C (Model: "three-phase-ec100")
   └─ ... (50 parameters for EC100 model)
```

## How It Integrates with Your New API

### Before Device Registration
```
ParameterValueSettingSchemes
└─ Templates exist for each device model
   (Pre-seeded in database)
```

### During Device Registration
```
CreateSetup / CreateSetupDevice
└─ Copies template → SetupDeviceParameterValues
   (Automatic initialization)
```

### After Device Registration
```
Your New Device Settings API
└─ Updates SetupDeviceParameterValues
   ├─ GET /parameters → Read current values
   ├─ PUT /parameters/{id} → Update single value
   ├─ PUT /parameters → Update multiple values
   └─ POST /mode → Update many values at once
```

## Complete Registration Example

### Scenario: Register a new inverter

**1. Create Device**
```bash
POST /api/devices
{
  "manufacturer": "OkLidis",
  "modelNumber": "single-phase-loftty",
  "serialNumber": "2509080139"
}

Response: Device created (ID: device-123)
Status: Device exists in DB, NO settings yet
```

**2. Create Setup (with device)**
```bash
POST /api/setups
{
  "siteId": "my-home-site-id",
  "name": "Home Energy System",
  "deviceSerialNumber": "2509080139"
}

Backend Process:
1. Creates Setup entity
2. Creates SetupDevice entity (ID: setup-device-456)
3. 🔥 INITIALIZATION HAPPENS HERE 🔥
   - Queries: SELECT * FROM ParameterValueSettingSchemes
             WHERE ModelNumber = 'single-phase-loftty'
   - Returns: 50 template parameters
   - Creates: 50 SetupDeviceParameterValue records
             linked to setup-device-456
4. Saves all to database

Response: Setup created (ID: setup-123)
Status: Device fully configured with default settings
```

**3. View Settings**
```bash
GET /api/devicesettings/setup-device-456/parameters

Response: [50 parameters with default values]
```

**4. Customize Settings**
```bash
PUT /api/devicesettings/setup-device-456/parameters
{
  "parameters": [
    {"parameterId": "guid-1", "value": "true"},  // Enable SolarSell
    {"parameterId": "guid-2", "value": "10000"}  // Change MaxSellPower
  ]
}

Backend Process:
1. Updates device via RPC (your new flow)
2. If successful, updates SetupDeviceParameterValues
3. Returns success

Result: Device AND database both updated
```

## Key Points

### ✅ Automatic Initialization
- Settings are **automatically copied** from template when device is added to setup
- No manual intervention required
- Ensures every device starts with valid configuration

### ✅ Template-Based
- One template per device model
- Templates defined in `ParameterValueSettingSchemes`
- Easy to add new device models - just add their template

### ✅ Instance Independence
- Each SetupDevice gets its **own copy** of parameters
- Modifying one device doesn't affect others
- Same model, different instances = independent settings

### ✅ Modification Freedom
- After initialization, values in `SetupDeviceParameterValues` can be freely modified
- Your new API endpoints handle these modifications
- Template remains unchanged (read-only)

### ✅ Database Relationships
```
Parameter (Definition)
    ↓ has many
ParameterValueSettingScheme (Template per model)
    ↓ copied to
SetupDeviceParameterValue (Instance per device)
    ↓ belongs to
SetupDevice (Device installation)
    ↓ belongs to
Device (Physical device)
```

## Summary

**The initialization happens in 2 places:**

1. **CreateSetup.cs** (lines 130-144)
   - When creating setup WITH a device
   - Inline parameter copying logic

2. **InitializeDeviceSettings.cs** (entire file)
   - When adding device to EXISTING setup
   - Reusable command pattern
   - Called from CreateSetupDevice

**Both approaches do the same thing:**
- Query template (`ParameterValueSettingSchemes`) by `ModelNumber`
- Copy all parameters to device instance (`SetupDeviceParameterValues`)
- Link to `SetupDeviceId`

**Result:**
- Every registered device gets complete settings automatically
- Settings are ready to use immediately
- Can be modified via your new Device Settings API
