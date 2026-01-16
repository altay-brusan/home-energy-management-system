# API Endpoint Refactoring Summary

## Overview

This refactoring reorganized device parameter management endpoints into two clear categories:
1. **DeviceSettings** - Device instance management (user-facing operations)
2. **ParameterSchemas** - Template/schema management (admin-only configuration)

## Changes Made

### 1. DeviceSettings Endpoint (`/api/DeviceSettings`)

**Purpose**: Manage actual device parameter values and configurations for specific device instances.

**Old Endpoints Merged**:
- `SetupDeviceParameterValues` endpoints (database-only operations)

**New Endpoints Added**:
- RPC-synced parameter operations (device + database updates)
- Device mode management
- Batch parameter updates

**Endpoint Structure**:

```
/api/DeviceSettings
├── RPC-SYNCED OPERATIONS (User-facing, recommended)
│   ├── GET    /device/{deviceId}/parameters            - Get all parameters
│   ├── GET    /device/{deviceId}/tree                  - Get parameters tree structure
│   ├── PUT    /device/{deviceId}/parameters/{parameterId} - Update single parameter
│   ├── PUT    /device/{deviceId}/parameters            - Update multiple parameters
│   ├── GET    /modes                                   - Get available modes
│   └── POST   /device/{deviceId}/mode                  - Set device mode
│
├── DATABASE-ONLY OPERATIONS (Admin tools, under /admin prefix)
│   ├── GET    /admin                                   - Get parameter values by setupDeviceId
│   ├── GET    /admin/tree                              - Get tree structure by setupDeviceId
│   ├── POST   /admin                                   - Create parameter value
│   ├── PUT    /admin/{id}                              - Update value (DB only)
│   ├── DELETE /admin/{id}                              - Delete value
│   ├── POST   /admin/device/{deviceId}/initialize      - Initialize device settings
│   └── PUT    /admin/device/{deviceId}/parameter/{parameterId} - Update value (DB only)
│
└── DEMO/TESTING
    └── POST   /demo/export-strategy                    - Demo endpoint for testing
```

**Authorization**:
- RPC-synced operations: All authenticated users
- Database-only operations (`/admin`): Admin only
- Demo operations (`/demo`): All authenticated users

---

### 2. ParameterSchemas Endpoint (`/api/ParameterSchemas`)

**Purpose**: Manage parameter definitions, groups, and model-specific default values (templates).

**Old Endpoints Merged**:
- `ParameterGroups` - Parameter category organization
- `Parameters` - Parameter definitions
- `ParameterValueSettingSchemes` - Model-specific default values

**Endpoint Structure**:

```
/api/ParameterSchemas
├── ADMIN OPERATIONS (Admin only)
│   ├── PARAMETER GROUPS
│   │   ├── GET    /Admin/Groups                - List all groups
│   │   ├── GET    /Admin/Groups/{id}           - Get specific group
│   │   ├── POST   /Admin/Groups                - Create group
│   │   ├── PUT    /Admin/Groups/{id}           - Update group
│   │   └── DELETE /Admin/Groups/{id}           - Delete group
│   │
│   ├── PARAMETERS
│   │   ├── GET    /Admin/Parameters            - List parameters
│   │   ├── GET    /Admin/Parameters/{id}       - Get specific parameter
│   │   ├── POST   /Admin/Parameters            - Create parameter
│   │   ├── PUT    /Admin/Parameters/{id}       - Update parameter
│   │   └── DELETE /Admin/Parameters/{id}       - Delete parameter
│   │
│   └── VALUE SCHEMES (Write operations)
│       ├── POST   /Admin/Values                - Create scheme
│       ├── PUT    /Admin/Values/{id}           - Update scheme
│       └── DELETE /Admin/Values/{id}           - Delete scheme
│
└── USER OPERATIONS (All authenticated users)
    └── VALUE SCHEMES (Read operations)
        ├── GET    /Values                      - List schemes
        └── GET    /Values/Tree                 - Get tree structure
```

**Authorization**:
- `/Admin/*`: Admin only (all operations)
- `/Values` (GET): All authenticated users

**Path Changes:**
| Old Path | New Path |
|----------|----------|
| `/groups` | `/Admin/Groups` |
| `/groups/{id}` | `/Admin/Groups/{id}` |
| `/parameters` | `/Admin/Parameters` |
| `/parameters/{id}` | `/Admin/Parameters/{id}` |
| `/values` (POST/PUT/DELETE) | `/Admin/Values` |
| `/values/{id}` (PUT/DELETE) | `/Admin/Values/{id}` |
| `/values` (GET) | `/Values` |
| `/values/tree` (GET) | `/Values/Tree` |

**Frontend Migration Note**:
The old endpoint `/ParameterValueSettingSchemes/tree` is now at `/ParameterSchemas/Values/Tree`

---

## New Application Layer Components

### Infrastructure Services

1. **IDeviceRpcExecutorService** (`Application/Common/Interfaces`)
   - Interface for RPC execution with parameter mapping
   - Methods: ExecuteParameterUpdateAsync, ExecuteParameterUpdatesAsync, GetParameterValueAsync, GetParameterValuesAsync

