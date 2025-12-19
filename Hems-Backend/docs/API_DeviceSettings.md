# Device Settings API Documentation

## Overview

The Device Settings API provides comprehensive endpoints for managing device parameters and operating modes. The API follows a two-step update pattern:

1. **Update device via RPC first** - Ensures the physical device is updated
2. **Update database** - Only if the device update succeeds

This ensures data consistency between the device and the database.

## Architecture

### Key Components

- **RPC Mapping Service**: Maps parameter names to specific RPC methods and field paths
- **Device RPC Executor Service**: Reusable service for executing RPC calls (can be used by other features like device modes)
- **Parameter Commands**: Handles individual and batch parameter updates
- **Device Mode Commands**: Applies preset configurations

### RPC Grouping

The API automatically groups parameters by RPC method to minimize network calls. For example, updating multiple parameters that belong to the same RPC method (e.g., `SystemWorkModeConfigState`) will result in a single RPC call.

## Endpoints

### 1. Get Device Parameters

**GET** `/api/devicesettings/{setupDeviceId}/parameters`

Retrieves all parameters for a setup device with their current values from the database.

#### Parameters
- `setupDeviceId` (path, required): The setup device GUID

#### Response
```json
[
  {
    "id": "guid",
    "parameterId": "guid",
    "parameterName": "SolarSell",
    "parameterDescription": "Solar Sell Enable/Disable",
    "parameterGroupId": "guid",
    "parameterGroupName": "InverterMode",
    "value": "false",
    "elementType": "Boolean",
    "unit": "",
    "defaultValue": "false",
    "sortOrder": 1,
    "modelNumber": "single-phase-loftty",
    "lastModified": "2025-12-18T20:38:46.662295+03:00",
    "lastModifiedBy": "user-id"
  }
]
```

---

### 2. Update Single Parameter

**PUT** `/api/devicesettings/{setupDeviceId}/parameters/{parameterId}`

Updates a single device parameter. Updates the device via RPC first, then updates the database if successful.

#### Parameters
- `setupDeviceId` (path, required): The setup device GUID
- `parameterId` (path, required): The parameter GUID

#### Request Body
```json
{
  "value": "true"
}
```

#### Response (Success)
```json
{
  "success": true,
  "parameterId": "guid",
  "parameterName": "SolarSell",
  "newValue": "true",
  "rpcMethod": "setSystemWorkModeConfig"
}
```

#### Response (Error)
```json
{
  "success": false,
  "message": "Failed to update device: Device timeout"
}
```

---

### 3. Update Multiple Parameters (Batch)

**PUT** `/api/devicesettings/{setupDeviceId}/parameters`

Updates multiple device parameters in a single request. Groups parameters by RPC method to minimize RPC calls.

#### Parameters
- `setupDeviceId` (path, required): The setup device GUID

#### Request Body
```json
{
  "parameters": [
    {
      "parameterId": "guid-1",
      "value": "true"
    },
    {
      "parameterId": "guid-2",
      "value": "12000"
    },
    {
      "parameterId": "guid-3",
      "value": "BatteryFirst"
    }
  ]
}
```

#### Response (Success)
```json
{
  "success": true,
  "updatedParameters": [
    {
      "parameterId": "guid-1",
      "parameterName": "SolarSell",
      "newValue": "true"
    },
    {
      "parameterId": "guid-2",
      "parameterName": "MaxSellPower",
      "newValue": "12000"
    },
    {
      "parameterId": "guid-3",
      "parameterName": "EnergyPrioritySelection",
      "newValue": "BatteryFirst"
    }
  ],
  "rpcMethods": [
    "setSystemWorkModeConfig"
  ]
}
```

#### Response (Partial Success)
```json
{
  "success": false,
  "message": "Failed to update device: setGeneratorAndSmartLoadConfig: Device timeout",
  "successfulUpdates": ["SolarSell", "MaxSellPower"],
  "failedUpdates": ["GeneratorCharge"]
}
```

---

### 4. Get Available Device Modes

**GET** `/api/devicesettings/modes`

Retrieves all available predefined device operating modes.

#### Response
```json
[
  {
    "mode": 1,
    "name": "Energy Save",
    "description": "Prioritizes battery preservation and minimizes grid consumption"
  },
  {
    "mode": 2,
    "name": "Max Performance",
    "description": "Prioritizes power output and allows grid charging"
  },
  {
    "mode": 3,
    "name": "Self Consumption",
    "description": "Maximizes use of solar power for own consumption"
  },
  {
    "mode": 4,
    "name": "Backup",
    "description": "Keeps battery charged for backup power during outages"
  },
  {
    "mode": 5,
    "name": "Time of Use",
    "description": "Optimizes based on electricity rate schedules"
  },
  {
    "mode": 6,
    "name": "Off-Grid",
    "description": "Operates independently from the grid"
  },
  {
    "mode": 7,
    "name": "Grid Selling",
    "description": "Prioritizes selling excess solar power to the grid"
  }
]
```

