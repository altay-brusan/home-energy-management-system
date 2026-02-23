# User Account Deletion — Business Logic

## Overview

Users can request deletion of their account through the `DELETE /api/users/delete-account` endpoint. The system performs a **soft-delete with PII anonymization** of the user account (preserving audit trail while complying with GDPR) and cleans up related data based on the user's role.

## Business Rules

| Scenario | Allowed? | What happens |
|----------|----------|-------------|
| User is only a guest (no owned sites) | Yes | Remove UserSite records, cancel invitations, soft-delete account |
| Owner + sites have active guests (UserSite records exist) | **No** | Return error with `USER_CANNOT_DELETE_WITH_ACTIVE_SHARES` code |
| Owner + no active guests on any owned site | Yes | Hard-delete Setup/SetupDevice chain, soft-delete Sites, cancel invitations, remove guest records, soft-delete account |
| Administrator user | **No** | Return error with `USER_ADMIN_CANNOT_DELETE` code |
| Demo user | Yes | Same deletion flow as regular users; demo system self-heals on next startup (see below) |

## Deletion Flow

```
1. Authenticate — verify current user identity
2. Block administrator users (IsInRole check)
3. Load all owned non-deleted Sites
4. For each owned site, check if ANY guest UserSite records exist
   → If yes: throw USER_CANNOT_DELETE_WITH_ACTIVE_SHARES (include sharedSiteCount)
5. For each owned site:
   a. Load Setup (include SetupDevice)
   b. If SetupDevice exists → hard-delete (frees Device for re-registration)
   c. If Setup exists → hard-delete
   d. Load & remove Invitations for this site
   e. Set site.IsDeleted = true (soft-delete)
6. Remove all UserSite records where UserId == current user
7. Remove all received Invitations where InvitedUserId == userId
8. Remove orphaned Notifications where UserId == userId
9. Soft-delete user with PII anonymization (see below)
10. SaveChangesAsync
```

## PII Anonymization (GDPR Compliance)

To comply with **GDPR Article 17 (Right to Erasure)**, personal data must be scrubbed when a user requests account deletion. The user record is kept (soft-delete) for referential integrity of audit fields (`CreatedBy`/`LastModifiedBy` store user IDs, not personal data), but all PII is anonymized.

### Fields Anonymized on Deletion

| Field | Before | After |
|-------|--------|-------|
| `FirstName` | "John" | `null` |
| `Surname` | "Doe" | `null` |
| `Email` | "john@example.com" | `deleted-{userId}@deleted.local` |
| `NormalizedEmail` | "JOHN@EXAMPLE.COM" | `DELETED-{userId}@DELETED.LOCAL` |
| `UserName` | "john@example.com" | `deleted-{userId}` |
| `NormalizedUserName` | "JOHN@EXAMPLE.COM" | `DELETED-{userId}` |
| `PhoneNumber` | "+905551234567" | `null` |
| `PasswordHash` | (hashed value) | `null` |
| `IsDeleted` | `false` | `true` |
| `DeletedAt` | `null` | Current UTC timestamp |

### Why Soft-Delete + Anonymization (Not Hard-Delete)

- `CreatedBy` / `LastModifiedBy` fields across all auditable entities reference `ApplicationUser.Id` (a GUID). Hard-deleting the user would break these FK references or require cascade behavior that destroys audit history.
- Soft-delete with anonymization satisfies both GDPR (no personal data retained) and audit requirements (user ID record still exists).

## Entity Impact

| Entity | Action | Reason |
|--------|--------|--------|
| **ApplicationUser** | Soft-delete + PII anonymization | Preserve audit trail (ID only), comply with GDPR |
| **Site** | Soft-delete (`IsDeleted=true`) | Preserve historical data, filtered by `!IsDeleted` in queries |
| **Setup** | Hard-delete | Free the device registration chain |
| **SetupDevice** | Hard-delete | Free the `Device` entity for re-registration by another user |
| **Device** | Preserved (no change) | Physical device and telemetry data remain intact |
| **UserSite** | Hard-delete | Remove all role associations (owner + guest) |
| **Invitation** (sent) | Hard-delete | Prevent stale invitation accepts to soft-deleted sites |
| **Invitation** (received) | Hard-delete | Clean up pending invitations for deleted user |
| **Notification** | Hard-delete | Remove orphaned notification records |

## Authentication / Token Invalidation

When an account is soft-deleted:
1. **Security stamp** is updated via `UserManager.UpdateSecurityStampAsync()`
2. This automatically invalidates all existing **refresh tokens** (the `/refresh` endpoint calls `ValidateSecurityStampAsync`)
3. Existing **access tokens** expire naturally (short-lived)
4. The `/login` endpoint checks `IsDeleted` and returns `401 Unauthorized`

## Error Codes

| Code | HTTP Status | When |
|------|-------------|------|
| `AUTH_USER_NOT_AUTHENTICATED` | 401 | No authenticated user |
| `USER_ADMIN_CANNOT_DELETE` | 400 | Administrator account attempts deletion |
| `USER_CANNOT_DELETE_WITH_ACTIVE_SHARES` | 400 | Owner has sites shared with guests |
| `USER_ACCOUNT_DELETED` | — | Success message code |

## Edge Cases

### Device Re-registration
If we only soft-deleted the Site without removing SetupDevice, the `CreateSetupCommand` would still see the device as "in use." **Solution**: hard-delete SetupDevice to free the Device.

### Pending Invitations to Deleted Sites
If not cleaned up, someone could accept an invitation to a soft-deleted site. **Solution**: explicitly delete Invitations for owned sites during account deletion.

### Audit Trail Integrity
`CreatedBy`/`LastModifiedBy` fields across entities reference the deleted user's ID (GUID). **Not a problem**: soft delete preserves the user record with its ID, and PII anonymization ensures no personal data is retained while keeping the reference valid.

### DeviceNotification Orphans
`DeviceNotification` has FKs to both `Site` and `SetupDevice`. Hard-deleting SetupDevice will cascade-delete DeviceNotifications linked via SetupDeviceId. Those linked only via SiteId remain but are filtered by `!Site.IsDeleted` in queries.

### Concurrent Operations
If a guest accepts an invitation while the owner is deleting, database transaction isolation handles the race condition. The invitation cleanup removes pending invitations before SaveChanges.

### Demo User Deletion and Self-Healing
Demo users follow the same deletion flow as regular users. The demo system automatically recovers:
1. PII anonymization changes the demo user's email from `demo@netzr.com` to `deleted-{id}@deleted.local`
2. On next backend startup, the seeder doesn't find a user with `demo@netzr.com` → creates a **new** demo user
3. The demo device is preserved (not deleted), so `SeedDemoDeviceAsync` skips device creation
4. On next demo login, `SeedDemoDataAsync` detects the missing Site/Setup/SetupDevice chain and rebuilds it, linking to the existing demo device

### Email Uniqueness After Deletion
Anonymized emails use the pattern `deleted-{userId}@deleted.local`, which is guaranteed unique (userId is a GUID). This prevents collisions and allows the original email to be re-used for a new account registration.

## API Endpoint

```
DELETE /api/users/delete-account
Authorization: Bearer {token}

Response: 204 No Content (success)
Response: 400 Bad Request (active shares or admin user)
Response: 401 Unauthorized (not authenticated or deleted account)
```

## Database Migration

Migration `AddSoftDeleteToApplicationUser` adds to `AspNetUsers` table:
- `IsDeleted` (boolean, NOT NULL, default: false)
- `DeletedAt` (timestamp with time zone, nullable)