2. **DeviceRpcExecutorService** (`Infrastructure/Services`)
   - Implementation with batch optimization
   - Groups parameters by RPC method to minimize network calls
   - Handles nested field paths (e.g., "inverterMode.solarSellEnabled")

3. **RpcMethodMapping** (`Application/Common/Models`)
   - Static mapping between parameter names and RPC methods
   - Maps ~50+ parameters to their respective RPC endpoints
   - Includes custom value mappers for complex types

### Commands

1. **UpdateDeviceParameter** (`DeviceSettings/Commands`)
   - Updates single device parameter via RPC + database
   - Returns detailed result including RPC method used

2. **UpdateDeviceParameters** (`DeviceSettings/Commands`)
   - Batch updates with RPC optimization
   - Groups parameters by RPC method
   - Partial failure handling with detailed status

3. **SetDeviceMode** (`DeviceSettings/Commands`)
   - Applies predefined device mode configuration
   - Updates multiple parameters in batch
   - 7 predefined modes: EnergySave, MaxPerformance, SelfConsumption, Backup, TimeOfUse, OffGrid, GridSelling

### Queries

1. **GetDeviceParameters** (`DeviceSettings/Queries`)
   - Retrieves all parameters for a device with metadata
   - Includes parameter name, description, type, unit, group info

2. **GetDeviceModes** (`DeviceSettings/Queries`)
   - Returns all available device modes with descriptions
   - Includes configuration for each mode

### Models

1. **DeviceMode** (`DeviceSettings/Models`)
   - Enum defining 7 predefined operating modes
   - DeviceModeConfigurations with parameter presets for each mode

---

## Files Changed

### New Files Created
- `src/Application/Common/Interfaces/IDeviceRpcExecutorService.cs`
- `src/Application/Common/Models/RpcMethodMapping.cs`
- `src/Application/DeviceSettings/Commands/SetDeviceMode/SetDeviceMode.cs`
- `src/Application/DeviceSettings/Commands/UpdateDeviceParameter/UpdateDeviceParameter.cs`
- `src/Application/DeviceSettings/Commands/UpdateDeviceParameters/UpdateDeviceParameters.cs`
- `src/Application/DeviceSettings/Models/DeviceMode.cs`
- `src/Application/DeviceSettings/Queries/GetDeviceModes/GetDeviceModes.cs`
- `src/Application/DeviceSettings/Queries/GetDeviceParameters/GetDeviceParameters.cs`
- `src/Infrastructure/Services/DeviceRpcExecutorService.cs`
- `src/Web/Endpoints/ParameterSchemas.cs`
- `docs/API_Architecture_Templates_vs_Instances.md`

### Files Modified
- `src/Infrastructure/DependencyInjection.cs` - Added DeviceRpcExecutorService registration
- `src/Web/Endpoints/DeviceSettings.cs` - Reorganized with RPC-synced and admin operations
- `src/Web/wwwroot/api/specification.json` - Updated OpenAPI specification

### Files Deleted
- `src/Web/Endpoints/ParameterGroups.cs` - Merged into ParameterSchemas
- `src/Web/Endpoints/Parameters.cs` - Merged into ParameterSchemas
- `src/Web/Endpoints/ParameterValueSettingSchemes.cs` - Merged into ParameterSchemas
- `src/Web/Endpoints/SetupDeviceParameterValues.cs` - Merged into DeviceSettings

---

## Key Improvements

### 1. Clear Separation of Concerns
- **Templates vs Instances**: Clear distinction between system configuration and device-specific settings
- **Admin vs User**: Admin-only template management, user-facing device operations

### 2. RPC-First Pattern
- Device updates happen via RPC FIRST
- Database only updated if RPC succeeds
- Ensures device and database stay synchronized

### 3. Batch Optimization
- Groups parameters by RPC method
- Minimizes network calls
- Improves performance for multi-parameter updates

### 4. Device Modes
- 7 predefined operating modes for common use cases
- Easy mode switching with single API call
- Consistent, tested configurations

### 5. Better Organization
- Logical endpoint grouping under `/groups`, `/parameters`, `/values`
- Admin operations clearly marked under `/admin` prefix
- Demo/testing operations under `/demo` prefix
- RESTful API design

---

## Migration Guide

### Frontend Migration

**Old API calls:**
```javascript
// Old: Database-only parameter value update
PUT /api/SetupDeviceParameterValues/{id}
{
  "value": "true"
}
```

**New API calls:**
```javascript
// New: RPC-synced parameter update (recommended)
PUT /api/DeviceSettings/device/{deviceId}/parameters/{parameterId}
{
  "value": "true"
}

// New: Batch update
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    {"parameterId": "guid-1", "value": "true"},
    {"parameterId": "guid-2", "value": "12000"}
  ]
}

// New: Set device mode
POST /api/DeviceSettings/device/{deviceId}/mode
{
  "mode": 1  // EnergySave mode
}

// New: Get parameters tree (available for all users)
GET /api/DeviceSettings/device/{deviceId}/tree
```

**Old schema API calls:**
```javascript
// Old: Separate endpoints
GET /api/ParameterGroups
GET /api/Parameters
GET /api/ParameterValueSettingSchemes
```

