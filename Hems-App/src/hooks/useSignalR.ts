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
        if (selectedSiteDevice?.siteId) {
            if (signalRService.currentSite && signalRService.currentSite !== selectedSiteDevice.siteId) {
                signalRService.disconnect().then(() => {
                    connect(selectedSiteDevice.siteId);
                }).catch(() => {});
            } else {
                connect(selectedSiteDevice.siteId);
            }
        } else {
            disconnect();
        }
    }, [selectedSiteDevice?.siteId]);

    const connect = useCallback(async (siteId?: string) => {
        try {
            await signalRService.connect(siteId);
        } catch (error) {
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

        const category = notification.deviceNotificationType.category?.toUpperCase();
        const title = notification.deviceNotificationType.message;
        const message = notification.deviceNotificationType.description;
        const deviceName = notification.deviceManufacturer || notification.siteName;
        const code = notification.deviceNotificationType.code;

        let alertLevel: 'info' | 'warning' | 'critical' = 'info';
        if (category === 'CRITICAL' || category === 'ALARM') {
            alertLevel = 'critical';
        } else if (category === 'WARNING') {
            alertLevel = 'warning';
        }

        let additionalInfo = '';
        if (notification.additionalData) {
            try {
                const parsed = typeof notification.additionalData === 'string'
                    ? JSON.parse(notification.additionalData)
                    : notification.additionalData;

                if (typeof parsed === 'object' && parsed !== null) {
                    // Map objeyi formatlanmış string'e çevir
                    additionalInfo = Object.entries(parsed)
                        .map(([key, value]) => `${key}: ${value}`)
                        .join('\n');
                } else {
                    additionalInfo = String(notification.additionalData);
                }
            } catch (e) {
                additionalInfo = String(notification.additionalData);
            }
        }

        showAlert({
            title: `${code} - ${title}`,
            message: message,
            level: alertLevel,
            deviceName: deviceName,
            recommendation: additionalInfo ||
                'Lütfen cihazı kontrol edin ve gerekli önlemleri alınız. Sorun devam ederse destek ile iletişime geçin.'
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
            return;
        }


        const notificationMethod = 'ReceiveDeviceNotification';
        const updateMethod = 'ReceiveDeviceNotificationUpdate';

        on(notificationMethod, handleCriticalNotification);

        on(updateMethod, handleNotificationUpdate);

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
