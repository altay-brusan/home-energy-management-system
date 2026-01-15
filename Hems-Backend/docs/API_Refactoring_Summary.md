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
/api/ParameterSchemas (Admin-only)
├── PARAMETER GROUPS (Category organization)
│   ├── GET    /groups                - List all groups
│   ├── GET    /groups/{id}           - Get specific group
│   ├── POST   /groups                - Create group
│   ├── PUT    /groups/{id}           - Update group
│   └── DELETE /groups/{id}           - Delete group
│
├── PARAMETERS (Parameter definitions)
│   ├── GET    /parameters            - List parameters
│   ├── GET    /parameters/{id}       - Get specific parameter
│   ├── POST   /parameters            - Create parameter
│   ├── PUT    /parameters/{id}       - Update parameter
│   └── DELETE /parameters/{id}       - Delete parameter
│
└── VALUE SCHEMES (Model-specific default values)
    ├── GET    /values                - List schemes
    ├── GET    /values/tree           - Get tree structure
    ├── POST   /values                - Create scheme
    ├── PUT    /values/{id}           - Update scheme
    └── DELETE /values/{id}           - Delete scheme
```

**Authorization**: Admin only (all operations)

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

## Admin Tool Endpoints Convention

All admin-only endpoints that are NOT used by the frontend mobile app follow the `/Tool/` prefix convention. These endpoints are for data management without direct database access.

### 3. Devices Endpoint (`/api/Devices`)

**User-facing Endpoints:**
```
/api/Devices
└── POST   /login                          - Device authentication (no auth required)
```

**Admin Tool Endpoints (require AdminOnly authorization):**
```
/api/Devices/Tool
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

**Admin Tool Endpoints (require AdminOnly authorization):**
```
/api/DeviceComponents/Tool
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

**Admin Tool Endpoints (require AdminOnly authorization):**
```
/api/DeviceNotifications/Tool
└── GET    /Types                          - Get all notification types
```

**Telemetry Processing (via DeviceData):**
```
/api/DeviceData
└── POST   /Alarm                          - Process alarm telemetry from gateway
```

---

## Tool Endpoint Summary

| Endpoint Group | Tool Path | Operations |
|----------------|-----------|------------|
| Devices | `/api/Devices/Tool/Device` | GET, POST, PUT, DELETE |
| DeviceComponents | `/api/DeviceComponents/Tool/Component` | GET, POST, PUT, DELETE |
| DeviceComponents | `/api/DeviceComponents/Tool/DeviceComponents/{deviceId}` | GET |
| DeviceNotifications | `/api/DeviceNotifications/Tool/Types` | GET |
| DeviceSettings | `/api/DeviceSettings/admin` | GET, POST, PUT, DELETE |
| ParameterSchemas | `/api/ParameterSchemas` | All (admin-only by default) |

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
