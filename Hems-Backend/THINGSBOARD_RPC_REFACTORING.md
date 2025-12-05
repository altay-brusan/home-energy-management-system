# ThingsBoard RPC Refactoring Summary

## Problem: Code Spaghetti 🍝

We had **3 different ways** to call ThingsBoard RPC:

1. **PowerFlowService** - Manual HTTP calls with custom auth handling
2. **GetDeviceSummaryQueryHandler** - Already using centralized `IThingsBoardConnectorService`
3. **Scattered RPC implementations** - Inconsistent error handling and logging

## Solution: Single Point of RPC Access ✅

All ThingsBoard RPC calls now go through **`IThingsBoardConnectorService`**

### Centralized RPC Methods

```csharp
// Application/Common/Interfaces/IThingsBoardConnectorService.cs

Task<bool> SendOneWayRpcAsync(
    Guid deviceId,
    string method,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken = default);

Task<TbRpcResponse?> SendTwoWayRpcAsync(
    Guid deviceId,
    string method,
    Dictionary<string, object> parameters,
    int timeoutMs = 5000,
    CancellationToken cancellationToken = default);
```

### Implementation Location

**Infrastructure/Services/ThingsBoardConnectorService.cs**
- Handles authentication automatically
- Consistent error handling
- Proper logging
- JWT token refresh
- Timeout management

## Changes Made

### 1. PowerFlowService Refactored ✅

**Before:**
```csharp
public class PowerFlowService : IPowerFlowService
{
    private readonly HttpClient _httpClient;
    private readonly IThingsBoardAuthService _authService;
    private readonly string _tbBaseUrl;
    
    public async Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, ...)
    {
        // Manual HTTP request
        var jwt = await _authService.GetJwtAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var body = new { method = "getPowerFlow", @params = new { } };
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        // Manual error handling...
    }
}
```

**After:**
```csharp
public class PowerFlowService : IPowerFlowService
{
    private readonly IThingsBoardConnectorService _tbConnector;
    private readonly ILogger<PowerFlowService> _logger;
    
    public async Task<PowerFlowDto?> GetPowerFlowAsync(Guid deviceId, ...)
    {
        // Use centralized RPC service
        var rpcResponse = await _tbConnector.SendTwoWayRpcAsync(
            thingsBoardDeviceId.Value,
            "getPowerFlow",
            new Dictionary<string, object>(),
            timeoutMs: 10000,
            cancellationToken);
            
        // Standardized response handling
        if (rpcResponse?.Success != true) return null;
    }
}
```

**Benefits:**
- ✅ No manual HTTP client management
- ✅ No manual JWT handling
- ✅ Consistent error handling
- ✅ Proper logging via ILogger
- ✅ Less code (removed ~30 lines)

### 2. Dependency Injection Updated ✅

**Before:**
```csharp
builder.Services.AddHttpClient<IPowerFlowService, PowerFlowService>();
```

**After:**
```csharp
builder.Services.AddScoped<IPowerFlowService, PowerFlowService>();
```

PowerFlowService no longer needs HttpClient - it uses the centralized ThingsBoardConnectorService which already has HttpClient configured.

## Current RPC Usage Across Codebase

### ✅ All Using Centralized Service:

1. **PowerFlowService** → `getPowerFlow` RPC
2. **GetDeviceSummaryQueryHandler** → Multiple RPCs:
   - `getInverterIdentity`
   - `getInverterInfo`
   - `getBatteryInfo` (if applicable)

## Architecture Benefits

### 1. Single Responsibility
- **ThingsBoardConnectorService**: All ThingsBoard communication
- **PowerFlowService**: Business logic for PowerFlow data
- **GetDeviceSummaryQueryHandler**: Business logic for device summary

### 2. Maintainability
- Change auth logic? → Update one place
- Change RPC endpoint? → Update one place
- Add retry logic? → Update one place

### 3. Testability
- Mock `IThingsBoardConnectorService` instead of HttpClient
- Easier unit testing
- Better separation of concerns

### 4. Consistency
- All RPC calls use same error handling
- All RPC calls use same logging format
- All RPC calls use same timeout management

## Example: How to Add New RPC Call

```csharp
// In any service (Application or Web layer)
public class MyService
{
    private readonly IThingsBoardConnectorService _tbConnector;
    
    public async Task<MyDto?> GetDataAsync(Guid deviceId, CancellationToken ct)
    {
        // Just call the centralized service!
        var response = await _tbConnector.SendTwoWayRpcAsync(
            deviceId,
            "getMyData",
            new Dictionary<string, object> 
            { 
                { "param1", "value1" } 
            },
            timeoutMs: 5000,
            ct);
            
        if (response?.Success != true) return null;
        
        // Deserialize and return
        var json = JsonSerializer.Serialize(response.Response);
        return JsonSerializer.Deserialize<MyDto>(json);
    }
}
```

## No More Code Spaghetti! 🎉

All ThingsBoard RPC calls now flow through one controlled, tested, and maintainable service.