**New schema API calls:**
```javascript
// New: Unified endpoint
GET /api/ParameterSchemas/groups
GET /api/ParameterSchemas/parameters
GET /api/ParameterSchemas/values
```

---

## Testing Checklist

- [ ] RPC-synced parameter updates work correctly
- [ ] Batch parameter updates optimize RPC calls
- [ ] Device modes apply correctly
- [ ] Database-only operations still work for admin tools
- [ ] Parameter schema CRUD operations work
- [ ] Authorization policies enforce correctly
- [ ] OpenAPI documentation is accurate
- [ ] Error handling works for RPC failures
- [ ] Partial failures in batch updates are handled correctly

---

## Future Enhancements

1. **UserPreferences Endpoint** - For user-specific settings (units, datetime format, etc.)
2. **Custom Modes** - Allow users to create and save custom device modes
3. **Mode Scheduling** - Schedule mode changes based on time or conditions
4. **RPC Retry Logic** - Automatic retry for transient RPC failures
5. **Change History** - Track parameter change history for audit

---

## Notes

- All existing SetupDeviceParameterValues Application layer commands/queries remain unchanged
- They are now accessed via DeviceSettings endpoint under `/admin` prefix
- RPC-synced operations are recommended for all user-facing features
- Database-only operations should be reserved for admin tools and offline scenarios
- Demo/testing operations are isolated under `/demo` prefix

---

## Admin Endpoints Convention

All admin-only endpoints that are NOT used by the frontend mobile app follow the `/Admin/` prefix convention. These endpoints are for data management without direct database access.

### 3. Devices Endpoint (`/api/Devices`)

**User-facing Endpoints:**
```
/api/Devices
└── POST   /login                          - Device authentication (no auth required)
```

**Admin Endpoints (require AdminOnly authorization):**
```
/api/Devices/Admin
├── GET    /Device/{id}                    - Get device by ID
├── POST   /Device                         - Create new device
├── PUT    /Device/{id}                    - Update device
└── DELETE /Device/{id}                    - Delete device
```

**Note:** These CRUD operations are NOT used by the frontend mobile app. Device management is handled through Setups and DeviceComponents endpoints.

---

### 4. DeviceComponents Endpoint (`/api/DeviceComponents`)

**User-facing Endpoints:**
```
/api/DeviceComponents
├── GET    /DeviceSummary                  - Get device summary for dashboard
└── GET    /Details/{componentId}          - Get component details
```

**Admin Endpoints (require AdminOnly authorization):**
```
/api/DeviceComponents/Admin
├── GET    /DeviceComponents/{deviceId}    - Get components by device ID
├── GET    /Component/{id}                 - Get component by ID
├── POST   /Component                      - Create new component
├── PUT    /Component/{id}                 - Update component
└── DELETE /Component/{id}                 - Delete component
```

---

### 5. DeviceNotifications Endpoint (`/api/DeviceNotifications`)

**Device Endpoints (require DeviceOnly authorization):**
```
/api/DeviceNotifications
└── POST   /                               - Create notification (device-initiated)
```

**User-facing Endpoints (require authorization):**
```
/api/DeviceNotifications
├── GET    /                               - Get paginated notifications
├── GET    /Deleted                        - Get soft-deleted notifications
├── PUT    /{id}/MarkRead                  - Mark notification as read
├── PUT    /MarkAllRead/{siteId}           - Mark all site notifications as read
├── PUT    /{id}/Restore                   - Restore deleted notification
├── DELETE /{id}                           - Soft delete notification
└── DELETE /{id}/Purge                     - Permanently delete notification
```

**Admin Endpoints (require AdminOnly authorization):**
```
/api/DeviceNotifications/Admin
└── GET    /Types                          - Get all notification types
```

**Telemetry Processing (via DeviceData):**
```
/api/DeviceData
└── POST   /Alarm                          - Process alarm telemetry from gateway
```

---

## Admin Endpoint Summary

| Endpoint Group | Admin Path | Operations |
|----------------|-----------|------------|
| Devices | `/api/Devices/Admin/Device` | GET, POST, PUT, DELETE |
| DeviceComponents | `/api/DeviceComponents/Admin/Component` | GET, POST, PUT, DELETE |
| DeviceComponents | `/api/DeviceComponents/Admin/DeviceComponents/{deviceId}` | GET |
| DeviceNotifications | `/api/DeviceNotifications/Admin/Types` | GET |
| DeviceSettings | `/api/DeviceSettings/admin` | GET, POST, PUT, DELETE |
| ParameterSchemas | `/api/ParameterSchemas/Admin/Groups` | All (admin-only) |
| ParameterSchemas | `/api/ParameterSchemas/Admin/Parameters` | All (admin-only) |
| ParameterSchemas | `/api/ParameterSchemas/Admin/Values` | POST/PUT/DELETE (admin-only) |
| ParameterSchemas | `/api/ParameterSchemas/Values` | GET (all users) |
| RoleManagement | `/api/RoleManagement/Admin/Roles` | GET, POST, DELETE (admin-only) |
| RoleManagement | `/api/RoleManagement/Admin/Assign` | PUT (admin-only) |
| RoleManagement | `/api/RoleManagement/Admin/User/{userId}/Roles` | GET (admin-only) |
| Setups | `/api/Setups/Admin/Setups` | GET (admin-only) |
| Setups | `/api/Setups/Admin/Setup/{id}` | GET, DELETE (admin-only) |
| Sites | `/api/Sites/Admin/Sites` | GET (admin-only) |
| Sites | `/api/Sites/Admin/Site/{id}` | GET, DELETE (admin-only) |

