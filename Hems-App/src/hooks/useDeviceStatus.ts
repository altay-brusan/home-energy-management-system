import { useCallback, useEffect, useRef, useState } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { SignalRConnectionState, DeviceStatusChangedDto, DeviceStatusType } from '../api/types/signalR.types';
import { deviceStatusService } from '../services/deviceStatusService';
import { deviceDataService } from '../api/services/deviceDataService';

export const useDeviceStatus = (deviceId?: string) => {
    const [isConnected, setIsConnected] = useState(false);
    const [connectionState, setConnectionState] = useState<SignalRConnectionState>('Disconnected');
    const [deviceStatus, setDeviceStatus] = useState<DeviceStatusType | null>(null);
    const [missedHeartbeats, setMissedHeartbeats] = useState<number>(0);
    const [lastUpdate, setLastUpdate] = useState<string | null>(null);
    const [isInitialStatus, setIsInitialStatus] = useState<boolean>(false);
    const [isLoadingInitialStatus, setIsLoadingInitialStatus] = useState<boolean>(false);

    const appState = useRef(AppState.currentState);
    const isAppActive = useRef(true);
    const currentDeviceIdRef = useRef<string | undefined>(undefined);
    const hasFetchedInitialStatus = useRef(false);

    // STEP 1: Fetch initial status via REST API (fast, reliable)
    // This provides immediate status while SignalR is connecting
    useEffect(() => {
        if (!deviceId) {
            hasFetchedInitialStatus.current = false;
            return;
        }

        // Reset flag when deviceId changes
        if (currentDeviceIdRef.current !== deviceId) {
            hasFetchedInitialStatus.current = false;
        }

        // Only fetch if we haven't fetched for this device yet
        if (hasFetchedInitialStatus.current) {
            return;
        }

        const fetchInitialStatus = async () => {
            setIsLoadingInitialStatus(true);
            try {
                console.log('[useDeviceStatus] Fetching initial status via REST API for device:', deviceId);
                const response = await deviceDataService.getStatus(deviceId);
                console.log('[useDeviceStatus] REST API status response:', response);

                // Only update if we don't have a status yet (SignalR might have provided one already)
                if (!deviceStatus || deviceStatus !== response.status) {
                    setDeviceStatus(response.status);
                    setMissedHeartbeats(response.missedHeartbeats);
                    setLastUpdate(response.timestamp);
                    setIsInitialStatus(true);
                }
                hasFetchedInitialStatus.current = true;
            } catch (error) {
                console.log('[useDeviceStatus] REST API status fetch failed:', error);
                // Don't set error state - SignalR might still work
            } finally {
                setIsLoadingInitialStatus(false);
            }
        };

        fetchInitialStatus();
    }, [deviceId]); // Note: deviceStatus intentionally excluded to avoid loops

    // STEP 2: Connection state listener for SignalR
    // When connection becomes 'Connected', automatically try to subscribe to device
    useEffect(() => {
        const unsubscribe = deviceStatusService.addConnectionStateListener(async (state) => {
            console.log('[useDeviceStatus] Connection state changed to:', state);
            setConnectionState(state);
            setIsConnected(state === 'Connected');

            // When connection becomes connected, try to subscribe to current device
            if (state === 'Connected' && currentDeviceIdRef.current) {
                console.log('[useDeviceStatus] Connection restored, subscribing to device:', currentDeviceIdRef.current);
                try {
                    await deviceStatusService.subscribeToDevice(currentDeviceIdRef.current);
                } catch (error) {
                    console.log('[useDeviceStatus] Auto-subscribe after connection failed:', error);
                }
            }
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
        isLoadingInitialStatus,
        connectAndSubscribe,
        unsubscribe,
        disconnect,
        isAppActive: isAppActive.current,
    };
};

export const DEVICE_STATUS_EVENTS = {
    DEVICE_STATUS_CHANGED: 'DeviceStatusChanged',
} as const;
