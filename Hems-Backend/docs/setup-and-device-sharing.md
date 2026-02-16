# Setup & Device Sharing Architecture

This document explains the Setup vs SetupDevice entity separation, the device registration flow, and the device sharing mechanism via UserSites.

## Entity Hierarchy

```
User (Identity)
  │
  ├── Site (Owner: Site.UserId)
  │     │
  │     └── Setup (1:1 with Site)
  │           │
  │           └── SetupDevice (1:1 with Setup)
  │                 │
  │                 ├── Device (1:1)
  │                 └── ExternalDevice (1:Many)
  │
  └── UserSite (Guest access)
        └── Site (shared)
```

## Setup vs SetupDevice — Why Two Entities?

### Setup (`src/Domain/Entities/Setup.cs`)

A **Setup** represents a logical installation configuration tied to a **Site**. It is the container that groups everything related to a site's energy management setup.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `SiteId` | Guid | FK to Site (unique — one Setup per Site) |
| `Name` | string | Display name, default "Default Setup" |

**Constraints:**
- Unique index on `SiteId` — enforces 1:1 with Site
- Cascade delete from Site

### SetupDevice (`src/Domain/Entities/SetupDevice.cs`)

A **SetupDevice** is the join entity that links a **Setup** to a physical **Device**. It represents "this device is installed in this setup."

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `SetupId` | Guid | FK to Setup (unique — one device per Setup) |
| `DeviceId` | Guid | FK to Device |

**Constraints:**
- Unique index on `SetupId` — enforces 1:1 with Setup
- Unique composite index on `(SetupId, DeviceId)`
- Cascade delete from Setup
- **Restrict delete** from Device — a Device cannot be deleted while assigned to a SetupDevice

### Why the Separation?

1. **Lifecycle independence**: A Setup can exist before a device is assigned. The setup represents the site's configuration intent; the device assignment comes later during registration.
2. **Device reassignment**: If a device fails, the SetupDevice link can be replaced without destroying the Setup and its associated configuration.
3. **External devices**: `ExternalDevice` entities (solar panels, chargers, wind turbines) hang off SetupDevice, not Setup. This keeps the physical device topology separate from the logical site configuration.
4. **Access control chain**: Queries traverse `Device → SetupDevice → Setup → Site → User/UserSite` to determine access. Separating the join entity makes this chain explicit and queryable.

### Related Entities

**Device** (`src/Domain/Entities/Device.cs`) — the physical device record:
- `ThingsBoardDeviceId` — external IoT platform ID
- `Name`, `Manufacturer`, `ModelNumber`, `SerialNumber`, `QRCode`
- `Specifications` (JsonDocument)
- `IsDeleted` (soft delete), `IsDemoDevice`

**ExternalDevice** (`src/Domain/Entities/ExternalDevice.cs`) — peripheral equipment linked to a SetupDevice:
- `SetupDeviceId` — FK to SetupDevice
- `Name`, `DeviceType` (SolarPanel, Charger, WindTurbine, etc.)
- `Specifications` (JsonDocument)

## Device Registration Flow

**Command:** `CreateSetupCommand` (`src/Application/Setups/Commands/CreateSetup/CreateSetup.cs`)

**Endpoint:** `POST /Setup` (`src/Web/Endpoints/Setups.cs`)

**Input:** `SiteId`, `Name`, `DeviceQRCode?`, `DeviceSerialNumber?`

### Steps

1. **Authenticate** — verify user identity
2. **Validate device identifier** — at least one of `DeviceQRCode` or `DeviceSerialNumber` must be provided
3. **Authorize site access** — verify user is site owner (`Site.UserId == userId`) **or** a guest via UserSites
4. **Check single-setup constraint** — site must not already have a Setup
5. **Lookup device in ThingsBoard** — find device by serial number or QR code in the external IoT platform
6. **Find or create local Device** — match by `ThingsBoardDeviceId` in local database
7. **Check device availability** — device must not already be assigned to another SetupDevice
8. **Create Setup** — new Setup linked to the Site
9. **Create SetupDevice** — link the Setup to the Device
10. **Initialize device settings** — auto-populate parameter values from model-specific schemes
11. **Set default device mode** — default to SelfConsumption

## Device Sharing via UserSites

### Core Entities

**Site** (`src/Domain/Entities/Site.cs`) — the primary owner is stored in `Site.UserId`.

**UserSite** (`src/Domain/Entities/UserSite.cs`) — represents guest access to a site:

| Property | Type | Description |
|----------|------|-------------|
| `Id` | Guid | Primary key |
| `UserId` | string | Guest user's identity ID |
| `SiteId` | Guid | FK to Site |
| `Role` | UserSiteRole | Owner or Guest |

**Constraint:** Unique composite index on `(UserId, SiteId)` — prevents duplicate shares.

**UserSiteRole** (`src/Domain/Enums/UserSiteRole.cs`):
```
Owner = 1
Guest = 2
```

In practice, all shared users are created with `Role = Guest`. The true owner is tracked via `Site.UserId`.

### Invitation Flow

**Endpoints:** `src/Web/Endpoints/Invitations.cs`

**Invitation** (`src/Domain/Entities/Invitation.cs`):

