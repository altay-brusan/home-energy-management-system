# RPC Timeout Configuration

## Overview

The RPC timeout for device communication has been set to **60 seconds (60000ms)** to accommodate the complex Modbus operations performed by the Node-RED gateway device.

## Why 60 Seconds?

### Device Architecture

The device uses Node-RED with Modbus RTU over RS485 (9600 baud) to communicate with the physical inverter. Each RPC operation involves multiple sequential Modbus register operations.

### SystemWorkModeConfig Operations

#### Read Operation (getSystemWorkModeConfig)

The device performs **9 parallel Modbus read operations**:

| Register Range | Purpose | Quantity |
|----------------|---------|----------|
| 241-246 | Basic config (work mode, export strategy, max sell, solar sell, TOU enable+days) | 6 registers |
| 248-253 | TOU time points (6 slots) | 6 registers |
| 254-259 | TOU power limits (6 slots) | 6 registers |
| 260-265 | TOU voltage thresholds (6 slots) | 6 registers |
| 266-271 | TOU capacity thresholds (6 slots) | 6 registers |
| 272-277 | TOU charge enable flags (6 slots) | 6 registers |
| 278 | Grid peak shaving enable (bit field) | 1 register |
| 279 | Force off-grid (bit field) | 1 register |
| 291 | Grid peak shaving power limit | 1 register |

**Total**: 9 separate Modbus read operations

**Processing**:
1. All 9 reads execute in parallel
2. Results are collected and assembled
3. JSON response is built from Modbus register values
4. Response is sent back to backend

#### Write Operation (setSystemWorkModeConfig)

The device performs **sequential Modbus write operations**:

1. Parses incoming JSON parameters
2. Queues register writes (can be 1-40+ registers depending on parameters)
3. Writes registers **sequentially** with **500ms delay** between each write
4. After all writes complete, **reads all 9 register groups again** to verify and return final state
5. Sends response with write results and final state

**Example timeline for updating 10 parameters**:
- Parse JSON: ~100ms
- Write 10 registers @ 500ms each: ~5 seconds
- Read 9 register groups: ~2-3 seconds (parallel)
- Build JSON response: ~100ms
- **Total**: ~8 seconds minimum

**Worst case (updating all TOU slots + all settings)**:
- Up to 40+ register writes @ 500ms each: ~20+ seconds
- Plus read-back time: ~2-3 seconds
- **Total**: ~25 seconds

### Network and Serial Communication Delays

Additional delays include:
- **Modbus RTU at 9600 baud**: Each register read/write takes ~100-200ms
- **Serial communication overhead**: Start bits, stop bits, parity, inter-frame delays
- **Network latency**: ThingsBoard MQTT → Gateway → Backend roundtrip
- **Processing overhead**: Node-RED flow execution, JSON parsing/building

### Why Not Less?

| Timeout | Risk |
|---------|------|
| 10 seconds | ❌ Insufficient - simple writes timeout |
| 20 seconds | ⚠️ Risky - complex writes (multiple TOU slots) may timeout |
| 30 seconds | ⚠️ Acceptable for most cases but no safety margin |
| 60 seconds | ✅ Safe - handles worst case + network delays |

## Timeout Locations

All timeouts are configured in `DeviceRpcExecutorService.cs`:

```csharp
// Read operations
var currentResponse = await _tbService.SendTwoWayRpcAsync(
    deviceId: tbDeviceId,
    method: config.RpcGetMethod,
    parameters: getCurrentParams,
    timeoutMs: 60000, // 60 seconds
    cancellationToken: cancellationToken
);

// Write operations
var updateResponse = await _tbService.SendTwoWayRpcAsync(
    deviceId: tbDeviceId,
    method: config.RpcMethod,
    parameters: normalizedConfig,
    timeoutMs: 60000, // 60 seconds
    cancellationToken: cancellationToken
);
```

## Methods Affected

All RPC executor methods have 60-second timeout:

1. **ExecuteParameterUpdateAsync** - Single parameter update
   - Reads current config (9 Modbus reads)
   - Writes single parameter
   - Reads back for verification

2. **ExecuteParameterUpdatesAsync** - Batch parameter update
   - Reads current config (9 Modbus reads)
   - Writes multiple parameters sequentially
   - Reads back for verification

3. **GetParameterValueAsync** - Single parameter read
   - Reads current config (9 Modbus reads)

4. **GetParameterValuesAsync** - Batch parameter read
   - Reads current config (9 Modbus reads)

## Alternative Configurations

### BatteryChargeSourceConfig & BatteryThresholdConfig

These configurations involve fewer Modbus registers and could potentially use shorter timeouts, but for consistency and safety, all RPC operations use 60 seconds.

If specific timeouts per RPC method are needed in the future, the timeout could be made configurable per `RpcMethodConfig`:

```csharp
public record RpcMethodConfig
{
    public required string RpcMethod { get; init; }
    public required string RpcGetMethod { get; init; }
    public required string FieldPath { get; init; }
    public required string RpcKind { get; init; }
    public Func<string, object>? ValueMapper { get; init; }
    public int TimeoutMs { get; init; } = 60000; // Default 60 seconds
}
```

## Performance Considerations

### Impact on User Experience

A 60-second timeout means:
- **Best case**: Response in 2-3 seconds (simple read)
- **Typical case**: Response in 5-10 seconds (simple write)
- **Worst case**: Response in 20-30 seconds (complex write with many parameters)
- **Timeout**: Only if device completely fails to respond after 60 seconds

### Frontend Recommendations

The frontend should:
1. Show loading indicators during parameter updates
2. Display progress messages for batch operations
3. Implement request timeout UI (with 60+ second timeout)
4. Consider disabling UI during writes to prevent multiple concurrent requests

### Example User Messages

```
// Simple update (e.g., toggle SolarSell)
"Updating parameter..." (completes in ~3-5 seconds)

// Time slot update
"Updating time slot configuration..." (completes in ~5-8 seconds)

// Mode change (updates multiple parameters)
"Applying device mode configuration..." (completes in ~10-15 seconds)

// Batch update of all 6 time slots
"Updating time-of-use schedule. This may take up to 30 seconds..." (completes in ~20-30 seconds)
```

## Monitoring and Debugging

### Logging

The service logs warnings for slow operations:
- If RPC takes > 30 seconds, log warning
- If RPC takes > 45 seconds, log error

### Metrics to Track

1. **RPC operation duration** by method
2. **Timeout rate** (operations that hit 60s limit)
3. **Average response time** per RPC method
4. **Parameter count vs duration** correlation (for batch updates)

## Future Optimizations

Potential improvements to reduce timeout requirements:

1. **Selective read-back**: Only read affected registers instead of all 9 groups
2. **Parallel writes**: If device supports, write multiple registers in parallel
3. **Caching**: Cache current state on gateway, reduce unnecessary reads
4. **Differential updates**: Only write registers that actually changed
5. **Faster baud rate**: Upgrade from 9600 to 19200 or 38400 baud if supported

## Summary

**Current Configuration**: 60 seconds for all RPC operations

**Rationale**:
- Accommodates up to 40+ sequential Modbus register writes @ 500ms each
- Includes time for 9 parallel Modbus reads for state verification
- Provides safety margin for network delays and serial communication overhead

**Status**: ✅ **Production Ready**

This timeout is conservative but ensures reliable operation even in worst-case scenarios with slow networks or complex multi-parameter updates.
