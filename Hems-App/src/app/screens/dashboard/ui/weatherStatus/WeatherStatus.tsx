import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, Image } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { useSelectedSiteUtils } from '../../../../../hooks/useSelectedSite';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './WeatherStatus.styles';
import type { WeatherForecastDto } from '../../../../../api/types/dashboard.types';

interface WeatherStatusProps {
  deviceId?: string;
}

type StatusType = 'onGrid' | 'off' | 'hybrid';

export const WeatherStatus: React.FC<WeatherStatusProps> = ({ deviceId }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);
  const { selectedSite } = useSelectedSiteUtils();

  const [weather, setWeather] = useState<WeatherForecastDto | null>(null);
  const [status, setStatus] = useState<StatusType>('onGrid');
  const { registerRefreshCallback } = useDashboardRefresh();

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

  // Fetch status from energy flow
  const fetchStatus = useCallback(async () => {
    if (!deviceId) {
      return;
    }

    // NOTE: EnergyFlow endpoint is deprecated and no longer available
    // Status calculation has been disabled until a replacement endpoint is provided
    // Default status is now 'onGrid'
    setStatus('onGrid');
  }, [deviceId]);

  // Load weather
  useEffect(() => {
    fetchWeather();
  }, [fetchWeather]);

  // Load status
  useEffect(() => {
    fetchStatus();
  }, [fetchStatus]);

  // Register refresh callbacks
  useEffect(() => {
    const unregisterWeather = registerRefreshCallback(fetchWeather);
    const unregisterStatus = registerRefreshCallback(fetchStatus);
    return () => {
      unregisterWeather();
      unregisterStatus();
    };
  }, [registerRefreshCallback, fetchWeather, fetchStatus]);

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
        <Text style={styles.statusLabel}>{t('dashboard.status')}</Text>
        <Text style={styles.statusValue}>
          {t(`dashboard.${status}`).toUpperCase()}
        </Text>
      </View>
    </Animated.View>
  );
};

export default WeatherStatus;
