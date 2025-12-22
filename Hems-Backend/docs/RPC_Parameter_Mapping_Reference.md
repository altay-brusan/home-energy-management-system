# RPC Parameter Mapping Reference

## Overview

This document describes how application parameters are mapped to ThingsBoard RPC methods. The system supports **partial JSON updates**, meaning you can send only the fields you want to update without providing the complete JSON structure.

## RPC Methods Summary

| RPC Method | Parameters Count | Purpose |
|------------|-----------------|---------|
| `setBatteryChargeSourceConfig` / `getBatteryChargeSourceConfig` | 8 | Battery charging from grid and generator |
| `setBatteryThresholdConfig` / `getBatteryThresholdConfig` | 3 | Battery SOC thresholds and warnings |
| `setSystemWorkModeConfig` / `getSystemWorkModeConfig` | 51 | System operation modes and time-of-use scheduling |

---

## 1. Battery Charge Source Configuration

**RPC Methods:** `setBatteryChargeSourceConfig` / `getBatteryChargeSourceConfig`
**RPC Kind:** `BatteryChargeSourceConfigState`
**Parameter Count:** 8

### Parameter Mappings

| Parameter Name | Field Path | Type | Example Value |
|----------------|------------|------|---------------|
| GridChargingEnabled | `grid.enabled` | boolean | `true` |
| GridChargingStartSOC | `grid.startSocPercent` | integer | `20` |
| GridChargingCurrent | `grid.chargeCurrentA` | integer | `50` |
| GeneratorChargingEnabled | `generator.enabled` | boolean | `false` |
| GeneratorChargingStartSOC | `generator.startSocPercent` | integer | `15` |
| GeneratorChargingCurrent | `generator.chargeCurrentA` | integer | `30` |
| GeneratorMaxRunTime | `generator.maxRunTimeHours` | decimal | `4.5` |
| GeneratorCoolingTime | `generator.coolingTimeHours` | decimal | `2.0` |

### Complete JSON Structure

```json
{
  "grid": {
    "enabled": true,
    "startSocPercent": 20,
    "chargeCurrentA": 50
  },
  "generator": {
    "enabled": false,
    "startSocPercent": 15,
    "chargeCurrentA": 30,
    "maxRunTimeHours": 4.5,
    "coolingTimeHours": 2.0
  }
}
```

### Sample RPC Calls

**Example 1: Update only Grid Charging settings (Partial Update)**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters/{parameterId}
{
  "value": "true"  // For GridChargingEnabled
}

// Resulting RPC payload sent to device:
{
  "method": "setBatteryChargeSourceConfig",
  "params": {
    "grid": {
      "enabled": true
    }
  }
}
```

**Example 2: Batch update multiple grid parameters**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    { "parameterId": "guid-1", "value": "true" },   // GridChargingEnabled
    { "parameterId": "guid-2", "value": "20" },     // GridChargingStartSOC
    { "parameterId": "guid-3", "value": "50" }      // GridChargingCurrent
  ]
}

// Resulting RPC payload (optimized - grouped into single call):
{
  "method": "setBatteryChargeSourceConfig",
  "params": {
    "grid": {
      "enabled": true,
      "startSocPercent": 20,
      "chargeCurrentA": 50
    }
  }
}
```

**Example 3: Update generator settings only**
```json
// Resulting RPC payload:
{
  "method": "setBatteryChargeSourceConfig",
  "params": {
    "generator": {
      "enabled": false,
      "maxRunTimeHours": 4.5
    }
  }
}
```

---

## 2. Battery Threshold Configuration

**RPC Methods:** `setBatteryThresholdConfig` / `getBatteryThresholdConfig`
**RPC Kind:** `BatteryThresholdConfigState`
**Parameter Count:** 3

### Parameter Mappings

| Parameter Name | Field Path | Type | Example Value |
|----------------|------------|------|---------------|
| ShutdownSOC | `shutdownCapacityPercent` | integer | `10` |
| LowBatteryWarning | `lowBatteryCapacityPercent` | integer | `20` |
| RestartSOC | `restartCapacityPercent` | integer | `15` |

