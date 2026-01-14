# Backend API Endpoint Changes

This document summarizes all endpoint modifications made during the refactoring session. Frontend team should update their code accordingly.

---

## 1. Renamed Endpoints

### Dashboard/DeviceComponentsStatus (formerly device/GetWithData)

| Property | Old Value | New Value |
|----------|-----------|-----------|
| **Path** | `/Dashboard/device/GetWithData` | `/Dashboard/DeviceComponentsStatus` |
| **Method** | GET | GET (unchanged) |
| **Query Params** | `deviceId` | `deviceId` (unchanged) |
| **Response** | `DeviceWithDataResponseDto` | `DeviceWithDataResponseDto` (unchanged) |

**Frontend Changes Required:**
```typescript
// config.ts
// OLD
GET_DEVICE_WITH_DATA: '/Dashboard/device/GetWithData',
// NEW
GET_DEVICE_COMPONENTS_STATUS: '/Dashboard/DeviceComponentsStatus',

// mobileDashboardService.ts
// OLD
async getDeviceWithData(deviceId: string)
// NEW
async getDeviceComponentsStatus(deviceId: string)
```

### DeviceComponents Admin Endpoints (moved to Tool/ prefix)

The following admin-only endpoints have been moved under a `Tool/` prefix to clearly indicate they are administrative utilities for managing data without direct database access:

| Old Path | New Path | Method | Description |
|----------|----------|--------|-------------|
| `/DeviceComponents/Device/{deviceId}` | `/DeviceComponents/Tool/DeviceComponents/{deviceId}` | GET | Get all components for a device (plural - returns list) |
| `/DeviceComponents/{id}` | `/DeviceComponents/Tool/Component/{id}` | GET | Get single component (singular - returns one) |
| `/DeviceComponents` | `/DeviceComponents/Tool/Component` | POST | Create a component |
| `/DeviceComponents/{id}` | `/DeviceComponents/Tool/Component/{id}` | PUT | Update a component |
| `/DeviceComponents/{id}` | `/DeviceComponents/Tool/Component/{id}` | DELETE | Delete a component |

**Note:** These endpoints are AdminOnly and not used by the mobile app. They exist for administrative data management.

**Frontend Changes Required:**
```typescript
// config.ts - Update paths if admin panel uses these
DEVICE_COMPONENTS: {
  // OLD
  GET_BY_DEVICE: '/DeviceComponents/device/:deviceId',
  GET_BY_ID: '/DeviceComponents/:id',
  CREATE: '/DeviceComponents',
  UPDATE: '/DeviceComponents/:id',
  DELETE: '/DeviceComponents/:id',
  // NEW
  GET_BY_DEVICE: '/DeviceComponents/Tool/DeviceComponents/:deviceId',
  GET_BY_ID: '/DeviceComponents/Tool/Component/:id',
  CREATE: '/DeviceComponents/Tool/Component',
  UPDATE: '/DeviceComponents/Tool/Component/:id',
  DELETE: '/DeviceComponents/Tool/Component/:id',
  // User-facing endpoints (unchanged)
  GET_DEVICE_SUMMARY: '/DeviceComponents/DeviceSummary',
  GET_COMPONENT_DETAILS: '/DeviceComponents/Details/:componentId',
}
```

### DeviceNotifications (CamelCase standardization)

The following endpoints have been updated to use CamelCase naming convention:

| Old Path | New Path | Method |
|----------|----------|--------|
| `/DeviceNotifications/types` | `/DeviceNotifications/Types` | GET |
| `/DeviceNotifications/deleted` | `/DeviceNotifications/Deleted` | GET |
| `/DeviceNotifications/{id}/mark-read` | `/DeviceNotifications/{id}/MarkRead` | PUT |
| `/DeviceNotifications/mark-all-read/{siteId}` | `/DeviceNotifications/MarkAllRead/{siteId}` | PUT |
| `/DeviceNotifications/{id}/restore` | `/DeviceNotifications/{id}/Restore` | PUT |
| `/DeviceNotifications/{id}/permanent` | `/DeviceNotifications/{id}/Permanent` | DELETE |

**Note:** `GET /DeviceNotifications/Types` is not currently used by the frontend but kept for potential admin use.

**Frontend Changes Required:**
```typescript
// config.ts
DEVICE_NOTIFICATIONS: {
  // OLD
  GET_DELETED: '/DeviceNotifications/deleted',
  GET_TYPES: '/DeviceNotifications/types',
  MARK_READ: '/DeviceNotifications/:id/mark-read',
  MARK_ALL_READ: '/DeviceNotifications/mark-all-read/:siteId',
  RESTORE: '/DeviceNotifications/:id/restore',
  PERMANENT_DELETE: '/DeviceNotifications/:id/permanent',
  // NEW
  GET_DELETED: '/DeviceNotifications/Deleted',
  GET_TYPES: '/DeviceNotifications/Types',
  MARK_READ: '/DeviceNotifications/:id/MarkRead',
  MARK_ALL_READ: '/DeviceNotifications/MarkAllRead/:siteId',
  RESTORE: '/DeviceNotifications/:id/Restore',
  PERMANENT_DELETE: '/DeviceNotifications/:id/Permanent',
}
```

