# Device Component Summary API

## Overview
The Device Component Summary API provides a high-level overview of all components (inverters and batteries) associated with a specific device. This endpoint returns identity information and current state data for each component.

**Important**: There is no SignalR hub for component data. The frontend must **periodically poll** this endpoint (recommended: every 5-10 seconds) while the dashboard/summary screen is active to get updated state data (power, SoC, etc.).

## Endpoint

### Get Device Summary
Retrieves summary information for all components of a device.

```
GET /api/DeviceComponents/DeviceSummary?deviceId={deviceId}
```

**Authentication**: Required (Bearer token)

**Query Parameters**:
- `deviceId` (required, GUID): The ID of the device

**Response**: `DeviceSummaryResponseDto`

---

## How Component IDs are Obtained

When a user logs in and selects a site, the backend provides:
- **Device IDs** for all devices in the site
- **Component IDs** for all components (inverters, batteries) associated with each device

The frontend should **store these IDs** and use them for subsequent API calls:
- Use `deviceId` for the Device Summary endpoint
- Use `componentId` for the Device Details endpoint

---

## Response Structure

### DeviceSummaryResponseDto
```json
{
  "components": [ComponentSummaryDto]
}
```

### ComponentSummaryDto (Base)
```json
{
  "id": "guid",
  "type": "inverter" | "battery",
  "name": "string",
  "model": "string",
  "manufacturer": "string",
  "serialNumber": "string"
}
```

### InverterSummaryDto (extends ComponentSummaryDto)
Includes all base fields plus:
```json
{
  "activePowerKw": number | null
}
```

### BatterySummaryDto (extends ComponentSummaryDto)
Includes all base fields plus:
```json
{
  "stateOfCharge": number | null,
  "packs": [BatteryPackDto]
}
```

### BatteryPackDto
```json
{
  "id": "string",
  "type": "battery",
  "name": "string",
  "model": "string",
  "manufacturer": "string",
  "serialNumber": "string",
  "stateOfCharge": number | null
}
```

---

## JSON Polymorphism

The `components` array uses **JSON polymorphism** with a type discriminator:
- The `type` field determines which subtype is used
- `type: "inverter"` → `InverterSummaryDto` (includes `activePowerKw`)
- `type: "battery"` → `BatterySummaryDto` (includes `stateOfCharge` and `packs`)

**Important**: Always check the `type` field to determine which additional properties are available.

---

## Example Request

```http
GET /api/DeviceComponents/DeviceSummary?deviceId=c35eb5b7-77c7-4bfb-8ca0-b8d8baada73e HTTP/1.1
Host: your-backend-url.com
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Example Response

```json
{
  "components": [
    {
      "id": "0fb51fc4-16a7-4243-b753-e83ac9bd25c4",
      "type": "inverter",
      "name": "Main Inverter",
      "model": "INV-5000",
      "manufacturer": "Ottomotive",
      "serialNumber": "INV-2024-001",
      "activePowerKw": 3.5
    },
    {
      "id": "fd54076d-b39a-42bf-ad48-b1777b7f0801",
      "type": "battery",
      "name": "Battery Management System",
      "model": "BMS-10KWH",
      "manufacturer": "Ottomotive",
      "serialNumber": "BMS-2024-001",
      "stateOfCharge": 85.5,
      "packs": [
        {
          "id": "pack-001",
          "type": "battery",
          "name": "Battery Pack 1",
          "model": "BMS-10KWH",
          "manufacturer": "Ottomotive",
          "serialNumber": "pack-001",
          "stateOfCharge": 85.5
        }
      ]
    }
  ]
}
```

---

## Testing

Use the following test data in your development environment:

**Device ID**: `c35eb5b7-77c7-4bfb-8ca0-b8d8baada73e`

**Expected Components**:
- Inverter Component ID: `0fb51fc4-16a7-4243-b753-e83ac9bd25c4`
- Battery Component ID: `fd54076d-b39a-42bf-ad48-b1777b7f0801`

---

## Frontend Implementation Guide

### 1. Store IDs After Site Selection
```typescript
interface SiteData {
  deviceId: string;
  components: {
    id: string;
    type: 'inverter' | 'battery';
  }[];
}

