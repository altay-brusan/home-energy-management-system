import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { useSignalR } from '../hooks/useSignalR';
import { DeviceNotificationDto } from '../api/types/signalR.types';
import { useAlertStore } from '../store/alertStore';

interface SignalRContextType {
  isConnected: boolean;
  connectionState: string;
  criticalNotifications: DeviceNotificationDto[];
  dismissCriticalNotification: (notificationId: string) => void;
  clearAllCriticalNotifications: () => void;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

interface SignalRProviderProps {
  children: ReactNode;
}

export const SignalRProvider: React.FC<SignalRProviderProps> = ({ children }) => {
  const { isConnected, connectionState, criticalNotifications, dismissCriticalNotification, clearAllCriticalNotifications } = useSignalR();
  const [processedNotifications, setProcessedNotifications] = useState<Set<string>>(new Set());
  const { show: showAlert } = useAlertStore();

  // App state tracking
  const appState = React.useRef(AppState.currentState);

  useEffect(() => {
    const handleAppStateChange = (nextAppState: AppStateStatus) => {
      appState.current = nextAppState;
    };

    const subscription = AppState.addEventListener('change', handleAppStateChange);
    return () => subscription?.remove();
  }, []);

  // Critical notification'ları işle
  useEffect(() => {
    if (criticalNotifications.length > 0) {
      // En son gelen notification'ı göster (henüz işlenmemişse)
      const latestNotification = criticalNotifications[0];
      
      if (!processedNotifications.has(latestNotification.id)) {
        showAlert({
          title: latestNotification.deviceNotificationType.message,
          message: latestNotification.deviceNotificationType.description,
          level: latestNotification.deviceNotificationType.category === 'critical' ? 'critical' : 'warning',
          deviceName: latestNotification.deviceManufacturer,
          recommendation: latestNotification.additionalData ? 
            `Ek Bilgi: ${latestNotification.additionalData}` : 
            'Lütfen cihazı kontrol edin ve gerekli önlemleri alınız. Sorun devam ederse destek ile iletişime geçin.',
          onDetails: () => {
            // Detay sayfasına yönlendirme
          },
          onSupport: () => {
            // Destek sayfasına yönlendirme
          }
        });
        
        // Notification'ı işlendi olarak işaretle
        setProcessedNotifications(prev => new Set([...prev, latestNotification.id]));
      }
    }
  }, [criticalNotifications, processedNotifications, showAlert]);

  const contextValue: SignalRContextType = {
    isConnected,
    connectionState,
    criticalNotifications,
    dismissCriticalNotification,
    clearAllCriticalNotifications,
  };

  return (
    <SignalRContext.Provider value={contextValue}>
      {children}
    </SignalRContext.Provider>
  );
};

export const useSignalRContext = (): SignalRContextType => {
  const context = useContext(SignalRContext);
  if (context === undefined) {
    throw new Error('useSignalRContext must be used within a SignalRProvider');
  }
  return context;
};
