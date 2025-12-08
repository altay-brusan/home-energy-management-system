import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, Image, ActivityIndicator } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useDeviceStatus } from '../../../../../hooks/useDeviceStatus';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { useSelectedSiteUtils } from '../../../../../hooks/useSelectedSite';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './WeatherStatus.styles';
import type { WeatherForecastDto } from '../../../../../api/types/dashboard.types';
import type { DeviceStatusType, SignalRConnectionState } from '../../../../../api/types/signalR.types';

interface WeatherStatusProps {
  deviceId?: string;
}

// Status renklerini belirleyen helper fonksiyon
const getStatusColor = (status: DeviceStatusType | null, connectionState: SignalRConnectionState, COLORS: Record<string, string>): string => {
  // Bağlantı durumu kontrolü
  if (connectionState === 'Connecting' || connectionState === 'Reconnecting') {
    return COLORS.warning || '#FFA500';
  }
  if (connectionState === 'Disconnected' || connectionState === 'Failed') {
    return COLORS.textSoft;
  }

  // Device status kontrolü
  if (!status) {
    return COLORS.textSoft;
  }

  switch (status) {
    case 'OK':
      return COLORS.success;
    case 'PENDING':
      return COLORS.warning || '#FFA500';
    case 'WAITING':
      return COLORS.warning || '#FFA500';
    case 'DISCONNECTED':
      return COLORS.error || '#FF4444';
    case 'NOT_TRACKED':
      return COLORS.textSoft;
  }
};

// Status text'ini belirleyen helper fonksiyon
const getStatusText = (status: DeviceStatusType | null, connectionState: SignalRConnectionState, t: (key: string) => string): string => {
  // Bağlantı durumu kontrolü
  if (connectionState === 'Connecting' || connectionState === 'Reconnecting') {
    return t('dashboard.deviceStatus.connecting');
  }
  if (connectionState === 'Disconnected' || connectionState === 'Failed') {
    return t('dashboard.deviceStatus.offline');
  }

  // Device status henüz gelmemişse
  if (!status) {
    return t('dashboard.deviceStatus.waiting');
  }

  switch (status) {
    case 'OK':
      return t('dashboard.deviceStatus.ok');
    case 'PENDING':
      return t('dashboard.deviceStatus.pending');
    case 'WAITING':
      return t('dashboard.deviceStatus.waiting');
    case 'DISCONNECTED':
      return t('dashboard.deviceStatus.disconnected');
    case 'NOT_TRACKED':
      return t('dashboard.deviceStatus.notTracked');
  }
};

// Bağlantı durumuna göre loading indicator göster
const shouldShowLoading = (connectionState: SignalRConnectionState, deviceStatus: DeviceStatusType | null): boolean => {
  return connectionState === 'Connecting' || connectionState === 'Reconnecting' ||
         (connectionState === 'Connected' && !deviceStatus);
};

export const WeatherStatus: React.FC<WeatherStatusProps> = ({ deviceId }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);
  const { selectedSite } = useSelectedSiteUtils();

  const [weather, setWeather] = useState<WeatherForecastDto | null>(null);
  const { registerRefreshCallback } = useDashboardRefresh();

  // DeviceStatus hook - SignalR üzerinden cihaz durumu
  const { deviceStatus, connectionState } = useDeviceStatus(deviceId);

  // Fetch weather separately
  const fetchWeather = useCallback(async () => {
    if (!selectedSite?.id) {
      return;
    }

    try {
      const weatherData = await dashboardService.getWeatherForecast(selectedSite.id);
      if (weatherData && weatherData.length > 0) {
        setWeather(weatherData[0]);
      } else {
        setWeather(null);
      }
    } catch (error: any) {
      // Canceled errors'ı silent handle et
      if (error?.message?.includes('canceled') || error?.name === 'CanceledError' || error?.code === 'ERR_CANCELED') {
        return;
      }
      setWeather(null);
    }
  }, [selectedSite?.id]);


  // Load weather
  useEffect(() => {
    fetchWeather();
  }, [fetchWeather]);

  // Register refresh callbacks
  useEffect(() => {
    const unregisterWeather = registerRefreshCallback(fetchWeather);
    return () => {
      unregisterWeather();
    };
  }, [registerRefreshCallback, fetchWeather]);

  if (!deviceId) {
    return null;
  }

  return (
    <Animated.View entering={FadeInDown.delay(50)} style={styles.container}>
      {/* Weather - Left */}
      <View style={styles.weatherContainer}>
        {weather ? (
          <>
            {weather.icon && (
              <Image
                source={{ uri: weather.icon.startsWith('http') ? weather.icon : `https:${weather.icon}` }}
                style={styles.weatherIcon}
                resizeMode="contain"
              />
            )}
            <Text style={styles.weatherTemp}>{weather.temperature}°</Text>
          </>
        ) : (
          <Text style={styles.weatherTemp}>--°</Text>
        )}
      </View>

      {/* Status - Right */}
      <View style={styles.statusContainer}>
        {shouldShowLoading(connectionState, deviceStatus) && (
          <ActivityIndicator size="small" color={COLORS.accent} style={{ marginRight: 4 }} />
        )}
        <Text style={styles.statusLabel}>{t('dashboard.status')}</Text>
        <Text style={[styles.statusValue, { color: getStatusColor(deviceStatus, connectionState, COLORS) }]}>
          {getStatusText(deviceStatus, connectionState, t).toUpperCase()}
        </Text>
      </View>
    </Animated.View>
  );
};

export default WeatherStatus;
