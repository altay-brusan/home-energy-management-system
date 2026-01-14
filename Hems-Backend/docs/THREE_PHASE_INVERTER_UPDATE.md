# Three-Phase Inverter Display Update

## Summary

This document describes the changes made to properly support three-phase inverter data display in the HEMS application.

---

## The Issue

### Problem
The inverter detail screen was showing incomplete data for three-phase inverters:
- **Output Section**: Displayed "Output Voltage: -" and "Output Current: -"
- **Grid Section**: Only showed Phase A and Phase B power, missing Phase C

### Root Cause
1. **Backend**: Was sending a `Phases` array with per-phase data, but frontend expected single `outputVoltage` and `outputCurrent` keys
2. **Frontend**: Grid Section code was missing Phase C display
3. **Data Structure Mismatch**: Three-phase inverters have 3 voltages and 3 currents (one per phase), not single values

---

## Solution

### Backend Changes (`GetDeviceDetailsQueryHandler.cs`)

The backend now sends inverter output data in the correct structure:

```json
{
  "details": [
    {
      "key": "Phases",
      "value": [
        { "Phase Id": 1, "Voltage": 230.5, "Current": 2.47, "Power": 569 },
        { "Phase Id": 2, "Voltage": 229.8, "Current": 2.45, "Power": 563 },
        { "Phase Id": 3, "Voltage": 231.2, "Current": 2.49, "Power": 576 }
      ],
      "unit": null,
      "labelKey": "realtime.phases"
    },
    {
      "key": "outputFrequency",
      "value": 50.02,
      "unit": "Hz",
      "labelKey": "realtime.outputFrequency"
    },
    {
      "key": "gridSideActivePower",
      "value": -500,
      "unit": "W",
      "labelKey": "realtime.gridSideActivePower"
    },
    {
      "key": "gridSidePhaseAPower",
      "value": -167,
      "unit": "W",
      "labelKey": "realtime.gridSidePhaseAPower"
    },
    {
      "key": "gridSidePhaseBPower",
      "value": -166,
      "unit": "W",
      "labelKey": "realtime.gridSidePhaseBPower"
    },
    {
      "key": "gridSidePhaseCPower",
      "value": -167,
      "unit": "W",
      "labelKey": "realtime.gridSidePhaseCPower"
    }
  ]
}
```

#### Key Points:
- **`Phases` array**: Contains per-phase Voltage, Current, and Power
- **`outputFrequency`**: Single value (frequency is shared across all phases)
- **`gridSidePhaseCPower`**: Now included (was missing before)
- **No `outputVoltage` or `outputCurrent`**: These single-value keys were removed because they don't make sense for three-phase systems

---

### Frontend Changes (`InverterDetailScreen.tsx`)

#### 1. Extract Phases Array

```typescript
// OLD - Single values (wrong for three-phase)
const outputVoltage = getDetailValue('outputVoltage');
const outputCurrent = getDetailValue('outputCurrent');

// NEW - Phases array
const phases = getDetailValue('Phases') as Array<{
  "Phase Id": number;
  Voltage: number;
  Current: number;
  Power: number
}> | null;
```

#### 2. Add Phase C to Grid Section

```typescript
// Add variable
const gridSidePhaseCPower = getDetailValue('gridSidePhaseCPower');

// Update condition
{(gridSideActivePower != null || gridSidePhaseAPower != null || gridSidePhaseBPower != null || gridSidePhaseCPower != null) && (
  // Grid Section content
)}

// Add Phase C display after Phase B
{gridSidePhaseCPower != null && (
  <View style={styles.row}>
    <Text style={[styles.label, { color: COLORS.textSoft }]}>
      {t("inverterDetail.phaseCPower")}
    </Text>
    <Text style={[styles.value, { color: COLORS.text }]}>
      {gridSidePhaseCPower.toFixed(1)} W
    </Text>
  </View>
)}
```

#### 3. Update Output Section to Display Per-Phase Data

```tsx
{/* Output Section */}
<View style={[styles.section, { backgroundColor: COLORS.surface, borderColor: COLORS.border }]}>
  <Text style={[styles.sectionTitle, { color: COLORS.text }]}>
    {t("inverterDetail.output")}
  </Text>

  {/* Output Frequency - single value shared across all phases */}
  <View style={styles.row}>
    <Text style={[styles.label, { color: COLORS.textSoft }]}>
      {t("inverterDetail.outputFrequency")}
    </Text>
    <Text style={[styles.value, { color: COLORS.text }]}>
      {outputFrequency != null ? `${outputFrequency.toFixed(1)} Hz` : "-"}
    </Text>
  </View>

  {/* Per-phase output data */}
  {phases && phases.length > 0 && phases.map((phase, index) => (
    <View key={phase["Phase Id"] || index} style={[styles.phaseCard, {
      backgroundColor: `${COLORS.accent}08`,
      marginTop: index === 0 ? 12 : 8,
      borderRadius: 8,
      padding: 12
    }]}>
      <Text style={[styles.phaseTitle, { color: COLORS.accent, marginBottom: 8 }]}>
        {t("inverterDetail.phaseLabel", { id: phase["Phase Id"] || index + 1 })}
      </Text>

      <View style={styles.row}>
        <Text style={[styles.label, { color: COLORS.textSoft }]}>
          {t("inverterDetail.voltage")}
        </Text>
        <Text style={[styles.value, { color: COLORS.text }]}>
          {phase.Voltage?.toFixed(1) || 0} V
        </Text>
      </View>

      <View style={styles.row}>
        <Text style={[styles.label, { color: COLORS.textSoft }]}>
          {t("inverterDetail.current")}
        </Text>
        <Text style={[styles.value, { color: COLORS.text }]}>
          {phase.Current?.toFixed(2) || 0} A
        </Text>
      </View>

      <View style={styles.row}>
        <Text style={[styles.label, { color: COLORS.textSoft }]}>
          {t("inverterDetail.power")}
        </Text>
        <Text style={[styles.value, { color: COLORS.text }]}>
          {phase.Power?.toFixed(0) || 0} W
        </Text>
      </View>
    </View>
  ))}
</View>
```

