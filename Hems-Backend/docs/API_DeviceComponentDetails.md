# Device Component Details API

## Overview
The Device Component Details API provides comprehensive information about a specific component (inverter or battery). This endpoint returns both **realtime data** (current power, temperature, SoC, etc.) and **parameter data** (model, serial number, rated capacity, etc.) in a structured, frontend-friendly format.

**Important**: There is no SignalR hub for component data. The frontend must **periodically poll** this endpoint (recommended: every 5-10 seconds) while the component detail screen is active to get updated realtime data.

## Endpoint

### Get Component Details
Retrieves detailed information for a specific component.

```
GET /api/DeviceComponents/Details/{componentId}
```

**Authentication**: Required (Bearer token)

**URL Parameters**:
- `componentId` (required, GUID): The ID of the component

**Response**: `DeviceDetailsResponseDto`

---

## How to Get Component IDs

When a user logs in and selects a site, the backend provides:
1. **Device IDs** for all devices in the site
2. **Component IDs** for all components associated with each device

The frontend should **store these component IDs** and use them to navigate to detail screens.

**Flow**:
```
User Login → Select Site → Receive Device/Component IDs → Store IDs
                                                           ↓
                                              User clicks component →
                                              Open detail screen with componentId →
                                              Call Details API with componentId
```

---

## Response Structure

### DeviceDetailsResponseDto
```json
{
  "details": [DeviceDetailItemDto]
}
```

### DeviceDetailItemDto
```json
{
  "key": "string",          // Unique identifier (e.g., "soc", "outputPower")
  "value": any,             // The value (number, string, boolean)
  "unit": "string | null",  // Unit of measurement (e.g., "W", "V", "%", "°C")
  "labelKey": "string"      // Frontend localization key (e.g., "realtime.soc")
}
```

### Label Key Convention
- **`realtime.*`** - Live data from device (changes frequently, poll every 5-10s)
- **`parameter.*`** - Static configuration from database (rarely changes)

---

## Component Types

### Inverter Components
**Realtime Data** (from device RPC call):
- Run state
- Output power (W, VA)
- Output voltage/current/frequency
- Grid-side power (import/export)
- Temperatures
- Grid/generator signal status

**Parameter Data** (from database):
- Name, manufacturer, model, serial number
- Rated power, voltage, frequency
- Firmware version

### Battery Components
**Realtime Data** (from device RPC call):
- State of Charge (SoC %)
- State of Health (SoH %) if available

**Parameter Data** (from database):
- Name, manufacturer, model, serial number
- Capacity (kWh)
- Rated voltage
- Chemistry type

---

## Example Request

### Inverter Component
```http
GET /api/DeviceComponents/Details/0fb51fc4-16a7-4243-b753-e83ac9bd25c4 HTTP/1.1
Host: your-backend-url.com
Authorization: Bearer YOUR_JWT_TOKEN
```

