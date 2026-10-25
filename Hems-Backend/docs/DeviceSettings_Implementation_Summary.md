# Device Settings Management System - Implementation Summary

## Overview

I've implemented a comprehensive device settings management system that allows you to:
- Get and update device parameters (single or batch)
- Apply predefined device operating modes
- Efficiently communicate with devices via RPC

## Key Features

### 1. **RPC-First Update Pattern**
- Updates device via RPC **FIRST**
- Only updates database if device update succeeds
- Ensures consistency between device and database

### 2. **Efficient Batch Operations**
- Automatically groups parameters by RPC method
- Minimizes network calls (e.g., updating 10 parameters might result in only 3 RPC calls)

### 3. **Reusable Architecture**
- `IDeviceRpcExecutorService` can be used by other features
- Already implemented for device modes
- Can be used for automation, scheduling, diagnostics, etc.

### 4. **Device-Level Concurrency Control** (NEW)
- Each device has its own lock (SemaphoreSlim) to prevent concurrent updates
- Multiple users updating the same device will be queued and processed sequentially
- Prevents race conditions and conflicting commands being sent to the device
- Lock timeout of 2 minutes prevents deadlocks

### 5. **Retry with Exponential Backoff** (NEW)
- Similar to CSMA/CD collision resolution algorithm used in Ethernet
- Automatic retry for transient failures (timeout, device busy, network errors)
- Configurable retry options:
  - `RpcRetryOptions.Default`: 3 retries, 500ms initial delay, 2x backoff
  - `RpcRetryOptions.Aggressive`: 5 retries, 1000ms initial delay (used for rollback)
  - `RpcRetryOptions.NoRetry`: Immediate failure, no retries
- Jitter added to prevent thundering herd problem
- Error categorization: Timeout, DeviceBusy, DeviceOffline, NetworkError, ValidationError, etc.

### 6. **Atomic Batch Updates with Rollback** (NEW)
- If any parameter in a batch fails, all previously successful updates are automatically rolled back
- Ensures all-or-nothing semantics for batch operations
- Rollback uses aggressive retry to maximize success
- Original values captured before update for compensation
- Detailed logging for rollback operations

### 7. **Predefined Device Modes**
Seven preset operating modes that configure multiple parameters at once:
1. **Energy Save** - Minimize consumption, preserve battery
2. **Max Performance** - Maximum power output
3. **Self Consumption** - Use solar for own consumption
4. **Backup** - Keep battery charged for outages
5. **Time of Use** - Optimize for electricity rates
6. **Off-Grid** - Independent operation
7. **Grid Selling** - Maximize solar selling revenue

## Implementation Details

### Files Created

#### Application Layer
```
src/Application/DeviceSettings/
├── Models/
│   ├── RpcMethodMapping.cs              # Parameter to RPC mapping configuration
│   └── DeviceMode.cs                    # Device modes enum and configurations
├── Commands/
│   ├── UpdateDeviceParameter/
│   │   ├── UpdateDeviceParameterCommand.cs
│   │   └── UpdateDeviceParameterValidator.cs
│   ├── UpdateDeviceParameters/
│   │   ├── UpdateDeviceParametersCommand.cs
│   │   └── UpdateDeviceParametersValidator.cs
│   └── SetDeviceMode/
│       ├── SetDeviceModeCommand.cs
│       └── SetDeviceModeValidator.cs
└── Queries/
    ├── GetDeviceParameters/
    │   ├── GetDeviceParametersQuery.cs
    │   └── DeviceParameterDto.cs
    └── GetDeviceModes/
        └── GetDeviceModesQuery.cs
```

#### Infrastructure Layer
```
src/Infrastructure/
└── Services/
    └── DeviceRpcExecutorService.cs      # Reusable RPC execution service
```

#### Common Interfaces
```
src/Application/Common/Interfaces/
└── IDeviceRpcExecutorService.cs         # Service interface
```

#### API Endpoints
```
src/Web/Endpoints/
└── DeviceSettings.cs                    # All device settings endpoints
```

#### Documentation
```
docs/
├── API_DeviceSettings.md                # Comprehensive API documentation
└── DeviceSettings_Implementation_Summary.md  # This file
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/devicesettings/{setupDeviceId}/parameters` | Get all device parameters |
| PUT | `/api/devicesettings/{setupDeviceId}/parameters/{parameterId}` | Update single parameter |
| PUT | `/api/devicesettings/{setupDeviceId}/parameters` | Update multiple parameters |
| GET | `/api/devicesettings/modes` | Get available device modes |
| POST | `/api/devicesettings/{setupDeviceId}/mode` | Set device mode |

