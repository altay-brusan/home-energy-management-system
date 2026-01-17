/**
 * Device Settings Hub Hook
 * Hook for managing SignalR connection to device settings hub
 * Tracks batch parameter update progress in real-time
 */

import { useCallback, useEffect, useRef, useState } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { SignalRConnectionState } from '../api/types/signalR.types';
import {
  BatchOperationState,
  ParameterUpdateCompletedDto,
} from '../api/types/deviceSettingsHub.types';
import { deviceSettingsHubService } from '../services/deviceSettingsHubService';
import { useDeviceSettingsUpdateStore } from '../store/deviceSettingsUpdateStore';

export interface UseDeviceSettingsHubOptions {
  /** Callback when batch operation state changes */
  onBatchUpdate?: (state: BatchOperationState) => void;
  /** Callback when batch operation completes */
  onCompleted?: (result: ParameterUpdateCompletedDto) => void;
}

export interface UseDeviceSettingsHubReturn {
  /** Whether the SignalR connection is established */
  isConnected: boolean;
  /** Current connection state */
  connectionState: SignalRConnectionState;
  /** Current batch operation state */
  batchState: BatchOperationState | null;
  /** Whether an update is currently in progress */
  isUpdateInProgress: boolean;

  // Actions
  /** Start tracking a batch operation by correlationId */
  startBatchTracking: (correlationId: string) => Promise<void>;
  /** Stop tracking the current batch operation */
  stopBatchTracking: () => Promise<void>;
  /** Disconnect from the hub entirely */
  disconnect: () => Promise<void>;
}

export const useDeviceSettingsHub = (
  options: UseDeviceSettingsHubOptions = {}
): UseDeviceSettingsHubReturn => {
  const { onBatchUpdate, onCompleted } = options;

  const [isConnected, setIsConnected] = useState(false);
  const [connectionState, setConnectionState] = useState<SignalRConnectionState>('Disconnected');
  const [batchState, setBatchState] = useState<BatchOperationState | null>(null);

  const appState = useRef(AppState.currentState);
  const isAppActive = useRef(true);
  const currentCorrelationRef = useRef<string | null>(null);

  // Store actions for progress updates
  const { updateProgress, completeUpdate } = useDeviceSettingsUpdateStore();

  // Connection state listener
  useEffect(() => {
    const unsubscribe = deviceSettingsHubService.addConnectionStateListener((state) => {
      setConnectionState(state);
      setIsConnected(state === 'Connected');
    });

    return unsubscribe;
  }, []);

  // Batch operation listener
  useEffect(() => {
    console.log('[useDeviceSettingsHub] Setting up batch operation listener');
    const unsubscribe = deviceSettingsHubService.addBatchOperationListener((state) => {
      console.log('[useDeviceSettingsHub] 🔔 Batch operation update received:', state.status, state.currentIndex, '/', state.totalParameters);
      setBatchState(state);
      // Update store
      updateProgress(state);
      // Call external callback
      onBatchUpdate?.(state);
    });

    return () => {
      console.log('[useDeviceSettingsHub] Cleaning up batch operation listener');
      unsubscribe();
    };
  }, [onBatchUpdate, updateProgress]);

  // Completion listener
  useEffect(() => {
    console.log('[useDeviceSettingsHub] Setting up completion listener');
    const unsubscribe = deviceSettingsHubService.addCompletionListener((result) => {
      console.log('[useDeviceSettingsHub] 🏁 Completion received:', result.successCount, 'success,', result.failedCount, 'failed');
      // Update store
      completeUpdate(result);
      // Call external callback
      onCompleted?.(result);
    });

    return () => {
      console.log('[useDeviceSettingsHub] Cleaning up completion listener');
      unsubscribe();
    };
  }, [onCompleted, completeUpdate]);

  // App state handling - reconnect when app becomes active
  useEffect(() => {
    const handleAppStateChange = async (nextAppState: AppStateStatus) => {
      if (appState.current.match(/inactive|background/) && nextAppState === 'active') {
        isAppActive.current = true;
        // Reconnect and re-subscribe if we had an active tracking
        if (currentCorrelationRef.current) {
          console.log('[useDeviceSettingsHub] App active - reconnecting...');
          try {
            await deviceSettingsHubService.connect();
            await deviceSettingsHubService.subscribeToBatchOperation(currentCorrelationRef.current);
          } catch (error) {
            console.log('[useDeviceSettingsHub] Re-connect on app active failed:', error);
          }
        }
      } else if (nextAppState.match(/inactive|background/)) {
        isAppActive.current = false;
      }

      appState.current = nextAppState;
    };

    const subscription = AppState.addEventListener('change', handleAppStateChange);
    return () => subscription?.remove();
  }, []);

  /**
   * Start tracking a batch operation
   * Connects to SignalR hub and subscribes to the correlationId
   */
  const startBatchTracking = useCallback(async (correlationId: string) => {
    console.log('[useDeviceSettingsHub] Starting batch tracking:', correlationId);
    try {
      currentCorrelationRef.current = correlationId;
      await deviceSettingsHubService.connect();
      await deviceSettingsHubService.subscribeToBatchOperation(correlationId);
      console.log('[useDeviceSettingsHub] Batch tracking started successfully');
    } catch (error) {
      console.log('[useDeviceSettingsHub] Failed to start batch tracking:', error);
      currentCorrelationRef.current = null;
      throw error;
    }
  }, []);

  /**
   * Stop tracking the current batch operation
   */
  const stopBatchTracking = useCallback(async () => {
    console.log('[useDeviceSettingsHub] Stopping batch tracking');
    currentCorrelationRef.current = null;
    try {
      await deviceSettingsHubService.unsubscribeFromBatchOperation();
      setBatchState(null);
    } catch (error) {
      console.log('[useDeviceSettingsHub] Failed to stop batch tracking:', error);
    }
  }, []);

  /**
   * Disconnect from the hub entirely
   */
  const disconnect = useCallback(async () => {
    console.log('[useDeviceSettingsHub] Disconnecting from hub');
    currentCorrelationRef.current = null;
    try {
      await deviceSettingsHubService.disconnect();
      setBatchState(null);
    } catch (error) {
      console.log('[useDeviceSettingsHub] Failed to disconnect:', error);
    }
  }, []);

  const isUpdateInProgress =
    batchState?.status === 'in_progress' || batchState?.status === 'pending';

  return {
    isConnected,
    connectionState,
    batchState,
    isUpdateInProgress,
    startBatchTracking,
    stopBatchTracking,
    disconnect,
  };
};
