# ALM (AI Learning Module) API Documentation

## Overview

The ALM module provides AI-powered predictions for energy consumption patterns. It integrates with an external AI container service to generate forecasts based on historical device power data.

### Key Features
- Power consumption prediction (24-hour forecast)
- Daily caching to avoid redundant AI calls
- Real-time status updates via SignalR
- Support for multiple model types (extensible)

### Architecture Flow

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│   Frontend  │      │  HEMS API   │      │ AI Container│
└──────┬──────┘      └──────┬──────┘      └──────┬──────┘
       │                    │                    │
       │ 1. POST /predict   │                    │
       │───────────────────>│                    │
       │                    │ 2. Submit job      │
       │                    │───────────────────>│
       │                    │                    │
       │ 3. SignalR:        │                    │
       │    PredictionStarted                    │
       │<───────────────────│                    │
       │                    │                    │
       │                    │    (AI processes)  │
       │                    │                    │
       │                    │ 4. Callback        │
       │                    │<───────────────────│
       │                    │                    │
       │ 5. SignalR:        │                    │
       │    PredictionCompleted                  │
       │<───────────────────│                    │
       │                    │                    │
       │ 6. GET /job/{id}   │                    │
       │───────────────────>│ 7. Fetch results   │
       │                    │───────────────────>│
       │                    │<───────────────────│
       │ 8. Forecast data   │                    │
       │<───────────────────│                    │
       │                    │                    │
```

---

## Authentication

All endpoints except `/api/Alm/callback` require JWT Bearer authentication.

```
Authorization: Bearer <token>
```

---

## API Endpoints

### 1. Request Prediction

Submits a prediction job for a device. Returns cached result if a completed prediction exists for today.

**Endpoint:** `POST /api/Alm/predict/{deviceId}`

**Parameters:**

| Name | Location | Type | Required | Description |
|------|----------|------|----------|-------------|
| `deviceId` | path | GUID | Yes | The device ID to predict for |
| `modelType` | query | string | No | Model type (default: `power-consumption`) |

**Available Model Types:**
- `power-consumption` - Predicts LoadTotalPowerW (default)

**Request Example:**
```http
POST /api/Alm/predict/3fa85f64-5717-4562-b3fc-2c963f66afa6?modelType=power-consumption
Authorization: Bearer <token>
```

**Response (Success - New Job):**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "modelType": "power-consumption",
  "fromCache": false,
  "message": "Prediction job submitted successfully"
}
```

**Response (Success - Cached):**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "modelType": "power-consumption",
  "fromCache": true,
  "message": "Returning cached prediction from today"
}
```

**Response (Error):**
```json
{
  "error": "No power data available for prediction"
}
```

**Status Codes:**
- `200 OK` - Job submitted or cached result returned
- `400 Bad Request` - Validation error or device not found
- `401 Unauthorized` - Missing or invalid token

---

### 2. Get Job Result

Fetches the full result of a prediction job from the AI container. Also stores the forecast data to the database if not already stored.

**Endpoint:** `GET /api/Alm/job/{jobId}`

**Parameters:**

| Name | Location | Type | Required | Description |
|------|----------|------|----------|-------------|
| `jobId` | path | GUID | Yes | The job ID (from predict response) |
| `modelType` | query | string | No | Model type override (uses job's stored type if not provided) |

**Request Example:**
```http
GET /api/Alm/job/7c9e6679-7425-40de-944b-e07fc1f90ae7
Authorization: Bearer <token>
```

**Response (Completed):**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "completed",
  "modelType": "power-consumption",
  "result": {
    "forecast": [
      {
        "timestamp": "2026-01-29T18:00:00",
        "value": 3.3334,
        "step": 1
      },
      {
        "timestamp": "2026-01-29T19:00:00",
        "value": 3.5012,
        "step": 2
      },
      {
        "timestamp": "2026-01-29T20:00:00",
        "value": 4.1256,
        "step": 3
      }
      // ... 24 hourly predictions
    ],
    "metadata": {
      "feature": "global_active_power",
      "intervalSeconds": 3600,
      "horizon": 24,
      "unit": "W"
    }
  },
  "error": null,
  "createdAt": "2026-01-29T07:46:08.953653+00:00",
  "completedAt": "2026-01-29T07:46:16.853809+00:00"
}
```

**Response (Processing):**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "processing",
  "modelType": "power-consumption",
  "result": null,
  "error": null,
  "createdAt": "2026-01-29T07:46:08.953653+00:00",
  "completedAt": null
}
```

**Response (Failed):**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "failed",
  "modelType": "power-consumption",
  "result": null,
  "error": "Insufficient data for prediction",
  "createdAt": "2026-01-29T07:46:08.953653+00:00",
  "completedAt": "2026-01-29T07:46:10.123456+00:00"
}
```

**Status Codes:**
- `200 OK` - Job found
- `404 Not Found` - Job not found
- `401 Unauthorized` - Missing or invalid token

---

### 3. Get Prediction History

Returns stored prediction data from the database (useful for displaying historical predictions without calling AI again).