### Battery Component
```http
GET /api/DeviceComponents/Details/fd54076d-b39a-42bf-ad48-b1777b7f0801 HTTP/1.1
Host: your-backend-url.com
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Example Responses

### Inverter Response
```json
{
  "details": [
    // Realtime Data
    {
      "key": "runState",
      "value": "normal",
      "unit": null,
      "labelKey": "realtime.runState"
    },
    {
      "key": "outputActivePower",
      "value": 3542.5,
      "unit": "W",
      "labelKey": "realtime.outputActivePower"
    },
    {
      "key": "outputApparentPower",
      "value": 3650.0,
      "unit": "VA",
      "labelKey": "realtime.outputApparentPower"
    },
    {
      "key": "outputVoltage",
      "value": 230.2,
      "unit": "V",
      "labelKey": "realtime.outputVoltage"
    },
    {
      "key": "outputCurrent",
      "value": 15.4,
      "unit": "A",
      "labelKey": "realtime.outputCurrent"
    },
    {
      "key": "outputFrequency",
      "value": 50.0,
      "unit": "Hz",
      "labelKey": "realtime.outputFrequency"
    },
    {
      "key": "gridSideActivePower",
      "value": -500.5,
      "unit": "W",
      "labelKey": "realtime.gridSideActivePower"
    },
    {
      "key": "gridSidePhaseAPower",
      "value": -500.5,
      "unit": "W",
      "labelKey": "realtime.gridSidePhaseAPower"
    },
    {
      "key": "inverterHeatsinkTemp",
      "value": 45.3,
      "unit": "°C",
      "labelKey": "realtime.inverterHeatsinkTemp"
    },
    {
      "key": "internalAmbientTemp",
      "value": 28.7,
      "unit": "°C",
      "labelKey": "realtime.internalAmbientTemp"
    },
    {
      "key": "gridSignalPresent",
      "value": true,
      "unit": null,
      "labelKey": "realtime.gridSignalPresent"
    },
    {
      "key": "generatorSignalPresent",
      "value": false,
      "unit": null,
      "labelKey": "realtime.generatorSignalPresent"
    },

    // Parameter Data
    {
      "key": "name",
      "value": "Main Inverter",
      "unit": null,
      "labelKey": "parameter.name"
    },
    {
      "key": "manufacturer",
      "value": "Ottomotive",
      "unit": null,
      "labelKey": "parameter.manufacturer"
    },
    {
      "key": "model",
      "value": "INV-5000",
      "unit": null,
      "labelKey": "parameter.model"
    },
    {
      "key": "serialNumber",
      "value": "INV-2024-001",
      "unit": null,
      "labelKey": "parameter.serialNumber"
    },
    {
      "key": "ratedPower",
      "value": 5000,
      "unit": "W",
      "labelKey": "parameter.ratedPower"
    },
    {
      "key": "firmwareVersion",
      "value": "2.1.5",
      "unit": null,
      "labelKey": "parameter.firmwareVersion"
    },
    {
      "key": "ratedVoltage",
      "value": 230,
      "unit": "V",
      "labelKey": "parameter.ratedVoltage"
    },
    {
      "key": "ratedFrequency",
      "value": 50,
      "unit": "Hz",
      "labelKey": "parameter.ratedFrequency"
    }
  ]
}
```

### Battery Response
```json
{
  "details": [
    // Realtime Data
    {
      "key": "soc",
      "value": 85.5,
      "unit": "%",
      "labelKey": "realtime.soc"
    },

    // Parameter Data
    {
      "key": "name",
      "value": "Battery Management System",
      "unit": null,
      "labelKey": "parameter.name"
    },
    {
      "key": "manufacturer",
      "value": "Ottomotive",
      "unit": null,
      "labelKey": "parameter.manufacturer"
    },
    {
      "key": "model",
      "value": "BMS-10KWH",
      "unit": null,
      "labelKey": "parameter.model"
    },
    {
      "key": "serialNumber",
      "value": "BMS-2024-001",
      "unit": null,
      "labelKey": "parameter.serialNumber"
    },
    {
      "key": "capacity",
      "value": 10.0,
      "unit": "kWh",
      "labelKey": "parameter.capacity"
    },
    {
      "key": "ratedVoltage",
      "value": 48,
      "unit": "V",
      "labelKey": "parameter.ratedVoltage"
    },
    {
      "key": "chemistry",
      "value": "LiFePO4",
      "unit": null,
      "labelKey": "parameter.chemistry"
    }
  ]
}
```

---

## Testing

Use the following test data in your development environment:

**Inverter Component ID**: `0fb51fc4-16a7-4243-b753-e83ac9bd25c4`
**Battery Component ID**: `fd54076d-b39a-42bf-ad48-b1777b7f0801`

---

## Frontend Implementation Guide

### 1. TypeScript Interfaces
```typescript
interface DeviceDetailItemDto {
  key: string;
  value: any;
  unit: string | null;
  labelKey: string;
}

