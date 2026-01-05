# Demo User Implementation Report

## Overview
This document describes the implementation of the demo user feature for Google Play Store and iOS App Store assessment.

**Demo User Credentials:**
- Email: `demo@oklidis.com.tr`
- Password: `demo@oklidis`

**Demo Device:**
- Serial Number: `DEMO-HEMS-001`
- ThingsBoardDeviceId: `00000000-DEMO-0000-0000-000000000001`

---

## Architecture

### Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PRODUCTION FLOW                                    │
│                                                                              │
│  ┌─────────────┐    ThingsBoard    ┌─────────────────┐      ┌────────────┐  │
│  │ Real Device │ ────────────────▶ │ Backend API     │ ────▶│ Database   │  │
│  └─────────────┘                   │ /api/DeviceData │      │ Tables     │  │
│                                    └─────────────────┘      └────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           DEMO MODE FLOW                                     │
│                                                                              │
│  ┌─────────────┐    HTTP POST      ┌─────────────────┐      ┌────────────┐  │
│  │ Simulator   │ ────────────────▶ │ Backend API     │ ────▶│ Database   │  │
│  │ (Hangfire)  │                   │ /api/DeviceData │      │ Tables     │  │
│  └─────────────┘                   └─────────────────┘      └────────────┘  │
│                                                                     │        │
│                                                                     ▼        │
│                                                              ┌────────────┐  │
│                                                              │ Mobile App │  │
│                                                              │ (reads DB) │  │
│                                                              └────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Key Insight
**Endpoints do NOT need changes.** The mobile app reads data from the same database tables regardless of whether the data came from a real device or the simulator.

---

## Authentication

### Two Types of Authentication

| Type | Used By | Login Endpoint | Token Type |
|------|---------|----------------|------------|
| **User Authentication** | Mobile App | `POST /api/login` | User Token |
| **Device Authentication** | Simulator | `POST /api/Devices/login` | Device Token |

### Simulator Authentication Flow

