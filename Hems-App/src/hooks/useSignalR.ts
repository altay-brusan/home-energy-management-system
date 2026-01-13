import { useCallback, useEffect, useRef, useState } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { DeviceNotificationDto, SignalRConnectionState } from '../api/types/signalR.types';
import { signalRService } from '../services/signalRService';
import { useSelectedSiteDevice } from './useSelectedSiteDevice';
import { useAlertStore } from '../store/alertStore';

export const useSignalR = () => {
    const { selectedSiteDevice } = useSelectedSiteDevice();
    const [isConnected, setIsConnected] = useState(false);
    const [connectionState, setConnectionState] = useState<SignalRConnectionState>('Disconnected');
    const [criticalNotifications, setCriticalNotifications] = useState<DeviceNotificationDto[]>([]);
    const { show: showAlert } = useAlertStore();

    const appState = useRef(AppState.currentState);
    const isAppActive = useRef(true);

    useEffect(() => {
        const unsubscribe = signalRService.addConnectionStateListener((state) => {
            console.log('[useSignalR] Connection state changed:', state);
            setConnectionState(state);
            setIsConnected(state === 'Connected');
        });

        return unsubscribe;
    }, []);

    useEffect(() => {
        const handleAppStateChange = (nextAppState: AppStateStatus) => {
            if (appState.current.match(/inactive|background/) && nextAppState === 'active') {
                isAppActive.current = true;
                if (selectedSiteDevice?.siteId) {
                    connect(selectedSiteDevice.siteId);
                }
            } else if (nextAppState.match(/inactive|background/)) {
                isAppActive.current = false;
            }

            appState.current = nextAppState;
        };

        const subscription = AppState.addEventListener('change', handleAppStateChange);
        return () => subscription?.remove();
    }, [selectedSiteDevice?.siteId]);

    useEffect(() => {
        console.log('[useSignalR] selectedSiteDevice changed:', selectedSiteDevice?.siteId || 'NO SITE ID');

        if (selectedSiteDevice?.siteId) {
            if (signalRService.currentSite && signalRService.currentSite !== selectedSiteDevice.siteId) {
                console.log('[useSignalR] Site changed, reconnecting...');
                signalRService.disconnect().then(() => {
                    connect(selectedSiteDevice.siteId);
                }).catch(() => {});
            } else {
                connect(selectedSiteDevice.siteId);
            }
        } else {
            console.log('[useSignalR] No siteId, disconnecting...');
            disconnect();
        }
    }, [selectedSiteDevice?.siteId]);

    const connect = useCallback(async (siteId?: string) => {
        try {
            console.log('[useSignalR] Connecting with siteId:', siteId);
            await signalRService.connect(siteId);
            console.log('[useSignalR] ✅ Connection successful');
        } catch (error: any) {
            console.log('[useSignalR] ❌ Connection error:', error?.message || error);
        }
    }, []);

    const disconnect = useCallback(async () => {
        try {
            await signalRService.disconnect();
        } catch (error) {
        }
    }, []);

    const invoke = useCallback(async <T = any> (methodName: string, ...args: any[]): Promise<T> => {
        try {
            return await signalRService.invoke<T>(methodName, ...args);
        } catch (error) {
            throw error;
        }
    }, []);

    const on = useCallback((methodName: string, callback: (...args: any[]) => void) => {
        signalRService.on(methodName, callback);
    }, []);

    const off = useCallback((methodName: string, callback?: (...args: any[]) => void) => {
        signalRService.off(methodName, callback);
    }, []);

    const handleCriticalNotification = useCallback((notification: DeviceNotificationDto) => {
        console.log('[useSignalR] ✅ Received notification:', notification.id);

        const category = notification.deviceNotificationType?.category?.toUpperCase();
        const notificationMessage = notification.deviceNotificationType?.message || '';
        const description = notification.deviceNotificationType?.description || '';
        const code = notification.deviceNotificationType?.code || '';
        const deviceName = notification.deviceManufacturer || notification.siteName || '';

        // Determine alert level based on category
        let alertLevel: 'info' | 'warning' | 'critical' = 'info';
        if (category === 'CRITICAL' || category === 'ALARM' || category === 'ERROR') {
            alertLevel = 'critical';
        } else if (category === 'WARNING') {
            alertLevel = 'warning';
        }

        // Format title: show code if available
        const alertTitle = code ? `${code} - ${notificationMessage}` : notificationMessage;

        console.log('[useSignalR] 🔔 Showing alert:', { title: alertTitle, level: alertLevel });
        showAlert({
            title: alertTitle,
            message: description,
            level: alertLevel,
            deviceName: deviceName,
        });

        if (category === 'CRITICAL' || category === 'ALARM' || category === 'WARNING') {
            setCriticalNotifications(prev => [notification, ...prev]);
        }
    }, [showAlert]);

    const handleNotificationUpdate = useCallback((notification: DeviceNotificationDto) => {

        setCriticalNotifications(prev =>
            prev.map(n =>
                n.id === notification.id
                    ? { ...n, isProcessed: notification.isProcessed }
                    : n
            )
        );
    }, []);

    useEffect(() => {
        if (!isConnected) {
            console.log('[useSignalR] Not connected, skipping event registration');
            return;
        }

        console.log('[useSignalR] ✅ Registering notification event listeners...');

        const notificationMethod = 'ReceiveDeviceNotification';
        const updateMethod = 'ReceiveDeviceNotificationUpdate';

        on(notificationMethod, handleCriticalNotification);
        console.log('[useSignalR] Registered listener for:', notificationMethod);

        on(updateMethod, handleNotificationUpdate);
        console.log('[useSignalR] Registered listener for:', updateMethod);

        return () => {
            off(notificationMethod, handleCriticalNotification);
            off(updateMethod, handleNotificationUpdate);
        };
    }, [isConnected, on, off, handleCriticalNotification, handleNotificationUpdate]);

    const dismissCriticalNotification = useCallback((notificationId: string) => {
        setCriticalNotifications(prev =>
            prev.filter(n => n.id !== notificationId)
        );
    }, []);

    const clearAllCriticalNotifications = useCallback(() => {
        setCriticalNotifications([]);
    }, []);

    return {
        isConnected,
        connectionState,
        currentSite: signalRService.currentSite,

        connect,
        disconnect,
        invoke,
        on,
        off,

        criticalNotifications,
        dismissCriticalNotification,
        clearAllCriticalNotifications,
        isAppActive: isAppActive.current,
    };
};

export const SIGNALR_EVENTS = {
    RECEIVE_DEVICE_NOTIFICATION: 'ReceiveDeviceNotification',
    RECEIVE_DEVICE_NOTIFICATION_UPDATE: 'ReceiveDeviceNotificationUpdate',
} as const;
