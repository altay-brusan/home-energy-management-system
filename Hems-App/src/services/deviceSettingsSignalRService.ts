/**
 * Device Settings SignalR Service
 * Dedicated SignalR connection for device parameter update progress
 * Handles real-time progress updates and reconnection recovery
 */

import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { AppState, AppStateStatus } from 'react-native';
import { apiClient } from '../api/client/apiClient';
import { SignalRConnectionState } from '../api/types/signalR.types';
import {
  OperationStartedEvent,
  ParameterProgressEvent,
  OperationCompletedEvent,
  OperationState,
  OperationStartedCallback,
  ParameterProgressCallback,
  OperationCompletedCallback,
} from '../api/types/deviceSettingsSignalR.types';
import 'react-native-url-polyfill/auto';

class DeviceSettingsSignalRService {
  private connection: HubConnection | null = null;
  private isManualDisconnect = false;
  private currentDeviceId: string | null = null;
  private connectionListeners: Set<(state: SignalRConnectionState) => void> = new Set();
  private connectingPromise: Promise<void> | null = null;
  private connectionAborted = false;

  // Reference counting for connection management
  private subscriberCount = 0;
  private disconnectTimeout: ReturnType<typeof setTimeout> | null = null;
  private readonly DISCONNECT_DELAY_MS = 2000; // Delay disconnect to handle screen transitions

  // Event callbacks
  private operationStartedCallbacks: Set<OperationStartedCallback> = new Set();
  private parameterProgressCallbacks: Set<ParameterProgressCallback> = new Set();
  private operationCompletedCallbacks: Set<OperationCompletedCallback> = new Set();

  // App state tracking for reconnection
  private appStateSubscription: { remove: () => void } | null = null;

  // LOCAL TESTING - change back to production URL
  // Android Emulator: use 10.0.2.2 to reach host machine's localhost
  private readonly HUB_URL = 'http://10.0.2.2:7163/deviceSettingsHub';

  /**
   * Subscribe to device settings updates (reference counted)
   * Use this instead of connect() from hooks to properly manage connection lifecycle
   */
  async subscribe(deviceId: string): Promise<void> {
    // Cancel any pending disconnect
    if (this.disconnectTimeout) {
      clearTimeout(this.disconnectTimeout);
      this.disconnectTimeout = null;
      console.log('[DeviceSettingsSignalR] Cancelled pending disconnect');
    }

    this.subscriberCount++;
    console.log('[DeviceSettingsSignalR] Subscriber added, count:', this.subscriberCount);

    // Connect if not already connected
    await this.connect(deviceId);
  }

  /**
   * Unsubscribe from device settings updates (reference counted)
   * Connection will be disconnected after a delay if no subscribers remain
   */
  unsubscribe(): void {
    this.subscriberCount = Math.max(0, this.subscriberCount - 1);
    console.log('[DeviceSettingsSignalR] Subscriber removed, count:', this.subscriberCount);

    if (this.subscriberCount === 0 && !this.disconnectTimeout) {
      // Schedule disconnect after delay to handle screen transitions
      console.log('[DeviceSettingsSignalR] Scheduling disconnect in', this.DISCONNECT_DELAY_MS, 'ms');
      this.disconnectTimeout = setTimeout(() => {
        this.disconnectTimeout = null;
        if (this.subscriberCount === 0) {
          console.log('[DeviceSettingsSignalR] No subscribers, disconnecting...');
          this.disconnect().catch(() => {
            // Silently ignore disconnect errors
          });
        }
      }, this.DISCONNECT_DELAY_MS);
    }
  }

