# PowerFlow Streaming - Usage Guide

## Overview

The PowerFlow streaming system allows real-time power flow data updates from devices using SignalR streaming.

**Files:**
- `src/services/powerFlowService.ts` - SignalR streaming service
- `src/hooks/usePowerFlowStream.ts` - React hook for components
- `src/api/types/signalR.types.ts` - TypeScript types

## Architecture

### Service Layer (`powerFlowService.ts`)

Singleton service that manages SignalR connection to `powerFlowHub`.

**Features:**
- ✅ Lazy connection (connects only when first stream starts)
- ✅ Auto-disconnect when no active streams
- ✅ Multiple concurrent streams support (different devices)
- ✅ Automatic reconnection with exponential backoff
- ✅ Connection state notifications

**Key Methods:**
```typescript
// Start streaming for a device
await powerFlowService.startStream(
  deviceId,
  intervalSeconds,
  onData,
  onError,
  onComplete
);

// Stop specific stream
powerFlowService.stopStream(deviceId);

// Stop all streams
powerFlowService.stopAllStreams();
```

### Hook Layer (`usePowerFlowStream`)

React hook for easy integration in components.

**Features:**
- ✅ Automatic cleanup on unmount
- ✅ Pauses when app goes to background
- ✅ Resumes when app returns to foreground
- ✅ Handles device ID changes automatically
- ✅ Error handling
- ✅ Manual enable/disable control

## Usage Examples

### Example 1: Basic Usage (Inverter Detail Screen)

```typescript
import { usePowerFlowStream } from '@/hooks/usePowerFlowStream';

export const InverterDetailScreen = ({ route }) => {
  const { deviceId } = route.params;

  const { powerFlow, isStreaming, error } = usePowerFlowStream({
    deviceId,
    intervalSeconds: 5, // Update every 5 seconds
  });

  if (error) {
    return <ErrorView message={error.message} />;
  }

  if (!powerFlow) {
    return <LoadingSpinner />;
  }

  return (
    <View>
      <Text>Grid Power: {powerFlow.gridPower} W</Text>
      <Text>Solar Power: {powerFlow.solarPower} W</Text>
      <Text>Battery Power: {powerFlow.batteryPower} W</Text>
      <Text>Load Power: {powerFlow.loadPower} W</Text>
      <Text>Battery SOC: {powerFlow.batterySOC}%</Text>

      {isStreaming && <Text>🔴 Live</Text>}
    </View>
  );
};
```

### Example 2: With Manual Control

```typescript
import { usePowerFlowStream } from '@/hooks/usePowerFlowStream';

export const SmartMeterDetailScreen = ({ route }) => {
  const { deviceId } = route.params;
  const [liveMode, setLiveMode] = useState(false);

  const { powerFlow, isStreaming, startStream, stopStream } = usePowerFlowStream({
    deviceId,
    intervalSeconds: 3,
    enabled: liveMode, // Only stream when enabled
  });

  return (
    <View>
      <Switch
        value={liveMode}
        onValueChange={setLiveMode}
        label="Live Updates"
      />

      {powerFlow && (
        <PowerFlowVisualization data={powerFlow} />
      )}

      {isStreaming && <LiveIndicator />}
    </View>
  );
};
```

### Example 3: Dashboard with Multiple Devices

```typescript
import { usePowerFlowStream } from '@/hooks/usePowerFlowStream';

export const DashboardScreen = () => {
  const { selectedDeviceId } = useSelectedSiteDevice();

  // Only stream for selected device
  const { powerFlow, isStreaming } = usePowerFlowStream({
    deviceId: selectedDeviceId,
    intervalSeconds: 5,
  });

  return (
    <ScrollView>
      <EnergyFlowRadial powerFlow={powerFlow} />
      <PowerMetricsChart data={powerFlow} />
      {isStreaming && <LiveBadge />}
    </ScrollView>
  );
};
```

### Example 4: With Error Handling

```typescript
import { usePowerFlowStream } from '@/hooks/usePowerFlowStream';

export const BatteryDetailScreen = ({ route }) => {
  const { deviceId } = route.params;

  const {
    powerFlow,
    isStreaming,
    streamState,
    error,
    startStream
  } = usePowerFlowStream({
    deviceId,
    intervalSeconds: 5,
  });

  if (streamState === 'error') {
    return (
      <ErrorView
        message={error?.message || 'Stream failed'}
        onRetry={startStream}
      />
    );
  }

  if (streamState === 'completed') {
    return <Text>Stream completed</Text>;
  }

  return (
    <BatteryVisualization
      data={powerFlow}
      isLive={isStreaming}
    />
  );
};
```

