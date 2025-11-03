import React, { useState, useEffect, useCallback } from 'react';
import { Image, View, Text, ActivityIndicator } from 'react-native';
import { getDashboardHeroImageStyles } from './DashboardHeroImage.styles';
import { useTranslation } from 'react-i18next';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { BatteryComponentDto, mobileDashboardService } from '../../../../../api/services/mobileDashboardService';

type Props = {
  COLORS: Record<string, string>;
};

export default function DashboardHeroImage ({ COLORS }: Props) {
  const styles = getDashboardHeroImageStyles(COLORS);
  const { t } = useTranslation();

  const { hasDevice, device } = useSelectedSiteDevice();

  const { registerRefreshCallback } = useDashboardRefresh();

  const [batteryData, setBatteryData] = useState<BatteryComponentDto | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const loadBatteryData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setBatteryData(null);
      return;
    }

    try {
      setLoading(true);
      const data = await mobileDashboardService.getBatteryComponent(device.id);
      setBatteryData(data);
    } catch (error) {setBatteryData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  useEffect(() => {
    // Sadece cihaz varsa veri yükle
    if (hasDevice && device?.id) {
      loadBatteryData();
    }
  }, [loadBatteryData, hasDevice, device?.id]);

  useEffect(() => {
    // Sadece cihaz varsa refresh callback'i kaydet
    if (hasDevice && device?.id) {
      const unregister = registerRefreshCallback(loadBatteryData);
      return unregister;
    }
  }, [registerRefreshCallback, loadBatteryData, hasDevice, device?.id]);
  if (loading) {
    return (
      <View style={styles.root}>
        <View style={[styles.content, { justifyContent: 'center', alignItems: 'center', height: 200 }]}>
          <ActivityIndicator color={COLORS.accent} size="large" />
        </View>
      </View>
    );
  }

  if (!hasDevice || !device?.id) {
    return null;
  }

  return (
    <View style={styles.root}>
      {/* <Image
        source={require('../../../../../assets/images/energy_home.bg.png')}
        style={styles.image}
        resizeMode="contain"
      /> */}
      <View style={styles.content}>
        <Text style={styles.title}>{t('dashboardHero.title')}</Text>
        <Text style={styles.subtitle}>{t('dashboardHero.subtitle')}</Text>
        <View style={styles.metaRow}>
          <Text style={styles.metaItem}>
            {t('dashboardHero.today', { value: batteryData?.today || 0 })}
          </Text>
          <Text style={styles.metaItem}>
            {t('dashboardHero.soc', { value: batteryData?.soc || 0 })}
          </Text>
          <Text style={styles.metaItem}>
            {t('dashboardHero.total', { value: batteryData?.total || 0 })}
          </Text>
        </View>
      </View>
    </View>
  );
}