  /**
   * Connect to the DeviceSettingsHub and subscribe to a device
   */
  async connect(deviceId: string): Promise<void> {
    // If already connecting, wait for that to complete
    if (this.connectingPromise) {
      await this.connectingPromise;
      // After waiting, check if we need to subscribe to a different device
      if (this.connection?.state === HubConnectionState.Connected && this.currentDeviceId !== deviceId) {
        await this.subscribeToDevice(deviceId);
      }
      return;
    }

    if (this.connection?.state === HubConnectionState.Connected) {
      // Already connected, just subscribe to the device if different
      if (this.currentDeviceId !== deviceId) {
        await this.subscribeToDevice(deviceId);
      }
      return;
    }

    this.connectingPromise = this.doConnect(deviceId);
    try {
      await this.connectingPromise;
    } finally {
      this.connectingPromise = null;
    }
  }

  private async doConnect(deviceId: string): Promise<void> {
    try {
      this.isManualDisconnect = false;
      this.connectionAborted = false;

      // Clean up any existing stale connection
      if (this.connection) {
        try {
          await this.connection.stop();
        } catch (e) {
          // Ignore cleanup errors
        }
        this.connection = null;
      }

      this.connection = new HubConnectionBuilder()
        .withUrl(this.HUB_URL, {
          accessTokenFactory: async () => {
            const token = await this.getAccessToken();
            return token || '';
          },
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        })
        .withServerTimeout(120000)
        .withKeepAliveInterval(15000)
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            const delay = Math.min(2000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            return delay;
          },
        })
        .build();

      this.setupConnectionEvents();
      this.registerServerCallbacks();

      await this.connection.start();

      // Check if disconnect was called during connection
      if (this.connectionAborted) {
        console.log('[DeviceSettingsSignalR] Connection aborted during startup');
        await this.connection.stop();
        return;
      }

      console.log('[DeviceSettingsSignalR] Connected to:', this.HUB_URL);
      console.log('[DeviceSettingsSignalR] Connection ID:', this.connection.connectionId);

      // Subscribe to device
      await this.subscribeToDevice(deviceId);