### Example 5: Connection Status Monitor (Debug)

```typescript
import { usePowerFlowConnection } from '@/hooks/usePowerFlowStream';

export const DebugPanel = () => {
  const { connectionState, isConnected, activeStreamCount } = usePowerFlowConnection();

  return (
    <View>
      <Text>Connection: {connectionState}</Text>
      <Text>Active Streams: {activeStreamCount}</Text>
      <Text>Status: {isConnected ? '✅ Connected' : '❌ Disconnected'}</Text>
    </View>
  );
};
```

## Best Practices

### ✅ DO:

1. **Use in Device Detail Screens**
   ```typescript
   // Good: Stream only for the device being viewed
   const { powerFlow } = usePowerFlowStream({ deviceId });
   ```

2. **Enable Only When Needed**
   ```typescript
   // Good: Only stream when tab is focused
   const { powerFlow } = usePowerFlowStream({
     deviceId,
     enabled: isFocused
   });
   ```

3. **Handle Errors Gracefully**
   ```typescript
   if (error) return <ErrorView onRetry={startStream} />;
   ```

4. **Show Live Indicator**
   ```typescript
   {isStreaming && <LiveBadge />}
   ```

### ❌ DON'T:

1. **Don't Stream Multiple Devices Simultaneously (unless necessary)**
   ```typescript
   // Bad: Creates multiple concurrent streams
   devices.map(d => usePowerFlowStream({ deviceId: d.id }))
   ```

2. **Don't Keep Streams Open When Not Visible**
   ```typescript
   // Bad: Always streaming even when screen is not visible
   usePowerFlowStream({ deviceId, enabled: true })

   // Good: Pause when not focused
   usePowerFlowStream({ deviceId, enabled: isFocused })
   ```

3. **Don't Ignore Errors**
   ```typescript
   // Bad: No error handling
   const { powerFlow } = usePowerFlowStream({ deviceId });

   // Good: Handle errors
   const { powerFlow, error } = usePowerFlowStream({ deviceId });
   if (error) { /* handle */ }
   ```

## Performance Optimization

### Interval Selection

```typescript
// High frequency (battery monitoring)
intervalSeconds: 2  // Backend minimum

// Normal frequency (general monitoring)
intervalSeconds: 5  // Default, recommended

// Low frequency (less critical data)
intervalSeconds: 10

// Very low frequency
intervalSeconds: 30 // Backend maximum: 60
```

### Conditional Streaming

```typescript
// Only stream when screen is focused
import { useIsFocused } from '@react-navigation/native';

const isFocused = useIsFocused();

const { powerFlow } = usePowerFlowStream({
  deviceId,
  enabled: isFocused, // Auto-pause when screen is not visible
});
```

## Troubleshooting

### Stream Not Starting

1. Check device ID is valid
2. Check backend hub URL: `powerFlowService.ts:46`
3. Verify authentication token
4. Check network connectivity

### Stream Stops Unexpectedly

- App went to background (expected behavior)
- Network disconnection
- Backend error (check error state)
- Device ID changed

### Multiple Streams Conflict

```typescript
// Each component creates its own stream
// Make sure only ONE component streams per device

// Solution: Lift state up or use global store
```

## Backend Requirements

The backend must implement:

```csharp
public class PowerFlowHub : Hub
{
    public async IAsyncEnumerable<PowerFlowDto> StreamPowerFlow(
        Guid deviceId,
        int intervalSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## Testing

```typescript
// Mock for unit tests
jest.mock('@/services/powerFlowService', () => ({
  powerFlowService: {
    startStream: jest.fn(),
    stopStream: jest.fn(),
    addConnectionStateListener: jest.fn(() => () => {}),
  }
}));
```

## Migration from Polling

If you're currently using polling (setInterval):

```typescript
// Before: Polling
useEffect(() => {
  const interval = setInterval(async () => {
    const data = await fetchPowerFlow(deviceId);
    setPowerFlow(data);
  }, 5000);
  return () => clearInterval(interval);
}, [deviceId]);

// After: Streaming
const { powerFlow } = usePowerFlowStream({
  deviceId,
  intervalSeconds: 5
});
```

**Benefits:**
- ✅ Automatic cleanup
- ✅ Background pause/resume
- ✅ Better error handling
- ✅ Less battery drain (single WebSocket vs multiple HTTP requests)
- ✅ Server controls the loop (cancellation token)
