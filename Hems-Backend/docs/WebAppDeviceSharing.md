# Web App Device Sharing - Manual Invitation Code Entry

## Overview

This document describes the requirements for implementing manual invitation code entry in the web application. This feature allows users to accept device sharing invitations by manually entering a 6-digit code.

## Problem Statement

Users receive invitation emails with a 6-digit code and a deep link. Currently:
- Deep links work well for mobile app users
- Desktop/web users clicking the link are redirected to the web dashboard
- **Missing**: There's no place in the web dashboard to manually enter the invitation code

## API Endpoint

### Accept Invitation

**Endpoint:** `POST /api/Invitations/Accept`

**Request Body:**
```json
{
  "token": "123456"
}
```

**Response:**
- `200 OK` - Invitation accepted successfully
- `400 Bad Request` - Invalid or expired token
- `401 Unauthorized` - User not authenticated
- `404 Not Found` - Invitation not found

**After Success:**
- A `UserSite` record is created linking the user to the shared site
- The user's site list (`GET /api/Sites/user`) will include the newly shared site

## Implementation Requirements

### 1. Device Sharing Page

Add a "Device Sharing" page to the web dashboard (if not already present) with the following sections:

#### A. Send Invitation Section (Existing)
- Email input field
- "Send Invitation" button
- List of pending/accepted invitations

#### B. Accept Invitation Section (NEW)
Add a new section for accepting invitations:

```
┌─────────────────────────────────────────┐
│  Have an invitation code?               │
│  ┌─────────────────────────────────┐    │
│  │  Enter 6-digit code             │    │
│  └─────────────────────────────────┘    │
│  [Accept Invitation]                    │
└─────────────────────────────────────────┘
```

**UI Requirements:**
- Input field for 6-digit numeric code
- "Accept Invitation" button
- Loading state while accepting
- Success message on acceptance
- Error message for invalid/expired codes

### 2. Validation

- Code must be exactly 6 digits
- Only numeric characters allowed
- Show error if code is empty or invalid format

### 3. Success Flow

After successfully accepting an invitation:
1. Show success toast/message: "Invitation accepted successfully"
2. Refresh the sites list to include the newly shared site
3. Optionally: Navigate to the newly shared site's dashboard

### 4. Error Handling

Handle and display appropriate messages for:
- Invalid invitation code
- Expired invitation
- Already accepted invitation
- Network errors

**Example error messages:**
- "Invalid invitation code. Please check and try again."
- "This invitation has expired."
- "You have already accepted this invitation."

## User Flow

### Scenario: Desktop User Accepts Invitation

1. User receives invitation email on desktop
2. User clicks link in email
3. Browser opens → Redirects to web dashboard login
4. User logs in (or is already logged in)
5. User navigates to "Device Sharing" page
6. User enters the 6-digit code from the email
7. User clicks "Accept Invitation"
8. Success → Shared site appears in site list

### Scenario: New User Accepts Invitation

1. User receives invitation email
2. User clicks link → Redirected to web app
3. User creates account / logs in
4. User navigates to "Device Sharing" page
5. User enters code → Accepts invitation
6. Shared site becomes available

## API Reference

### GET /api/Invitations/Notifications
Get pending invitation notifications for the current user.

### POST /api/Invitations/Accept
Accept an invitation using the token.

### GET /api/Sites/user
Get all sites accessible to the current user (includes shared sites after accepting invitation).

## Notes

- The invitation code is sent in the email body
- The code is embedded in the deep link URL as `inviteCode` parameter
- Codes are typically 6-digit numeric strings
- Each code can only be used once
- Codes have an expiration period (check backend configuration)
