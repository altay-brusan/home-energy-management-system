import React from 'react';
import { View, ActivityIndicator } from 'react-native';
import { useNetInfo } from '../../hooks/useNetInfo';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import OfflineScreen from '../../app/screens/offline/OfflineScreen';
import { apiClient } from '../../api/client/apiClient';

interface NetworkWrapperProps {
  children: React.ReactNode;
  onRetry?: () => void;
  showOfflineScreen?: boolean;
}

export default function NetworkWrapper({ 
  children, 
  onRetry, 
  showOfflineScreen = true 
}: NetworkWrapperProps) {
  const { isConnected } = useNetInfo();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);

  // Logout sırasında offline screen gösterme
  const isLoggingOut = apiClient.getIsLoggingOut();

  if (isLoggingOut) {return <>{children}</>;
  }

  if (isConnected === null) {
    return (
      <View style={{ 
        flex: 1, 
        justifyContent: 'center', 
        alignItems: 'center',
        backgroundColor: COLORS.dark 
      }}>
        <ActivityIndicator size="large" color={COLORS.accent} />
      </View>
    );
  }

  if (!isConnected && showOfflineScreen) {
    return <OfflineScreen onRetry={onRetry} />;
  }

  return <>{children}</>;
}