---

## 2. Deleted Endpoints

### Dashboard/GetComponentLiveData (REMOVED)

| Property | Value |
|----------|-------|
| **Old Path** | `/Dashboard/device/component/getComponentLiveData` |
| **Method** | GET |
| **Status** | **DELETED** |
| **Reason** | Only returned simulated data via DeviceSimulator. Frontend code was commented out and not in use. |

**Frontend Changes Required:**
- Remove `GET_COMPONENT_LIVE_DATA` from `config.ts`
- Remove `getComponentLiveData()` function from `mobileDashboardService.ts`
- Delete `useDeviceLiveData.ts` hook file
- Remove `GetComponentLiveDataResponse` interface from types

### Auth/user-status (REMOVED)

| Property | Value |
|----------|-------|
| **Old Path** | `/Auth/user-status` |
| **Method** | GET |
| **Status** | **DELETED** |
| **Reason** | Function defined but never called in frontend. |

### Auth/mobile/get-user-status (REMOVED)

| Property | Value |
|----------|-------|
| **Old Path** | `/Auth/mobile/get-user-status` |
| **Method** | GET |
| **Status** | **DELETED** |
| **Reason** | Function defined but never called in frontend. |

**Frontend Changes Required:**
- Remove `USER_STATUS` from `config.ts`
- Remove `USER_STATUS_MOBILE` from `config.ts`
- Remove `getUserStatus()` function from `authService.ts`
- Remove `getUserStatusMobile()` function from `authService.ts`
- Remove `UserStatusDto`, `UserStatusType`, `UserStatusDataDto` interfaces from `auth.types.ts`

---

## 3. Obsolete Endpoints (Marked for Future Deletion)

The following endpoints have been marked with `[Obsolete]` attribute and are scheduled for removal:

### RoleManagement (All Endpoints)

| Endpoint | Path | Status |
|----------|------|--------|
| GET_ALL | `/RoleManagement` | Obsolete |
| CREATE | `/RoleManagement` | Obsolete |
| DELETE | `/RoleManagement/{roleName}` | Obsolete |
| ASSIGN | `/RoleManagement/assign` | Obsolete |
| GET_USER_ROLES | `/RoleManagement/user/{userId}` | Obsolete |

### Invitations (Partial)

| Endpoint | Path | Status |
|----------|------|--------|
| USER_HOME_SITES | `/Invitations/user-home-sites` | Obsolete |
| GET_NOTIFICATIONS | `/Invitations/notifications` | Obsolete |
| MARK_NOTIFICATION_READ | `/Invitations/notifications/{notificationId}/mark-read` | Obsolete |

### Identity (Partial)

| Endpoint | Path | Status |
|----------|------|--------|
| MANAGE_2FA | `/Users/manage/2fa` | Obsolete |

---

## 4. Unused Endpoints (Not Called by Frontend)

The following endpoints exist in the backend but are **NOT used** by the frontend:

| Endpoint | Path | Notes |
|----------|------|-------|
| LifetimeEnergy | `/Dashboard/LifetimeEnergy` | Not defined in frontend config |
| Battery | `/Dashboard/graphs/Battery` | BatteryChart component is orphaned |
| BatteryComponent | `/Dashboard/graphs/Components/Battery` | All usages commented out |
| InverterComponent | `/Dashboard/graphs/Components/Inverter` | getInverterComponent() never called |

---

## Summary of Required Frontend Updates

### Files to Modify:

1. **`src/api/client/config.ts`**
   - Rename `GET_DEVICE_WITH_DATA` to `GET_DEVICE_COMPONENTS_STATUS`
   - Update path to `/Dashboard/DeviceComponentsStatus`
   - Remove `GET_COMPONENT_LIVE_DATA`
   - Remove `USER_STATUS`
   - Remove `USER_STATUS_MOBILE`

2. **`src/api/services/mobileDashboardService.ts`**
   - Rename `getDeviceWithData()` to `getDeviceComponentsStatus()`
   - Remove `getComponentLiveData()` function
   - Remove `GetComponentLiveDataResponse` interface

3. **`src/api/services/authService.ts`**
   - Remove `getUserStatus()` function
   - Remove `getUserStatusMobile()` function

4. **`src/app/screens/deviceDetail/DeviceDetail.tsx`**
   - Update call from `getDeviceWithData()` to `getDeviceComponentsStatus()`
   - Remove import of `useDeviceLiveData`

5. **`src/api/types/mobileDashboard.types.ts`**
   - Remove `GetComponentLiveDataResponse` interface

6. **`src/api/types/auth.types.ts`**
   - Remove `UserStatusType` enum
   - Remove `UserStatusDataDto` interface
   - Remove `UserStatusDto` interface

### Files to Delete:

1. **`src/hooks/useDeviceLiveData.ts`** - Entire file can be deleted

---

*Document generated: January 2026*