**Endpoint:** `GET /api/Alm/prediction-history/{deviceId}`

**Parameters:**

| Name | Location | Type | Required | Description |
|------|----------|------|----------|-------------|
| `deviceId` | path | GUID | Yes | The device ID |
| `jobId` | query | GUID | No | Specific job ID (if omitted, returns most recent) |

**Request Example:**
```http
GET /api/Alm/prediction-history/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <token>
```

**Response:**
```json
{
  "success": true,
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "modelType": "PowerConsumption",
  "status": "Completed",
  "predictions": [
    {
      "predictedTime": "2026-01-29T18:00:00",
      "predictedValue": 3.3334,
      "featureName": "global_active_power"
    },
    {
      "predictedTime": "2026-01-29T19:00:00",
      "predictedValue": 3.5012,
      "featureName": "global_active_power"
    }
    // ...
  ]
}
```

**Status Codes:**
- `200 OK` - Data found
- `404 Not Found` - No prediction job found for device
- `401 Unauthorized` - Missing or invalid token

---

### 4. ALM Health Check

Checks if the AI container service is healthy and reachable.

**Endpoint:** `GET /api/Alm/health`

**Request Example:**
```http
GET /api/Alm/health
Authorization: Bearer <token>
```

**Response:**
```json
{
  "healthy": true,
  "timestamp": "2026-01-29T10:30:00+00:00"
}
```

---

### 5. Callback (Internal)

**Note:** This endpoint is called by the AI container service, not by the frontend.

**Endpoint:** `POST /api/Alm/callback`

**No authentication required** (internal service communication)

---

## SignalR Hub

### Connection

**Hub URL:** `/hubs/alm-notification`

**Connection Example (JavaScript):**
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://api.example.com/hubs/alm-notification", {
    accessTokenFactory: () => getAccessToken()
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Subscribe to Device

Before receiving notifications, subscribe to a device:

```javascript
// Subscribe to device notifications
await connection.invoke("SubscribeToDevice", deviceId);

// Unsubscribe when done
await connection.invoke("UnsubscribeFromDevice", deviceId);
```

### Events

#### PredictionStarted

Fired when a prediction job is submitted or returned from cache.

```javascript
connection.on("PredictionStarted", (notification) => {
  console.log("Prediction started:", notification);
  // notification.status will be "started" or "cached"
});
```

**Payload:**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "started",
  "modelType": "power-consumption",
  "fromCache": false,
  "predictionCount": 0,
  "error": null,
  "timestamp": "2026-01-29T10:30:00+00:00"
}
```

#### PredictionCompleted

Fired when the AI container successfully completes a prediction.

```javascript
connection.on("PredictionCompleted", (notification) => {
  console.log("Prediction completed:", notification);
  // Fetch full results using GET /api/Alm/job/{jobId}
});
```

**Payload:**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "completed",
  "modelType": "power-consumption",
  "fromCache": false,
  "predictionCount": 0,
  "error": null,
  "timestamp": "2026-01-29T10:30:15+00:00"
}
```

#### PredictionFailed

Fired when a prediction job fails.

```javascript
connection.on("PredictionFailed", (notification) => {
  console.error("Prediction failed:", notification.error);
});
```

**Payload:**
```json
{
  "jobId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "deviceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "almJobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "failed",
  "modelType": "power-consumption",
  "fromCache": false,
  "predictionCount": 0,
  "error": "Insufficient data for prediction",
  "timestamp": "2026-01-29T10:30:05+00:00"
}
```

---

## Complete Frontend Integration Example

### TypeScript/React Example