interface DeviceDetailsResponseDto {
  details: DeviceDetailItemDto[];
}

type DetailGroup = 'realtime' | 'parameter';

interface GroupedDetails {
  realtime: DeviceDetailItemDto[];
  parameter: DeviceDetailItemDto[];
}
```

### 2. Fetch Component Details
```typescript
async function fetchComponentDetails(componentId: string): Promise<DeviceDetailsResponseDto> {
  const response = await fetch(
    `/api/DeviceComponents/Details/${componentId}`,
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

### 3. Periodic Polling
```typescript
function useComponentDetails(componentId: string) {
  const [details, setDetails] = useState<DeviceDetailsResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    let pollTimer: NodeJS.Timeout;

    async function fetchAndUpdate() {
      try {
        const data = await fetchComponentDetails(componentId);
        if (isMounted) {
          setDetails(data);
          setError(null);
        }
      } catch (err) {
        if (isMounted) {
          setError(err.message);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    // Initial fetch
    fetchAndUpdate();

    // Poll every 5 seconds
    pollTimer = setInterval(fetchAndUpdate, 5000);

    return () => {
      isMounted = false;
      clearInterval(pollTimer);
    };
  }, [componentId]);

  return { details, loading, error };
}
```

### 4. Group Details by Type
```typescript
function groupDetailsByType(details: DeviceDetailItemDto[]): GroupedDetails {
  return details.reduce<GroupedDetails>(
    (acc, detail) => {
      const group = detail.labelKey.startsWith('realtime.') ? 'realtime' : 'parameter';
      acc[group].push(detail);
      return acc;
    },
    { realtime: [], parameter: [] }
  );
}
```

### 5. Render Details with Localization
```typescript
import { useTranslation } from 'react-i18next';

function ComponentDetailsScreen({ componentId }: { componentId: string }) {
  const { t } = useTranslation();
  const { details, loading, error } = useComponentDetails(componentId);

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage message={error} />;
  if (!details) return <NotFound />;

  const grouped = groupDetailsByType(details.details);

  return (
    <div>
      {/* Realtime Data Section */}
      <Section title={t('sections.realtime')}>
        {grouped.realtime.map(detail => (
          <DetailItem
            key={detail.key}
            label={t(detail.labelKey)}
            value={detail.value}
            unit={detail.unit}
          />
        ))}
      </Section>

      {/* Parameter Data Section */}
      <Section title={t('sections.parameters')}>
        {grouped.parameter.map(detail => (
          <DetailItem
            key={detail.key}
            label={t(detail.labelKey)}
            value={detail.value}
            unit={detail.unit}
          />
        ))}
      </Section>
    </div>
  );
}
```

### 6. Detail Item Component
```typescript
function DetailItem({
  label,
  value,
  unit
}: {
  label: string;
  value: any;
  unit: string | null;
}) {
  // Format value based on type
  const formattedValue = typeof value === 'number'
    ? value.toFixed(1)
    : String(value);

  return (
    <div className="detail-item">
      <span className="label">{label}</span>
      <span className="value">
        {formattedValue}
        {unit && <span className="unit"> {unit}</span>}
      </span>
    </div>
  );
}
```

### 7. Localization File (i18n)
```json
// en.json
{
  "sections": {
    "realtime": "Realtime Data",
    "parameters": "Device Parameters"
  },
  "realtime": {
    "runState": "Run State",
    "outputActivePower": "Output Active Power",
    "outputVoltage": "Output Voltage",
    "outputCurrent": "Output Current",
    "outputFrequency": "Output Frequency",
    "gridSideActivePower": "Grid Power",
    "inverterHeatsinkTemp": "Heatsink Temperature",
    "internalAmbientTemp": "Internal Temperature",
    "gridSignalPresent": "Grid Connected",
    "generatorSignalPresent": "Generator Connected",
    "soc": "State of Charge"
  },
  "parameter": {
    "name": "Name",
    "manufacturer": "Manufacturer",
    "model": "Model",
    "serialNumber": "Serial Number",
    "ratedPower": "Rated Power",
    "firmwareVersion": "Firmware Version",
    "ratedVoltage": "Rated Voltage",
    "ratedFrequency": "Rated Frequency",
    "capacity": "Capacity",
    "chemistry": "Battery Chemistry"
  }
}
```

---

## Data Refresh Strategy

### What to Poll
Only **realtime data** needs frequent polling:
- Inverter: power, voltage, current, temperature
- Battery: SoC

**Refresh Rate**: Every 5-10 seconds while detail screen is active

### What NOT to Poll
**Parameter data** is static and doesn't change:
- Name, model, manufacturer, serial number
- Rated values, firmware version

**Refresh**: Only on initial load

---

## Error Handling

### Possible HTTP Status Codes

| Status | Meaning | Action |
|--------|---------|--------|
| 200 OK | Success | Process response normally |
| 401 Unauthorized | Invalid/expired token | Redirect to login |
| 404 Not Found | Component not found | Show "Component not found" |
| 500 Server Error | Backend/device error | Show error, retry after delay |

### Example Error Handler
```typescript
async function fetchWithRetry(componentId: string, retries = 3): Promise<DeviceDetailsResponseDto | null> {
  for (let i = 0; i < retries; i++) {
    try {
      const response = await fetch(`/api/DeviceComponents/Details/${componentId}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (response.status === 401) {
        redirectToLogin();
        return null;
      }

      if (response.status === 404) {
        showError('Component not found');
        return null;
      }

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`Attempt ${i + 1} failed:`, error);
      if (i === retries - 1) {
        showError('Failed to load component details');
        return null;
      }
      // Wait before retry (exponential backoff)
      await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, i)));
    }
  }
  return null;
}
```

---

## Value Formatting Guidelines

### Numbers
- **Power**: Display with 1 decimal place (e.g., `3542.5 W`)
- **Voltage**: Display with 1 decimal place (e.g., `230.2 V`)
- **Current**: Display with 1 decimal place (e.g., `15.4 A`)
- **Temperature**: Display with 1 decimal place (e.g., `45.3 °C`)
- **Percentage**: Display with 1 decimal place (e.g., `85.5 %`)
- **Frequency**: Display with 1 decimal place (e.g., `50.0 Hz`)

### Booleans
- **Grid Connected**: `true` → "Connected", `false` → "Disconnected"
- Use color coding: green for true, red for false

### Strings
- **Run State**: Capitalize first letter, show as status badge
  - `"normal"` → Green badge "Normal"
  - `"alarm"` → Yellow badge "Alarm"
  - `"fault"` → Red badge "Fault"

### Negative Values
- **Grid Power**: Negative means export, positive means import
  - `-500 W` → "500 W (Exporting to grid)"
  - `+300 W` → "300 W (Importing from grid)"

---

## Navigation Flow

```
1. User logs in
   ↓
2. User selects a site
   ↓
3. Backend provides Device IDs + Component IDs
   ↓
4. Frontend stores IDs
   ↓
5. User clicks on component card
   ↓
6. Navigate to detail screen with componentId
   ↓
7. Fetch /api/DeviceComponents/Details/{componentId}
   ↓
8. Display details with periodic polling (every 5-10s)
   ↓
9. User leaves screen → Stop polling
```

---

## Summary

1. ✅ Use `componentId` from site selection to fetch details
2. ✅ Poll every 5-10 seconds for realtime data updates
3. ✅ Group details by `realtime.*` and `parameter.*` label keys
4. ✅ Use localization keys from `labelKey` field
5. ✅ Format values appropriately based on type and unit
6. ✅ Handle errors gracefully with retry logic
7. ✅ Stop polling when user leaves the screen

For overview/summary of all components, see [API_DeviceComponentSummary.md](./API_DeviceComponentSummary.md).
