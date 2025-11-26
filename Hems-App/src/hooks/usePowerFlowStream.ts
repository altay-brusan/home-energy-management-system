import { useEffect, useState, useRef, useCallback } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { PowerFlowDto, SignalRConnectionState, PowerFlowStreamState } from '../api/types/signalR.types';
import { powerFlowService } from '../services/powerFlowService';

export interface UsePowerFlowStreamOptions {
    deviceId: string | null | undefined;
    intervalSeconds?: number;
    enabled?: boolean; // Allow manual control
}

export interface UsePowerFlowStreamResult {
    powerFlow: PowerFlowDto | null;
    isStreaming: boolean;
    streamState: PowerFlowStreamState;
    connectionState: SignalRConnectionState;
    error: Error | null;
    startStream: () => Promise<void>;
    stopStream: () => void;
}

/**
 * Hook to stream real-time power flow data for a device
 *
 * Features:
 * - Automatic cleanup on unmount
 * - Pauses stream when app goes to background
 * - Resumes stream when app returns to foreground
 * - Handles device ID changes automatically
 *
 * @example
 * ```tsx
 * const DeviceDetail = ({ deviceId }) => {
 *   const { powerFlow, isStreaming, error } = usePowerFlowStream({
 *     deviceId,
 *     intervalSeconds: 5,
 *   });
 *
 *   if (error) return <Text>Error: {error.message}</Text>;
 *   if (!powerFlow) return <Text>Loading...</Text>;
 *
 *   return <PowerFlowDisplay data={powerFlow} />;
 * };
 * ```
 */
export const usePowerFlowStream = (options: UsePowerFlowStreamOptions): UsePowerFlowStreamResult => {
    const { deviceId, intervalSeconds = 5, enabled = true } = options;

    const [powerFlow, setPowerFlow] = useState<PowerFlowDto | null>(null);
    const [streamState, setStreamState] = useState<PowerFlowStreamState>('idle');
    const [connectionState, setConnectionState] = useState<SignalRConnectionState>('Disconnected');
    const [error, setError] = useState<Error | null>(null);

    const cleanupRef = useRef<(() => void) | null>(null);
    const appState = useRef(AppState.currentState);
    const isAppActive = useRef(true);
    const isStreamingRef = useRef(false);

    // Listen to connection state changes
    useEffect(() => {
        const unsubscribe = powerFlowService.addConnectionStateListener((state) => {
            setConnectionState(state);
        });
        return unsubscribe;
    }, []);

    // Start stream function
    const startStream = useCallback(async () => {
        if (!deviceId || !enabled || isStreamingRef.current) {
            return;
        }

        try {
            setStreamState('streaming');
            setError(null);
            isStreamingRef.current = true;

            const cleanup = await powerFlowService.startStream(
                deviceId,
                intervalSeconds,
                // onData
                (data) => {
                    setPowerFlow(data);
                },
                // onError
                (err) => {
                    setError(err);
                    setStreamState('error');
                    isStreamingRef.current = false;
                },
                // onComplete
                () => {
                    setStreamState('completed');
                    isStreamingRef.current = false;
                }
            );

            cleanupRef.current = cleanup;

        } catch (err) {
            setError(err instanceof Error ? err : new Error(String(err)));
            setStreamState('error');
            isStreamingRef.current = false;
        }
    }, [deviceId, intervalSeconds, enabled]);

    // Stop stream function
    const stopStream = useCallback(() => {
        if (cleanupRef.current) {
            cleanupRef.current();
            cleanupRef.current = null;
        }

        if (deviceId) {
            powerFlowService.stopStream(deviceId);
        }

        setStreamState('idle');
        isStreamingRef.current = false;
    }, [deviceId]);

    // Handle app state changes (background/foreground)
    useEffect(() => {
        const handleAppStateChange = (nextAppState: AppStateStatus) => {
            if (appState.current.match(/inactive|background/) && nextAppState === 'active') {
                // App came to foreground
                isAppActive.current = true;
                if (deviceId && enabled) {
                    startStream();
                }
            } else if (nextAppState.match(/inactive|background/)) {
                // App went to background
                isAppActive.current = false;
                stopStream();
            }

            appState.current = nextAppState;
        };

        const subscription = AppState.addEventListener('change', handleAppStateChange);
        return () => subscription?.remove();
    }, [deviceId, enabled, startStream, stopStream]);

    // Start/stop stream when deviceId or enabled changes
    useEffect(() => {
        if (deviceId && enabled && isAppActive.current) {
            startStream();
        } else {
            stopStream();
        }

        // Cleanup on unmount or when dependencies change
        return () => {
            stopStream();
        };
    }, [deviceId, enabled, startStream, stopStream]);

    return {
        powerFlow,
        isStreaming: streamState === 'streaming',
        streamState,
        connectionState,
        error,
        startStream,
        stopStream,
    };
};

/**
 * Hook to get PowerFlow service connection state only (without streaming)
 * Useful for debugging or showing connection status
 */
export const usePowerFlowConnection = () => {
    const [connectionState, setConnectionState] = useState<SignalRConnectionState>(
        powerFlowService.connectionState
    );

    useEffect(() => {
        const unsubscribe = powerFlowService.addConnectionStateListener((state) => {
            setConnectionState(state);
        });
        return unsubscribe;
    }, []);

    return {
        connectionState,
        isConnected: powerFlowService.isConnected,
        activeStreamCount: powerFlowService.activeStreamCount,
    };
};