// After user selects a site, store this data
const selectedSite: SiteData = {
  deviceId: 'c35eb5b7-77c7-4bfb-8ca0-b8d8baada73e',
  components: [
    { id: '0fb51fc4-16a7-4243-b753-e83ac9bd25c4', type: 'inverter' },
    { id: 'fd54076d-b39a-42bf-ad48-b1777b7f0801', type: 'battery' }
  ]
};
```

### 2. Periodic Polling
```typescript
// Poll every 5 seconds while dashboard is active
const pollInterval = 5000;
let pollingTimer: NodeJS.Timeout;

function startPolling(deviceId: string) {
  pollingTimer = setInterval(async () => {
    const summary = await fetchDeviceSummary(deviceId);
    updateUI(summary);
  }, pollInterval);
}

function stopPolling() {
  clearInterval(pollingTimer);
}

// Start polling when dashboard opens
useEffect(() => {
  startPolling(selectedSite.deviceId);
  return () => stopPolling(); // Clean up on unmount
}, [selectedSite.deviceId]);
```

### 3. Fetch Device Summary
```typescript
async function fetchDeviceSummary(deviceId: string): Promise<DeviceSummaryResponseDto> {
  const response = await fetch(
    `/api/DeviceComponents/DeviceSummary?deviceId=${deviceId}`,
    {
      headers: {
        'Authorization': `Bearer ${yourJwtToken}`,
        'Content-Type': 'application/json'
      }
    }
  );

  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }

  return await response.json();
}
```

### 4. Type-Safe Response Handling
```typescript
interface ComponentSummaryDto {
  id: string;
  type: 'inverter' | 'battery';
  name: string;
  model: string;
  manufacturer: string;
  serialNumber: string;
}

interface InverterSummaryDto extends ComponentSummaryDto {
  type: 'inverter';
  activePowerKw: number | null;
}

interface BatterySummaryDto extends ComponentSummaryDto {
  type: 'battery';
  stateOfCharge: number | null;
  packs: BatteryPackDto[];
}

interface DeviceSummaryResponseDto {
  components: (InverterSummaryDto | BatterySummaryDto)[];
}

// Type guard
function isInverter(component: ComponentSummaryDto): component is InverterSummaryDto {
  return component.type === 'inverter';
}

function isBattery(component: ComponentSummaryDto): component is BatterySummaryDto {
  return component.type === 'battery';
}

// Usage
function renderComponent(component: ComponentSummaryDto) {
  if (isInverter(component)) {
    // TypeScript knows component.activePowerKw exists here
    return <InverterCard power={component.activePowerKw} />;
  } else if (isBattery(component)) {
    // TypeScript knows component.stateOfCharge and component.packs exist here
    return <BatteryCard soc={component.stateOfCharge} packs={component.packs} />;
  }
}
```

---

## Data Refresh Strategy

### Realtime Data (Changes Frequently)
- `activePowerKw` - Inverter output power
- `stateOfCharge` - Battery SoC

**Refresh**: Poll every 5-10 seconds while screen is active

### Static Data (Rarely Changes)
- `name`, `model`, `manufacturer`, `serialNumber`

**Refresh**: Only on initial load or manual refresh

---

## Error Handling

### Possible HTTP Status Codes

| Status | Meaning | Action |
|--------|---------|--------|
| 200 OK | Success | Process response normally |
| 401 Unauthorized | Invalid/expired token | Redirect to login |
| 404 Not Found | Device not found | Show "Device not found" message |
| 500 Server Error | Backend error | Show error, retry after delay |

### Example Error Handler
```typescript
async function fetchWithErrorHandling(deviceId: string) {
  try {
    const response = await fetch(`/api/DeviceComponents/DeviceSummary?deviceId=${deviceId}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    if (response.status === 401) {
      // Redirect to login
      redirectToLogin();
      return null;
    }

    if (response.status === 404) {
      showError('Device not found');
      return null;
    }

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Failed to fetch device summary:', error);
    showError('Failed to load device data. Retrying...');
    // Retry after 5 seconds
    setTimeout(() => fetchWithErrorHandling(deviceId), 5000);
    return null;
  }
}
```

---

## Summary

1. ✅ Use `deviceId` to fetch component summary
2. ✅ Poll every 5-10 seconds for fresh data
3. ✅ Check `type` field for polymorphic components
4. ✅ Store component IDs for detail screens
5. ✅ Handle errors gracefully with retry logic
6. ✅ Stop polling when screen is unmounted

For detailed information about individual components, see [API_DeviceComponentDetails.md](./API_DeviceComponentDetails.md).
