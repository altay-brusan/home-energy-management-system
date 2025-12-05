# ThingsBoard RPC Centralization - Complete Audit

## ✅ STATUS: FULLY CENTRALIZED

All ThingsBoard RPC calls in the project now use the centralized `IThingsBoardConnectorService`.

---

## Current RPC Call Locations

### 1. ✅ PowerFlowService (Web Layer)
**Location**: `src/Web/Services/PowerFlowService.cs`

**Method**: `getPowerFlow`

**Status**: ✅ **REFACTORED** - Now uses `_tbConnector.SendTwoWayRpcAsync()`

```csharp
var rpcResponse = await _tbConnector.SendTwoWayRpcAsync(
    thingsBoardDeviceId.Value,
    "getPowerFlow",
    new Dictionary<string, object>(),
    timeoutMs: 10000,
    cancellationToken);
```

**Dependencies**:
- `IThingsBoardConnectorService` (centralized)
- `IDeviceMappingService` (maps local ID → ThingsBoard ID)
- `IDeviceHeartbeatCache` (tracks device heartbeat)

---

### 2. ✅ GetDeviceSummaryQueryHandler (Application Layer)
**Location**: `src/Application/DeviceComponents/Queries/GetDeviceSummary/GetDeviceSummaryQueryHandler.cs`

**Methods**:
1. `getInverterIdentity` - Gets inverter metadata
2. `getInverterInfo` - Gets inverter state (active power, etc.)
3. `getBatterySocState` - Gets battery state of charge

**Status**: ✅ **ALREADY USING CENTRALIZED SERVICE**

```csharp
// Example: GetInverterIdentityFromRpcAsync
var rpcResponse = await _tbService.SendTwoWayRpcAsync(
    tbDeviceId,
    "getInverterIdentity",
    new Dictionary<string, object>(),
    timeoutMs: 10000,
    cancellationToken);
```

**Dependencies**:
- `IThingsBoardConnectorService` (centralized)
- `IDeviceMappingService`
- `IApplicationDbContext`

---

### 3. ✅ GetComponentLiveDataQueryHandler (Application Layer)
**Location**: `src/Application/Devices/Queries/GetComponentLiveData/GetComponentLiveDataQueryHandler.cs`

**Target**: DeviceSimulator (NOT ThingsBoard)

**Status**: ✅ **NOT APPLICABLE** - Calls local DeviceSimulator service, not ThingsBoard

```csharp
// Calls DeviceSimulator, not ThingsBoard RPC
var endpoint = $"{deviceSimulatorUrl}/api/Device/takeLiveData";
var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
```

---

## RPC Centralization Architecture

```
┌────────────────────────────────────────────────┐
│           APPLICATION LAYER                    │
│                                                │
│  • GetDeviceSummaryQueryHandler                │
│    - getInverterIdentity                       │
│    - getInverterInfo                           │
│    - getBatterySocState                        │
│                                                │
│  • Other handlers/commands                     │
└──────────────┬─────────────────────────────────┘
               │
               │ IThingsBoardConnectorService
               │
               ▼
┌────────────────────────────────────────────────┐
│              WEB LAYER                         │
│                                                │
│  • PowerFlowService                            │
│    - getPowerFlow                              │
│                                                │
└──────────────┬─────────────────────────────────┘
               │
               │ IThingsBoardConnectorService
               │
               ▼
┌────────────────────────────────────────────────┐
│        INFRASTRUCTURE LAYER                    │
│                                                │
│  ThingsBoardConnectorService                   │
│  (Single Source of Truth)                      │
│                                                │
│  • SendTwoWayRpcAsync()                        │
│  • SendOneWayRpcAsync()                        │
│  • EnsureAuthenticatedAsync()                  │
│  • JWT token management                        │
│  • Error handling & logging                    │
│  • Timeout management                          │
└────────────────────────────────────────────────┘
               │
               ▼
     ThingsBoard Platform
```

---

## Code Pattern Analysis

### ✅ CORRECT Pattern (All current code follows this)