The simulator authenticates as a **device** (not as a user):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      SIMULATOR AUTHENTICATION FLOW                           │
│                                                                              │
│  1. Simulator calls:                                                         │
│     POST /api/Devices/login                                                  │
│     Body: { "SerialNumber": "DEMO-HEMS-001" }                               │
│                              │                                               │
│                              ▼                                               │
│  2. Backend returns Device Token:                                            │
│     { "accessToken": "eyJ...", "expiresAt": "..." }                         │
│                              │                                               │
│                              ▼                                               │
│  3. Simulator uses Device Token for protected endpoints:                     │
│     Authorization: Bearer eyJ...                                             │
│                              │                                               │
│                              ▼                                               │
│  4. Sends telemetry data to /api/DeviceData/*                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Endpoint Authentication Requirements

| Endpoint | Auth Required | Token Type | Used By |
|----------|---------------|------------|---------|
| `/api/DeviceData/PowerFlow` | No | - | Simulator |
| `/api/DeviceData/DailyEnergyFlow` | No | - | Simulator |
| `/api/DeviceData/TotalEnergyFlow` | No | - | Simulator |
| `/api/DeviceData/BatterySoc` | No | - | Simulator |
| `/api/DeviceData/Heartbeat` | No | - | Simulator |
| `/api/DeviceData/Alarm` | No | - | Simulator |
| `/api/DeviceData` (POST) | Yes | Device Token | Simulator |
| `/api/DeviceNotifications` | Yes | Device Token | Simulator |
| `/api/Sites/*` | Yes | User Token | Mobile App |
| `/api/Devices/*` (except login) | Yes | User Token | Mobile App |
| `/api/Dashboard/*` | Yes | User Token | Mobile App |

### How Simulator Gets Device Token

```csharp
// Simulator calls this endpoint
POST /api/Devices/login
Content-Type: application/json

{
    "SerialNumber": "DEMO-HEMS-001"
}

// Response
{
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2025-01-01T12:00:00Z"
}
```

### Device Token Policy

The `[Authorize(Policies.DeviceOnly)]` attribute validates:
- Token is a valid Device Token (not User Token)
- Device exists in database
- Device is not deleted

---

## Startup Sequence

### Backend Startup Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         BACKEND STARTUP SEQUENCE                             │
│                                                                              │
│  1. Application starts                                                       │
│           │                                                                  │
│           ▼                                                                  │
│  2. Database migration applied                                               │
│           │                                                                  │
│           ▼                                                                  │
│  3. TrySeedAsync() called                                                    │
│           │                                                                  │
│           ├──▶ Create Administrator user                                     │
│           │                                                                  │
│           ├──▶ Create MobilUser role                                         │
│           │                                                                  │
│           ├──▶ Create Demo User (demo@oklidis.com.tr)                        │
│           │                                                                  │
│           ├──▶ SeedDemoDeviceAsync() ◀── NEW: Demo device seeded at startup │
│           │         │                                                        │
│           │         ├──▶ Create Demo Site (Istanbul)                         │
│           │         ├──▶ Create Demo Setup                                   │
│           │         ├──▶ Create Demo Device (DEMO-HEMS-001)                  │
│           │         ├──▶ Create Demo Components (Inverter, Battery, BMS)     │
│           │         └──▶ Initialize DeviceModeHistory                        │
│           │                                                                  │
│           └──▶ Seed other default data                                       │
│                                                                              │
│  4. Application ready - Simulator can now authenticate                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Why Demo Device is Seeded at Startup

**Problem (Before):** Demo device was only created when demo user logged in.
- Simulator couldn't authenticate until someone logged in as demo user
- Chicken-and-egg problem

**Solution (Now):** Demo device is seeded during application startup.
- Simulator can authenticate immediately
- No dependency on user login

---

## Implementation Status

### Completed (Backend)

| Component | File | Description |
|-----------|------|-------------|
| IsDemo flag | `ApplicationUser.cs` | Flag to identify demo users |
| IsDemoDevice flag | `Device.cs` | Flag to identify demo devices |
| IDemoDataService | `IDemoDataService.cs` | Interface for demo data operations |
| DemoDataService | `DemoDataService.cs` | Implementation with reset/seed logic |
| Demo user seeding | `ApplicationDbContextInitialiser.cs` | Seeds demo user on startup |
| Demo device seeding | `ApplicationDbContextInitialiser.cs` | Seeds demo device on startup |
| Login reset | `CustomIdentityApiEndpointRouteBuilderExtensions.cs` | Resets demo data on each login |
| ThingsBoard skip | `SetDeviceMode.cs` | Skips RPC calls for demo devices |
| EF Migration | `AddDemoUserAndDemoDeviceFlags.cs` | Database migration |

### Simulator Components (All Implemented)

| Service | File | Data Type | Frequency |
|---------|------|-----------|-----------|
| DeviceSimulationJobService | `DeviceSimulationJobService.cs` | Inverter, Battery, BMS | Every 5 min |
| EnergyDataJobService | `EnergyDataJobService.cs` | Daily/Lifetime Energy | Hourly/Daily |
| DeviceNotificationJobService | `DeviceNotificationService.cs` | Alarms/Notifications | Every 5 min |
| **PowerFlowJobService** | `PowerFlowJobService.cs` | Real-time Power (PV, Grid, Battery, Load) | Every 5 min |
| **WeatherDataJobService** | `WeatherDataJobService.cs` | Temperature, Humidity, Conditions | Every 30 min |

---

## Database Tables for Telemetry

The mobile app reads from these tables (no changes needed):

| Table | Data Type | Source Field |
|-------|-----------|--------------|
| `DeviceData` | Generic telemetry | `ThingsBoardDeviceId` |
| `DeviceEnergyData` | Energy (kWh) | `ThingsBoardDeviceId` |
| `DevicePowerData` | Power (W) | `ThingsBoardDeviceId` |
| `DeviceWeatherData` | Weather | `ThingsBoardDeviceId` |
| `DeviceNotifications` | Alarms | `DeviceId` |
| `DeviceModeHistory` | Operating mode | `DeviceId` |

---

## Completed Changes

### 1. Demo Device ThingsBoardDeviceId (DONE)

Demo device uses a well-known GUID for data identification:
```csharp
// Well-known demo ThingsBoard device ID in DemoDataService.cs
public static readonly Guid DemoThingsBoardDeviceId =
    new Guid("00000000-DEMO-0000-0000-000000000001");
```

### 2. PowerFlow Data Simulation (DONE)

New `PowerFlowJobService` generates realistic power data:
- `PvTotalPowerW` - Solar power (based on time of day)
- `GridTotalPowerW` - Grid import/export
- `BatteryPowerW` - Battery charge/discharge
- `LoadTotalPowerW` - Home consumption (500W-3000W)

### 3. Weather Data Simulation (DONE)

New `WeatherDataJobService` generates weather data:
- `Temperature` - Based on month/hour (Istanbul climate)
- `Humidity` - Inverse to temperature
- `Condition` - Sunny, Partly Cloudy, Cloudy, Clear, Light Rain
- `WindSpeed`, `WindDirection`, `CloudCover`, `UvIndex`

### 4. Demo-Only Mode Configuration (DONE)

Simulator configuration in `appsettings.json`:
```json
{
  "DeviceSimulator": {
    "DemoModeOnly": true,
    "DemoDeviceSerial": "DEMO-HEMS-001",
    "PowerFlowCronExpression": "*/5 * * * *",
    "WeatherCronExpression": "*/30 * * * *"
  }
}
```

### 5. Demo Device Seeding at Startup (DONE)

Demo device is now created during application startup in `ApplicationDbContextInitialiser.cs`:
- No dependency on demo user login
- Simulator can authenticate immediately after backend starts

---

## Demo User Login Flow

```
1. User logs in with demo@oklidis.com.tr / demo@oklidis
           │
           ▼
