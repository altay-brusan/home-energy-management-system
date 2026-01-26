# Energy Source Mix API

## Overview

The Energy Source Mix API provides data for a pie chart showing how much of the total load energy came from each source (Solar, Battery, Grid, Generator). This endpoint uses **proportional attribution** algorithm to accurately calculate the contribution of each energy source to the load consumption.

**Energy values are returned in Wh (Watt-hours).**

## Endpoint

### Get Energy Source Mix

Retrieves energy source mix for a specific device and date.

```
GET /api/Dashboard/EnergySourceMix?deviceId={deviceId}&date={date}
```

**Authentication**: Required (Bearer token)

**Query Parameters**:
- `deviceId` (required, GUID): The local device ID (not ThingsBoard ID)
- `date` (required, DateOnly): The date for which to calculate the mix (format: `yyyy-MM-dd`)

**Response**: `EnergySourceMixDto`

---

## Algorithm: Proportional Attribution

This endpoint uses a **proportional attribution** algorithm to determine how much energy from each source contributed to the load.

### Power to Energy Conversion

The algorithm handles **variable sampling intervals**:
1. For each sample, calculate the time interval to the NEXT sample
2. Convert power (W) to energy (Wh): `Energy = Power × (Interval_Minutes / 60)`
3. Cap intervals at 15 minutes to prevent outliers from skewing results
4. Use 5-minute default for the last sample

### Attribution Formula

For each sample:

1. **Separate SOURCES** (only positive values):
   - `PV` = PvTotalPowerW (solar generation)
   - `Battery` = max(BatteryPowerW, 0) (discharge only)
   - `Grid` = max(GridTotalPowerW, 0) (import only)
   - `Generator` = GeneratorTotalPowerW

2. **Calculate Total Supply**:
   ```
   Total_Supply = PV + Battery + Grid + Generator
   ```

3. **Proportional Attribution to Load**:
   ```
   PV_to_Load = Load × (PV / Total_Supply)
   Battery_to_Load = Load × (Battery / Total_Supply)
   Grid_to_Load = Load × (Grid / Total_Supply)
   Generator_to_Load = Load × (Generator / Total_Supply)
   ```

4. **Convert to Energy**:
   ```
   Energy_Wh = Power_W × (Interval_Minutes / 60)
   ```

---

## Response Structure

### EnergySourceMixDto

```json
{
  "date": "2025-01-20",
  "deviceId": "c35eb5b7-77c7-4bfb-8ca0-b8d8baada73e",
  "loadDemandWh": 1592.25,
  "sources": [
    {
      "source": "Grid",
      "providedEnergyWh": 1507.97,
      "contribution": 94.7
    },
    {
      "source": "Battery",
      "providedEnergyWh": 84.28,
      "contribution": 5.3
    }
  ],
  "summary": {
    "sampleCount": 26,
    "firstSampleTime": "10:00",
    "lastSampleTime": "12:05",
    "averageIntervalMinutes": 5.0,
    "totalTimeSpanMinutes": 130.0,
    "zeroSupplySamples": 0
  }
}
```

### Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `date` | string | Date in `yyyy-MM-dd` format (device local time) |
| `deviceId` | GUID | The local device ID |
| `loadDemandWh` | number | Total load energy demand in **Wh** |
| `sources` | array | List of energy sources with contributions |
| `summary` | object | Processing statistics |

### EnergySourceContribution

| Field | Type | Description |
|-------|------|-------------|
| `source` | string | Source name: `"Solar"`, `"Battery"`, `"Grid"`, `"Generator"` |
| `providedEnergyWh` | number | Energy provided to load in **Wh** |
| `contribution` | number | Percentage contribution to total load (0-100) |

### EnergySourceMixSummary

| Field | Type | Description |
|-------|------|-------------|
| `sampleCount` | int | Number of power samples processed |
| `firstSampleTime` | string | First sample time (`HH:mm` format) |
| `lastSampleTime` | string | Last sample time (`HH:mm` format) |
| `averageIntervalMinutes` | number | Average time between samples |
| `totalTimeSpanMinutes` | number | Total time span covered |
| `zeroSupplySamples` | int | Samples with load but no supply (edge case) |

---

## Example Calculation

Given 26 power samples with 5-minute intervals:

| Sample | Grid (W) | Load (W) | Battery (W) | Grid→Load (W) | Bat→Load (W) |
|--------|----------|----------|-------------|---------------|--------------|
| 1 | 777 | 777 | 36 | 742.58 | 34.42 |
| 2 | 775 | 775 | 41 | 736.05 | 38.95 |
| ... | ... | ... | ... | ... | ... |
| 26 | 787 | 787 | 37 | 751.65 | 35.35 |

**Totals:**
- Sum of Grid→Load: 18,095.60 W
- Sum of Bat→Load: 1,011.40 W
- Sum of Load: 19,107 W ✓

**Energy Conversion (5-min = 1/12 hour per sample × 26 samples):**
- Grid Energy: 18,095.60 × (5/60) = **1,507.97 Wh** (94.7%)
- Battery Energy: 1,011.40 × (5/60) = **84.28 Wh** (5.3%)
- Total Load: **1,592.25 Wh**

---

## Example Request

```http
GET /api/Dashboard/EnergySourceMix?deviceId=c35eb5b7-77c7-4bfb-8ca0-b8d8baada73e&date=2025-01-20 HTTP/1.1
Host: your-backend-url.com
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Frontend Implementation Guide

### TypeScript Interface

```typescript
interface EnergySourceContribution {
  source: 'Solar' | 'Battery' | 'Grid' | 'Generator';
  providedEnergyWh: number;
  contribution: number;
}

interface EnergySourceMixSummary {
  sampleCount: number;
  firstSampleTime: string | null;
  lastSampleTime: string | null;
  averageIntervalMinutes: number;
  totalTimeSpanMinutes: number;
  zeroSupplySamples: number;
}

interface EnergySourceMixDto {
  date: string;
  deviceId: string;
  loadDemandWh: number;
  sources: EnergySourceContribution[];
  summary: EnergySourceMixSummary;
}
```

### Fetch and Render

```typescript
async function fetchEnergySourceMix(
  deviceId: string,
  date: string
): Promise<EnergySourceMixDto> {
  const response = await fetch(
    `/api/Dashboard/EnergySourceMix?deviceId=${deviceId}&date=${date}`,
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

// Render pie chart
function renderPieChart(data: EnergySourceMixDto) {
  const chartData = data.sources.map(source => ({
    name: source.source,
    value: source.providedEnergyWh,
    percentage: source.contribution
  }));

  const colors: Record<string, string> = {
    Solar: '#FFC107',    // Yellow/Gold
    Battery: '#4CAF50',  // Green
    Grid: '#2196F3',     // Blue
    Generator: '#FF5722' // Orange/Red
  };

  // Use your preferred charting library (Chart.js, Recharts, etc.)
}

// Display summary with unit conversion
function formatEnergy(wh: number): string {
  if (wh >= 1000) {
    return `${(wh / 1000).toFixed(2)} kWh`;
  }
  return `${wh.toFixed(2)} Wh`;
}
```

---

## Error Handling

### Possible HTTP Status Codes

| Status | Meaning | Action |
|--------|---------|--------|
| 200 OK | Success | Process response normally |
| 400 Bad Request | Invalid parameters | Check deviceId and date format |
| 401 Unauthorized | Invalid/expired token | Redirect to login |
| 403 Forbidden | No access to device | Show "Access denied" message |
| 404 Not Found | Device not found | Show "Device not found" message |
| 500 Server Error | Backend error | Show error, retry after delay |

---

## Variable Sampling Intervals

The algorithm handles non-uniform sampling:

1. **Normal case**: Uses actual time delta between consecutive samples
2. **Large gaps** (>15 min): Capped at 15 minutes to prevent outliers
3. **Last sample**: Uses default 5-minute interval
4. **Summary**: Reports `averageIntervalMinutes` and `totalTimeSpanMinutes`

This ensures accurate energy calculations even when:
- Device goes offline temporarily
- Network delays cause irregular data transmission
- Sampling frequency varies

---

## Demo Device Support

For demo devices (IsDemo=true), the endpoint returns simulated data in Wh:
- Total load: 15,000-25,000 Wh (15-25 kWh)
- Solar: 40-60%
- Battery: 15-30%
- Grid: Remaining percentage
- Generator: Occasionally included

---

## Summary

1. Returns energy in **Wh** (Watt-hours)
2. Handles **variable sampling intervals** automatically
3. Sources sorted by contribution (highest first)
4. Only sources that contributed energy are included
5. Use for pie charts showing "Where did my load energy come from?"