### Complete JSON Structure

```json
{
  "shutdownCapacityPercent": 10,
  "lowBatteryCapacityPercent": 20,
  "restartCapacityPercent": 15
}
```

### Sample RPC Calls

**Example 1: Update shutdown SOC only (Partial Update)**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters/{parameterId}
{
  "value": "10"  // For ShutdownSOC
}

// Resulting RPC payload:
{
  "method": "setBatteryThresholdConfig",
  "params": {
    "shutdownCapacityPercent": 10
  }
}
```

**Example 2: Update all battery thresholds**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    { "parameterId": "guid-1", "value": "10" },  // ShutdownSOC
    { "parameterId": "guid-2", "value": "20" },  // LowBatteryWarning
    { "parameterId": "guid-3", "value": "15" }   // RestartSOC
  ]
}

// Resulting RPC payload (optimized - single call):
{
  "method": "setBatteryThresholdConfig",
  "params": {
    "shutdownCapacityPercent": 10,
    "lowBatteryCapacityPercent": 20,
    "restartCapacityPercent": 15
  }
}
```

---

## 3. System Work Mode Configuration

**RPC Methods:** `setSystemWorkModeConfig` / `getSystemWorkModeConfig`
**RPC Kind:** `SystemWorkModeConfigState`
**Parameter Count:** 51

### 3.1 Inverter Mode Settings (5 parameters)

| Parameter Name | Field Path | Type | Example Value |
|----------------|------------|------|---------------|
| ExportStrategySelection | `inverterMode.exportStrategy` | string (enum) | `"selling_first"` |
| SolarSell | `inverterMode.solarSellEnabled` | boolean | `true` |
| MaxSellPower | `inverterMode.maxSellPowerW` | integer | `5000` |
| ZeroExportLimit | `inverterMode.zeroExportLimitPowerW` | integer | `100` |
| ForceOffGrid | `inverterMode.forceOffGrid` | boolean | `false` |

**Export Strategy Enum Mapping:**
- `"SellingFirst"` → `"selling_first"`
- `"ZeroExportToLoad"` → `"zero_export_to_load"`
- `"ZeroExportToCT"` → `"zero_export_to_ct"`

### 3.2 Energy Priority (1 parameter)

| Parameter Name | Field Path | Type | Example Value |
|----------------|------------|------|---------------|
| EnergyPrioritySelection | `energyPriority.pattern` | string (enum) | `"battery_first"` |

**Energy Priority Enum Mapping:**
- `"BatteryFirst"` → `"battery_first"`
- `"LoadFirst"` → `"load_first"`

### 3.3 Grid Peak Shaving (2 parameters)

| Parameter Name | Field Path | Type | Example Value |
|----------------|------------|------|---------------|
| GridPeakShavingEnabled | `gridPeakShaving.enabled` | boolean | `true` |
| GridPeakShavingLimitPower | `gridPeakShaving.limitPowerW` | integer | `8000` |

### 3.4 Time of Use - General (1 parameter)

| Parameter Name | Field Path | Type | Example Value |
|----------------|------------|------|---------------|
| TimeOfUseEnabled | `timeOfUse.enabled` | boolean | `true` |

### 3.5 Time of Use - Active Days (7 parameters)

| Parameter Name | Field Path | Type | Example Value |
|----------------|------------|------|---------------|
| Monday | `timeOfUse.daysActive.monday` | boolean | `true` |
| Tuesday | `timeOfUse.daysActive.tuesday` | boolean | `true` |
| Wednesday | `timeOfUse.daysActive.wednesday` | boolean | `true` |
| Thursday | `timeOfUse.daysActive.thursday` | boolean | `true` |
| Friday | `timeOfUse.daysActive.friday` | boolean | `true` |
| Saturday | `timeOfUse.daysActive.saturday` | boolean | `false` |
| Sunday | `timeOfUse.daysActive.sunday` | boolean | `false` |