```typescript
import * as signalR from "@microsoft/signalr";
import { useState, useEffect, useCallback } from "react";

interface AlmNotification {
  jobId: string;
  deviceId: string;
  almJobId: string;
  status: string;
  modelType: string;
  fromCache: boolean;
  predictionCount: number;
  error: string | null;
  timestamp: string;
}

interface ForecastPoint {
  timestamp: string;
  value: number;
  step: number;
}

interface ForecastResult {
  forecast: ForecastPoint[];
  metadata: {
    feature: string;
    intervalSeconds: number;
    horizon: number;
    unit: string;
  };
}

interface JobResult {
  jobId: string;
  deviceId: string;
  status: string;
  modelType: string;
  result: ForecastResult | null;
  error: string | null;
  createdAt: string;
  completedAt: string | null;
}

export function useAlmPrediction(deviceId: string, accessToken: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [jobId, setJobId] = useState<string | null>(null);
  const [status, setStatus] = useState<string>("idle");
  const [forecast, setForecast] = useState<ForecastPoint[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // Setup SignalR connection
  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/alm-notification", {
        accessTokenFactory: () => accessToken
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, [accessToken]);

  // Connect and subscribe to device
  useEffect(() => {
    if (!connection || !deviceId) return;

    const startConnection = async () => {
      try {
        await connection.start();
        await connection.invoke("SubscribeToDevice", deviceId);

        // Listen for events
        connection.on("PredictionStarted", (notification: AlmNotification) => {
          console.log("Prediction started:", notification);
          setJobId(notification.jobId);
          setStatus(notification.fromCache ? "cached" : "processing");

          // If cached, fetch results immediately
          if (notification.fromCache) {
            fetchJobResult(notification.jobId);
          }
        });

        connection.on("PredictionCompleted", (notification: AlmNotification) => {
          console.log("Prediction completed:", notification);
          setStatus("completed");
          fetchJobResult(notification.jobId);
        });

        connection.on("PredictionFailed", (notification: AlmNotification) => {
          console.error("Prediction failed:", notification.error);
          setStatus("failed");
          setError(notification.error);
          setIsLoading(false);
        });

      } catch (err) {
        console.error("SignalR connection error:", err);
      }
    };

    startConnection();

    return () => {
      connection.invoke("UnsubscribeFromDevice", deviceId).catch(() => {});
    };
  }, [connection, deviceId]);

  // Fetch job results
  const fetchJobResult = useCallback(async (id: string) => {
    try {
      const response = await fetch(`/api/Alm/job/${id}`, {
        headers: {
          "Authorization": `Bearer ${accessToken}`
        }
      });

      if (!response.ok) {
        throw new Error("Failed to fetch job result");
      }

      const result: JobResult = await response.json();

      if (result.result?.forecast) {
        setForecast(result.result.forecast);
      }

      setStatus(result.status);
      setError(result.error);
      setIsLoading(false);
    } catch (err) {
      console.error("Error fetching job result:", err);
      setError("Failed to fetch prediction results");
      setIsLoading(false);
    }
  }, [accessToken]);

  // Request prediction
  const requestPrediction = useCallback(async (modelType: string = "power-consumption") => {
    setIsLoading(true);
    setError(null);
    setForecast(null);
    setStatus("requesting");

    try {
      const response = await fetch(
        `/api/Alm/predict/${deviceId}?modelType=${modelType}`,
        {
          method: "POST",
          headers: {
            "Authorization": `Bearer ${accessToken}`
          }
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || "Failed to request prediction");
      }

      const data = await response.json();
      setJobId(data.jobId);

      // If from cache, SignalR will notify and we'll fetch results
      // If new job, wait for PredictionCompleted event

    } catch (err: any) {
      setError(err.message);
      setStatus("failed");
      setIsLoading(false);
    }
  }, [deviceId, accessToken]);

  return {
    requestPrediction,
    jobId,
    status,
    forecast,
    error,
    isLoading
  };
}

// Usage in a component
function PowerPredictionChart({ deviceId }: { deviceId: string }) {
  const { requestPrediction, status, forecast, error, isLoading } = useAlmPrediction(
    deviceId,
    getAccessToken()
  );

  return (
    <div>
      <button
        onClick={() => requestPrediction("power-consumption")}
        disabled={isLoading}
      >
        {isLoading ? "Loading..." : "Get Prediction"}
      </button>

      {status === "processing" && <p>AI is processing your request...</p>}

      {error && <p className="error">{error}</p>}

      {forecast && (
        <div>
          <h3>24-Hour Power Consumption Forecast</h3>
          <ul>
            {forecast.map((point) => (
              <li key={point.step}>
                {new Date(point.timestamp).toLocaleTimeString()}: {point.value.toFixed(2)} W
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
```

---

## Job Status Values

| Status | Description |
|--------|-------------|
| `queued` | Job is waiting in the AI container queue |
| `processing` | AI container is actively processing the job |
| `completed` | Job finished successfully, results available |
| `failed` | Job failed, check `error` field for details |
| `cached` | (SignalR only) Result returned from today's cache |
| `started` | (SignalR only) New job submitted to AI container |

---

## Model Types

| Display Name | Enum Value | Description |
|--------------|------------|-------------|
| `power-consumption` | `PowerConsumption` | Predicts total load power (LoadTotalPowerW) |

*Future model types will be added as needed (e.g., `pv-generation`, `grid-power`).*

---

## Caching Behavior

- Predictions are cached **per device, per model type, per day (UTC)**
- If a completed prediction exists for today, the API returns the cached result
- Cached responses have `fromCache: true`
- To force a new prediction, wait until the next UTC day

---

## Error Handling

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `Device not found` | Invalid deviceId | Verify the device ID exists |
| `Site location not configured` | Device's site missing lat/long | Configure site coordinates |
| `No power data available` | No historical data for device | Ensure device has PowerFlow data |
| `ALM service request timed out` | AI container not responding | Check AI service health |
| `Job not found` | Invalid jobId | Use jobId from predict response |

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| `200` | Success |
| `400` | Bad request (validation error) |
| `401` | Unauthorized (missing/invalid token) |
| `404` | Resource not found |
| `500` | Server error |

---

## Rate Limiting

- No explicit rate limiting on prediction endpoints
- Daily caching naturally limits AI calls to once per device per day
- Consider implementing client-side debouncing for UX

---

## Changelog

### Version 1.0.0 (2026-01-29)
- Initial release
- Power consumption prediction support
- SignalR real-time notifications
- Daily caching mechanism
- Model type extensibility
