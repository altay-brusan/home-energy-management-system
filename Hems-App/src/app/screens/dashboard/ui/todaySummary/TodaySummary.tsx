import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './TodaySummary.styles';

// History Icon Component
const HistoryIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={20} height={20} viewBox="0 0 24 24" fill="none">
    <Path
      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
      stroke={color}
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </Svg>
);

interface MetricCardProps {
  label: string;
  value: string;
  unit: string;
  color: string;
}

const MetricCard: React.FC<MetricCardProps> = ({ label, value, unit, color }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = createStyles(COLORS);

  return (
    <View style={styles.metricCard}>
      <Text style={styles.metricLabel}>{label}</Text>
      <View style={styles.metricValueContainer}>
        <Text style={[styles.metricValue, { color }]}>{value}</Text>
        <Text style={styles.metricUnit}>{unit}</Text>
      </View>
    </View>
  );
};

export const TodaySummary: React.FC = () => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();

  const [loading, setLoading] = useState(false);

  // Mock data - replace with actual API call
  const metrics = {
    gridImport: { value: '12.5', unit: 'kWh' },
    gridExport: { value: '8.3', unit: 'kWh' },
    consumption: { value: '15.2', unit: 'kWh' },
    production: { value: '20.6', unit: 'kWh' },
  };

  const loadTodayData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      return;
    }

    try {
      setLoading(true);
      // TODO: Fetch today's summary data from API
      // const data = await someSummaryService.getTodaySummary(device.id);
    } catch (err) {
      console.error('Error loading today summary:', err);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  // Initial load
  useEffect(() => {
    loadTodayData();
  }, [loadTodayData]);

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(loadTodayData);
    return () => unregister();
  }, [registerRefreshCallback, loadTodayData]);

  const handleHistoryPress = () => {
    // TODO: Navigate to historical data screen or show modal
    console.log('History pressed');
  };

  if (!hasDevice) {
    return null;
  }

  return (
    <Animated.View entering={FadeInDown.delay(200)} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>{t('todaySummary.title')}</Text>
        <TouchableOpacity onPress={handleHistoryPress} style={styles.historyButton}>
          <HistoryIcon color={COLORS.accent} />
        </TouchableOpacity>
      </View>

      {/* Grid Layout - 2x2 */}
      <View style={styles.gridContainer}>
        {/* Row 1 */}
        <View style={styles.row}>
          <MetricCard
            label={t('todaySummary.gridImport')}
            value={metrics.gridImport.value}
            unit={metrics.gridImport.unit}
            color={COLORS.accent}
          />
          <MetricCard
            label={t('todaySummary.gridExport')}
            value={metrics.gridExport.value}
            unit={metrics.gridExport.unit}
            color={COLORS.success}
          />
        </View>

        {/* Row 2 */}
        <View style={styles.row}>
          <MetricCard
            label={t('todaySummary.consumption')}
            value={metrics.consumption.value}
            unit={metrics.consumption.unit}
            color={COLORS.error}
          />
          <MetricCard
            label={t('todaySummary.production')}
            value={metrics.production.value}
            unit={metrics.production.unit}
            color={COLORS.success}
          />
        </View>
      </View>
    </Animated.View>
  );
};

export default TodaySummary;