### 3.6 Time of Use - Time Slots (42 parameters: 6 slots × 7 parameters)

Each time slot has the following 7 parameters:

| Parameter Suffix | Field Path Pattern | Type | Example Value |
|------------------|-------------------|------|---------------|
| Start | `timeOfUse.slots[N].startTimeLocal` | string (time) | `"08:00"` |
| End | `timeOfUse.slots[N].endTimeLocal` | string (time) | `"12:00"` |
| GridCharge | `timeOfUse.slots[N].allowGridCharge` | boolean | `true` |
| GenCharge | `timeOfUse.slots[N].allowGeneratorCharge` | boolean | `false` |
| MaxDischarge | `timeOfUse.slots[N].maxBatteryDischargePowerW` | integer | `3000` |
| MinBatterySOC | `timeOfUse.slots[N].batteryThresholdSocPercent` | integer | `20` |
| MinBatteryVoltage | `timeOfUse.slots[N].batteryThresholdVoltageV` | decimal | `48.0` |

**Time Slot Parameters (all 42):**

**Slot 1 (slots[0]):** TimeSlot1Start, TimeSlot1End, TimeSlot1GridCharge, TimeSlot1GenCharge, TimeSlot1MaxDischarge, TimeSlot1MinBatterySOC, TimeSlot1MinBatteryVoltage

**Slot 2 (slots[1]):** TimeSlot2Start, TimeSlot2End, TimeSlot2GridCharge, TimeSlot2GenCharge, TimeSlot2MaxDischarge, TimeSlot2MinBatterySOC, TimeSlot2MinBatteryVoltage

**Slot 3 (slots[2]):** TimeSlot3Start, TimeSlot3End, TimeSlot3GridCharge, TimeSlot3GenCharge, TimeSlot3MaxDischarge, TimeSlot3MinBatterySOC, TimeSlot3MinBatteryVoltage

**Slot 4 (slots[3]):** TimeSlot4Start, TimeSlot4End, TimeSlot4GridCharge, TimeSlot4GenCharge, TimeSlot4MaxDischarge, TimeSlot4MinBatterySOC, TimeSlot4MinBatteryVoltage

**Slot 5 (slots[4]):** TimeSlot5Start, TimeSlot5End, TimeSlot5GridCharge, TimeSlot5GenCharge, TimeSlot5MaxDischarge, TimeSlot5MinBatterySOC, TimeSlot5MinBatteryVoltage

**Slot 6 (slots[5]):** TimeSlot6Start, TimeSlot6End, TimeSlot6GridCharge, TimeSlot6GenCharge, TimeSlot6MaxDischarge, TimeSlot6MinBatterySOC, TimeSlot6MinBatteryVoltage

### Complete JSON Structure