---

### 6. Invitations Endpoint (`/api/Invitations`)

**User-facing Endpoints (require authorization):**
```
/api/Invitations
├── POST   /Create                         - Create invitation to share site
├── GET    /BySite/{siteId}                - Get invitations for a site
├── POST   /Accept                         - Accept invitation
├── POST   /Decline                        - Decline invitation
└── DELETE /RemoveUser/{invitationId}      - Remove user from shared site
```

**Path Changes (kebab-case → PascalCase):**
| Old Path | New Path |
|----------|----------|
| `/create` | `/Create` |
| `/by-site/{siteId}` | `/BySite/{siteId}` |
| `/accept` | `/Accept` |
| `/decline` | `/Decline` |
| `/remove-user/{invitationId}` | `/RemoveUser/{invitationId}` |

**Removed Obsolete Endpoints:**
- ~~`GET /user-home-sites`~~ - Use `/api/Sites/mobile/get-sites` instead
- ~~`GET /notifications`~~ - Not used by any client
- ~~`PUT /notifications/{id}/mark-read`~~ - Not used by any client

**Localization Changes:**
- All comments and messages translated to English
- All validation messages translated to English
- Response messages translated to English

**Validation Messages:**
| Field | Message |
|-------|---------|
| Email | "Email is required." |
| Email | "Please enter a valid email address." |
| SiteId | "Site ID is required." |
| Token | "Token is required." |
| Token | "Token must be 6 digits." |
| Token | "Token must contain only numbers." |

**Error Messages:**
| Endpoint | Error Message |
|----------|---------------|
| All | "User not authenticated" |
| All | "User email address not found" |
| Create | "A pending invitation already exists for this user and site." |
| Accept | "This invitation has expired. Please request a new invitation." |
| Accept | "This site is already shared with you." |

**Success Response Messages:**
| Endpoint | Response |
|----------|----------|
| POST /Accept | `{ "message": "Invitation accepted successfully." }` |
| POST /Decline | `{ "message": "Invitation declined." }` |
| DELETE /RemoveUser/{invitationId} | `{ "message": "User removed from site successfully." }` |

---

### 7. DeviceNotifications Response Messages

**Validation Messages:**
| Field | Message |
|-------|---------|
| NotificationId | "Notification ID is required." |

**Error Messages:**
| Context | Message |
|---------|---------|
| Not Found | "Notification with ID {id} not found." |
| Access Denied | "You don't have access to site {siteId}" |

---

### 8. Locations Endpoint (`/api/Locations`)

**User-facing Endpoints (require authorization):**
```
/api/Locations
├── GET    /                              - Get all countries
└── GET    /Cities/{countryId}            - Get cities by country
```

**Path Changes:**
| Old Path | New Path |
|----------|----------|
| `/cities/{countryId}` | `/Cities/{countryId}` |

**Method Renamed:**
| Old Name | New Name |
|----------|----------|
| `GetCitiesByCountry` | `GetCountryCities` |

---

### 9. RoleManagement Endpoint (`/api/RoleManagement`)

**Purpose**: Admin-only access for managing roles and user role assignments.

**Admin Tool Endpoints (require AdminOnly authorization):**
```
/api/RoleManagement/Admin
├── GET    /Roles                         - Get all roles
├── POST   /Roles                         - Create a new role
├── DELETE /Roles/{roleName}              - Delete a role by name
├── PUT    /Assign                        - Assign a role to a user
└── GET    /User/{userId}/Roles           - Get all roles for a user
```

**Path Changes (Root → Admin/ prefix with PascalCase):**
| Old Path | New Path |
|----------|----------|
| `/` (GET) | `/Admin/Roles` |
| `/` (POST) | `/Admin/Roles` |
| `/{roleName}` | `/Admin/Roles/{roleName}` |
| `/assign` | `/Admin/Assign` |
| `/user/{userId}` | `/Admin/User/{userId}/Roles` |

**Authorization**: Admin only (all operations)

**Note**: Obsolete markers have been removed. These endpoints are now active admin tools.

---

### 10. Setups Endpoint (`/api/Setups`)

**Purpose**: Manages device setups and configurations.

**Admin Tool Endpoints (require AdminOnly authorization):**
```
/api/Setups/Admin
├── GET    /Setups                        - Get all setups (paginated)
├── GET    /Setup/{id}                    - Get setup by ID
└── DELETE /Setup/{id}                    - Delete setup
```

**User-facing Endpoints (require authorization):**
```
/api/Setups
├── GET    /SetupDevice                   - Get setup device for current user (by siteId)
├── POST   /Setup                         - Create a new setup
├── PUT    /Setup/{id}                    - Update setup
└── DELETE /Setup/{id}                    - Delete setup
```