## Usage Examples

### 1. Get All Parameters for a Device

```bash
GET /api/devicesettings/a089891e-ad8c-42ef-8ce0-6516fcddbb51/parameters
```

Returns all parameters with current values, metadata, and last modified info.

### 2. Update a Single Parameter

```bash
PUT /api/devicesettings/a089891e-ad8c-42ef-8ce0-6516fcddbb51/parameters/00c97bc6-e3a6-4b07-9fd9-2f5812ca9801

{
  "value": "true"
}
```

Updates "SolarSell" to enabled. First updates device, then database.

### 3. Batch Update Multiple Parameters

```bash
PUT /api/devicesettings/a089891e-ad8c-42ef-8ce0-6516fcddbb51/parameters

{
  "parameters": [
    {"parameterId": "guid-1", "value": "true"},
    {"parameterId": "guid-2", "value": "12000"},
    {"parameterId": "guid-3", "value": "BatteryFirst"}
  ]
}
```

Efficiently updates multiple parameters, grouping by RPC method.

### 4. Set Device to Energy Save Mode

```bash
POST /api/devicesettings/a089891e-ad8c-42ef-8ce0-6516fcddbb51/mode

{
  "mode": 1
}
```

Applies all Energy Save mode settings in one operation.

## Parameter to RPC Mapping

The system includes comprehensive mapping for all parameters from your tree structure:

### Example Mappings

| Parameter Name | RPC Method | Field Path |
|----------------|------------|------------|
| SolarSell | setSystemWorkModeConfig | inverterMode.solarSellEnabled |
| GridChargingEnabled | setBatteryChargeSourceConfig | grid.enabled |
| ShutdownSOC | setBatteryThresholdConfig | shutdownCapacityPercent |
| SmartLoadEnabled | setGeneratorAndSmartLoadConfig | smartLoad.enabled |

**Total Mappings**: ~50 parameters mapped to 5 different RPC methods

## Device Mode Configurations

Each mode has predefined parameter values optimized for specific use cases:

### Energy Save Mode Example
```csharp
["GridChargingEnabled"] = "false"
["GeneratorChargingEnabled"] = "false"
["ShutdownSOC"] = "15"
["ExportStrategySelection"] = "ZeroExportToCT"
["GridPeakShavingEnabled"] = "true"
// ... and more
```

## Reusability

The `IDeviceRpcExecutorService` is designed for reuse:

### Current Usage
- Device parameter updates
- Device mode application

### Future Usage
- **Automation Rules**: Automatically adjust parameters based on conditions
- **Scheduling**: Time-based parameter changes
- **Remote Diagnostics**: Read device configuration
- **Bulk Operations**: Update same parameter across multiple devices
- **Parameter Synchronization**: Sync parameters from device to database

### Example: Using in Another Feature

```csharp
public class MyFeatureCommandHandler
{
    private readonly IDeviceRpcExecutorService _rpcExecutor;

    public async Task Handle(MyCommand command, CancellationToken ct)
    {
        // Update parameters for automation
        var parameters = new Dictionary<string, string>
        {
            ["SolarSell"] = "true",
            ["MaxSellPower"] = "12000"
        };

        var results = await _rpcExecutor.ExecuteParameterUpdatesAsync(
            command.DeviceId,
            parameters,
            ct);

        // Handle results...
    }
}
```

## Error Handling

The system provides detailed error information:

### Success Response
```json
{
  "success": true,
  "parameterId": "guid",
  "parameterName": "SolarSell",
  "newValue": "true",
  "rpcMethod": "setSystemWorkModeConfig"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Failed to update device: Device timeout"
}
```

### Partial Success (Batch)
```json
{
  "success": false,
  "message": "Failed to update device: setGeneratorAndSmartLoadConfig: Device timeout",
  "successfulUpdates": ["SolarSell", "MaxSellPower"],
  "failedUpdates": ["GeneratorCharge"]
}
```

## Benefits

