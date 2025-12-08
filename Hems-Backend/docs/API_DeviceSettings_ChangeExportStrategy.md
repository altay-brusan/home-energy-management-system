# Device Settings - Change Export Strategy API

## Overview
The Change Export Strategy API allows you to remotely change the operating mode of an inverter device. This endpoint sends an RPC command to the device through ThingsBoard to modify the inverter's export strategy in real-time.

The export strategy determines how the inverter manages energy flow between solar panels, batteries, grid, and load consumption.

## Endpoint

### Change Export Strategy
Changes the export strategy of an inverter device.

```
POST /api/DeviceSettings/ChangeExportStrategy
```

**Authentication**: Required (Bearer token)

**Request Body**: `ChangeExportStrategyCommand`

**Response**: `ExportStrategyStateDto` with device confirmation

---

## Export Strategies

The API supports three export strategys for inverter operation:

### 1. SellFirst (Value: 0)
**Description**: Prioritizes selling excess energy to the grid.
- Solar energy first powers the load
- Excess energy charges the battery
- Once battery is full, remaining energy is exported to the grid
- **Use Case**: Maximize revenue from feed-in tariffs

### 2. ZeroExportToLoad (Value: 1)
**Description**: Zero export mode focused on load consumption.
- Solar energy prioritizes local consumption
- Excess energy charges the battery
- Minimizes or eliminates grid export
- **Use Case**: Self-consumption optimization, areas with no feed-in tariff

### 3. ZeroExportToCt (Value: 2)
**Description**: Zero export mode with current transformer (CT) monitoring.
- Uses CT sensor for precise grid monitoring
- Actively prevents any export to grid
- Dynamically adjusts inverter output based on load
- **Use Case**: Locations where grid export is prohibited or penalized

---

## Request Structure

### ChangeExportStrategyCommand
```json
{
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "exportStrategy": 0
}
```

**Fields**:
- `deviceId` (required, GUID): The ID of the device whose strategy should be changed
- `exportStrategy` (required, integer): The export strategy value (0, 1, or 2)
  - `0` = SellFirst
  - `1` = ZeroExportToLoad
  - `2` = ZeroExportToCt

---

## Response Structure

### ExportStrategyStateDto
```json
{
  "kind": "ExportStrategyState",
  "timestampUtc": "2025-12-08T07:10:00.000Z",
  "version": 1,
  "gatewayId": "EC100-GW-001",
  "success": true,
  "message": "Export strategy updated",
  "exportStrategy": "selling_first",
  "exportStrategyCode": 0
}
```

**Fields**:
- `kind` (string): Response type identifier ("ExportStrategyState")
- `timestampUtc` (string): UTC timestamp of the response from device
- `version` (integer): Protocol version
- `gatewayId` (string): Gateway/device identifier
- `success` (boolean): Whether the strategy change succeeded
- `message` (string): Human-readable status message from device
- `exportStrategy` (string): String representation of the strategy set on device
- `exportStrategyCode` (integer): Numeric code of the strategy (0, 1, or 2)

### Success Response
**HTTP Status**: `200 OK`

```json
{
  "kind": "ExportStrategyState",
  "timestampUtc": "2025-12-08T07:10:00.000Z",
  "version": 1,
  "gatewayId": "EC100-GW-001",
  "success": true,
  "message": "Export strategy updated",
  "exportStrategy": "selling_first",
  "exportStrategyCode": 0
}
```

### Device Failure Response
**HTTP Status**: `400 Bad Request`

When device responds with failure:
```json
{
  "kind": "ExportStrategyState",
  "timestampUtc": "2025-12-08T07:10:00.000Z",
  "version": 1,
  "gatewayId": "EC100-GW-001",
  "success": false,
  "message": "Invalid mode or device error",
  "exportStrategy": null,
  "exportStrategyCode": null
}
```

### Communication Failure Response
**HTTP Status**: `400 Bad Request`

When device doesn't respond (offline, timeout):
```json
{
  "success": false,
  "message": "Failed to communicate with device. Device may be offline or timed out."
}
```

---

## Example Requests

### Change to SellFirst Mode
```http
POST /api/DeviceSettings/ChangeExportStrategy HTTP/1.1
Host: your-backend-url.com
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "exportStrategy": 0
}
```

### Change to ZeroExportToLoad Mode
```http
POST /api/DeviceSettings/ChangeExportStrategy HTTP/1.1
Host: your-backend-url.com
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "exportStrategy": 1
}
```

### Change to ZeroExportToCt Mode
```http
POST /api/DeviceSettings/ChangeExportStrategy HTTP/1.1
Host: your-backend-url.com
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "exportStrategy": 2
}
```

---

## Example Responses