---

### 5. Set Device Mode

**POST** `/api/devicesettings/{setupDeviceId}/mode`

Applies a predefined operating mode to the device. This updates multiple parameters at once based on the mode configuration.

#### Parameters
- `setupDeviceId` (path, required): The setup device GUID

#### Request Body
```json
{
  "mode": 1
}
```

**Available Modes:**
- `1` - Energy Save
- `2` - Max Performance
- `3` - Self Consumption
- `4` - Backup
- `5` - Time of Use
- `6` - Off-Grid
- `7` - Grid Selling

#### Response (Success)
```json
{
  "success": true,
  "mode": 1,
  "updatedParametersCount": 12,
  "rpcMethods": [
    "setBatteryChargeSourceConfig",
    "setBatteryThresholdConfig",
    "setSystemWorkModeConfig",
    "setGeneratorAndSmartLoadConfig"
  ]
}
```

#### Response (Error)
```json
{
  "success": false,
  "message": "Failed to apply mode to device: Device offline",
  "successfulParameters": ["GridChargingEnabled", "ShutdownSOC"],
  "failedParameters": ["SolarSell", "MaxSellPower"]
}
```

---

## Device Modes Configuration

### 1. Energy Save Mode
- **Goal**: Minimize energy consumption and preserve battery
- **Settings**:
  - Grid/Generator charging: Disabled
  - Battery protection: Conservative (shutdown at 15%)
  - Export strategy: Zero export
  - Peak shaving: Enabled

### 2. Max Performance Mode
- **Goal**: Maximum power output
- **Settings**:
  - Grid/Generator charging: Enabled
  - Battery protection: Aggressive (shutdown at 10%)
  - Export strategy: Selling first
  - Solar sell: Enabled
  - Peak shaving: Disabled

### 3. Self Consumption Mode
- **Goal**: Use solar power for own consumption
- **Settings**:
  - Grid/Generator charging: Disabled
  - Export strategy: Zero export
  - Smart load: Enabled
  - Focus on self-sufficiency

### 4. Backup Mode
- **Goal**: Keep battery charged for power outages
- **Settings**:
  - Grid/Generator charging: Enabled (high SOC thresholds)
  - Battery protection: Conservative
  - Peak shaving: Enabled
  - Generator peak shaving: Enabled

### 5. Time of Use Mode
- **Goal**: Optimize based on electricity rates
- **Settings**:
  - Grid charging: Enabled
  - Time of use scheduling: Enabled
  - All days active
  - Peak shaving: Enabled

### 6. Off-Grid Mode
- **Goal**: Independent operation
- **Settings**:
  - Grid charging: Disabled
  - Generator charging: Enabled
  - Force off-grid: Enabled
  - Smart load: Enabled

### 7. Grid Selling Mode
- **Goal**: Maximize revenue from excess solar
- **Settings**:
  - Export strategy: Selling first
  - Solar sell: Enabled
  - Max sell power: 12000W
  - Grid/Generator charging: Disabled

---

## Parameter to RPC Mapping

The API uses a comprehensive mapping system to route parameter updates to the correct RPC methods:

### Battery Charge Source Parameters → `setBatteryChargeSourceConfig`
- GridChargingEnabled → `grid.enabled`
- GridChargingStartSOC → `grid.startSocPercent`
- GridChargingCurrent → `grid.chargeCurrentA`
- GeneratorChargingEnabled → `generator.enabled`
- GeneratorChargingStartSOC → `generator.startSocPercent`
- GeneratorChargingCurrent → `generator.chargeCurrentA`
- GeneratorMaxRunTime → `generator.maxRunTimeHours`
- GeneratorCoolingTime → `generator.coolingTimeHours`

### Battery Threshold Parameters → `setBatteryThresholdConfig`
- ShutdownSOC → `shutdownCapacityPercent`
- LowBatteryWarning → `lowBatteryCapacityPercent`
- RestartSOC → `restartCapacityPercent`

### System Work Mode Parameters → `setSystemWorkModeConfig`
- ExportStrategySelection → `exportStrategy` (with custom mapping)
- EnergyPrioritySelection → `energyPriority.pattern`
- SolarSell → `inverterMode.solarSellEnabled`
- MaxSellPower → `inverterMode.maxSellPowerW`
- ZeroExportLimit → `inverterMode.zeroExportLimit`
- ForceOffGrid → `inverterMode.forceOffGrid`
- GridPeakShavingEnabled → `gridPeakShaving.enabled`
- GridPeakShavingLimitPower → `gridPeakShaving.limitPowerW`
- TimeOfUseEnabled → `timeOfUse.enabled`
- Monday through Sunday → `timeOfUse.daysActive.*`