1. **Consistency**: Device and database always in sync
2. **Efficiency**: Batch operations minimize RPC calls
3. **Reusability**: Services can be used across features
4. **Maintainability**: Clear separation of concerns
5. **User Experience**: Preset modes make configuration easy
6. **Reliability**: Comprehensive error handling and validation

## Next Steps / Future Enhancements

1. **Parameter Validation**: Add range validation based on element type
2. ~~**Rollback Support**: Auto-rollback if database fails after device update~~ IMPLEMENTED
3. **Audit Logging**: Track all parameter changes
4. **Custom Modes**: Let users save their own modes
5. **Parameter History**: Track value changes over time
6. **Bulk Device Updates**: Update same parameter across multiple devices
7. **Real-time Sync**: Automatically sync parameters from device events

## Concurrency & Reliability Improvements (January 2025)

The following improvements were added to enhance reliability when multiple users or processes update device settings:

### Device-Level Locking
- **Problem Solved**: Multiple users updating the same device simultaneously could cause race conditions
- **Solution**: `ConcurrentDictionary<Guid, SemaphoreSlim>` provides per-device locks
- **Behavior**: Updates to the same device are queued; different devices can update in parallel

### Exponential Backoff Retry
- **Problem Solved**: Transient failures (device busy, network hiccup) caused immediate failure
- **Solution**: CSMA/CD-style retry with exponential backoff and jitter
- **Configuration**:
  ```csharp
  // Default: 3 retries, 500ms->1000ms->2000ms delays
  RpcRetryOptions.Default

  // Aggressive: 5 retries, longer delays (for critical operations like rollback)
  RpcRetryOptions.Aggressive

  // Custom configuration
  new RpcRetryOptions
  {
      MaxRetries = 4,
      InitialDelayMs = 1000,
      MaxDelayMs = 10000,
      BackoffMultiplier = 2.0,
      UseJitter = true
  }
  ```

### Error Categorization
- **Problem Solved**: Generic "Device did not confirm update" messages were unhelpful
- **Solution**: Errors are now categorized:
  - `Timeout` - Device didn't respond in time
  - `DeviceBusy` - Device is processing another request
  - `DeviceOffline` - Device is unreachable
  - `NetworkError` - Network connectivity issue
  - `ValidationError` - Invalid parameter value
  - `DeviceRejected` - Device rejected the command
  - `MappingNotFound` - No RPC mapping for parameter

### Atomic Batch Updates with Rollback
- **Problem Solved**: Partial updates left device and database in inconsistent state
- **Solution**: If any parameter fails, all successful updates are rolled back
- **Flow**:
  ```
  1. Capture original values
  2. Update parameters one by one
  3. On failure:
     a. Stop processing
     b. Rollback successful updates in reverse order
     c. Report all parameters as failed
  ```

## Testing the Implementation

### 1. Get Parameters
```bash
GET /api/devicesettings/{your-setup-device-id}/parameters
```

### 2. Update Single Parameter
Find a parameter from step 1, then:
```bash
PUT /api/devicesettings/{setup-device-id}/parameters/{parameter-id}
{
  "value": "new-value"
}
```

### 3. Get Available Modes
```bash
GET /api/devicesettings/modes
```

### 4. Apply a Mode
```bash
POST /api/devicesettings/{setup-device-id}/mode
{
  "mode": 1
}
```

## Architecture Benefits for Device Mode Feature

You mentioned wanting to use this for device modes. Here's how it's perfectly suited:

1. **Single Endpoint Call**: `/api/devicesettings/{setupDeviceId}/mode`
2. **All Settings Applied**: One mode ID applies 10-15 parameters
3. **Efficient**: Parameters grouped by RPC method (3-4 RPC calls instead of 15)
4. **Atomic**: All parameters updated together
5. **Reusable Service**: Same `IDeviceRpcExecutorService` used by both features

### Example Mode Flow

```
Frontend: POST /mode with mode=1 (Energy Save)
    ↓
Backend: Gets Energy Save configuration (12 parameters)
    ↓
RPC Executor: Groups into 4 RPC methods
    ↓
Device: Updates via 4 RPC calls (instead of 12)
    ↓
Database: Updates all 12 parameters
    ↓
Response: Success with details
```

## Questions?

The implementation follows clean architecture principles and is designed for:
- Maintainability
- Reusability
- Scalability
- Testability

See `API_DeviceSettings.md` for complete API documentation with all examples and specifications.
