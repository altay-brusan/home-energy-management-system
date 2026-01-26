# Grid Standard Settings - Device Restart Handling Implementation

## Overview

This document describes the backend implementation for handling device settings that cause the device to restart (specifically Grid Standard settings like `setGridCodeConfig` and `setSystemConnectionStructure`).

## Problem Statement

When certain device parameters are changed (Grid Standard settings), the device restarts. This creates issues:
1. Normal write-verify pattern fails because device goes offline during verification
2. API times out waiting for verification that will never come
3. Frontend doesn't know the device will restart
4. No way to distinguish between "update failed" and "update succeeded but device is restarting"

## Solution Implemented

### 1. Restart-Causing Method Detection

**File:** `src/Application/Common/Models/RpcMethodMapping.cs`

Added static detection of which RPC methods cause device restart:

```csharp
public static readonly HashSet<string> RestartCausingMethods = new()
{
    "setGridCodeConfig",
    "setSystemConnectionStructure"
};

public static bool IsRestartCausingMethod(string rpcMethod);
public static bool IsRestartCausingParameter(string parameterName);
```

**Parameters that cause restart:**
- `GridStandard` (uses `setGridCodeConfig`)
- `GridFrequency` (uses `setGridCodeConfig`)
- `OutputVoltageLevel` (uses `setGridCodeConfig`)
- `PhaseType` (uses `setSystemConnectionStructure`)
- `GridLevel` (uses `setSystemConnectionStructure`)
- `ITSystemNeutralGrounded` (uses `setSystemConnectionStructure`)
- And other parameters mapped to these RPC methods

### 2. Fire-and-Forget RPC Execution

**File:** `src/Infrastructure/Services/DeviceRpcExecutorService.cs`

Added new method for restart-causing parameters:

```csharp
Task<RpcExecutionResult> ExecuteFireAndForgetUpdateAsync(
    Guid tbDeviceId,
    string parameterName,
    string value,
    CancellationToken cancellationToken = default);
```

**Behavior:**
- Sends write RPC with shorter timeout (30 seconds)
- Does NOT attempt verification read
- Treats timeout/network errors as "soft success" (device likely started restarting)
- Logs informative messages about the fire-and-forget nature

### 3. Two-Phase Parameter Processing

**File:** `src/Infrastructure/Services/DeviceParameterUpdateService.cs`

Modified `UpdateParametersAsync` to process parameters in two phases:

**Phase 1: Regular Parameters**
- Uses existing write-verify-retry pattern
- Full verification that parameter was applied

**Phase 2: Restart-Causing Parameters (processed LAST)**
- Uses fire-and-forget pattern
- No verification (device will restart)
- Updates local database assuming write was sent
- Sets restart flags in result

**Why process restart-causing parameters last?**
- Ensures all other parameters are applied before device restarts
- Prevents partial updates where some parameters might be lost

### 4. Enhanced Response DTOs

**Files:**
- `src/Application/Common/Interfaces/IDeviceParameterUpdateService.cs`
- `src/Application/DeviceSettings/Commands/UpdateDeviceParameters/UpdateDeviceParameters.cs`

Added new fields to results:

```csharp
public class ParameterUpdateResult
{
    // ... existing fields ...

    /// <summary>
    /// Indicates that the device will restart as a result of this update.
    /// </summary>
    public bool DeviceWillRestart { get; set; }

    /// <summary>
    /// Reason for device restart (e.g., "Grid Standard settings changed").
    /// </summary>
    public string? RestartReason { get; set; }

    /// <summary>
    /// List of parameter names that caused the restart.
    /// </summary>
    public List<string> RestartCausingParameters { get; set; } = new();
}
```

### 5. Updated API Response

**File:** `src/Web/Endpoints/DeviceSettings.cs`

The `PUT /api/DeviceSettings/device/{deviceId}/parameters` endpoint now returns:

```json
{
  "success": true,
  "totalParameters": 5,
  "successCount": 5,
  "failedCount": 0,
  "failedParameters": [],
  "duration": "00:00:03.245",
  "deviceWillRestart": true,
  "restartReason": "Grid Standard settings changed",
  "restartCausingParameters": ["GridStandard", "PhaseType"]
}
```

## Flow Diagram

```
Frontend sends parameter update request
         │
         ▼
┌─────────────────────────────────────┐
│ Separate parameters into:           │
│ - Regular parameters                │
│ - Restart-causing parameters        │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ PHASE 1: Process regular parameters │
│ - Write RPC                         │
│ - Wait for verification             │
│ - Retry if needed                   │
│ - Update database                   │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ PHASE 2: Process restart-causing    │
│ parameters (LAST)                   │
│ - Write RPC (fire-and-forget)       │
│ - NO verification                   │
│ - Update database                   │
│ - Set DeviceWillRestart=true        │
└─────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ Return response with restart info   │
└─────────────────────────────────────┘
         │
         ▼
    Device restarts
```

## What Frontend Needs To Do

Based on the `deviceWillRestart` flag, frontend should:

1. **Before submitting** (optional pre-check):
   - Check if any changed parameters are restart-causing
   - Show confirmation dialog: "Changing Grid Standard settings will restart the device. Continue?"

2. **After receiving response with `deviceWillRestart: true`**:
   - Show "Device is restarting..." overlay
   - Disable all device controls
   - Start polling device status (or use SignalR `DeviceStatusHub`)
   - Set timeout (4 minutes recommended)
   - Re-enable UI when device comes back online

3. **Handle mixed changes** (optional):
   - If user changed both regular AND restart-causing parameters
   - Consider showing: "Grid Standard changes will be applied. Other changes were also saved."

## Files Changed Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `RpcMethodMapping.cs` | Added | Restart-causing method detection |
| `IDeviceRpcExecutorService.cs` | Modified | Added fire-and-forget interface method |
| `DeviceRpcExecutorService.cs` | Modified | Implemented fire-and-forget execution |
| `IDeviceParameterUpdateService.cs` | Modified | Added restart fields to result DTO |
| `DeviceParameterUpdateService.cs` | Modified | Two-phase processing logic |
| `UpdateDeviceParameters.cs` | Modified | Added restart fields to command result |
| `DeviceSettings.cs` | Modified | Include restart info in API response |

## Testing Verification

- All 36 existing unit tests pass
- Application and Infrastructure projects build successfully

## Open Questions / Future Considerations

1. **Should we add a pre-check endpoint?**
   - `GET /api/DeviceSettings/WillCauseRestart?parameters=...`
   - Frontend could check before showing confirmation

2. **Should we prevent mixed changes?**
   - Currently: All parameters are processed (regular first, then restart-causing)
   - Alternative: Reject requests that mix regular and restart-causing parameters

3. **Persist pending restart state?**
   - If app closes during device restart, how to handle?
   - Could store "device restarting" state in database

4. **Exact restart timeout?**
   - Implementation assumes ~2-4 minutes
   - May need adjustment based on real device behavior