| Property | Type | Description |
|----------|------|-------------|
| `Email` | string | Invitee's email |
| `InvitedUserId` | string? | Resolved when user exists or accepts |
| `InvitedByUserId` | string | Inviter (site owner) |
| `SiteId` | Guid | Target site |
| `Token` | string | 6-digit verification code |
| `Status` | InvitationStatus | Pending/Accepted/Declined/Cancelled/Expired |

**InvitationStatus** (`src/Domain/Enums/InvitationStatus.cs`):
```
Pending = 1, Accepted = 2, Declined = 3, Cancelled = 4, Expired = 5
```

#### 1. Create Invitation (`POST /Create`)

**Command:** `CreateInvitationCommand` (`src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs`)

- Only the **site owner** can send invitations
- Validates: no duplicate pending invitations, no self-invite, invitee not already a member
- Generates a 6-digit token (unique within 24 hours)
- Creates an `Invitation` (status = Pending) and a `Notification` record
- Sends invitation email via `InvitationEmailService`

#### 2. Accept Invitation (`POST /Accept`)

**Command:** `AcceptInvitationCommand` (`src/Application/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs`)

- Matches token to a Pending invitation for the user's email
- Checks expiration (24-hour window from creation)
- Updates invitation status to Accepted
- **Creates a UserSite record** with `Role = Guest`
- Links notification to user if needed

#### 3. Decline Invitation (`POST /Decline`)

**Command:** `DeclineInvitationCommand` (`src/Application/Invitations/Commands/DeclineInvitation/DeclineInvitation.cs`)

- Updates invitation status to Declined
- No UserSite is created

#### 4. Remove User from Site (`DELETE /RemoveUser/{invitationId}`)

**Command:** `RemoveUserFromSiteCommand` (`src/Application/Invitations/Commands/RemoveUserFromSite/RemoveUserFromSite.cs`)

- Can be executed by the **site owner** or by the **guest themselves** (self-removal)
- Deletes the UserSite record
- Deletes the Invitation record

### Access Control Pattern

Throughout the application, access checks follow the **Owner OR Guest** pattern:

```csharp
// Owner check
site.UserId == currentUserId

// Guest check
_context.UserSites.Any(us => us.SiteId == siteId && us.UserId == currentUserId)

// Combined
site.UserId == userId || userSharedSiteIds.Contains(site.Id)
```

**Examples of this pattern:**

- **GetSitesByUserQuery** (`src/Application/Sites/Queries/GetSitesByUser/GetSitesByUser.cs`) — returns all sites where user is owner or guest
- **CheckSiteAccessQuery** (`src/Application/Sites/Queries/CheckSiteAccess/CheckSiteAccess.cs`) — boolean access check for a specific site
- **CheckDeviceAccessQuery** (`src/Application/Devices/Queries/CheckDeviceAccess/CheckDeviceAccess.cs`) — traverses Device → SetupDevice → Setup → Site → (Owner OR Guest)
- **CreateSetupCommand** — allows both owners and guests to register devices

### Access Chain for Device Operations

To verify a user can access a device, queries walk the full entity chain:

```
Device
  → SetupDevice (sd.DeviceId == deviceId)
    → Setup (s.Id == sd.SetupId)
      → Site (site.Id == s.SiteId)
        → Owner? (site.CreatedBy == userId)
        → Guest? (UserSites.Any(us => us.SiteId == site.Id && us.UserId == userId))
```

## Key File References

| Concern | File |
|---------|------|
| Setup entity | `src/Domain/Entities/Setup.cs` |
| SetupDevice entity | `src/Domain/Entities/SetupDevice.cs` |
| Device entity | `src/Domain/Entities/Device.cs` |
| ExternalDevice entity | `src/Domain/Entities/ExternalDevice.cs` |
| Site entity | `src/Domain/Entities/Site.cs` |
| UserSite entity | `src/Domain/Entities/UserSite.cs` |
| Invitation entity | `src/Domain/Entities/Invitation.cs` |
| Notification entity | `src/Domain/Entities/Notification.cs` |
| UserSiteRole enum | `src/Domain/Enums/UserSiteRole.cs` |
| InvitationStatus enum | `src/Domain/Enums/InvitationStatus.cs` |
| Setup EF config | `src/Infrastructure/Data/Configurations/SetupConfiguration.cs` |
| SetupDevice EF config | `src/Infrastructure/Data/Configurations/SetupDeviceConfiguration.cs` |
| UserSite EF config | `src/Infrastructure/Data/Configurations/UserSiteConfiguration.cs` |
| CreateSetup command | `src/Application/Setups/Commands/CreateSetup/CreateSetup.cs` |
| CreateInvitation command | `src/Application/Invitations/Commands/CreateInvitation/CreateInvitation.cs` |
| AcceptInvitation command | `src/Application/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs` |
| RemoveUserFromSite command | `src/Application/Invitations/Commands/RemoveUserFromSite/RemoveUserFromSite.cs` |
| CheckSiteAccess query | `src/Application/Sites/Queries/CheckSiteAccess/CheckSiteAccess.cs` |
| CheckDeviceAccess query | `src/Application/Devices/Queries/CheckDeviceAccess/CheckDeviceAccess.cs` |
| GetSitesByUser query | `src/Application/Sites/Queries/GetSitesByUser/GetSitesByUser.cs` |
| Setup endpoints | `src/Web/Endpoints/Setups.cs` |
| Site endpoints | `src/Web/Endpoints/Sites.cs` |
| Invitation endpoints | `src/Web/Endpoints/Invitations.cs` |
