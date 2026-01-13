import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { useThemeStore } from '../../../../../store/themeStore';
import { usePowerFlowStore } from '../../../../../store/powerFlowStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './BatteryStateCard.styles';
import { BatteryStateCardProps } from './BatteryStateCard.types';

const BatteryStateCard: React.FC<BatteryStateCardProps> = ({ deviceId }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const [loading, setLoading] = useState(true);
  const [soc, setSoc] = useState<number>(0);

  // Get live battery power from PowerFlowDiagram
  const batteryPowerKw = usePowerFlowStore(s => s.batteryPowerKw);

  const fetchBatteryData = useCallback(async () => {
    if (!deviceId) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);

      // Fetch SOC only (battery power comes from PowerFlowDiagram via store)
      const socValue = await dashboardService.getDeviceSoc(deviceId);
      setSoc(socValue || 0);
    } catch (error) {
      console.error('Error fetching battery data:', error);
      setSoc(0);
    } finally {
      setLoading(false);
    }
  }, [deviceId]);

  const { registerRefreshCallback } = useDashboardRefresh();

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(fetchBatteryData);
    return () => {
      unregister();
    };
  }, [registerRefreshCallback, fetchBatteryData]);

  useEffect(() => {
    fetchBatteryData();
  }, [fetchBatteryData]);

  if (!deviceId) return null;

  if (loading) {
    return (
      <Animated.View entering={FadeInDown.delay(300).duration(600)} style={styles.container}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      </Animated.View>
    );
  }

  // Determine battery state based on live battery power from PowerFlowDiagram
  // Battery power: negative = charging, positive = discharging, zero or near-zero = idle
  const IDLE_THRESHOLD = 0.01; // kW threshold for idle state
  const isCharging = batteryPowerKw < -IDLE_THRESHOLD;
  const isDischarging = batteryPowerKw > IDLE_THRESHOLD;
  const isIdle = Math.abs(batteryPowerKw) <= IDLE_THRESHOLD;

  let statusText = t('batteryStateCard.status.idle');
  if (isCharging) {
    statusText = t('batteryStateCard.status.charging');
  } else if (isDischarging) {
    statusText = t('batteryStateCard.status.discharging');
  }

  // Determine progress bar color based on SOC level and state
  let progressColor = COLORS.success;
  if (soc < 20) {
    progressColor = '#FF6B6B'; // Red for low battery
  } else if (soc < 50) {
    progressColor = '#FDB52E'; // Yellow/Orange for medium
  } else {
    progressColor = '#12C6A9'; // Green for good
  }

  // If charging, use a different color
  if (isCharging) {
    progressColor = COLORS.accent;
  }

  return (
    <Animated.View entering={FadeInDown.delay(300).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{t('batteryStateCard.title')}</Text>
        <Text style={[styles.percentage, { color: progressColor }]}>
          {Math.round(soc)}%
        </Text>
      </View>

      <View style={styles.progressBarContainer}>
        <View
          style={[
            styles.progressBar,
            {
              width: `${Math.min(100, Math.max(0, soc))}%`,
              backgroundColor: progressColor,
            },
          ]}
        />
      </View>

      <Text style={styles.statusText}>{statusText}</Text>
    </Animated.View>
  );
};

export default BatteryStateCard;