2. Backend detects IsDemo = true
           │
           ▼
3. ResetDemoDataAsync() is called
   - Deletes DeviceData, DeviceEnergyData, DevicePowerData
   - Deletes DeviceWeatherData, DeviceDisconnectLogs
   - Deletes DeviceNotifications, Notifications
   - Resets DeviceModeHistory to SelfConsumption
           │
           ▼
4. SeedDemoDataAsync() is called (if not exists)
   - Creates Demo Site (Istanbul)
   - Creates Demo Setup
   - Creates Demo Device (DEMO-HEMS-001)
   - Creates Demo Components (Inverter, Battery, BMS)
           │
           ▼
5. User sees fresh demo environment
           │
           ▼
6. Simulator continuously sends data to demo device
   (via Hangfire scheduled jobs)
```

---

## Complete System Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          COMPLETE DEMO SYSTEM FLOW                           │
│                                                                              │
│  BACKEND STARTUP:                                                            │
│  ├── Create demo user (demo@oklidis.com.tr)                                 │
│  ├── Create demo device (DEMO-HEMS-001)                                     │
│  └── Ready for connections                                                   │
│                                                                              │
│  SIMULATOR:                                                                  │
│  ├── POST /api/Devices/login (SerialNumber: "DEMO-HEMS-001")                │
│  ├── Receive Device Token                                                    │
│  ├── Start Hangfire jobs:                                                    │
│  │   ├── PowerFlowJobService (every 5 min)                                  │
│  │   ├── EnergyDataJobService (hourly/daily)                                │
│  │   ├── WeatherDataJobService (every 30 min)                               │
│  │   ├── DeviceNotificationJobService (every 5 min)                         │
│  │   └── DeviceSimulationJobService (every 5 min)                           │
│  └── Send telemetry data continuously                                        │
│                                                                              │
│  MOBILE APP:                                                                 │
│  ├── POST /api/login (demo@oklidis.com.tr / demo@oklidis)                   │
│  ├── Receive User Token                                                      │
│  ├── Data reset + seed (if needed)                                          │
│  ├── GET /api/Sites - See Demo Home                                         │
│  ├── GET /api/DeviceData/PowerFlow - See real-time power                    │
│  ├── GET /api/DeviceEnergyData - See energy charts                          │
│  └── View dashboard with simulated data                                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Device Sharing in Demo Mode

Device sharing should work normally for demo devices:
- Demo user can share device with other users
- Shared users can view demo device data
- No special handling needed (uses same SiteUser/Invitation system)

**Note:** Invitations sent from demo user should be clearly marked as demo invitations.

---

## Testing Checklist

- [ ] Demo user can login with demo@oklidis.com.tr / demo@oklidis
- [ ] Demo data is reset on each login
- [ ] Demo site/setup/device is created automatically
- [ ] Dashboard shows simulated power flow data
- [ ] Energy charts show simulated daily/lifetime data
- [ ] Weather widget shows simulated weather
- [ ] Notifications/alarms appear in notification list
- [ ] Device mode can be changed (simulated, no RPC)
- [ ] Device sharing works for demo device
- [ ] Simulator can authenticate after backend restart

---

## Files Changed

### Backend (`Hems-Backend/src/`)

```
Domain/Entities/Device.cs                    - Added IsDemoDevice flag
Infrastructure/Identity/ApplicationUser.cs   - Added IsDemo flag
Application/Common/Interfaces/IDemoDataService.cs - New interface
Infrastructure/Services/DemoDataService.cs   - New implementation
Infrastructure/DependencyInjection.cs        - Registered service
Infrastructure/Data/ApplicationDbContextInitialiser.cs - Demo user + device seeding
Web/Infrastructure/CustomIdentityApiEndpointRouteBuilderExtensions.cs - Login reset
Application/DeviceSettings/Commands/SetDeviceMode/SetDeviceMode.cs - Skip RPC for demo
Infrastructure/Data/Migrations/..._AddDemoUserAndDemoDeviceFlags.cs - Migration
```

### Simulator (`Hems.DeviceSimulator/`)

```
Services/PowerFlowJobService.cs   - NEW: Real-time power simulation
Services/WeatherDataJobService.cs - NEW: Weather data simulation
Controllers/DeviceController.cs   - Updated to include new services
Program.cs                        - Registered new services
appsettings.json                  - Added demo mode configuration
```

---

## Deployment Steps

1. **Apply migration:**
   ```bash
   dotnet ef database update --project src/Infrastructure --startup-project src/Web
   ```

2. **Configure simulator for demo mode (production):**
   Set `DemoModeOnly: true` in simulator's `appsettings.json`

3. **Start backend and simulator:**
   ```bash
   # Start backend
   dotnet run --project src/Web

   # Start simulator
   dotnet run --project Hems.DeviceSimulator
   ```

4. **Start simulation jobs:**
   ```bash
   POST http://localhost:5001/api/Device/start
   ```

5. **Test demo login:**
   - Email: `demo@oklidis.com.tr`
   - Password: `demo@oklidis`

---

## Troubleshooting

### Simulator can't authenticate

**Symptom:** Simulator logs show "Device not found" or "Login failed"

**Cause:** Demo device doesn't exist in database

**Solution:**
1. Ensure backend has started and run migrations
2. Check logs for "Demo device seeded" message
3. Verify device exists: `SELECT * FROM "Devices" WHERE "SerialNumber" = 'DEMO-HEMS-001'`

### No data appearing in mobile app

**Symptom:** Dashboard shows no data after demo login

**Cause:** Simulator jobs not running

**Solution:**
1. Start simulator: `dotnet run --project Hems.DeviceSimulator`
2. Trigger jobs: `POST http://localhost:5001/api/Device/start`
3. Check Hangfire dashboard: `http://localhost:5001/hangfire`

### Demo data not resetting on login

**Symptom:** Old data persists after demo user logs in

**Cause:** ResetDemoDataAsync failed

**Solution:**
1. Check backend logs for errors during login
2. Verify demo user has `IsDemo = true` flag
3. Manually clear data if needed
