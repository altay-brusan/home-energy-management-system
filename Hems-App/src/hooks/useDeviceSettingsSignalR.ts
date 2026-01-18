/**
 * useDeviceSettingsSignalR Hook
 * Manages SignalR connection for device settings updates with automatic reconnection
 * Handles real-time progress updates and failed parameter rollback
 */

import { useEffect, useRef, useCallback } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { deviceSettingsSignalRService } from '../services/deviceSettingsSignalRService';
import { useDeviceSettingsStore } from '../store/deviceSettingsStore';
import { useDeviceSettingsUpdateStore } from '../store/deviceSettingsUpdateStore';
import {
  OperationStartedEvent,
  ParameterProgressEvent,
  OperationCompletedEvent,
} from '../api/types/deviceSettingsSignalR.types';

export interface UseDeviceSettingsSignalROptions {
  deviceId: string | undefined;
  enabled?: boolean;
}

export const useDeviceSettingsSignalR = ({ deviceId, enabled = true }: UseDeviceSettingsSignalROptions) => {
  const appState = useRef(AppState.currentState);
  const isConnectedRef = useRef(false);

  // Store actions
  const rollbackParameters = useDeviceSettingsStore(state => state.rollbackParameters);
  const clearPreSaveSnapshot = useDeviceSettingsStore(state => state.clearPreSaveSnapshot);

  // Update store actions
  const {
    setOperationStarted,
    updateProgress,
    setOperationCompleted,
  } = useDeviceSettingsUpdateStore();

  // Handle OperationStarted event
  const handleOperationStarted = useCallback(
    (event: OperationStartedEvent) => {
      console.log('[useDeviceSettingsSignalR] OperationStarted:', event.operationId);
      setOperationStarted(event);
    },
    [setOperationStarted]
  );

  // Handle ParameterProgress event
  const handleParameterProgress = useCallback(
    (event: ParameterProgressEvent) => {
      console.log(
        '[useDeviceSettingsSignalR] ParameterProgress:',
        event.parameterName,
        event.status,
        `${event.currentIndex}/${event.totalParameters}`
      );
      updateProgress(event);
    },
    [updateProgress]
  );

  // Handle OperationCompleted event
  const handleOperationCompleted = useCallback(
    (event: OperationCompletedEvent) => {
      console.log(
        '[useDeviceSettingsSignalR] OperationCompleted:',
        event.operationId,
        'Success:',
        event.success,
        'Failed:',
        event.failedCount
      );

      // Rollback failed parameters to original values
      if (event.failedParameters.length > 0) {
        const failedIds = event.failedParameters.map(p => p.parameterId);
        rollbackParameters(failedIds);
      }

      // Clear snapshot if all succeeded
      if (event.success) {
        clearPreSaveSnapshot();
      }

      setOperationCompleted(event);
    },
    [setOperationCompleted, rollbackParameters, clearPreSaveSnapshot]
  );

  // Handle app returning from background
  const handleAppStateChange = useCallback(
    async (nextAppState: AppStateStatus) => {
      if (appState.current.match(/inactive|background/) && nextAppState === 'active' && deviceId && enabled) {
        console.log('[useDeviceSettingsSignalR] App active, checking connection...');

        if (!deviceSettingsSignalRService.isConnected) {
          try {
            await deviceSettingsSignalRService.connect(deviceId);

            // Check for active operation after reconnection
            const activeOp = await deviceSettingsSignalRService.getActiveOperation(deviceId);
            if (activeOp && !activeOp.isCompleted) {
              console.log('[useDeviceSettingsSignalR] Found active operation:', activeOp.operationId);
              // Restore progress state
              setOperationStarted({
                operationId: activeOp.operationId,
                deviceId: activeOp.deviceId,
                totalParameters: activeOp.totalParameters,
                startedAt: activeOp.startedAt,
              });
            }
          } catch (error) {
            console.error('[useDeviceSettingsSignalR] Reconnect failed:', error);
          }
        }
      }

      appState.current = nextAppState;
    },
    [deviceId, enabled, setOperationStarted]
  );

  // Main connection effect - uses subscribe/unsubscribe for reference counting
  useEffect(() => {
    if (!deviceId || !enabled) {
      return;
    }

    let isCancelled = false;
    let unsubOperationStarted: (() => void) | undefined;
    let unsubParameterProgress: (() => void) | undefined;
    let unsubOperationCompleted: (() => void) | undefined;

    const connect = async () => {
      try {
        console.log('[useDeviceSettingsSignalR] Subscribing to device:', deviceId);

        // Register event handlers first
        unsubOperationStarted = deviceSettingsSignalRService.onOperationStarted(handleOperationStarted);
        unsubParameterProgress = deviceSettingsSignalRService.onParameterProgress(handleParameterProgress);
        unsubOperationCompleted = deviceSettingsSignalRService.onOperationCompleted(handleOperationCompleted);

        // Use subscribe instead of connect for reference counting
        // This prevents disconnection during screen transitions
        await deviceSettingsSignalRService.subscribe(deviceId);

        // Check if we were cancelled during connection
        if (isCancelled) {
          console.log('[useDeviceSettingsSignalR] Connection completed but hook was unmounted');
          return;
        }

        isConnectedRef.current = true;
        console.log('[useDeviceSettingsSignalR] Subscribed successfully');
      } catch (error) {
        // Don't log errors if we were cancelled
        if (!isCancelled) {
          console.error('[useDeviceSettingsSignalR] Subscription failed:', error);
        }
        isConnectedRef.current = false;
      }
    };

    connect();

    return () => {
      isCancelled = true;
      console.log('[useDeviceSettingsSignalR] Cleaning up subscription');

      // Unsubscribe from events
      unsubOperationStarted?.();
      unsubParameterProgress?.();
      unsubOperationCompleted?.();

      // Use unsubscribe instead of disconnect for reference counting
      // Connection will be kept alive if other screens still need it
      deviceSettingsSignalRService.unsubscribe();
      isConnectedRef.current = false;
    };
  }, [deviceId, enabled, handleOperationStarted, handleParameterProgress, handleOperationCompleted]);

  // App state listener for reconnection
  useEffect(() => {
    const subscription = AppState.addEventListener('change', handleAppStateChange);
    return () => subscription.remove();
  }, [handleAppStateChange]);

  return {
    isConnected: isConnectedRef.current,
    connect: useCallback(
      () => (deviceId ? deviceSettingsSignalRService.connect(deviceId) : Promise.resolve()),
      [deviceId]
    ),
    disconnect: useCallback(() => deviceSettingsSignalRService.disconnect(), []),
    getActiveOperation: useCallback(
      () => (deviceId ? deviceSettingsSignalRService.getActiveOperation(deviceId) : Promise.resolve(null)),
      [deviceId]
    ),
    getOperationStatus: useCallback(
      (operationId: string) => deviceSettingsSignalRService.getOperationStatus(operationId),
      []
    ),
  };
};