**Path Changes:**
| Old Path | New Path |
|----------|----------|
| `/` (GET list) | `/Admin/Setups` |
| `/{id}` (GET) | `/Admin/Setup/{id}` |
| `/{id}` (DELETE admin) | `/Admin/Setup/{id}` |
| `/mobile/get-setup-device` | `/SetupDevice` |
| `/mobile/create-setup` | `/Setup` |
| `/mobile/update-setup/{id}` | `/Setup/{id}` |
| `/mobile/delete-setup/{id}` | `/Setup/{id}` |

**Authorization**:
- `/Admin/*`: Admin only
- Other endpoints: All authenticated users

---

### 11. Sites Endpoint (`/api/Sites`)

**Purpose**: Manages user sites and locations.

**Admin Tool Endpoints (require AdminOnly authorization):**
```
/api/Sites/Admin
├── GET    /Sites                         - Get all sites (paginated)
├── GET    /Site/{id}                     - Get site by ID
└── DELETE /Site/{id}                     - Delete site
```

**User-facing Endpoints (require authorization):**
```
/api/Sites
├── GET    /UserSites                     - Get all sites for current user
├── POST   /Site                          - Create a new site
├── PUT    /Site/{id}                     - Update site
└── DELETE /Site/{id}                     - Delete site
```

**Path Changes:**
| Old Path | New Path |
|----------|----------|
| `/` (GET list) | `/Admin/Sites` |
| `/{id}` (GET) | `/Admin/Site/{id}` |
| `/{id}` (DELETE admin) | `/Admin/Site/{id}` |
| `/mobile/get-sites` | `/UserSites` |
| `/mobile/create-site` | `/Site` |
| `/mobile/update-site/{id}` | `/Site/{id}` |
| `/mobile/delete-site/{id}` | `/Site/{id}` |

**Authorization**:
- `/Admin/*`: Admin only
- Other endpoints: All authenticated users

---

## Device Status Display Fix

### Issue

Device status was incorrectly showing "offline" even when the device was actively sending power flow data. This caused confusion for users as the device appeared offline despite receiving real-time data.

### Root Cause

The `DeviceDetail.tsx` screen was determining device status based on **data availability** (`activePowerKw !== null`) from the DeviceSummary API instead of using the **heartbeat-based status** from the DeviceStatusHub SignalR connection.

**Previous incorrect logic:**
```typescript
// DeviceDetail.tsx - WRONG approach
status={inverterSummary.activePowerKw !== null ? "online" : "offline"}
```

This approach fails when:
- Device is online but has zero active power (e.g., nighttime for solar systems)
- RPC call fails temporarily but heartbeat is still active
- Data is cached/stale but device heartbeat continues

### Correct Status System

The backend uses a heartbeat-based status system with progression:

| Time Since Last Heartbeat | Status |
|---------------------------|--------|
| 0-90 seconds | `OK` |
| 90-150 seconds | `PENDING` |
| 150-210 seconds | `WAITING` |
| 210+ seconds | `DISCONNECTED` |

This status is managed by `DeviceHeartbeatMonitorService` which runs every 60 seconds and broadcasts via the `DeviceStatusHub` SignalR connection.

### Frontend Fix Required

**DeviceDetail.tsx** must use the `useDeviceStatus` hook instead of checking data availability:

```typescript
// Import the hook
import { useDeviceStatus } from "../../../hooks/useDeviceStatus";

// Inside the component
const { deviceStatus } = useDeviceStatus(deviceId ?? undefined);

// Convert heartbeat status to component status
const componentStatus = useMemo(() => {
  if (!deviceStatus) return 'warning'; // Still waiting for status
  if (deviceStatus === 'OK') return 'online';
  if (deviceStatus === 'DISCONNECTED') return 'offline';
  return 'warning'; // PENDING, WAITING, NOT_TRACKED
}, [deviceStatus]);

// Use componentStatus for UI display
<InverterCard status={componentStatus} ... />
<BatteryCard status={componentStatus} ... />
```

### Backend Changes

All device data endpoints now update the heartbeat cache when receiving successful RPC responses. This ensures that ANY successful device communication marks the device as online.

**Updated Handlers:**

| Handler | Telemetry Source |
|---------|-----------------|
| `GetDailyEnergySummaryHandler` | `DailyEnergyFlow` |
| `GetDeviceSummaryQueryHandler` (inverter) | `PowerFlow` |
| `GetDeviceSummaryQueryHandler` (battery SOC) | `BatterySoc` |
| `GetDeviceDetailsQueryHandler` (inverter) | `PowerFlow` |
| `GetDeviceDetailsQueryHandler` (battery SOC) | `BatterySoc` |
| `EnergyDataCollectionService` (daily) | `DailyEnergyFlow` |
| `EnergyDataCollectionService` (lifetime) | `TotalEnergyFlow` |

Each handler calls `IDeviceActivityService.RecordActivityAsync()` after a successful RPC response:

```csharp
// Example from GetDeviceSummaryQueryHandler
await _activityService.RecordActivityAsync(
    tbDeviceId,
    TelemetrySource.PowerFlow,
    cancellationToken: cancellationToken);
```

### Summary

