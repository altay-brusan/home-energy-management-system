import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, Image } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService } from '../../../../../api/services/mobileDashboardService';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { useSelectedSiteUtils } from '../../../../../hooks/useSelectedSite';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './WeatherStatus.styles';

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

  const [weather, setWeather] = useState<any>(null);
  const [status, setStatus] = useState<StatusType>('onGrid');
  const { registerRefreshCallback } = useDashboardRefresh();

  const fetchData = useCallback(async () => {
    if (!deviceId) {
      return;
    }

    try {
      // Get today's date range
      const today = new Date();
      const startDate = new Date(today);
      startDate.setHours(0, 0, 0, 0);
      const endDate = new Date(today);
      endDate.setHours(23, 59, 59, 999);

      // Fetch energy flow data and weather in parallel
      const [energyFlow, weatherData] = await Promise.all([
        mobileDashboardService.getEnergyFlow(
          deviceId,
          startDate.toISOString(),
          endDate.toISOString()
        ),
        selectedSite?.id ? dashboardService.getWeatherForecast(selectedSite.id).catch(() => null) : Promise.resolve(null)
      ]);

      // Set weather data
      if (weatherData && weatherData.length > 0) {
        setWeather(weatherData[0]);
      } else {
        setWeather(null);
      }

      // Calculate status
      if (energyFlow && energyFlow.sources) {
        const gridSource = energyFlow.sources.find((s: any) => s.id.toLowerCase() === 'grid');
        const batterySource = energyFlow.sources.find((s: any) => s.id.toLowerCase() === 'battery');
        const gridEnergy = gridSource?.value || 0;
        const batteryValue = batterySource?.value || 0;

        let newStatus: StatusType = 'onGrid';
        if (gridEnergy === 0) {
          newStatus = 'off';
        } else if (gridEnergy > 0 && batteryValue > 0) {
          newStatus = 'hybrid';
        }
        setStatus(newStatus);
      }
    } catch (error) {
      console.error('Error loading weather/status data:', error);
    }
  }, [deviceId, selectedSite?.id]);

  // Initial load
  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(fetchData);
    return () => unregister();
  }, [registerRefreshCallback, fetchData]);

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
