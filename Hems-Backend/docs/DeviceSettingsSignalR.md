# Device Settings Update - SignalR Integration

This document describes how to integrate with the real-time device parameter update system using SignalR.

## Overview

When updating device parameters, the API returns immediately with a `correlationId`. The actual updates happen in the background, and progress is sent via SignalR in real-time.

**Flow:**
1. Call the API to update parameters
2. Receive `correlationId` in response
3. Connect to SignalR hub and subscribe with the `correlationId`
4. Receive real-time updates as each parameter is processed
5. Receive completion notification when all parameters are done

---

## 1. API Endpoint

### Update Device Parameters

```
PUT /api/device-settings/{deviceId}/parameters
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

**Response (immediate):**
```json
{
  "success": true,
  "correlationId": "a1b2c3d4e5f6...",
  "totalParameters": 3,
  "message": "Parameter update queued. Subscribe to SignalR hub 'deviceSettingsHub' with correlation ID 'a1b2c3d4e5f6...' to receive progress updates."
}
```

---

## 2. SignalR Hub Connection

**Hub URL:** `/deviceSettingsHub`

**Authentication:** JWT Bearer token required (same as API)

### Connection Example (TypeScript)

```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://your-api-url/deviceSettingsHub', {
    accessTokenFactory: () => getAccessToken() // Your JWT token
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

---

## 3. Subscribe to Updates

After calling the API and receiving the `correlationId`, subscribe to receive updates:

```typescript
// Subscribe to a specific batch operation
await connection.invoke('SubscribeToBatchOperation', correlationId);

// OR subscribe to all updates for a device
await connection.invoke('SubscribeToDeviceSettings', deviceId);
```

**Unsubscribe when done:**
```typescript
await connection.invoke('UnsubscribeFromBatchOperation', correlationId);
await connection.invoke('UnsubscribeFromDeviceSettings', deviceId);
```

---

## 4. SignalR Events

### 4.1 ParameterUpdateStarted

Fired when the batch processing begins.

```typescript
connection.on('ParameterUpdateStarted', (message) => {
  console.log('Update started:', message);
  // Show progress indicator
});
```

**Message Structure:**
```typescript
interface ParameterUpdateStartedMessage {
  correlationId: string;
  deviceId: string;        // GUID
  totalParameters: number;
  startedAt: string;       // ISO date
}
```

### 4.2 ParameterUpdated

Fired for each successfully updated parameter.

```typescript
connection.on('ParameterUpdated', (message) => {
  console.log(`Updated ${message.currentIndex}/${message.totalParameters}: ${message.parameterName} = ${message.newValue}`);
  // Update progress bar
  const progress = (message.currentIndex / message.totalParameters) * 100;
});
```

**Message Structure:**
```typescript
interface ParameterUpdatedMessage {
  correlationId: string;
  deviceId: string;
  parameterId: string;     // GUID
  parameterName: string;
  newValue: string;
  rpcMethod?: string;
  currentIndex: number;    // 1-based
  totalParameters: number;
  updatedAt: string;       // ISO date
}
```

### 4.3 ParameterUpdateFailed

Fired when a parameter update fails.

```typescript
connection.on('ParameterUpdateFailed', (message) => {
  console.error(`Failed ${message.currentIndex}/${message.totalParameters}: ${message.parameterName} - ${message.errorMessage}`);
  // Show error for this parameter
});
```

**Message Structure:**
```typescript
interface ParameterUpdateFailedMessage {
  correlationId: string;
  deviceId: string;
  parameterId: string;
  parameterName: string;
  errorMessage: string;
  rpcMethod?: string;
  currentIndex: number;
  totalParameters: number;
  failedAt: string;        // ISO date
}
```

### 4.4 ParameterUpdateCompleted

Fired when all parameters have been processed.

```typescript
connection.on('ParameterUpdateCompleted', (message) => {
  console.log(`Completed: ${message.successCount} succeeded, ${message.failedCount} failed`);
  // Hide progress, show summary
});
```

**Message Structure:**
```typescript
interface ParameterUpdateCompletedMessage {
  correlationId: string;
  deviceId: string;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  completedAt: string;     // ISO date
  duration: string;        // TimeSpan format: "00:00:05.123"
}
```