| Component | Before | After |
|-----------|--------|-------|
| Frontend Status Source | Data availability check | `useDeviceStatus` hook (heartbeat) |
| Backend Heartbeat Update | Only on explicit heartbeat | All successful RPC responses |
| Status Accuracy | False "offline" when no data | True connectivity status |

**Key Takeaway**: Device status should ALWAYS come from the heartbeat system (DeviceStatusHub), not from data availability checks. The heartbeat cache is updated by all device data endpoints, ensuring any successful device communication keeps the device status as "online"

---

## Device Status - REST API Fallback with SignalR Live Updates

### Issue

When users opened the home page, the device status would show "Offline" even though the backend reported "OK". This happened because:

1. **SignalR Initial Connection Delay**: The SignalR hub connection takes time to establish
2. **No Retry Mechanism**: If the initial SignalR connection failed, there was no retry logic
3. **60-Second Status Check Interval**: The `DeviceHeartbeatMonitorService` only checks every 60 seconds, so users had to wait for the next broadcast
4. **Initial State Problem**: The `useDeviceStatus` hook initialized `connectionState` to `'Disconnected'`, causing immediate "Offline" display

### Solution: Hybrid REST + SignalR Pattern

We implemented a two-step approach:
1. **Immediate REST API Call**: Fetch device status via REST API on page load (fast, reliable)
2. **SignalR for Live Updates**: After initial status is displayed, SignalR provides real-time updates

### Backend Changes

**New Endpoint Added**: `GET /api/DeviceData/Status/{deviceId}`

This endpoint returns the current device heartbeat status immediately without waiting for SignalR:

```csharp
// Response DTO
public class DeviceHeartbeatStatusDto
{
    public string DeviceId { get; set; }
    public DeviceStatusType Status { get; set; }  // OK, PENDING, WAITING, DISCONNECTED, NOT_TRACKED
    public string Timestamp { get; set; }
    public string? LastHeartbeat { get; set; }
    public string? LastPowerFlow { get; set; }
    public int MissedHeartbeats { get; set; }
}
```

### Frontend Changes

**1. API Client Configuration** (`config.ts`):
```typescript
DEVICE_DATA: {
  GET_ALL: '/DeviceData',
  CREATE: '/DeviceData',
  CREATE_ENERGY: '/DeviceData/energy',
  BULK_CREATE: '/DeviceData/bulk',
  GET_STATUS: '/DeviceData/Status/:deviceId',  // NEW
},
```

**2. Device Data Service** (`deviceDataService.ts`):
```typescript
export interface DeviceHeartbeatStatusDto {
  deviceId: string;
  status: DeviceStatusType;
  timestamp: string;
  lastHeartbeat?: string;
  lastPowerFlow?: string;
  missedHeartbeats: number;
}

async getStatus(deviceId: string): Promise<DeviceHeartbeatStatusDto> {
  const url = ENDPOINTS.DEVICE_DATA.GET_STATUS.replace(':deviceId', deviceId);
  const response = await apiClient.get<DeviceHeartbeatStatusDto>(url);
  return response.data;
},
```

**3. useDeviceStatus Hook** (`useDeviceStatus.ts`):

The hook now implements a two-step status fetching:

```typescript
// New state for loading indicator
const [isLoadingInitialStatus, setIsLoadingInitialStatus] = useState<boolean>(false);
const hasFetchedInitialStatus = useRef(false);

// STEP 1: Fetch initial status via REST API (fast, reliable)
useEffect(() => {
  if (!deviceId) return;
  if (hasFetchedInitialStatus.current) return;

  const fetchInitialStatus = async () => {
    setIsLoadingInitialStatus(true);
    try {
      const response = await deviceDataService.getStatus(deviceId);
      // Only update if we don't have status yet (SignalR might have provided one)
      if (!deviceStatus || deviceStatus !== response.status) {
        setDeviceStatus(response.status);
        setMissedHeartbeats(response.missedHeartbeats);
        setLastUpdate(response.timestamp);
      }
      hasFetchedInitialStatus.current = true;
    } catch (error) {
      // Don't set error state - SignalR might still work
    } finally {
      setIsLoadingInitialStatus(false);
    }
  };
  fetchInitialStatus();
}, [deviceId]);

// STEP 2: SignalR provides live updates after that
// (existing SignalR subscription logic)
```

**4. SignalR Retry Mechanism** (`deviceStatusService.ts`):

Added retry logic for initial SignalR connection with exponential backoff:

```typescript
// Retry configuration
private readonly MAX_INITIAL_RETRIES = 5;
private readonly INITIAL_RETRY_DELAY_MS = 2000;
private readonly MAX_RETRY_DELAY_MS = 30000;

private async connectWithRetry(attemptNumber: number): Promise<void> {
  try {
    this.notifyConnectionState('Connecting');
    await this.connection.start();
    this.notifyConnectionState('Connected');
  } catch (error) {
    if (attemptNumber < this.MAX_INITIAL_RETRIES) {
      const delay = Math.min(
        this.INITIAL_RETRY_DELAY_MS * Math.pow(2, attemptNumber),
        this.MAX_RETRY_DELAY_MS
      );
      this.notifyConnectionState('Reconnecting');
      setTimeout(() => this.connectWithRetry(attemptNumber + 1), delay);
    } else {
      this.notifyConnectionState('Failed');
    }
  }
}
```

