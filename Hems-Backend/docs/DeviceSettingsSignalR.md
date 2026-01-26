# Device Settings Update - SignalR Integration

This document describes how to integrate with the real-time device parameter update system using SignalR.

## Overview

The API uses a **synchronous REST + SignalR progress** pattern:
1. HTTP POST waits for all parameters to complete (synchronous)
2. SignalR sends real-time progress updates during processing
3. HTTP response contains the final result

**Benefits:**
- Simple frontend implementation (single HTTP call)
- Real-time progress feedback via SignalR
- Final result guaranteed in HTTP response (no polling needed)

---

## 1. API Endpoint

### Update Device Parameters

```
POST /api/DeviceSettings/device/{deviceId}/parameters
```

**Request Body:**
```json
{
  "parameters": [
    { "parameterId": "guid-1", "value": "100" },
    { "parameterId": "guid-2", "value": "50" },
    { "parameterId": "guid-3", "value": "ON" }
  ]
}
```

**Response (waits for completion):**
```json
{
  "success": true,
  "totalParameters": 3,
  "successCount": 3,
  "failedCount": 0,
  "failedParameters": [],
  "duration": "00:00:05.234"
}
```

**Partial Failure Response:**
```json
{
  "success": false,
  "totalParameters": 3,
  "successCount": 2,
  "failedCount": 1,
  "failedParameters": [
    {
      "parameterId": "guid-3",
      "parameterName": "SolarSell",
      "errorMessage": "Verification failed: expected 'true', got 'false'"
    }
  ],
  "duration": "00:00:08.456"
}
```

**Device Busy Response:**
```json
{
  "success": false,
  "errorCode": "DEVICE_BUSY",
  "message": "Another update operation is in progress for this device."
}
```

---

## 2. SignalR Hub Connection

**Hub URL:** `/hubs/device-settings`

**Authentication:** JWT Bearer token via query string

### Connection Example (TypeScript)

```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://your-api-url/hubs/device-settings', {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

---

## 3. Subscribe to Device Updates

Subscribe to receive updates for a specific device:

```typescript
// Subscribe to device updates
await connection.invoke('SubscribeToDevice', deviceId);

// Unsubscribe when done
await connection.invoke('UnsubscribeFromDevice', deviceId);
```

---

## 4. SignalR Events

### 4.1 OperationStarted

Fired when the batch processing begins.

```typescript
connection.on('OperationStarted', (message) => {
  console.log('Update started:', message);
  // Show progress modal
});
```

**Message Structure:**
```typescript
interface OperationStartedEvent {
  operationId: string;      // Correlation ID
  deviceId: string;         // GUID
  totalParameters: number;
  startedAt: string;        // ISO date
}
```

### 4.2 ParameterProgress

Fired for each parameter as it's processed (success or failure).

```typescript
connection.on('ParameterProgress', (message) => {
  const progress = (message.currentIndex / message.totalCount) * 100;
  console.log(`Progress: ${progress}% - ${message.parameterName}: ${message.status}`);
});
```

**Message Structure:**
```typescript
interface ParameterProgressEvent {
  operationId: string;
  deviceId: string;
  parameterId: string;       // GUID
  parameterName: string;
  status: 'Success' | 'Failed' | 'Skipped';
  errorMessage?: string;     // Only if status is 'Failed'
  currentIndex: number;      // 1-based
  totalCount: number;
  successCount: number;      // Running total
  failedCount: number;       // Running total
}
```

### 4.3 OperationCompleted

Fired when all parameters have been processed.

```typescript
connection.on('OperationCompleted', (message) => {
  console.log(`Completed: ${message.successCount} succeeded, ${message.failedCount} failed`);
  // Hide progress modal, show summary
});
```

**Message Structure:**
```typescript
interface OperationCompletedEvent {
  operationId: string;
  deviceId: string;
  success: boolean;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  failedParameters: Array<{
    parameterId: string;
    parameterName: string;
    errorMessage: string;
  }>;
  duration: string;          // TimeSpan format: "00:00:05.123"
  completedAt: string;       // ISO date
}
```

---

## 5. Complete Integration Example

```typescript
import * as signalR from '@microsoft/signalr';

class DeviceSettingsService {
  private connection: signalR.HubConnection;
  private currentDeviceId: string | null = null;