### Successful Mode Change (SellFirst)
**HTTP 200 OK**
```json
{
  "kind": "ExportStrategyState",
  "timestampUtc": "2025-12-08T08:15:30.123Z",
  "version": 1,
  "gatewayId": "EC100-GW-001",
  "success": true,
  "message": "Export strategy updated",
  "exportStrategy": "selling_first",
  "exportStrategyCode": 0
}
```

### Successful Mode Change (ZeroExportToLoad)
**HTTP 200 OK**
```json
{
  "kind": "ExportStrategyState",
  "timestampUtc": "2025-12-08T08:16:45.456Z",
  "version": 1,
  "gatewayId": "EC100-GW-001",
  "success": true,
  "message": "Export strategy updated",
  "exportStrategy": "zero_export_to_load",
  "exportStrategyCode": 1
}
```

### Device Rejected Mode Change
**HTTP 400 Bad Request**
```json
{
  "kind": "ExportStrategyState",
  "timestampUtc": "2025-12-08T08:17:12.789Z",
  "version": 1,
  "gatewayId": "EC100-GW-001",
  "success": false,
  "message": "Cannot change strategy while battery is charging",
  "exportStrategy": null,
  "exportStrategyCode": null
}
```

### Device Offline/Timeout
**HTTP 400 Bad Request**
```json
{
  "success": false,
  "message": "Failed to communicate with device. Device may be offline or timed out."
}
```

---

## Validation Rules

The API validates the following:

1. **DeviceId**: Must not be empty (must be a valid GUID)
2. **ExportStrategy**: Must be a valid enum value (0, 1, or 2)

### Validation Error Response
**HTTP Status**: `400 Bad Request`

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ExportStrategy": [
      "ExportStrategy must be a valid ExportStrategy value (SellFirst, ZeroExportToLoad, or ZeroExportToCt)."
    ]
  }
}
```

---

## Error Handling

### Possible HTTP Status Codes

| Status | Meaning | Action |
|--------|---------|--------|
| 200 OK | Strategy changed successfully | Display success message to user |
| 400 Bad Request | Invalid request or RPC failed | Show error message, allow retry |
| 401 Unauthorized | Invalid/expired token | Redirect to login |
| 404 Not Found | Device not found in database | Show "Device not found" error |
| 500 Server Error | Backend/ThingsBoard error | Show error, retry after delay |

---

## Frontend Implementation Guide

### 1. TypeScript Interfaces

```typescript
enum ExportStrategy {
  SellFirst = 0,
  ZeroExportToLoad = 1,
  ZeroExportToCt = 2
}

interface ChangeExportStrategyCommand {
  deviceId: string;
  exportStrategy: ExportStrategy;
}

interface ExportStrategyStateDto {
  kind?: string;
  timestampUtc?: string;
  version?: number;
  gatewayId?: string;
  success: boolean;
  message?: string;
  exportStrategy?: string; // "selling_first" | "zero_export_to_load" | "zero_export_to_ct"
  exportStrategyCode?: number; // 0 | 1 | 2
}
```

### 2. API Call Function

```typescript
async function changeInverterMode(
  deviceId: string,
  mode: ExportStrategy
): Promise<ExportStrategyStateDto> {
  const command: ChangeExportStrategyCommand = {
    deviceId,
    exportStrategy: mode
  };

  const response = await fetch('/api/DeviceSettings/ChangeExportStrategy', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${yourJwtToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(command)
  });

  const result = await response.json();

  if (!response.ok) {
    // Device responded with failure or communication error
    throw new Error(result.message || 'Failed to change strategy');
  }

  return result;
}
```

### 3. React Hook Example

```typescript
import { useState } from 'react';

function useChangeExportStrategy() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  async function changeMode(deviceId: string, mode: ExportStrategy) {
    setLoading(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await changeInverterMode(deviceId, mode);

      if (result.success) {
        setSuccess(result.message || 'Strategy changed successfully');
        return result;
      } else {
        setError(result.message || 'Device rejected strategy change');
        return null;
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to change strategy');
      return null;
    } finally {
      setLoading(false);
    }
  }

  return { changeMode, loading, error, success };
}
```

### 4. UI Component Example

```typescript
import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';

interface ModeSelectorProps {
  deviceId: string;
  currentMode?: ExportStrategy;
}

