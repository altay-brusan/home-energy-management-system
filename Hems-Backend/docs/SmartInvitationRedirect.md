# Smart Invitation Link Redirect

This document describes the smart invitation link redirect system that provides platform-aware deep linking for invitation emails.

## Overview

When users receive an invitation email, clicking the link takes them to `/invite?inviteCode=XXXXXX`. The system detects the user's platform (iOS, Android, or Desktop) and provides an optimal experience:

- **Mobile (iOS/Android)**: Attempts to open the Netzr app automatically, shows manual code if app isn't installed
- **Desktop**: Shows instructions to use the mobile app (invitations can only be accepted on mobile)
- **In-app browsers**: Shows a warning to open in native browser

## Invitation Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  1. INVITATION CREATED                                                      │
│     - Site owner invites user via API                                       │
│     - Email sent with link to /invite?inviteCode=TOKEN                      │
│     - URL uses SmtpSettings.InvitationUrl (environment-specific)            │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  2. USER CLICKS EMAIL LINK                                                  │
│     Dev:   https://hems-api-dev.oklidis.com.tr/invite?inviteCode=...        │
│     Stage: https://hems-api-stg.oklidis.com.tr/invite?inviteCode=...        │
│     Prod:  https://hems-api.oklidis.com.tr/invite?inviteCode=...            │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  3. SMART REDIRECT PAGE                                                     │
│     - Detects platform from User-Agent                                      │
│     - Shows platform-specific content                                       │
│     - Attempts to open app on mobile                                        │
│     - Shows invitation code for manual entry                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┼─────────────────┐
                    ▼                 ▼                 ▼
             ┌──────────┐      ┌──────────┐      ┌──────────┐
             │   iOS    │      │ Android  │      │ Desktop  │
             └──────────┘      └──────────┘      └──────────┘
                    │                 │                 │
                    ▼                 ▼                 ▼
            ┌─────────────┐   ┌─────────────┐   ┌─────────────┐
            │ Deep link   │   │ Intent URL  │   │ Shows info  │
            │ attempt     │   │ attempt     │   │ box: "Use   │
            │             │   │             │   │ mobile app" │
            └─────────────┘   └─────────────┘   └─────────────┘
                    │                 │
                    └────────┬────────┘
                             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  4. USER OPENS NETZR APP                                                    │
│     - App receives inviteCode via deep link, OR                             │
│     - User manually enters 6-digit code                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  5. ACCEPT INVITATION API                                                   │
│     - Mobile app calls POST /api/Invitations/accept with token              │
│     - Backend validates token, creates UserSite record                      │
│     - User gains access to the site                                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Environment-Specific URLs

The invitation email link uses `SmtpSettings.InvitationUrl` which is configured per environment:

| Environment | API URL | Invite Link |
|-------------|---------|-------------|
| Development | `https://hems-api-dev.oklidis.com.tr/api` | `https://hems-api-dev.oklidis.com.tr/invite?inviteCode=...` |
| Staging | `https://hems-api-stg.oklidis.com.tr/api` | `https://hems-api-stg.oklidis.com.tr/invite?inviteCode=...` |
| Production | `https://hems-api.oklidis.com.tr/api` | `https://hems-api.oklidis.com.tr/invite?inviteCode=...` |

## Configuration

### appsettings.json (Base)

```json
{
  "SmtpSettings": {
    "InvitationUrl": "https://hems-api-dev.oklidis.com.tr"
  },
  "AppLinks": {
    "IosAppStoreUrl": "",
    "AndroidPlayStoreUrl": "https://play.google.com/store/apps/details?id=com.netzrenergy",
    "IosBundleId": "",
    "IosTeamId": ""
  }
}
```

### appsettings.Staging.json

```json
{
  "SmtpSettings": {
    "InvitationUrl": "https://hems-api-stg.oklidis.com.tr"
  },
  "AppLinks": {
    "IosAppStoreUrl": "",
    "AndroidPlayStoreUrl": "https://play.google.com/store/apps/details?id=com.netzrenergy",
    "WebDashboardUrl": "https://hems-app.n2nenergy.com/dashboard",
    "IosBundleId": "",
    "IosTeamId": ""
  }
}
```

| Setting | Description |
|---------|-------------|
| `SmtpSettings.InvitationUrl` | Base URL for invitation links (environment-specific) |
| `AppLinks.IosAppStoreUrl` | App Store URL (empty until iOS app is published) |
| `AppLinks.AndroidPlayStoreUrl` | Google Play Store URL |
| `AppLinks.WebDashboardUrl` | Web dashboard URL for desktop redirect (TODO: make environment-specific) |
| `AppLinks.IosBundleId` | iOS bundle ID for Smart App Banner |
| `AppLinks.IosTeamId` | Apple Team ID for Universal Links |

## User Experience by Platform

| Platform | App Installed | Behavior |
|----------|---------------|----------|
| iOS | Yes | Deep link attempt via custom scheme |
| iOS | No | Shows code + "iOS app coming soon" message |
| Android | Yes | App opens via Intent URL |
| Android | No | Shows Play Store button + code |
| Desktop | N/A | Auto-redirects to web dashboard after 2 seconds |
| In-app browser | Any | Warning to open in Safari/Chrome + code display |

