import { useCallback, useEffect, useRef, useState } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { SignalRConnectionState, DeviceStatusChangedDto, DeviceStatusType } from '../api/types/signalR.types';
import { deviceStatusService } from '../services/deviceStatusService';

export const useDeviceStatus = (deviceId?: string) => {
    const [isConnected, setIsConnected] = useState(false);
    const [connectionState, setConnectionState] = useState<SignalRConnectionState>('Disconnected');
    const [deviceStatus, setDeviceStatus] = useState<DeviceStatusType | null>(null);
    const [missedHeartbeats, setMissedHeartbeats] = useState<number>(0);
    const [lastUpdate, setLastUpdate] = useState<string | null>(null);
    const [isInitialStatus, setIsInitialStatus] = useState<boolean>(false);

    const appState = useRef(AppState.currentState);
    const isAppActive = useRef(true);
    const currentDeviceIdRef = useRef<string | undefined>(undefined);

    // Connection state listener
    useEffect(() => {
        const unsubscribe = deviceStatusService.addConnectionStateListener((state) => {
            setConnectionState(state);
            setIsConnected(state === 'Connected');
        });

        return unsubscribe;
    }, []);

    // Status change listener
    useEffect(() => {
        const unsubscribe = deviceStatusService.addStatusListener((data: DeviceStatusChangedDto) => {
            if (deviceId && data.deviceId === deviceId) {
                console.log('[useDeviceStatus] Status update received:', data);
                setDeviceStatus(data.status);
                setMissedHeartbeats(data.missedHeartbeats);
                setLastUpdate(data.timestamp);
                setIsInitialStatus(data.isInitialStatus ?? false);
            }
        });

        return unsubscribe;
    }, [deviceId]);

    // App state handling
    useEffect(() => {
        const handleAppStateChange = async (nextAppState: AppStateStatus) => {
            if (appState.current.match(/inactive|background/) && nextAppState === 'active') {
                isAppActive.current = true;
                // App aktif olduğunda bağlantıyı ve subscription'ı yenile
                if (currentDeviceIdRef.current) {
                    try {
                        await deviceStatusService.connect();
                        await deviceStatusService.subscribeToDevice(currentDeviceIdRef.current);
                    } catch (error) {
                        console.log('[useDeviceStatus] Re-connect on app active failed:', error);
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

    // Device subscription management
    useEffect(() => {
        currentDeviceIdRef.current = deviceId;

        const setupSubscription = async () => {
            if (deviceId) {
                try {
                    await deviceStatusService.connect();
                    await deviceStatusService.subscribeToDevice(deviceId);
                } catch (error) {
                    console.log('[useDeviceStatus] Connect/Subscribe failed:', error);
                }
            } else {
                // DeviceId yoksa subscription'ı kaldır
                try {
                    await deviceStatusService.unsubscribeFromDevice();
                    setDeviceStatus(null);
                    setMissedHeartbeats(0);
                    setLastUpdate(null);
                    setIsInitialStatus(false);
                } catch (error) {
                    // Silent error handling
                }
            }
        };

        setupSubscription();

        // Cleanup: component unmount olduğunda unsubscribe
        return () => {
            if (deviceId) {
                deviceStatusService.unsubscribeFromDevice().catch(() => {});
            }
        };
    }, [deviceId]);

    const connectAndSubscribe = useCallback(async (targetDeviceId: string) => {
        try {
            // Önce bağlantı kur (zaten bağlıysa hızlıca dönecek)
            await deviceStatusService.connect();
            // Sonra device'a subscribe ol
            await deviceStatusService.subscribeToDevice(targetDeviceId);
        } catch (error) {
            console.log('[useDeviceStatus] Connect/Subscribe failed:', error);
        }
    }, []);

    const unsubscribe = useCallback(async () => {
        try {
            await deviceStatusService.unsubscribeFromDevice();
            // State'leri temizle
            setDeviceStatus(null);
            setMissedHeartbeats(0);
            setLastUpdate(null);
            setIsInitialStatus(false);
        } catch (error) {
            // Silent error handling
        }
    }, []);

    const disconnect = useCallback(async () => {
        try {
            await deviceStatusService.disconnect();
        } catch (error) {
            // Silent error handling
        }
    }, []);

    return {
        isConnected,
        connectionState,
        deviceStatus,
        missedHeartbeats,
        lastUpdate,
        isInitialStatus,
        connectAndSubscribe,
        unsubscribe,
        disconnect,
        isAppActive: isAppActive.current,
    };
};

export const DEVICE_STATUS_EVENTS = {
    DEVICE_STATUS_CHANGED: 'DeviceStatusChanged',
} as const;