function ModeSelector({ deviceId, currentMode }: ModeSelectorProps) {
  const { t } = useTranslation();
  const { changeMode, loading, error, success } = useChangeExportStrategy();
  const [selectedMode, setSelectedMode] = useState<ExportStrategy>(
    currentMode ?? ExportStrategy.SellFirst
  );

  const handleModeChange = async () => {
    const result = await changeMode(deviceId, selectedMode);
    if (result) {
      // Optionally refresh device data or update UI
      console.log('Strategy changed successfully');
    }
  };

  return (
    <div className="mode-selector">
      <h3>{t('settings.inverterMode')}</h3>

      {/* Mode Selection */}
      <div className="mode-options">
        <label>
          <input
            type="radio"
            value={ExportStrategy.SellFirst}
            checked={selectedMode === ExportStrategy.SellFirst}
            onChange={(e) => setSelectedMode(Number(e.target.value))}
            disabled={loading}
          />
          <div>
            <strong>{t('mode.sellFirst')}</strong>
            <p>{t('mode.sellFirstDescription')}</p>
          </div>
        </label>

        <label>
          <input
            type="radio"
            value={ExportStrategy.ZeroExportToLoad}
            checked={selectedMode === ExportStrategy.ZeroExportToLoad}
            onChange={(e) => setSelectedMode(Number(e.target.value))}
            disabled={loading}
          />
          <div>
            <strong>{t('mode.zeroExportToLoad')}</strong>
            <p>{t('mode.zeroExportToLoadDescription')}</p>
          </div>
        </label>

        <label>
          <input
            type="radio"
            value={ExportStrategy.ZeroExportToCt}
            checked={selectedMode === ExportStrategy.ZeroExportToCt}
            onChange={(e) => setSelectedMode(Number(e.target.value))}
            disabled={loading}
          />
          <div>
            <strong>{t('mode.zeroExportToCt')}</strong>
            <p>{t('mode.zeroExportToCtDescription')}</p>
          </div>
        </label>
      </div>

      {/* Apply Button */}
      <button
        onClick={handleModeChange}
        disabled={loading || selectedMode === currentMode}
        className="btn-primary"
      >
        {loading ? t('common.applying') : t('common.apply')}
      </button>

      {/* Success/Error Messages */}
      {success && <div className="alert alert-success">{success}</div>}
      {error && <div className="alert alert-error">{error}</div>}
    </div>
  );
}