### Generator & Smart Load Parameters → `setGeneratorAndSmartLoadConfig`
- GeneratorCharge → `generatorChargeEnabled`
- GeneratorGridCharge → `gridChargeEnabled`
- GeneratorStartSOC → `startSocPercent`
- GeneratorChargeCurrent → `chargeCurrentA`
- GeneratorPeakShavingEnabled → `generatorPeakShaving.enabled`
- GeneratorPeakShavingLimitPower → `generatorPeakShaving.limitPowerW`
- SmartLoadEnabled → `smartLoad.enabled`
- SmartLoadOffSOC → `smartLoad.offSocPercent`
- SmartLoadOnSOC → `smartLoad.onSocPercent`
- SmartLoadPVPowerThreshold → `smartLoad.pvPowerThresholdW`
- SmartLoadOpenDelay → `smartLoad.openDelayMinutes`
- SmartLoadOnGridAlwaysOn → `smartLoad.onGridAlwaysOn`

---

## Error Handling

### Common Error Responses

#### Device Offline
```json
{
  "success": false,
  "message": "Failed to update device: RPC call failed"
}
```

#### Parameter Not Found
```json
{
  "success": false,
  "message": "Parameter not found for this device"
}
```

#### Validation Error
```json
{
  "errors": {
    "Value": ["Value cannot be null"],
    "ParameterId": ["ParameterId is required"]
  }
}
```

#### Device Timeout
```json
{
  "success": false,
  "message": "Failed to communicate with device. Device may be offline or timed out."
}
```

---

## Usage Examples

### Example 1: Update Solar Sell Setting

```bash
curl -X PUT "https://api.example.com/api/devicesettings/a089891e-ad8c-42ef-8ce0-6516fcddbb51/parameters/00c97bc6-e3a6-4b07-9fd9-2f5812ca9801" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"value": "true"}'
```

### Example 2: Batch Update Multiple Settings

```bash
curl -X PUT "https://api.example.com/api/devicesettings/a089891e-ad8c-42ef-8ce0-6516fcddbb51/parameters" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "parameters": [
      {"parameterId": "00c97bc6-e3a6-4b07-9fd9-2f5812ca9801", "value": "true"},
      {"parameterId": "56c0de53-f2f0-4457-96eb-20d43be27e51", "value": "12000"},
      {"parameterId": "738dcf34-f285-46ea-8988-9f33edf1a6d0", "value": "BatteryFirst"}
    ]
  }'
```

### Example 3: Set Device to Energy Save Mode

```bash
curl -X POST "https://api.example.com/api/devicesettings/a089891e-ad8c-42ef-8ce0-6516fcddbb51/mode" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"mode": 1}'
```

---

## Best Practices

1. **Use batch updates when possible**: Batch updates are more efficient as they group parameters by RPC method
2. **Check device online status**: Before updating, ensure the device is online to avoid timeouts
3. **Use device modes for common scenarios**: Instead of updating individual parameters, use predefined modes when appropriate
4. **Handle partial failures**: When batch updating, check for partial failures and retry failed parameters
5. **Monitor RPC methods**: The response includes which RPC methods were called, useful for debugging

---

## Integration with Other Features

The `IDeviceRpcExecutorService` is designed to be reusable across different features:

- **Device Settings**: Current implementation
- **Device Modes**: Preset configurations (implemented)
- **Automation Rules**: Future feature for automatic parameter adjustments
- **Scheduling**: Time-based parameter changes
- **Remote Diagnostics**: Reading device configuration

---

## Troubleshooting

### Device not responding
- Check if device is online via device status endpoint
- Verify ThingsBoard connectivity
- Check RPC timeout settings (default: 10 seconds)

### Parameter not updating in database
- Verify RPC call succeeded (check response)
- Check database permissions
- Review application logs for errors

### Partial batch update failure
- Review which RPC method failed
- Check device logs for specific error
- Retry failed parameters individually

---

## Future Enhancements

1. **Parameter validation**: Add value range validation based on parameter type
2. **Rollback support**: Automatic rollback if database update fails after successful RPC
3. **Audit logging**: Track all parameter changes with user and timestamp
4. **Bulk device updates**: Update same parameter across multiple devices
5. **Parameter history**: Track historical values of parameters
6. **Custom modes**: Allow users to save their own custom modes
