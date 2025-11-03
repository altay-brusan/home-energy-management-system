import React, { useEffect, useState, useCallback } from 'react';
import { Text, View, useWindowDimensions, ActivityIndicator } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { Badge } from '../../../../../components';
import { getDashboardWelcomeCardStyles } from './DashboardWelcomeCard.styles';
import type { DashboardWelcomeCardProps } from './DashboardWelcomeCard.types';
import { useTranslation } from 'react-i18next';
import { mobileDashboardService, HeroResponseDto } from '../../../../../api/services/mobileDashboardService';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';

export default function DashboardWelcomeCard({ isDark, COLORS }: DashboardWelcomeCardProps) {
  const { width } = useWindowDimensions();
  const styles = getDashboardWelcomeCardStyles(COLORS, isDark, width);
  const { t } = useTranslation();
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const [heroData, setHeroData] = useState<HeroResponseDto | null>(null);
  const [loading, setLoading] = useState(true);

  const loadHeroData = useCallback(async () => {if (!hasDevice || !device?.id) {setLoading(false);
      return;
    }setLoading(true);
    try {
      const data = await mobileDashboardService.getHero(device.id);setHeroData(data);
    } catch (error) {setHeroData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  useEffect(() => {
    // İlk mount'ta ve device değiştiğinde veri yükle
    if (hasDevice && device?.id) {loadHeroData();
    } else {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDevice, device?.id]);

  // Register refresh callback
  useEffect(() => {const unregister = registerRefreshCallback(loadHeroData);return () => {unregister();
    };
  }, [registerRefreshCallback, loadHeroData]);

  if (loading) {
    return (
      <Animated.View entering={FadeInDown.duration(600)} style={styles.root}>
        <View style={[styles.content, { justifyContent: 'center', alignItems: 'center' }]}>
          <ActivityIndicator color={COLORS.accent} size="large" />
        </View>
      </Animated.View>
    );
  }

  // Don't render if no device or no hero data
  if (!hasDevice || !device?.id || !heroData) {
    return null;
  }

  return (
    <Animated.View entering={FadeInDown.duration(600)} style={styles.root}>
      <Animated.Image
        entering={FadeInDown.delay(200).duration(700)}
        source={{
          uri: 'https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=600&q=80',
        }}
        style={styles.image}
        resizeMode="cover"
      />
      <View style={styles.content}>
     <Text style={styles.title} numberOfLines={2} ellipsizeMode="tail">
  {t('dashboardHeroCard.title')}
</Text>
<Text style={styles.subtitle} numberOfLines={2} ellipsizeMode="tail">
  {t('dashboardHeroCard.subtitle')}
</Text>
        <View style={styles.statusRow}>
          <Badge text={t('dashboardWelcome.status.active')} variant="success" />
          <Text style={styles.lastSync}>
            {t('dashboardWelcome.lastSync', { value: heroData.metrics.lastSync })}
          </Text>
        </View>
        <View style={styles.metricsRow}>
          
          <View style={styles.metricPill}>
            <Text style={styles.metricLabel}>{t('dashboardWelcome.metrics.today')}</Text>
            <Text style={styles.metricValue}>
              {heroData.metrics.todayEnergy} KW
            </Text>
          </View>
          <View style={styles.metricPill}>
            <Text style={styles.metricLabel}>{t('dashboardWelcome.metrics.power')}</Text>
            <Text style={styles.metricValue}>
              {heroData.metrics.currentPower} W
            </Text>
          </View>
        </View>
      </View>
    </Animated.View>
  );
}