export default ModeSelector;
```

### 5. Localization File (i18n)

```json
{
  "settings": {
    "inverterMode": "Inverter Export Strategy"
  },
  "mode": {
    "sellFirst": "Sell First",
    "sellFirstDescription": "Prioritize selling excess energy to the grid. Best for maximizing revenue from feed-in tariffs.",
    "zeroExportToLoad": "Zero Export to Load",
    "zeroExportToLoadDescription": "Minimize grid export and maximize self-consumption. Ideal for areas without feed-in tariffs.",
    "zeroExportToCt": "Zero Export with CT",
    "zeroExportToCtDescription": "Actively prevent grid export using CT sensor monitoring. Required where export is prohibited.",
    "changeSuccess": "Working strategy changed successfully",
    "changeFailed": "Failed to change export strategy. Please try again."
  },
  "common": {
    "apply": "Apply Changes",
    "applying": "Applying..."
  }
}
```

### 6. Error Handler with Retry

```typescript
async function changeModeWithRetry(
  deviceId: string,
  mode: ExportStrategy,
  retries = 2
): Promise<boolean> {
  for (let i = 0; i <= retries; i++) {
    try {
      const result = await changeInverterMode(deviceId, mode);
      console.log('Strategy changed:', result.message);
      return true;
    } catch (error) {
      console.error(`Attempt ${i + 1} failed:`, error);

      if (i === retries) {
        // Final attempt failed
        showNotification('error', 'Failed to change strategy after multiple attempts');
        return false;
      }

      // Wait before retry (exponential backoff)
      await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, i)));
    }
  }
  return false;
}
```

---

## How It Works (Backend Flow)

1. **Frontend** sends POST request with `deviceId` and `exportStrategy`
2. **Backend** validates the request using FluentValidation
3. **Backend** looks up the device in the database by `deviceId`
4. **Backend** retrieves the ThingsBoard device ID
5. **Backend** maps the enum to device export strategy code:
   - `SellFirst` (0) → `exportStrategyCode: 0` (device: "selling_first")
   - `ZeroExportToLoad` (1) → `exportStrategyCode: 1` (device: "zero_export_to_load")
   - `ZeroExportToCt` (2) → `exportStrategyCode: 2` (device: "zero_export_to_ct")
6. **Backend** calls `SendOneWayRpcAsync()` to ThingsBoard with:
   - Method: `"setExportStrategy"`
   - Parameters: `{ "exportStrategyCode": 0 }` (or 1, 2)
7. **ThingsBoard** forwards the RPC command to the physical device
8. **Device** receives the command and updates register 244 with the export strategy
9. **Backend** returns success/failure response to frontend

---

## Device-Level RPC Specification

The backend translates the API request into a device-level RPC command. Here's the complete device specification:

### Device RPC Details
- **RPC Name**: `setExportStrategy`
- **Kind**: `ExportStrategyState`
- **Type**: `write`
- **Paired With**: `getExportStrategy` (read operation)
- **Register**: 244 on the inverter
- **Description**: Sets the export/grid strategy mode on inverter register 244

### Device Request Format
The backend sends:
```json
{
  "exportStrategyCode": 0
}
```

Alternative format (also supported by device):
```json
{
  "exportStrategy": "selling_first"
}
```

### Valid Device Values
| Backend Enum | Enum Value | Device Code | Device String |
|-------------|------------|-------------|---------------|
| `SellFirst` | 0 | 0 | `"selling_first"` |
| `ZeroExportToLoad` | 1 | 1 | `"zero_export_to_load"` |
| `ZeroExportToCt` | 2 | 2 | `"zero_export_to_ct"` |

### Device Response (Two-Way RPC)
If using two-way RPC, the device would respond with:
```json
{
  "kind": "ExportStrategyState",
  "timestampUtc": "2025-12-08T07:10:00.000Z",
  "version": 1,
  "gatewayId": "EC100-GW-001",
  "success": true,
  "message": "Export strategy updated",
  "exportStrategy": "selling_first",
  "exportStrategyCode": 0
}
```

**Note**: The backend uses two-way RPC to get confirmation from the device. The above response structure is what the device returns after successfully updating the mode.

---

## Important Notes

### RPC Command Details
- **RPC Method Name**: `setExportStrategy`
- **Device Register**: 244 (Export/Grid Strategy Mode)
- **RPC Type**: Two-way (waits for device response with 10-second timeout)
- **Parameter Name**: `exportStrategyCode`
- **Parameter Value**: Integer code (0, 1, or 2)
- **Device Mapping**:
  - `0` → `"selling_first"` on device
  - `1` → `"zero_export_to_load"` on device
  - `2` → `"zero_export_to_ct"` on device
- **Response**: `ExportStrategyStateDto` with success confirmation and device state

### Device Communication
- The command is sent to ThingsBoard, which forwards it to the device
- The device must be online to receive the command
- **Two-way RPC**: Backend waits for device response (10-second timeout)
- Device responds with confirmation including success status and updated mode
- If device is offline or doesn't respond within 10 seconds, API returns communication failure

### Recommended UI/UX
1. **Show current mode**: Display the device's current export strategy before allowing changes
2. **Confirmation dialog**: Ask user to confirm before changing mode
3. **Loading indicator**: Show spinner while the command is being sent
4. **Success feedback**: Display success message and update UI
5. **Error handling**: Show clear error messages and allow retry
6. **Disable during loading**: Disable mode selector while command is processing

### Best Practices
- **Poll for confirmation**: After changing mode, poll the device to confirm the mode actually changed (use telemetry or attributes)
- **Audit logging**: Log all strategy changes for compliance and troubleshooting
- **User permissions**: Restrict strategy changes to authorized users only
- **Rate limiting**: Prevent users from rapidly changing modes (frontend + backend)

---

## Testing

### Manual Testing with cURL

```bash
# Change to SellFirst mode
curl -X POST "https://your-backend-url.com/api/DeviceSettings/ChangeExportStrategy" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "exportStrategy": 0
  }'

# Change to ZeroExportToLoad mode
curl -X POST "https://your-backend-url.com/api/DeviceSettings/ChangeExportStrategy" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "exportStrategy": 1
  }'
```

### Testing Scenarios

1. ✅ **Valid strategy change**: Test with valid deviceId and mode (0, 1, 2)
2. ✅ **Invalid mode**: Test with invalid mode value (e.g., 99)
3. ✅ **Empty deviceId**: Test with empty or invalid GUID
4. ✅ **Non-existent device**: Test with valid GUID that doesn't exist
5. ✅ **Offline device**: Test when device is offline (should still return success)
6. ✅ **Unauthorized**: Test without Bearer token (should return 401)

---

## Summary

1. ✅ Use `POST /api/DeviceSettings/ChangeExportStrategy` to change inverter export strategy
2. ✅ Three modes available: SellFirst (0), ZeroExportToLoad (1), ZeroExportToCt (2)
3. ✅ Command is sent via one-way RPC to ThingsBoard
4. ✅ No device response confirmation (fire-and-forget)
5. ✅ Validate user permissions before allowing strategy changes
6. ✅ Provide clear UI feedback during and after strategy change
7. ✅ Implement retry logic for failed commands
8. ✅ Consider polling device status after strategy change to confirm

---

## Related APIs

- **Device Management**: See device CRUD operations documentation
- **Device Components**: See [API_DeviceComponentDetails.md](./API_DeviceComponentDetails.md)
- **Device Summary**: See [API_DeviceComponentSummary.md](./API_DeviceComponentSummary.md)