```json
{
  "inverterMode": {
    "exportStrategy": "selling_first",
    "solarSellEnabled": true,
    "maxSellPowerW": 5000,
    "zeroExportLimitPowerW": 100,
    "forceOffGrid": false
  },
  "energyPriority": {
    "pattern": "battery_first"
  },
  "gridPeakShaving": {
    "enabled": true,
    "limitPowerW": 8000
  },
  "timeOfUse": {
    "enabled": true,
    "daysActive": {
      "monday": true,
      "tuesday": true,
      "wednesday": true,
      "thursday": true,
      "friday": true,
      "saturday": false,
      "sunday": false
    },
    "slots": [
      {
        "startTimeLocal": "08:00",
        "endTimeLocal": "12:00",
        "allowGridCharge": true,
        "allowGeneratorCharge": false,
        "maxBatteryDischargePowerW": 3000,
        "batteryThresholdSocPercent": 20,
        "batteryThresholdVoltageV": 48.0
      },
      {
        "startTimeLocal": "12:00",
        "endTimeLocal": "16:00",
        "allowGridCharge": false,
        "allowGeneratorCharge": false,
        "maxBatteryDischargePowerW": 5000,
        "batteryThresholdSocPercent": 10,
        "batteryThresholdVoltageV": 46.0
      },
      {
        "startTimeLocal": "16:00",
        "endTimeLocal": "20:00",
        "allowGridCharge": false,
        "allowGeneratorCharge": true,
        "maxBatteryDischargePowerW": 4000,
        "batteryThresholdSocPercent": 15,
        "batteryThresholdVoltageV": 47.0
      },
      {
        "startTimeLocal": "20:00",
        "endTimeLocal": "23:00",
        "allowGridCharge": true,
        "allowGeneratorCharge": false,
        "maxBatteryDischargePowerW": 2000,
        "batteryThresholdSocPercent": 30,
        "batteryThresholdVoltageV": 50.0
      },
      {
        "startTimeLocal": "23:00",
        "endTimeLocal": "02:00",
        "allowGridCharge": true,
        "allowGeneratorCharge": false,
        "maxBatteryDischargePowerW": 1000,
        "batteryThresholdSocPercent": 40,
        "batteryThresholdVoltageV": 51.0
      },
      {
        "startTimeLocal": "02:00",
        "endTimeLocal": "08:00",
        "allowGridCharge": false,
        "allowGeneratorCharge": false,
        "maxBatteryDischargePowerW": 1500,
        "batteryThresholdSocPercent": 25,
        "batteryThresholdVoltageV": 48.5
      }
    ]
  }
}
```

### Sample RPC Calls

**Example 1: Update only solar sell setting (Partial Update)**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters/{parameterId}
{
  "value": "true"  // For SolarSell
}

// Resulting RPC payload:
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "inverterMode": {
      "solarSellEnabled": true
    }
  }
}
```

**Example 2: Update export strategy (with enum mapping)**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters/{parameterId}
{
  "value": "SellingFirst"  // Application value
}

// Resulting RPC payload (value mapped to device format):
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "inverterMode": {
      "exportStrategy": "selling_first"
    }
  }
}
```

**Example 3: Update Time Slot 3 grid charging (Partial Array Update)**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters/{parameterId}
{
  "value": "true"  // For TimeSlot3GridCharge
}

// Resulting RPC payload (updates only slots[2].allowGridCharge):
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "timeOfUse": {
      "slots": [
        null,
        null,
        {
          "allowGridCharge": true
        }
      ]
    }
  }
}
```

**Example 4: Update multiple inverter mode settings**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    { "parameterId": "guid-1", "value": "SellingFirst" },  // ExportStrategySelection
    { "parameterId": "guid-2", "value": "true" },          // SolarSell
    { "parameterId": "guid-3", "value": "5000" }           // MaxSellPower
  ]
}

// Resulting RPC payload (optimized - grouped):
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "inverterMode": {
      "exportStrategy": "selling_first",
      "solarSellEnabled": true,
      "maxSellPowerW": 5000
    }
  }
}
```

**Example 5: Configure active days for Time of Use**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    { "parameterId": "guid-1", "value": "true" },   // Monday
    { "parameterId": "guid-2", "value": "true" },   // Tuesday
    { "parameterId": "guid-3", "value": "true" },   // Wednesday
    { "parameterId": "guid-4", "value": "true" },   // Thursday
    { "parameterId": "guid-5", "value": "true" },   // Friday
    { "parameterId": "guid-6", "value": "false" },  // Saturday
    { "parameterId": "guid-7", "value": "false" }   // Sunday
  ]
}