---

## 5. Complete Example

```typescript
import * as signalR from '@microsoft/signalr';

class DeviceSettingsService {
  private connection: signalR.HubConnection;

  constructor(private apiUrl: string, private getToken: () => string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}/deviceSettingsHub`, {
        accessTokenFactory: () => this.getToken()
      })
      .withAutomaticReconnect()
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers() {
    this.connection.on('ParameterUpdateStarted', (msg) => {
      this.onUpdateStarted?.(msg);
    });

    this.connection.on('ParameterUpdated', (msg) => {
      this.onParameterUpdated?.(msg);
    });

    this.connection.on('ParameterUpdateFailed', (msg) => {
      this.onParameterFailed?.(msg);
    });

    this.connection.on('ParameterUpdateCompleted', (msg) => {
      this.onUpdateCompleted?.(msg);
    });
  }

  // Callbacks - set these from your component
  onUpdateStarted?: (msg: ParameterUpdateStartedMessage) => void;
  onParameterUpdated?: (msg: ParameterUpdatedMessage) => void;
  onParameterFailed?: (msg: ParameterUpdateFailedMessage) => void;
  onUpdateCompleted?: (msg: ParameterUpdateCompletedMessage) => void;

  async connect() {
    await this.connection.start();
  }

  async disconnect() {
    await this.connection.stop();
  }

  async updateParameters(
    deviceId: string,
    parameters: Array<{ parameterId: string; value: string }>
  ): Promise<{ correlationId: string; totalParameters: number }> {

    // 1. Call API
    const response = await fetch(`${this.apiUrl}/api/device-settings/${deviceId}/parameters`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getToken()}`
      },
      body: JSON.stringify({ parameters })
    });

    const result = await response.json();

    if (!result.success) {
      throw new Error(result.errorMessage);
    }

    // 2. Subscribe to updates
    await this.connection.invoke('SubscribeToBatchOperation', result.correlationId);

    return {
      correlationId: result.correlationId,
      totalParameters: result.totalParameters
    };
  }

  async unsubscribe(correlationId: string) {
    await this.connection.invoke('UnsubscribeFromBatchOperation', correlationId);
  }
}

// Usage in a React component:
const service = new DeviceSettingsService('https://api.example.com', () => authToken);

service.onUpdateStarted = (msg) => {
  setProgress({ current: 0, total: msg.totalParameters, status: 'started' });
};

service.onParameterUpdated = (msg) => {
  setProgress(prev => ({ ...prev, current: msg.currentIndex }));
  addSuccessLog(`${msg.parameterName} = ${msg.newValue}`);
};

service.onParameterFailed = (msg) => {
  setProgress(prev => ({ ...prev, current: msg.currentIndex }));
  addErrorLog(`${msg.parameterName}: ${msg.errorMessage}`);
};

service.onUpdateCompleted = (msg) => {
  setProgress(prev => ({ ...prev, status: 'completed' }));
  showSummary(`${msg.successCount} succeeded, ${msg.failedCount} failed`);
  service.unsubscribe(msg.correlationId);
};

await service.connect();
const { correlationId } = await service.updateParameters(deviceId, parameterList);
```

---

## 6. Error Handling

### Connection Errors
```typescript
connection.onclose((error) => {
  console.error('SignalR connection closed:', error);
  // Attempt reconnect or notify user
});

connection.onreconnecting((error) => {
  console.warn('SignalR reconnecting:', error);
});

connection.onreconnected((connectionId) => {
  console.log('SignalR reconnected:', connectionId);
  // Re-subscribe to active operations
});
```

### API Errors

The API may return errors before queueing:

```json
{
  "success": false,
  "errorMessage": "Access denied for 2 admin-only parameter(s): MaxPower, SafetyLimit"
}
```

---

## 7. Notes

- Each parameter is updated sequentially (one at a time) to avoid overwhelming the device
- The `currentIndex` in events is 1-based (starts at 1, not 0)
- Duration in `ParameterUpdateCompleted` is in .NET TimeSpan format
- If you subscribe by device ID, you'll receive updates for ALL parameter operations on that device
- If you subscribe by correlation ID, you'll only receive updates for that specific batch