      this.notifyConnectionState('Connected');
      this.setupAppStateListener();
    } catch (error: any) {
      // Don't log abort errors as failures
      if (this.connectionAborted) {
        console.log('[DeviceSettingsSignalR] Connection aborted');
        return;
      }
      console.log('[DeviceSettingsSignalR] Connection failed:', error?.message || error);
      this.notifyConnectionState('Failed');
      throw error;
    }
  }

  /**
   * Disconnect from the hub
   */
  async disconnect(): Promise<void> {
    this.isManualDisconnect = true;
    this.connectionAborted = true;

    // Clear any pending disconnect timeout
    if (this.disconnectTimeout) {
      clearTimeout(this.disconnectTimeout);
      this.disconnectTimeout = null;
    }

    // Reset subscriber count on explicit disconnect
    this.subscriberCount = 0;

    if (this.appStateSubscription) {
      this.appStateSubscription.remove();
      this.appStateSubscription = null;
    }

    // If we're in the middle of connecting, just set the abort flag and return
    // The connect method will handle cleanup when it sees the flag
    if (this.connectingPromise) {
      console.log('[DeviceSettingsSignalR] Disconnect called during connection, aborting...');
      // Wait for connection attempt to complete/abort
      try {
        await this.connectingPromise;
      } catch {
        // Ignore errors from aborted connection
      }
    }

    if (this.connection) {
      try {
        // Only try to unsubscribe/stop if actually connected
        if (this.connection.state === HubConnectionState.Connected) {
          if (this.currentDeviceId) {
            await this.unsubscribeFromDevice(this.currentDeviceId);
          }
          await this.connection.stop();
        } else if (this.connection.state !== HubConnectionState.Disconnected) {
          // If connecting/reconnecting, just stop without unsubscribe
          await this.connection.stop();
        }
      } catch (error) {
        // Silent error handling
      }
      this.connection = null;
    }

    this.currentDeviceId = null;
    this.notifyConnectionState('Disconnected');
  }

  /**
   * Subscribe to device settings updates
   */
  private async subscribeToDevice(deviceId: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    try {
      // Unsubscribe from previous device if different
      if (this.currentDeviceId && this.currentDeviceId !== deviceId) {
        await this.unsubscribeFromDevice(this.currentDeviceId);
      }

      await this.connection.invoke('SubscribeToDevice', deviceId);
      this.currentDeviceId = deviceId;
      console.log('[DeviceSettingsSignalR] Subscribed to device:', deviceId);
    } catch (error) {
      console.error('[DeviceSettingsSignalR] Subscribe failed:', error);
      throw error;
    }
  }

  /**
   * Unsubscribe from device settings updates
   */
  private async unsubscribeFromDevice(deviceId: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('UnsubscribeFromDevice', deviceId);
      console.log('[DeviceSettingsSignalR] Unsubscribed from device:', deviceId);
    } catch (error) {
      // Silently ignore unsubscribe errors during manual disconnect
      // This is expected when the connection is closed before unsubscribe completes
      if (!this.isManualDisconnect) {
        console.error('[DeviceSettingsSignalR] Unsubscribe failed:', error);
      }
    }
  }

  /**
   * Get operation status (for reconnection recovery)
   */
  async getOperationStatus(operationId: string): Promise<OperationState | null> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return null;
    }

    try {
      return await this.connection.invoke<OperationState | null>('GetOperationStatus', operationId);
    } catch (error) {
      console.error('[DeviceSettingsSignalR] GetOperationStatus failed:', error);
      return null;
    }
  }

  /**
   * Get active operation for device (for reconnection recovery)
   */
  async getActiveOperation(deviceId: string): Promise<OperationState | null> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return null;
    }

    try {
      return await this.connection.invoke<OperationState | null>('GetActiveOperation', deviceId);
    } catch (error) {
      console.error('[DeviceSettingsSignalR] GetActiveOperation failed:', error);
      return null;
    }
  }

  /**
   * Register event callbacks
   */
  onOperationStarted(callback: OperationStartedCallback): () => void {
    this.operationStartedCallbacks.add(callback);
    return () => this.operationStartedCallbacks.delete(callback);
  }

  onParameterProgress(callback: ParameterProgressCallback): () => void {
    this.parameterProgressCallbacks.add(callback);
    return () => this.parameterProgressCallbacks.delete(callback);
  }

  onOperationCompleted(callback: OperationCompletedCallback): () => void {
    this.operationCompletedCallbacks.add(callback);
    return () => this.operationCompletedCallbacks.delete(callback);
  }

  /**
   * Connection state listener
   */
  addConnectionStateListener(listener: (state: SignalRConnectionState) => void): () => void {
    this.connectionListeners.add(listener);
    return () => this.connectionListeners.delete(listener);
  }

  private setupConnectionEvents(): void {
    if (!this.connection) return;

    this.connection.onclose(async error => {
      console.log('[DeviceSettingsSignalR] Connection closed:', error?.message || 'No error');
      this.notifyConnectionState('Disconnected');
    });

    this.connection.onreconnecting(error => {
      console.log('[DeviceSettingsSignalR] Reconnecting:', error?.message || 'No error');
      this.notifyConnectionState('Reconnecting');
    });

    this.connection.onreconnected(async connectionId => {
      console.log('[DeviceSettingsSignalR] Reconnected:', connectionId);
      this.notifyConnectionState('Connected');

      // Re-subscribe to device
      if (this.currentDeviceId) {
        try {
          await this.subscribeToDevice(this.currentDeviceId);

          // Check for active operation
          const activeOp = await this.getActiveOperation(this.currentDeviceId);
          if (activeOp && !activeOp.isCompleted) {
            console.log('[DeviceSettingsSignalR] Found active operation:', activeOp.operationId);
            // Notify callbacks about the recovered operation
            this.operationStartedCallbacks.forEach(cb =>
              cb({
                operationId: activeOp.operationId,
                deviceId: activeOp.deviceId,
                totalParameters: activeOp.totalParameters,
                startedAt: activeOp.startedAt,
              })
            );
          }
        } catch (error) {
          console.error('[DeviceSettingsSignalR] Re-subscribe failed:', error);
        }
      }
    });
  }

  private registerServerCallbacks(): void {
    if (!this.connection) return;

    this.connection.on('OperationStarted', (event: OperationStartedEvent) => {
      console.log('[DeviceSettingsSignalR] OperationStarted:', event.operationId);
      this.operationStartedCallbacks.forEach(cb => cb(event));
    });

    this.connection.on('ParameterProgress', (event: ParameterProgressEvent) => {
      console.log(
        '[DeviceSettingsSignalR] ParameterProgress:',
        event.parameterName,
        event.currentIndex,
        '/',
        event.totalParameters
      );
      this.parameterProgressCallbacks.forEach(cb => cb(event));
    });

    this.connection.on('OperationCompleted', (event: OperationCompletedEvent) => {
      console.log('[DeviceSettingsSignalR] OperationCompleted:', event.operationId, 'Success:', event.success);
      this.operationCompletedCallbacks.forEach(cb => cb(event));
    });
  }

  private setupAppStateListener(): void {
    if (this.appStateSubscription) return;

    this.appStateSubscription = AppState.addEventListener('change', async (nextAppState: AppStateStatus) => {
      if (nextAppState === 'active' && this.currentDeviceId) {
        // App returned to foreground - check if we need to reconnect
        if (this.connection?.state !== HubConnectionState.Connected) {
          console.log('[DeviceSettingsSignalR] App active, reconnecting...');
          try {
            await this.connect(this.currentDeviceId);
          } catch (error) {
            console.error('[DeviceSettingsSignalR] Reconnect failed:', error);
          }
        }
      }
    });
  }

  private notifyConnectionState(state: SignalRConnectionState): void {
    this.connectionListeners.forEach(listener => {
      try {
        listener(state);
      } catch (error) {
        // Silent error handling
      }
    });
  }

  private async getAccessToken(): Promise<string> {
    const token = await apiClient.getAccessToken();
    if (!token) {
      throw new Error('No access token found');
    }
    return token;
  }

  /**
   * Wait for the connection to be ready (if connecting in progress)
   * Returns true if connected, false if timeout or no connection
   */
  async waitForConnection(timeoutMs: number = 5000): Promise<boolean> {
    // If already connected, return immediately
    if (this.connection?.state === HubConnectionState.Connected) {
      return true;
    }

    // If connecting, wait for it to complete
    if (this.connectingPromise) {
      try {
        await Promise.race([
          this.connectingPromise,
          new Promise((_, reject) =>
            setTimeout(() => reject(new Error('Connection timeout')), timeoutMs)
          ),
        ]);
        return this.connection?.state === HubConnectionState.Connected;
      } catch {
        return false;
      }
    }

    // Not connected and not connecting
    return false;
  }

  /**
   * Ensure connected to the specified device
   * If not connected, will attempt to connect
   * Returns true if connected, false otherwise
   */
  async ensureConnected(deviceId: string, timeoutMs: number = 5000): Promise<boolean> {
    // Check if already connected to this device
    if (this.connection?.state === HubConnectionState.Connected && this.currentDeviceId === deviceId) {
      return true;
    }

    // Try to connect
    try {
      await Promise.race([
        this.connect(deviceId),
        new Promise((_, reject) =>
          setTimeout(() => reject(new Error('Connection timeout')), timeoutMs)
        ),
      ]);
      return this.connection?.state === HubConnectionState.Connected;
    } catch {
      return false;
    }
  }

  get isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  get connectionState(): SignalRConnectionState {
    if (!this.connection) return 'Disconnected';

    switch (this.connection.state) {
      case HubConnectionState.Connected:
        return 'Connected';
      case HubConnectionState.Connecting:
        return 'Connecting';
      case HubConnectionState.Reconnecting:
        return 'Reconnecting';
      case HubConnectionState.Disconnected:
        return 'Disconnected';
      default:
        return 'Failed';
    }
  }

  get currentDevice(): string | null {
    return this.currentDeviceId;
  }
}

export const deviceSettingsSignalRService = new DeviceSettingsSignalRService();
