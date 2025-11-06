import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService } from '../../../../../api/services/mobileDashboardService';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './CO2Card.styles';
import { CO2CardProps } from './CO2Card.types';

const CO2Card: React.FC<CO2CardProps> = ({ deviceId }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const [loading, setLoading] = useState(true);
  const [co2Saved, setCo2Saved] = useState<number>(0);
  const [treesEquivalent, setTreesEquivalent] = useState<number>(0);

  const fetchCO2Data = useCallback(async () => {
    if (!deviceId) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);

      // Fetch battery component to get total load (lifetime)
      const batteryComponent = await mobileDashboardService.getBatteryComponent(deviceId);

      // Formula 1: P = Total Load (lifetime) × 0.25
      const totalLoad = batteryComponent.total || 0;
      const co2SavedKg = totalLoad * 0.25;

      // Formula 2: Trees = CO2 / 22 (based on 45/22 from requirements)
      const trees = co2SavedKg / 22;

      setCo2Saved(Math.round(co2SavedKg * 100) / 100); // Round to 2 decimals
      setTreesEquivalent(Math.round(trees * 10) / 10); // Round to 1 decimal
    } catch (error) {
      console.error('Error fetching CO2 data:', error);
      setCo2Saved(0);
      setTreesEquivalent(0);
    } finally {
      setLoading(false);
    }
  }, [deviceId]);

  const { registerRefreshCallback } = useDashboardRefresh();

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(fetchCO2Data);
    return () => {
      unregister();
    };
  }, [registerRefreshCallback, fetchCO2Data]);

  useEffect(() => {
    fetchCO2Data();
  }, [fetchCO2Data]);

  if (loading) {
    return (
      <Animated.View entering={FadeInDown.delay(400).duration(600)} style={styles.container}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      </Animated.View>
    );
  }

  if (!deviceId) return null;

  return (
    <Animated.View entering={FadeInDown.delay(400).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>CO₂ Emission Savings</Text>
        <Text style={styles.subtitle}>Your environmental impact</Text>
      </View>

      <View style={styles.metricsContainer}>
        <View style={styles.metricBox}>
          <Text style={styles.metricIcon}>🌍</Text>
          <Text style={styles.metricValue}>{co2Saved.toLocaleString()}</Text>
          <Text style={styles.metricLabel}>kg CO₂ Saved</Text>
        </View>

        <View style={styles.metricBox}>
          <Text style={styles.metricIcon}>🌳</Text>
          <Text style={styles.metricValue}>{treesEquivalent.toLocaleString()}</Text>
          <Text style={styles.metricLabel}>Trees Equivalent</Text>
        </View>
      </View>
    </Animated.View>
  );
};

export default CO2Card;