### Desktop Redirect

Desktop users are automatically redirected to the web dashboard (`WebDashboardUrl` in appsettings).

**Current configuration**: All environments use `https://hems-app.n2nenergy.com/dashboard`

> **TODO (Future)**: Make `WebDashboardUrl` environment-specific:
> - Dev: `https://hems-app-dev.n2nenergy.com/dashboard` (or similar)
> - Stage: `https://hems-app-stg.n2nenergy.com/dashboard` (or similar)
> - Prod: `https://hems-app.n2nenergy.com/dashboard`

## Components

### 1. Platform Detection Service

**Interface**: `src/Application/Common/Interfaces/IPlatformDetectionService.cs`
**Implementation**: `src/Infrastructure/Services/PlatformDetectionService.cs`

```csharp
public enum DetectedPlatform { iOS, Android, Desktop }

public interface IPlatformDetectionService
{
    DetectedPlatform DetectPlatform(string? userAgent);
    bool IsInAppBrowser(string? userAgent);
}
```

Detects:
- **iOS**: User-Agent contains "iphone", "ipad", or "ipod"
- **Android**: User-Agent contains "android"
- **In-app browsers**: Gmail, Facebook, Instagram, WhatsApp, LinkedIn, etc.

### 2. Invite Endpoint

**Location**: `src/Web/Endpoints/Invite.cs`

Handles `/invite` GET requests and returns platform-specific HTML with:
- Netzr branding (green theme #00A270)
- Platform-specific messaging
- Auto deep link attempt on mobile
- Manual invitation code display

### 3. Invitation Email Service

**Location**: `src/Infrastructure/Services/InvitationEmailService.cs`

Sends invitation emails with:
- Netzr branding
- 6-digit invitation code prominently displayed
- "Open App" button linking to `/invite?inviteCode=...`
- Direct link for fallback

### 4. Accept Invitation Handler

**Location**: `src/Application/Invitations/Commands/AcceptInvitation/AcceptInvitation.cs`

Mobile app calls this API to accept the invitation:
- Validates token and expiration (24 hours)
- Creates UserSite record with Guest role
- Links user to the site

## Deep Link URLs

| Type | URL Format |
|------|------------|
| Custom Scheme | `n2nenergymobile://invite?inviteCode={code}` |
| Android Intent | `intent://invite?inviteCode={code}#Intent;scheme=n2nenergymobile;package=com.n2nenergymobile;end` |

## Files Summary

| File | Purpose |
|------|---------|
| `src/Web/Endpoints/Invite.cs` | Smart redirect endpoint |
| `src/Infrastructure/Services/InvitationEmailService.cs` | Email template with Netzr branding |
| `src/Infrastructure/Services/PlatformDetectionService.cs` | Platform detection logic |
| `src/Infrastructure/Services/AppLinksSettings.cs` | App links configuration |
| `src/Application/Invitations/Commands/AcceptInvitation/` | Accept invitation handler |
| `src/Web/wwwroot/.well-known/assetlinks.json` | Android App Links |
| `src/Web/wwwroot/.well-known/apple-app-site-association` | iOS Universal Links (placeholder) |

## Testing

### 1. Run the Application

```bash
dotnet run --project src/Web
```

### 2. Test Invite Page

```bash
# Desktop view
curl -k "https://localhost:7164/invite?inviteCode=TEST123"

# iOS simulation
curl -k -H "User-Agent: Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X)" \
  "https://localhost:7164/invite?inviteCode=TEST123"

# Android simulation
curl -k -H "User-Agent: Mozilla/5.0 (Linux; Android 13; Pixel 7)" \
  "https://localhost:7164/invite?inviteCode=TEST123"

# Gmail in-app browser (shows warning)
curl -k -H "User-Agent: Mozilla/5.0 Gmail/2023.01.01" \
  "https://localhost:7164/invite?inviteCode=TEST123"
```

### 3. Verify in Browser

Open: `https://localhost:7164/invite?inviteCode=TEST123`

Expected (on desktop):
- Green gradient background
- "You're Invited!" heading
- "Redirecting to web dashboard..." message
- "Open Web Dashboard" button
- Invitation code box with "TEST123"
- Auto-redirects to `https://hems-app.n2nenergy.com/dashboard` after 2 seconds

### 4. Test Email Flow

1. Create an invitation via the API
2. Check the email received
3. Verify email shows Netzr branding
4. Click link and verify redirect page appears
5. On mobile: verify app opens (if installed) or shows store button

## Mobile App Requirements

### Android

The Android app needs:

1. Intent filter in `AndroidManifest.xml`:
```xml
<intent-filter android:autoVerify="true">
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="n2nenergymobile" android:host="invite" />
</intent-filter>
```

2. Handle deep link and extract `inviteCode` parameter

### iOS (When Ready)

1. Update `apple-app-site-association` with Team ID and Bundle ID
2. Update `appsettings.json` with `IosAppStoreUrl` and `IosBundleId`
3. Add Associated Domains entitlement in iOS app
4. Handle universal links in AppDelegate/SceneDelegate
