import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, ActivityIndicator, Image } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { getDashboardWeatherStyles } from './DashboardWeather.styles';
import type { DashboardWeatherProps } from './DashboardWeather.types';
import { useTranslation } from 'react-i18next';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { useSelectedSiteUtils } from '../../../../../hooks/useSelectedSite';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';

export default function DashboardWeather ({ COLORS, isDark }: DashboardWeatherProps) {
  const styles = getDashboardWeatherStyles(COLORS);
  const { t } = useTranslation();
  const { selectedSite } = useSelectedSiteUtils();
  const { registerRefreshCallback } = useDashboardRefresh();
  const [loading, setLoading] = useState(true);
  const [weather, setWeather] = useState<any>(null);

  const loadWeather = useCallback(async () => {if (!selectedSite?.id) {setLoading(false);
      return;
    }setLoading(true);
    try {
      const data = await dashboardService.getWeatherForecast(selectedSite.id);if (data?.length > 0) {
        setWeather(data[0]);
      } else {
        setWeather(null);
      }
    } catch (err: any) {
      // 401 hatasını silent handle et - interceptor zaten token refresh yapacak
      if (err?.response?.status === 401) {setWeather(null);
      } else {setWeather(null);
      }
    } finally {
      setLoading(false);
    }
  }, [selectedSite?.id]);

  useEffect(() => {
    loadWeather();
  }, [loadWeather, selectedSite?.id]);

  // Register refresh callback
  useEffect(() => {const unregister = registerRefreshCallback(loadWeather);return () => {unregister();
    };
  }, [registerRefreshCallback, loadWeather]);

  if (loading)
    return (
      <View style={{ paddingVertical: 20 }}>
        <ActivityIndicator color={COLORS.accent} />
      </View>
    );

  if (!weather)
    return (
      <View style={{ paddingVertical: 20 }}>
        <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
          {t('dashboardWeather.noData')}
        </Text>
      </View>
    );

  return (
    <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.widget}>
      <Text style={styles.title}>{t('dashboardWeather.title')}</Text>

      <View style={styles.row}>
        {weather.icon ? (
          <Image
            source={{ uri: weather.icon.startsWith('http') ? weather.icon : `https:${weather.icon}` }}
            style={styles.weatherIcon}
          />
        ) : (
          <Text style={styles.icon}>☀️</Text>
        )}

        <View style={styles.contentWrapper}>
          <Text style={styles.temp}>{weather.temperature}°C</Text>
          <Text style={styles.desc}>{weather.description}</Text>
          <Text style={styles.meta} numberOfLines={2}>
            💧 {t('dashboardWeather.humidity')}: {weather.humidity}% {'   '}
            💨 {t('dashboardWeather.wind')}: {weather.windSpeed.toFixed(1)} m/s
          </Text>
        </View>
      </View>
    </Animated.View>
  );
}