---

## Single-Phase vs Three-Phase Support

The implementation automatically handles both inverter types:

### Single-Phase Inverter
Backend returns only Phase 1 in the array:
```json
{
  "key": "Phases",
  "value": [
    { "Phase Id": 1, "Voltage": 230.5, "Current": 5.2, "Power": 1198 }
  ]
}
```

**Display:**
```
Output
├── Output Frequency: 50.0 Hz
└── Phase 1
    ├── Voltage: 230.5 V
    ├── Current: 5.20 A
    └── Power: 1198 W
```

### Three-Phase Inverter
Backend returns all 3 phases:
```json
{
  "key": "Phases",
  "value": [
    { "Phase Id": 1, "Voltage": 230.5, "Current": 2.47, "Power": 569 },
    { "Phase Id": 2, "Voltage": 229.8, "Current": 2.45, "Power": 563 },
    { "Phase Id": 3, "Voltage": 231.2, "Current": 2.49, "Power": 576 }
  ]
}
```

**Display:**
```
Output
├── Output Frequency: 50.0 Hz
├── Phase 1
│   ├── Voltage: 230.5 V
│   ├── Current: 2.47 A
│   └── Power: 569 W
├── Phase 2
│   ├── Voltage: 229.8 V
│   ├── Current: 2.45 A
│   └── Power: 563 W
└── Phase 3
    ├── Voltage: 231.2 V
    ├── Current: 2.49 A
    └── Power: 576 W
```

---

## Translation Keys Required

Ensure these translation keys exist in the localization files:

```json
{
  "inverterDetail": {
    "output": "Output",
    "outputFrequency": "Output Frequency",
    "phaseLabel": "Phase {{id}}",
    "voltage": "Voltage",
    "current": "Current",
    "power": "Power",
    "phaseAPower": "Phase A Power",
    "phaseBPower": "Phase B Power",
    "phaseCPower": "Phase C Power"
  }
}
```

---

## API & RPC Reference

### REST Endpoint
```
GET /api/DeviceComponents/Details/{componentId}
```

### RPC Call
The backend uses the `getInverterDeviceState` RPC to fetch inverter data from ThingsBoard:

```json
{
  "rpcName": "getInverterDeviceState",
  "kind": "InverterDeviceState",
  "type": "read",
  "response": {
    "kind": "InverterDeviceState",
    "timestampUtc": "2025-12-18T06:51:27.209Z",
    "version": 1,
    "DeviceId": "EC100",
    "runState": "normal",
    "output": {
      "totalActivePowerW": 55,
      "totalApparentPowerVA": 55,
      "frequencyHz": 50,
      "phaseA": {
        "voltageV": 220,
        "currentA": 0,
        "activePowerW": 6
      },
      "phaseB": {
        "voltageV": 220,
        "currentA": 0,
        "activePowerW": 7
      },
      "phaseC": {
        "voltageV": 219.9,
        "currentA": 0.1,
        "activePowerW": 42
      }
    },
    "gridSide": {
      "totalActivePowerW": 0,
      "phaseAPowerW": 0,
      "phaseBPowerW": 0,
      "phaseCPowerW": 0
    },
    "temperatures": {
      "dcTransformerTempC": 25,
      "heatsinkTempC": 137.5
    },
    "acInputStatus": {
      "genGridSignalRaw": 2,
      "gridSignalPresent": false,
      "generatorSignalPresent": true
    }
  }
}
```

### Data Flow
```
ThingsBoard Device
       │
       ▼
getInverterDeviceState (RPC)
       │
       ▼
InverterDeviceStateDto (Backend DTO)
       │
       ▼
GetDeviceDetailsQueryHandler.AddInverterRealtimeDetails()
       │
       ▼
DeviceDetailItemDto[] with "Phases" array
       │
       ▼
GET /api/DeviceComponents/Details/{componentId}
       │
       ▼
Frontend InverterDetailScreen
```

### Response Structure
```typescript
interface DeviceDetailsResponseDto {
  details: DeviceDetailItemDto[];
}

interface DeviceDetailItemDto {
  key: string;
  value: any;
  unit: string | null;
  labelKey: string;
}

// Phases array structure
interface PhaseData {
  "Phase Id": number;  // 1, 2, or 3
  Voltage: number;     // in Volts
  Current: number;     // in Amps
  Power: number;       // in Watts
}
```

---

## Testing Checklist

- [ ] Three-phase inverter shows all 3 phases in Output section
- [ ] Three-phase inverter shows Phase C in Grid section
- [ ] Single-phase inverter shows only Phase 1
- [ ] Output Frequency displays correctly
- [ ] Demo device simulation shows 3 phases
- [ ] Real device with three-phase data displays correctly
- [ ] Translations display correctly for all phase labels

---

## Files Changed

### Backend
- `src/Application/DeviceComponents/Queries/GetDeviceDetails/GetDeviceDetailsQueryHandler.cs`

### Frontend
- `src/app/screens/deviceDetail/InverterDetailScreen.tsx`

---

## Contact

For questions about this update, contact the backend team.
