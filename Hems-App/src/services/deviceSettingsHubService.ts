/**
 * Device Settings Hub Service
 * SignalR service for real-time device parameter update tracking
 * Follows the pattern of deviceStatusService.ts
 */

import {
  HttpTransportType,
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { apiClient } from '../api/client/apiClient';
import { SignalRConnectionState } from '../api/types/signalR.types';
import {
  BatchOperationState,
  BatchOperationListener,
  CompletionListener,
  ParameterUpdateStartedDto,
  ParameterUpdatedDto,
  ParameterUpdateFailedDto,
  ParameterUpdateCompletedDto,
  DEVICE_SETTINGS_HUB_EVENTS,
  DEVICE_SETTINGS_HUB_METHODS,
} from '../api/types/deviceSettingsHub.types';
import 'react-native-url-polyfill/auto';

class DeviceSettingsHubService {
  private connection: HubConnection | null = null;
  private isManualDisconnect = false;
  private currentCorrelationId: string | null = null;

  // Listener sets
  private connectionListeners: Set<(state: SignalRConnectionState) => void> = new Set();
  private batchOperationListeners: Set<BatchOperationListener> = new Set();
  private completionListeners: Set<CompletionListener> = new Set();

  // Current batch operation state
  private batchState: BatchOperationState | null = null;

  private readonly HUB_URL = 'https://hems-api-dev.oklidis.com.tr/deviceSettingsHub';

  // ============================================================================
  // Connection Management
  // ============================================================================

  async connect(): Promise<void> {
    // Already connected, skip
    if (this.connection?.state === HubConnectionState.Connected) {
      console.log(
        '[DeviceSettingsHubService] Already connected, skipping. Connection ID:',
        this.connection.connectionId
      );
      return;
    }

    console.log('[DeviceSettingsHubService] Current connection state:', this.connection?.state ?? 'null');

    try {
      this.isManualDisconnect = false;

      console.log('[DeviceSettingsHubService] Connecting to:', this.HUB_URL);

      this.connection = new HubConnectionBuilder()
        .withUrl(this.HUB_URL, {
          accessTokenFactory: async () => {
            const freshToken = await this.getAccessToken();
            return freshToken || '';
          },
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        })
        .withServerTimeout(120000)
        .withKeepAliveInterval(15000)
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            const delay = Math.min(2000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            return delay;
          },
        })
        .build();

      this.setupConnectionEvents();
      // NOTE: setupMessageHandlers() is called AFTER start() - handlers must be registered
      // on a started connection in React Native for reliable event delivery

      await this.connection.start();
      console.log('[DeviceSettingsHubService] Connected successfully to:', this.HUB_URL);
      console.log('[DeviceSettingsHubService] Connection ID:', this.connection.connectionId);

      // CRITICAL: Set up message handlers AFTER connection is started
      // In React Native, handlers registered before start() may not work reliably
      this.setupMessageHandlers();

      this.notifyConnectionState('Connected');
    } catch (error) {
      console.log('[DeviceSettingsHubService] Connection failed:', error);
      this.notifyConnectionState('Failed');
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    this.isManualDisconnect = true;

    // Unsubscribe from batch operation first
    await this.unsubscribeFromBatchOperation();

    if (this.connection) {
      try {
        await this.connection.stop();
      } catch (error) {
        // Silent error handling
      }
      this.connection = null;
    }

    this.currentCorrelationId = null;
    this.batchState = null;
    this.notifyConnectionState('Disconnected');
  }

  // ============================================================================
  // Subscription Management
  // ============================================================================

  async subscribeToBatchOperation(correlationId: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      console.log('[DeviceSettingsHubService] Cannot subscribe - not connected');
      throw new Error('DeviceSettingsHub connection is not established');
    }

    // Unsubscribe from previous if exists and different
    if (this.currentCorrelationId && this.currentCorrelationId !== correlationId) {
      await this.unsubscribeFromBatchOperation();
    }

    // Already subscribed to this correlation
    if (this.currentCorrelationId === correlationId) {
      console.log('[DeviceSettingsHubService] Already subscribed to correlationId:', correlationId);
      return;
    }

    try {
      console.log('[DeviceSettingsHubService] Subscribing to batch operation:', correlationId);
      try {
        await this.connection.invoke(DEVICE_SETTINGS_HUB_METHODS.SUBSCRIBE_TO_BATCH, correlationId);
        console.log('[DeviceSettingsHubService] Subscribe invoke completed');
      } catch (invokeError) {
        console.error('[DeviceSettingsHubService] Subscribe invoke FAILED:', invokeError);
        throw invokeError;
      }
      this.currentCorrelationId = correlationId;

      // Initialize batch state
      this.batchState = {
        correlationId,
        deviceId: '',
        status: 'pending',
        totalParameters: 0,
        currentIndex: 0,
        successCount: 0,
        failedCount: 0,
        skippedCount: 0,
        wasCancelled: false,
        failedParameters: [],
        failedParametersInfo: [],
        startedAt: null,
        completedAt: null,
        duration: null,
      };

      console.log('[DeviceSettingsHubService] Subscribed to batch operation:', correlationId);
    } catch (error) {
      console.log('[DeviceSettingsHubService] Subscribe failed:', error);
      throw error;
    }
  }

  async unsubscribeFromBatchOperation(): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    if (!this.currentCorrelationId) {
      return;
    }

    try {
      console.log(
        '[DeviceSettingsHubService] Unsubscribing from batch operation:',
        this.currentCorrelationId
      );
      await this.connection.invoke(
        DEVICE_SETTINGS_HUB_METHODS.UNSUBSCRIBE_FROM_BATCH,
        this.currentCorrelationId
      );
      console.log('[DeviceSettingsHubService] Unsubscribed successfully');
    } catch (error) {
      console.log('[DeviceSettingsHubService] Unsubscribe failed:', error);
    }

    this.currentCorrelationId = null;
    this.batchState = null;
  }

  // ============================================================================
  // Event Setup
  // ============================================================================

  private setupConnectionEvents(): void {
    if (!this.connection) return;

    this.connection.onclose(async (_error) => {
      console.log('[DeviceSettingsHubService] Connection closed');
      this.notifyConnectionState('Disconnected');
    });

    this.connection.onreconnecting((_error) => {
      console.log('[DeviceSettingsHubService] Reconnecting...');
      this.notifyConnectionState('Reconnecting');
    });

    this.connection.onreconnected(async (_connectionId) => {
      console.log('[DeviceSettingsHubService] Reconnected');
      this.notifyConnectionState('Connected');

      // Re-subscribe to batch operation if was active
      if (this.currentCorrelationId) {
        const correlationId = this.currentCorrelationId;
        this.currentCorrelationId = null; // Clear for re-subscribe
        try {
          await this.subscribeToBatchOperation(correlationId);
        } catch (error) {
          console.log('[DeviceSettingsHubService] Re-subscribe failed after reconnect:', error);
        }
      }
    });
  }

  private setupMessageHandlers(): void {
    if (!this.connection) return;

    console.log('[DeviceSettingsHubService] Setting up message handlers');

    // Debug: Log ALL incoming messages
    const originalOn = this.connection.on.bind(this.connection);
    this.connection.on = (methodName: string, callback: (...args: any[]) => void) => {
      return originalOn(methodName, (...args: any[]) => {
        console.log(`[DeviceSettingsHubService] RAW MESSAGE RECEIVED: ${methodName}`, args);
        callback(...args);
      });
    };

    // ParameterUpdateStarted
    this.connection.on(
      DEVICE_SETTINGS_HUB_EVENTS.PARAMETER_UPDATE_STARTED,
      (data: ParameterUpdateStartedDto) => {
        console.log('[DeviceSettingsHubService] ParameterUpdateStarted:', data);

        if (this.batchState && this.batchState.correlationId === data.correlationId) {
          this.batchState = {
            ...this.batchState,
            deviceId: data.deviceId,
            status: 'in_progress',
            totalParameters: data.totalParameters,
            startedAt: data.startedAt,
          };
          this.notifyBatchOperationUpdate(this.batchState);
        }
      }
    );

    // ParameterUpdated
    this.connection.on(
      DEVICE_SETTINGS_HUB_EVENTS.PARAMETER_UPDATED,
      (data: ParameterUpdatedDto) => {
        console.log('[DeviceSettingsHubService] ParameterUpdated:', data.parameterName, data.currentIndex);

        if (this.batchState && this.batchState.correlationId === data.correlationId) {
          // Check if this is a progress sync message from late subscription
          // Progress sync messages have parameterName = "[Progress Sync]" and are used to
          // update the UI with current progress when a client subscribes after operation started
          const isProgressSync = data.parameterName === '[Progress Sync]';

          this.batchState = {
            ...this.batchState,
            deviceId: data.deviceId || this.batchState.deviceId,
            status: 'in_progress',
            currentIndex: data.currentIndex,
            totalParameters: data.totalParameters || this.batchState.totalParameters,
            // Only increment successCount for real parameter updates, not progress syncs
            successCount: isProgressSync ? data.currentIndex : this.batchState.successCount + 1,
          };
          this.notifyBatchOperationUpdate(this.batchState);
        }
      }
    );

    // ParameterUpdateFailed
    this.connection.on(
      DEVICE_SETTINGS_HUB_EVENTS.PARAMETER_UPDATE_FAILED,
      (data: ParameterUpdateFailedDto) => {
        console.log('[DeviceSettingsHubService] ParameterUpdateFailed:', data.parameterName, data.errorMessage);

        if (this.batchState && this.batchState.correlationId === data.correlationId) {
          this.batchState = {
            ...this.batchState,
            currentIndex: data.currentIndex,
            failedCount: this.batchState.failedCount + 1,
            failedParameters: [...this.batchState.failedParameters, data],
          };
          this.notifyBatchOperationUpdate(this.batchState);
        }
      }
    );

    // ParameterUpdateCompleted
    this.connection.on(
      DEVICE_SETTINGS_HUB_EVENTS.PARAMETER_UPDATE_COMPLETED,
      (data: ParameterUpdateCompletedDto) => {
        console.log('[DeviceSettingsHubService] ParameterUpdateCompleted:', data);

        if (this.batchState && this.batchState.correlationId === data.correlationId) {
          // Determine status based on results
          let status: 'completed' | 'failed' = 'completed';
          if (data.wasCancelled || data.failedCount > 0) {
            status = 'failed';
          }

          this.batchState = {
            ...this.batchState,
            status,
            successCount: data.successCount,
            failedCount: data.failedCount,
            skippedCount: data.skippedCount,
            wasCancelled: data.wasCancelled,
            failedParametersInfo: data.failedParameters || [],
            completedAt: data.completedAt,
            duration: data.duration,
          };
          this.notifyBatchOperationUpdate(this.batchState);
          this.notifyCompletion(data);
        }
      }
    );

    console.log('[DeviceSettingsHubService] Message handlers registered');
  }

  // ============================================================================
  // Listener Management
  // ============================================================================

  addBatchOperationListener(listener: BatchOperationListener): () => void {
    this.batchOperationListeners.add(listener);
    return () => {
      this.batchOperationListeners.delete(listener);
    };
  }

  addCompletionListener(listener: CompletionListener): () => void {
    this.completionListeners.add(listener);
    return () => {
      this.completionListeners.delete(listener);
    };
  }

  addConnectionStateListener(listener: (state: SignalRConnectionState) => void): () => void {
    this.connectionListeners.add(listener);
    return () => {
      this.connectionListeners.delete(listener);
    };
  }

  private notifyBatchOperationUpdate(state: BatchOperationState): void {
    this.batchOperationListeners.forEach((listener) => {
      try {
        listener(state);
      } catch (error) {
        // Silent error handling
      }
    });
  }

  private notifyCompletion(result: ParameterUpdateCompletedDto): void {
    this.completionListeners.forEach((listener) => {
      try {
        listener(result);
      } catch (error) {
        // Silent error handling
      }
    });
  }

  private notifyConnectionState(state: SignalRConnectionState): void {
    this.connectionListeners.forEach((listener) => {
      try {
        listener(state);
      } catch (error) {
        // Silent error handling
      }
    });
  }

  // ============================================================================
  // Utilities
  // ============================================================================

  private async getAccessToken(): Promise<string> {
    const token = await apiClient.getAccessToken();
    if (!token) {
      throw new Error('No access token found');
    }
    return token;
  }

  // ============================================================================
  // Getters
  // ============================================================================

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

  get currentBatchState(): BatchOperationState | null {
    return this.batchState;
  }

  get currentCorrelation(): string | null {
    return this.currentCorrelationId;
  }
}

// Export singleton instance
export const deviceSettingsHubService = new DeviceSettingsHubService();