// Resulting RPC payload (optimized):
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "timeOfUse": {
      "daysActive": {
        "monday": true,
        "tuesday": true,
        "wednesday": true,
        "thursday": true,
        "friday": true,
        "saturday": false,
        "sunday": false
      }
    }
  }
}
```

**Example 6: Update complete Time Slot 1 configuration**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    { "parameterId": "guid-1", "value": "08:00" },  // TimeSlot1Start
    { "parameterId": "guid-2", "value": "12:00" },  // TimeSlot1End
    { "parameterId": "guid-3", "value": "true" },   // TimeSlot1GridCharge
    { "parameterId": "guid-4", "value": "false" },  // TimeSlot1GenCharge
    { "parameterId": "guid-5", "value": "3000" },   // TimeSlot1MaxDischarge
    { "parameterId": "guid-6", "value": "20" },     // TimeSlot1MinBatterySOC
    { "parameterId": "guid-7", "value": "48.0" }    // TimeSlot1MinBatteryVoltage
  ]
}

// Resulting RPC payload (optimized):
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "timeOfUse": {
      "slots": [
        {
          "startTimeLocal": "08:00",
          "endTimeLocal": "12:00",
          "allowGridCharge": true,
          "allowGeneratorCharge": false,
          "maxBatteryDischargePowerW": 3000,
          "batteryThresholdSocPercent": 20,
          "batteryThresholdVoltageV": 48.0
        }
      ]
    }
  }
}
```

---

## Batch Optimization

The system automatically groups parameters by RPC method to minimize network calls:

**Input: Mixed parameters across different RPC methods**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    { "parameterId": "guid-1", "value": "true" },   // GridChargingEnabled (Battery Charge Source)
    { "parameterId": "guid-2", "value": "10" },     // ShutdownSOC (Battery Threshold)
    { "parameterId": "guid-3", "value": "true" }    // SolarSell (System Work Mode)
  ]
}
```

**System automatically sends 3 separate RPC calls:**

**Call 1:**
```json
{
  "method": "setBatteryChargeSourceConfig",
  "params": {
    "grid": {
      "enabled": true
    }
  }
}
```

**Call 2:**
```json
{
  "method": "setBatteryThresholdConfig",
  "params": {
    "shutdownCapacityPercent": 10
  }
}
```

**Call 3:**
```json
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "inverterMode": {
      "solarSellEnabled": true
    }
  }
}
```

---

## Device Modes

The system also supports predefined device modes that update multiple parameters in a single API call:

```json
POST /api/DeviceSettings/device/{deviceId}/mode
{
  "mode": 2  // SelfConsumption mode
}
```

Each mode applies a preset configuration optimized for specific use cases:
- **0 - EnergySave**: Maximize battery life
- **1 - MaxPerformance**: Maximum power output
- **2 - SelfConsumption**: Maximize self-consumption of solar
- **3 - Backup**: Prioritize battery reserve for backup
- **4 - TimeOfUse**: Optimize based on time-of-use schedule
- **5 - OffGrid**: Operate independently from grid
- **6 - GridSelling**: Maximize grid export revenue

See `DeviceMode.cs` for complete mode configurations.

---

## Value Type Conversions

The system automatically handles type conversions:

| Parameter Type | Storage Format | RPC Format | Example |
|---------------|----------------|------------|---------|
| Boolean | `"true"` / `"false"` (string) | `true` / `false` (boolean) | `"true"` → `true` |
| Integer | `"100"` (string) | `100` (number) | `"100"` → `100` |
| Decimal | `"48.5"` (string) | `48.5` (number) | `"48.5"` → `48.5` |
| Enum | `"SellingFirst"` (PascalCase) | `"selling_first"` (snake_case) | `"SellingFirst"` → `"selling_first"` |
| Time | `"08:00"` (string) | `"08:00"` (string) | No conversion |

---

## Implementation Details

- **File**: `src/Application/Common/Models/RpcMethodMapping.cs`
- **Service**: `src/Infrastructure/Services/DeviceRpcExecutorService.cs`
- **Interface**: `src/Application/Common/Interfaces/IDeviceRpcExecutorService.cs`

The mapping is used by:
- `UpdateDeviceParameter` command (single parameter update)
- `UpdateDeviceParameters` command (batch parameter update)
- `SetDeviceMode` command (mode-based configuration)

All RPC operations follow the **RPC-first pattern**: the physical device is updated first via RPC, and the database is only updated if the RPC call succeeds, ensuring device and database stay synchronized.
