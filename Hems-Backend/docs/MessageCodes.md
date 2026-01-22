# HEMS Backend - Message Codes Documentation

This document defines all message codes used in the HEMS backend API. Frontend applications should use these codes to display localized messages to users.

## Response Structure

All API error responses follow this structure:

```json
{
  "statusCode": 404,
  "code": "INVITATION_NOT_FOUND",
  "message": "Invitation not found",
  "params": {
    "token": "283977"
  },
  "timestamp": "2026-01-22T10:30:00Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `statusCode` | number | HTTP status code |
| `code` | string | Machine-readable message code for i18n lookup |
| `message` | string | Default English message (fallback) |
| `params` | object | Parameters for message interpolation (optional) |
| `timestamp` | string | ISO 8601 timestamp |

## Message Types

| Type | Description | HTTP Status Range |
|------|-------------|-------------------|
| `ERROR` | Operation failed | 4xx, 5xx |
| `WARNING` | Operation succeeded with warnings | 2xx |
| `INFO` | Informational message | 2xx |
| `SUCCESS` | Operation completed successfully | 2xx |

---

## Authentication & Authorization

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `AUTH_USER_NOT_AUTHENTICATED` | 401 | User not authenticated | - | No valid authentication token provided |
| `AUTH_INVALID_CREDENTIALS` | 401 | Invalid login credentials | - | Email or password is incorrect |
| `AUTH_EMAIL_NOT_CONFIRMED` | 401 | Email not confirmed | `email` | User must confirm email before login |
| `AUTH_ACCOUNT_LOCKED` | 401 | Account is locked | - | Too many failed login attempts |
| `AUTH_TOKEN_EXPIRED` | 401 | Token has expired | - | JWT token has expired |
| `AUTH_PERMISSION_DENIED` | 403 | Permission denied | - | User lacks required permissions |
| `AUTH_AUTHORIZATION_REQUIRED` | 403 | Authorization required | `policy` | Specific authorization policy not met |

---

## Invitations

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `INVITATION_NOT_FOUND` | 404 | Invitation not found | `token` | Invitation with given token doesn't exist |
| `INVITATION_EXPIRED` | 400 | Invitation has expired | `token` | Invitation is past its validity period |
| `INVITATION_ALREADY_ACCEPTED` | 400 | Invitation already accepted | `token` | Invitation was previously accepted |
| `INVITATION_ALREADY_DECLINED` | 400 | Invitation already declined | `token` | Invitation was previously declined |
| `INVITATION_ALREADY_EXISTS` | 400 | Pending invitation already exists | `email`, `siteId` | Active invitation exists for this user/site |
| `INVITATION_CANNOT_INVITE_SELF` | 400 | Cannot invite yourself | - | User tried to invite their own email |
| `INVITATION_CANNOT_INVITE_OWNER` | 400 | Cannot invite site owner | `email` | Cannot invite the owner of the site |
| `INVITATION_ONLY_OWNER_CAN_INVITE` | 403 | Only site owner can send invitations | `siteId` | User is not the owner of this site |
| `INVITATION_ONLY_OWNER_CAN_REMOVE` | 403 | Only site owner can remove users | `siteId` | Must be owner or removing self |
| `INVITATION_USER_ALREADY_MEMBER` | 400 | User is already a member | `email`, `siteId` | User already has access to this site |
| `INVITATION_SITE_ALREADY_SHARED` | 400 | Site already shared with you | `siteId` | User already has access to this site |

### Success Messages

| Code | HTTP | Message | Params |
|------|------|---------|--------|
| `INVITATION_ACCEPTED` | 200 | Invitation accepted successfully | `siteName` |
| `INVITATION_DECLINED` | 200 | Invitation declined | - |
| `INVITATION_SENT` | 200 | Invitation sent successfully | `email` |
| `INVITATION_USER_REMOVED` | 200 | User removed from site | `email` |

---

## Sites

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `SITE_NOT_FOUND` | 404 | Site not found | `siteId` | Site doesn't exist or is deleted |
| `SITE_ACCESS_DENIED` | 403 | Access denied to this site | `siteId` | User doesn't have access to this site |
| `SITE_ONLY_OWNER_CAN_DELETE` | 403 | Only site owner can delete | `siteId` | Must be owner to delete site |
| `SITE_ONLY_OWNER_CAN_UPDATE` | 403 | Only site owner can update | `siteId` | Must be owner to update site |
| `SITE_CANNOT_DELETE_WITH_SETUP` | 400 | Cannot delete site with setup | `siteId` | Must delete setup first |
| `SITE_COORDINATES_NOT_SET` | 400 | Site coordinates not set | `siteId` | Latitude/longitude not configured |

### Success Messages

| Code | HTTP | Message | Params |
|------|------|---------|--------|
| `SITE_CREATED` | 201 | Site created successfully | `siteId`, `name` |
| `SITE_UPDATED` | 200 | Site updated successfully | `siteId` |
| `SITE_DELETED` | 200 | Site deleted successfully | `siteId` |

---

## Setups

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `SETUP_NOT_FOUND` | 404 | Setup not found | `setupId` | Setup doesn't exist |
| `SETUP_MISSING_DEVICE_IDENTIFIER` | 400 | Device identifier required | - | Must provide QR code or serial number |
| `SETUP_SITE_ALREADY_HAS_SETUP` | 400 | Site already has a setup | `siteId` | Each site can only have one setup |
| `SETUP_DEVICE_NOT_FOUND_THINGSBOARD` | 404 | Device not found in ThingsBoard | `identifier` | Device not registered in ThingsBoard |
| `SETUP_DEVICE_NOT_FOUND_LOCAL` | 404 | Device not found | `identifier` | Device not in local database |
| `SETUP_DEVICE_ALREADY_IN_USE` | 400 | Device already in use | `deviceId` | Device assigned to another setup |
| `SETUP_CANNOT_DELETE_WITH_DEVICES` | 400 | Cannot delete setup with devices | `setupId` | Must disconnect devices first |

### Success Messages

| Code | HTTP | Message | Params |
|------|------|---------|--------|
| `SETUP_CREATED` | 201 | Setup created successfully | `setupId` |
| `SETUP_UPDATED` | 200 | Setup updated successfully | `setupId` |
| `SETUP_DELETED` | 200 | Setup deleted successfully | `setupId` |

---

## Devices

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `DEVICE_NOT_FOUND` | 404 | Device not found | `deviceId` | Device doesn't exist |
| `DEVICE_ACCESS_DENIED` | 403 | Device access denied | `deviceId` | User doesn't have access |
| `DEVICE_NOT_IN_SETUP` | 400 | Device not in any setup | `deviceId` | Device must be assigned to setup |
| `DEVICE_NOT_ACTIVE` | 400 | Device not active | `deviceId` | Device is inactive |
| `DEVICE_NO_THINGSBOARD_MAPPING` | 400 | Device has no ThingsBoard mapping | `deviceId` | Missing ThingsBoard configuration |

---

## Device Settings & Parameters

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `PARAMETER_NOT_FOUND` | 404 | Parameter not found | `parameterId` | Parameter doesn't exist |
| `PARAMETER_VALUE_NOT_FOUND` | 404 | Parameter value not found | `parameterId`, `deviceId` | No value set for this parameter |
| `PARAMETER_ALREADY_EXISTS` | 400 | Parameter already exists | `parameterId`, `deviceId` | Duplicate parameter value |
| `PARAMETER_IN_USE` | 400 | Parameter is in use | `parameterId` | Cannot delete, parameter is referenced |
| `PARAMETER_IN_SCHEMA` | 400 | Parameter used in schema | `parameterId` | Cannot delete, used in value scheme |
| `PARAMETER_GROUP_NOT_FOUND` | 404 | Parameter group not found | `groupId` | Group doesn't exist |
| `PARAMETER_GROUP_CIRCULAR_REF` | 400 | Circular reference detected | `groupId` | Group cannot be its own parent |
| `PARAMETER_GROUP_HAS_SUBGROUPS` | 400 | Group has subgroups | `groupId` | Cannot delete, has child groups |
| `PARAMETER_GROUP_HAS_PARAMETERS` | 400 | Group has parameters | `groupId` | Cannot delete, has parameters |

### Success Messages

| Code | HTTP | Message | Params |
|------|------|---------|--------|
| `PARAMETER_UPDATED` | 200 | Parameter updated successfully | `parameterId` |
| `PARAMETER_CREATED` | 201 | Parameter created successfully | `parameterId` |
| `PARAMETER_DELETED` | 200 | Parameter deleted successfully | `parameterId` |

---

## Device Notifications

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `NOTIFICATION_NOT_FOUND` | 404 | Notification not found | `notificationId` | Notification doesn't exist |
| `NOTIFICATION_ACCESS_DENIED` | 403 | Notification access denied | `notificationId` | User doesn't have access |
| `NOTIFICATION_TYPE_NOT_FOUND` | 404 | Notification type not found | `typeCode` | Invalid notification type |
| `NOTIFICATION_DEVICE_NOT_ASSIGNED` | 400 | Device not properly assigned | `deviceId` | Device missing setup/site |

### Success Messages

| Code | HTTP | Message | Params |
|------|------|---------|--------|
| `NOTIFICATION_MARKED_READ` | 200 | Notification marked as read | `notificationId` |
| `NOTIFICATIONS_ALL_MARKED_READ` | 200 | All notifications marked as read | `count` |
| `NOTIFICATION_DELETED` | 200 | Notification deleted | `notificationId` |

---

## Mobile App Versions

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `VERSION_NOT_FOUND` | 404 | Version not found | `versionId` | App version doesn't exist |
| `VERSION_ALREADY_EXISTS` | 400 | Version already exists | `version`, `platform` | Duplicate version/platform |
| `VERSION_CANNOT_SET_INACTIVE_LATEST` | 400 | Cannot set inactive as latest | `versionId` | Must be active to be latest |

### Success Messages

| Code | HTTP | Message | Params |
|------|------|---------|--------|
| `VERSION_MARKED_LATEST` | 200 | Version marked as latest | `version` |
| `VERSION_REVOKED` | 200 | Version has been revoked | `version` |

---

## Dashboard & Data

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `DASHBOARD_INVALID_PERIOD` | 400 | Invalid period | `period` | Must be day/week/month/year |
| `DASHBOARD_MISSING_DATE_RANGE` | 400 | Date range required | - | Start and end date required |
| `DASHBOARD_MISSING_DATE` | 400 | Date required | - | Date required for daily queries |
| `DASHBOARD_MISSING_WEEK` | 400 | Week parameters required | - | Year and week required |
| `DASHBOARD_MISSING_MONTH` | 400 | Month parameters required | - | Year and month required |
| `DASHBOARD_MISSING_YEAR` | 400 | Year required | - | Year required for yearly queries |

---

## Weather

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `WEATHER_FETCH_FAILED` | 400 | Failed to fetch weather data | `siteId` | External API error |
| `WEATHER_SITE_NO_COORDINATES` | 400 | Site has no coordinates | `siteId` | Cannot fetch weather without location |

---

## Generic/Common

| Code | HTTP | Message | Params | Description |
|------|------|---------|--------|-------------|
| `RECORD_NOT_FOUND` | 404 | Record not found | `entityType`, `id` | Generic not found |
| `VALIDATION_FAILED` | 400 | Validation failed | `errors` | Request validation errors |
| `INTERNAL_ERROR` | 500 | An internal error occurred | - | Unexpected server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable | - | External service down |

---

## Frontend i18n Usage

Frontend should map these codes to localized messages:

```typescript
// en.json
{
  "errors": {
    "INVITATION_NOT_FOUND": "The invitation code is invalid or has expired.",
    "INVITATION_EXPIRED": "This invitation has expired. Please request a new one.",
    "SITE_ACCESS_DENIED": "You don't have access to this site."
  }
}

// Usage in React
const handleError = (error: ApiError) => {
  const message = t(`errors.${error.code}`, error.params)
    || error.message; // fallback to English
  showToast({ type: 'error', message });
};
```

### Parameter Interpolation

Messages can include parameters using `{{paramName}}` syntax:

```json
{
  "INVITATION_ALREADY_EXISTS": "An invitation has already been sent to {{email}}."
}
```

---

## Adding New Message Codes

When adding a new message code:

1. Add to appropriate section in this document
2. Add to `MessageCodes.cs` enum in backend
3. Add to frontend i18n files (en.json, tr.json, nl.json)
4. Use in exception: `throw new AppException(MessageCodes.YOUR_CODE, params)`

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-22 | Initial documentation |