```csharp
public class MyService
{
    private readonly IThingsBoardConnectorService _tbConnector;
    
    public async Task<MyDto?> GetDataAsync(Guid deviceId, CancellationToken ct)
    {
        var rpcResponse = await _tbConnector.SendTwoWayRpcAsync(
            deviceId,
            "myRpcMethod",
            new Dictionary<string, object> { /* params */ },
            timeoutMs: 10000,
            ct);
            
        if (rpcResponse?.Success != true || rpcResponse.Response == null)
        {
            _logger.LogWarning("RPC failed: {Error}", rpcResponse?.Error);
            return null;
        }
        
        // Deserialize response
        var json = JsonSerializer.Serialize(rpcResponse.Response);
        return JsonSerializer.Deserialize<MyDto>(json);
    }
}
```

### ❌ OLD Pattern (No longer exists in codebase)

```csharp
// REMOVED - This pattern no longer exists
public class OldService
{
    private readonly HttpClient _httpClient;
    private readonly IThingsBoardAuthService _authService;
    private readonly string _tbBaseUrl;
    
    // Manual HTTP calls - REMOVED
    var jwt = await _authService.GetJwtAsync(cancellationToken);
    var url = $"{_tbBaseUrl}/api/plugins/rpc/twoway/{deviceId}?timeout={timeoutMs}";
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
    // ... manual request building
}
```

---

## Benefits Achieved

### 1. ✅ Single Responsibility
- **ThingsBoardConnectorService**: All TB communication
- **PowerFlowService**: Business logic for PowerFlow
- **GetDeviceSummaryQueryHandler**: Business logic for device summary

### 2. ✅ Maintainability
- Authentication logic: **1 place**
- RPC endpoint format: **1 place**
- Error handling: **1 place**
- Logging: **1 place**

### 3. ✅ Testability
- Mock `IThingsBoardConnectorService` instead of `HttpClient`
- No need to mock JWT authentication
- Cleaner unit tests

### 4. ✅ Consistency
- All RPC calls use same timeout (configurable)
- All RPC calls use same error handling
- All RPC calls use same logging format

### 5. ✅ No Code Duplication
- Before: 3+ implementations of RPC calling logic
- After: 1 centralized implementation

---

## Audit Results

| Service/Handler | RPC Methods | Status | Uses Centralized Service |
|----------------|-------------|--------|-------------------------|
| PowerFlowService | getPowerFlow | ✅ Refactored | Yes |
| GetDeviceSummaryQueryHandler | getInverterIdentity | ✅ Already Good | Yes |
| GetDeviceSummaryQueryHandler | getInverterInfo | ✅ Already Good | Yes |
| GetDeviceSummaryQueryHandler | getBatterySocState | ✅ Already Good | Yes |
| GetComponentLiveDataQueryHandler | N/A | ✅ Not Applicable | Calls DeviceSimulator |

---

## No Manual RPC Calls Remaining

**Search Results:**
- ❌ No manual `HttpClient.SendAsync` to ThingsBoard
- ❌ No manual JWT auth handling for RPC
- ❌ No hardcoded `api/plugins/rpc/twoway` URLs outside connector
- ❌ No duplicate RPC implementations

**All RPC logic is in ThingsBoardConnectorService** ✅

---

## Future RPC Additions

To add a new RPC call, follow this pattern:

```csharp
public class MyNewService
{
    private readonly IThingsBoardConnectorService _tbConnector;
    private readonly ILogger<MyNewService> _logger;
    
    public MyNewService(
        IThingsBoardConnectorService tbConnector,
        ILogger<MyNewService> logger)
    {
        _tbConnector = tbConnector;
        _logger = logger;
    }
    
    public async Task<MyDataDto?> GetMyDataAsync(
        Guid deviceId, 
        CancellationToken ct = default)
    {
        // Call centralized RPC service
        var rpcResponse = await _tbConnector.SendTwoWayRpcAsync(
            deviceId,
            "getMyData",  // Your RPC method name
            new Dictionary<string, object> 
            { 
                // Optional parameters
                { "param1", "value1" }
            },
            timeoutMs: 10000,
            ct);
            
        if (rpcResponse?.Success != true || rpcResponse.Response == null)
        {
            _logger.LogWarning(
                "Failed to get my data for device {DeviceId}. Error: {Error}", 
                deviceId, 
                rpcResponse?.Error);
            return null;
        }
        
        // Deserialize response
        var json = JsonSerializer.Serialize(rpcResponse.Response);
        return JsonSerializer.Deserialize<MyDataDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
```

---

## Conclusion

✅ **All ThingsBoard RPC calls are now centralized through `IThingsBoardConnectorService`**

✅ **No code spaghetti - Single point of responsibility**

✅ **Consistent, maintainable, testable architecture**