**5. Auto-Subscribe on Connection Restore**:

When SignalR connection is restored, automatically re-subscribe to device updates:

```typescript
useEffect(() => {
  const unsubscribe = deviceStatusService.addConnectionStateListener(async (state) => {
    if (state === 'Connected' && currentDeviceIdRef.current) {
      await deviceStatusService.subscribeToDevice(currentDeviceIdRef.current);
    }
  });
  return unsubscribe;
}, []);
```

**6. WeatherStatus Component** (`WeatherStatus.tsx`):

Updated loading indicator logic:

```typescript
const shouldShowLoading = (
  connectionState: SignalRConnectionState,
  deviceStatus: DeviceStatusType | null,
  isLoadingInitialStatus: boolean
): boolean => {
  // Show loading if fetching initial status via REST API
  if (isLoadingInitialStatus) return true;
  // Show loading if SignalR is connecting and we don't have status yet
  return (connectionState === 'Connecting' || connectionState === 'Reconnecting') && !deviceStatus;
};

const { deviceStatus, connectionState, isLoadingInitialStatus } = useDeviceStatus(deviceId);
```

### Status Flow Diagram

```
User Opens Home Page
        │
        ▼
┌───────────────────────────────┐
│ 1. REST API Call              │
│    GET /DeviceData/Status/xxx │
│    (Immediate, ~100-200ms)    │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│ 2. Display Status from REST   │
│    Shows "OK" immediately     │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│ 3. SignalR Connects           │
│    (Background, with retry)   │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│ 4. Live Updates via SignalR   │
│    Real-time status changes   │
└───────────────────────────────┘
```

### Return Values from useDeviceStatus

| Property | Type | Description |
|----------|------|-------------|
| `deviceStatus` | `DeviceStatusType \| null` | Current status: OK, PENDING, WAITING, DISCONNECTED, NOT_TRACKED |
| `connectionState` | `SignalRConnectionState` | SignalR state: Connected, Connecting, Reconnecting, Disconnected, Failed |
| `isLoadingInitialStatus` | `boolean` | True while REST API call is in progress |
| `missedHeartbeats` | `number` | Number of consecutive missed heartbeats |
| `lastUpdate` | `string \| null` | Timestamp of last status update |
| `isInitialStatus` | `boolean` | True if status came from initial REST call |

---

## PowerFlow Cache Fix - Device Offline Detection

### Issue

When a device went offline, the PowerFlow data continued showing the last cached values instead of zeroing out. This caused users to see stale data even though the device was disconnected.

### Root Cause

The `SharedDevicePowerFlowStreamManager` had several issues:

1. **Null Return Not Treated as Error**: When `GetPowerFlowAsync` returned `null` (device offline), the code did nothing - no error increment, no cache clear
2. **New Subscribers Got Stale Cache**: When a new user subscribed to an offline device, they received the last cached (stale) data
3. **Zero Data Sent Only Once**: Zero data was only broadcast when `consecutiveErrors == DeviceOfflineThreshold` (exactly 3), not on subsequent errors

### Solution

**1. Treat Null Return as Failure** (`SharedDevicePowerFlowStreamManager.cs`):

```csharp
// In RunPollingLoopAsync
var data = await GetPowerFlowAsync(deviceId, cancellationToken);

if (data != null)
{
    // Success path - reset errors, broadcast data
    consecutiveErrors = 0;
    IsDeviceOffline = false;
    LastData = data;
    await BroadcastToSubscribersAsync(data, logger);
}
else
{
    // NULL return = device offline, treat as failure
    consecutiveErrors++;
    await HandleConsecutiveErrorsAsync(consecutiveErrors, maxConsecutiveErrors, logger, cancellationToken);
}
```

**2. New Subscribers Get Zero Data When Offline**:

```csharp
// In SubscribeAsync - check IsDeviceOffline before sending cached data
if (stream.IsDeviceOffline)
{
    // Device is offline - send zero data to new subscriber
    var zeroData = CreateZeroData(deviceId);
    await subscription.SendDataAsync(zeroData);
}
else
{
    // Device is online - send cached data
    await subscription.SendDataAsync(stream.LastData);
}
```

**3. Broadcast Zero Data on EVERY Error After Threshold**:

```csharp
private async Task HandleConsecutiveErrorsAsync(
    int consecutiveErrors,
    int maxConsecutiveErrors,
    ILogger logger,
    CancellationToken cancellationToken)
{
    // Changed from == to >= to broadcast on every error after threshold
    if (consecutiveErrors >= DeviceOfflineThreshold)
    {
        if (!IsDeviceOffline)
        {
            IsDeviceOffline = true;
            logger.LogWarning("Device {DeviceId} marked as offline after {Errors} consecutive errors",
                DeviceId, consecutiveErrors);
        }

        // Broadcast zero data on EVERY error after threshold (not just once)
        var zeroData = CreateZeroData(DeviceId);
        LastData = zeroData;
        await BroadcastToSubscribersAsync(zeroData, logger);
    }
}
```

**4. Zero Data Structure**:

```csharp
private static PowerFlowDto CreateZeroData(string deviceId) => new PowerFlowDto
{
    DeviceId = deviceId,
    Timestamp = DateTime.UtcNow,
    GridPowerKw = 0,
    SolarPowerKw = 0,
    BatteryPowerKw = 0,
    LoadPowerKw = 0,
    BatterySoc = 0,
    // ... all values set to 0
};
```

### Cache Behavior Summary

| Scenario | Before Fix | After Fix |
|----------|------------|-----------|
| Device goes offline | Last cached data shown | Zero data broadcast |
| New subscriber to offline device | Gets stale cached data | Gets zero data |
| 4th, 5th, Nth error | Nothing (only 3rd triggered zero) | Zero data broadcast each time |
| `GetPowerFlowAsync` returns null | Ignored, no error count | Treated as error, count incremented |

### Device Offline Detection Flow

```
Device Communication
        │
        ▼
┌─────────────────────────┐
│ GetPowerFlowAsync()     │
└───────────┬─────────────┘
            │
    ┌───────┴───────┐
    ▼               ▼
 Success          Null/Error
    │               │
    ▼               ▼
Reset errors    Increment errors
IsOffline=false     │
    │               │
    ▼               ▼
Broadcast data  errors >= 3?
                    │
            ┌───────┴───────┐
            ▼               ▼
           Yes              No
            │               │
            ▼               ▼
    IsOffline=true      Wait for
    Broadcast zeros     next poll
```

### Files Modified for PowerFlow Cache Fix

| File | Changes |
|------|---------|
| `SharedDevicePowerFlowStreamManager.cs` | Added null check, HandleConsecutiveErrorsAsync, CreateZeroData, IsDeviceOffline flag |
| `SharedDevicePowerFlowStreamManager.cs` | Fixed >= vs == for threshold check |
| `SharedDevicePowerFlowStreamManager.cs` | New subscribers get zero data when device offline |

---

## Inverter Detail - Missing Phase C Power Translation

### Issue

On the Inverter Detail screen under the "Grid" section, Phase C Power was displaying as `inverterDetails.PhaseCPower` instead of the human-readable label "Phase C Power".

**What user saw:**
```
Grid
├── Grid Power          1234.5 W
├── Phase A Power        411.5 W
├── Phase B Power        411.5 W
└── inverterDetails.PhaseCPower   411.5 W  ← BUG: Raw i18n key shown
```

### Root Cause

The translation key `phaseCPower` was missing from all language files (`en.json`, `tr.json`, `nl.json`). When i18n cannot find a translation key, it falls back to displaying the raw key path.

The `phaseAPower` and `phaseBPower` keys existed, but `phaseCPower` was accidentally omitted.

### Solution

Added the missing translation key to all three language files:

**English (`en.json`):**
```json
"phaseAPower": "Phase A Power",
"phaseBPower": "Phase B Power",
"phaseCPower": "Phase C Power",  // ADDED
"gridStatus": "Grid Status",
```

**Turkish (`tr.json`):**
```json
"phaseAPower": "Faz A Gücü",
"phaseBPower": "Faz B Gücü",
"phaseCPower": "Faz C Gücü",  // ADDED
"gridStatus": "Şebeke Durumu",
```

**Dutch (`nl.json`):**
```json
"phaseAPower": "Fase A Vermogen",
"phaseBPower": "Fase B Vermogen",
"phaseCPower": "Fase C Vermogen",  // ADDED
"gridStatus": "Netstatus",
```

### Files Changed

| File | Change |
|------|--------|
| `src/lib/i18n/locales/en.json` | Added `phaseCPower` translation |
| `src/lib/i18n/locales/tr.json` | Added `phaseCPower` translation |
| `src/lib/i18n/locales/nl.json` | Added `phaseCPower` translation |

### Notes

- The frontend code in `InverterDetailScreen.tsx` was correct - it properly used `t("inverterDetail.phaseCPower")`
- The backend was returning the correct data with `gridSidePhaseCPower` field
- This was purely a missing i18n translation issue

---

## Frontend Files Changed Summary

| File | Change Description |
|------|-------------------|
| `src/api/client/config.ts` | Added `GET_STATUS` endpoint |
| `src/api/services/deviceDataService.ts` | Added `getStatus()` method and `DeviceHeartbeatStatusDto` |
| `src/hooks/useDeviceStatus.ts` | Added REST API fallback, auto-subscribe on reconnect, `isLoadingInitialStatus` state |
| `src/services/deviceStatusService.ts` | Added SignalR retry mechanism with exponential backoff |
| `src/app/screens/dashboard/ui/weatherStatus/WeatherStatus.tsx` | Updated loading indicator logic |
| `src/lib/i18n/locales/en.json` | Added missing `phaseCPower` translation |
| `src/lib/i18n/locales/tr.json` | Added missing `phaseCPower` translation |
| `src/lib/i18n/locales/nl.json` | Added missing `phaseCPower` translation |

## Backend Files Changed Summary

| File | Change Description |
|------|-------------------|
| `src/Infrastructure/Services/SharedDevicePowerFlowStreamManager.cs` | PowerFlow cache fixes for offline detection |
| `src/Web/Endpoints/DeviceData.cs` | New endpoint `GET /Status/{deviceId}` |