  constructor(private apiUrl: string, private getToken: () => string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/hubs/device-settings`, {
        accessTokenFactory: () => this.getToken()
      })
      .withAutomaticReconnect()
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    this.connection.on('OperationStarted', (msg) => {
      this.onOperationStarted?.(msg);
    });

    this.connection.on('ParameterProgress', (msg) => {
      this.onParameterProgress?.(msg);
    });

    this.connection.on('OperationCompleted', (msg) => {
      this.onOperationCompleted?.(msg);
    });
  }

  // Callbacks - set these from your component
  onOperationStarted?: (msg: OperationStartedEvent) => void;
  onParameterProgress?: (msg: ParameterProgressEvent) => void;
  onOperationCompleted?: (msg: OperationCompletedEvent) => void;

  async connect() {
    await this.connection.start();
  }

  async disconnect() {
    await this.connection.stop();
  }

  async subscribeToDevice(deviceId: string) {
    this.currentDeviceId = deviceId;
    await this.connection.invoke('SubscribeToDevice', deviceId);
  }

  async unsubscribeFromDevice(deviceId: string) {
    await this.connection.invoke('UnsubscribeFromDevice', deviceId);
    this.currentDeviceId = null;
  }

  async updateParameters(
    deviceId: string,
    parameters: Array<{ parameterId: string; value: string }>
  ): Promise<UpdateResult> {

    // Ensure SignalR is connected before calling API
    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      await this.connect();
    }

    // Subscribe to device if not already
    if (this.currentDeviceId !== deviceId) {
      await this.subscribeToDevice(deviceId);
    }

    // Call API - this waits for completion
    const response = await fetch(`${this.apiUrl}/api/DeviceSettings/device/${deviceId}/parameters`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getToken()}`
      },
      body: JSON.stringify({ parameters })
    });

    return await response.json();
  }
}

// Usage in a React component:
const service = new DeviceSettingsService('https://api.example.com', () => authToken);

service.onOperationStarted = (msg) => {
  setShowProgressModal(true);
  setProgress({ current: 0, total: msg.totalParameters });
};

service.onParameterProgress = (msg) => {
  setProgress({ current: msg.currentIndex, total: msg.totalCount });
  if (msg.status === 'Success') {
    addLog(`✓ ${msg.parameterName}`);
  } else {
    addLog(`✗ ${msg.parameterName}: ${msg.errorMessage}`);
  }
};

service.onOperationCompleted = (msg) => {
  setShowProgressModal(false);
  if (msg.success) {
    showSuccess(`${msg.successCount} parameters updated successfully`);
  } else {
    showWarning(`${msg.successCount} succeeded, ${msg.failedCount} failed`);
  }
};

await service.connect();
await service.subscribeToDevice(deviceId);

// This call waits for completion and returns final result
const result = await service.updateParameters(deviceId, parameterList);
```

---

## 6. Backend Processing Details

### Write-Verify-Retry Pattern

For each parameter:
1. **Write**: Send RPC to device with new value
2. **Wait**: 150ms for device to process
3. **Verify**: Read back value from device
4. **Compare**: Check if written value matches read value
5. **Retry**: If mismatch, retry once (max 2 attempts total)

### Time Slot End Parameters

Time slot END parameters (`TimeSlot1End`, `TimeSlot2End`, etc.) are:
- **Skipped for RPC** (device handles internally based on start times)
- **Saved to database** (for UI display)
- **Marked as Success** in progress events

### Update History Tracking

Every parameter update attempt is recorded in `DeviceSettingUpdateHistory`:
- Success/Failure status
- Old and new values
- Verified value (read back from device)
- Error message if failed
- Retry attempt count

---

## 7. Error Handling

### Connection Errors
```typescript
connection.onclose((error) => {
  console.error('SignalR connection closed:', error);
  // Progress will still be shown via HTTP response
});

connection.onreconnecting((error) => {
  console.warn('SignalR reconnecting:', error);
});

connection.onreconnected((connectionId) => {
  console.log('SignalR reconnected:', connectionId);
  // Re-subscribe to device
  if (currentDeviceId) {
    connection.invoke('SubscribeToDevice', currentDeviceId);
  }
});
```

### HTTP Timeout

The API is configured with a 5-minute timeout for long parameter updates. For very large batches, consider:
- Breaking into smaller batches
- Showing intermediate progress via SignalR

---

## 8. Notes

- SignalR is optional - HTTP response always contains the final result
- SignalR provides real-time progress during the (potentially long) operation
- Each parameter is processed sequentially (one at a time)
- The `currentIndex` in progress events is 1-based
- Device lock prevents concurrent updates to the same device
- If SignalR disconnects during operation, HTTP will still return the final result
