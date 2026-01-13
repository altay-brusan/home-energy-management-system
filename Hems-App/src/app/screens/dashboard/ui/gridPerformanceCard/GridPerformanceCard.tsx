import React, { useState, useEffect, useCallback } from 'react';
import { View, Text, ActivityIndicator, TouchableOpacity } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import Svg, { Path } from 'react-native-svg';
import { useNavigation } from '@react-navigation/native';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { gridPerformanceService } from '../../../../../api/services/gridPerformanceService';
import { GridPeriod } from '../../../../../api/types/gridPerformance.types';
import { createStyles } from './GridPerformanceCard.styles';

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

interface GridPerformanceCardProps {
  deviceId: string | undefined;
}

interface PeriodData {
  buy: number;
  sell: number;
}

const GridPerformanceCard: React.FC<GridPerformanceCardProps> = ({ deviceId }) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const navigation = useNavigation();
  const styles = createStyles(COLORS);

  // State
  const [loading, setLoading] = useState(true);
  const [monthData, setMonthData] = useState<PeriodData>({ buy: 0, sell: 0 });
  const [yearData, setYearData] = useState<PeriodData>({ buy: 0, sell: 0 });
  const [lifetimeData, setLifetimeData] = useState<PeriodData>({ buy: 0, sell: 0 });

  // Fetch all periods data
  const fetchData = useCallback(async () => {
    if (!deviceId) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);

      // Fetch summary data from the new API endpoint
      const summaryResponse = await gridPerformanceService.getGridPerformanceSummary(deviceId);

      setMonthData({
        buy: summaryResponse.month.gridImport,
        sell: summaryResponse.month.gridExport,
      });

      setYearData({
        buy: summaryResponse.year.gridImport,
        sell: summaryResponse.year.gridExport,
      });

      setLifetimeData({
        buy: summaryResponse.lifetime.gridImport,
        sell: summaryResponse.lifetime.gridExport,
      });
    } catch (err: any) {
      console.error('Error fetching grid performance summary:', err);
      // Hata durumunda varsayılan değerler
      setMonthData({ buy: 0, sell: 0 });
      setYearData({ buy: 0, sell: 0 });
      setLifetimeData({ buy: 0, sell: 0 });
    } finally {
      setLoading(false);
    }
  }, [deviceId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Register dashboard refresh
  const { registerRefreshCallback } = useDashboardRefresh();

  useEffect(() => {
    return registerRefreshCallback(fetchData);
  }, [registerRefreshCallback, fetchData]);

  // Format number for display
  const formatNumber = (value: number): string => {
    if (Math.abs(value) >= 1000) {
      return `${(value / 1000).toFixed(1)}k`;
    }
    return value.toFixed(1);
  };

  const handleHistoryPress = () => {
    navigation.navigate('GridPerformanceDetail' as never);
  };

  if (!deviceId) {
    return null;
  }

  return (
    <Animated.View entering={FadeInDown.duration(600).delay(200)} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>{t('dashboardGridPerformance.title')}</Text>
        <TouchableOpacity onPress={handleHistoryPress} style={styles.historyButton}>
          <HistoryIcon color={COLORS.accent} />
        </TouchableOpacity>
      </View>

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      ) : (
        <View style={styles.gridContainer}>
          {/* Row 1: Buy (Month, Year, Lifetime) */}
          <View style={styles.row}>
            {/* Month Buy */}
            <View style={styles.metricCard}>
              <Text style={styles.metricLabel}>{t('dashboardEnergyReview.buy')}</Text>
              <View style={styles.metricValueContainer}>
                <Text style={[styles.metricValue, styles.buyColor]}>
                  {formatNumber(monthData.buy)}
                </Text>
                <Text style={styles.metricUnit}>kWh</Text>
              </View>
            </View>

            {/* Year Buy */}
            <View style={styles.metricCard}>
              <Text style={styles.metricLabel}>{t('dashboardEnergyReview.buy')}</Text>
              <View style={styles.metricValueContainer}>
                <Text style={[styles.metricValue, styles.buyColor]}>
                  {formatNumber(yearData.buy)}
                </Text>
                <Text style={styles.metricUnit}>kWh</Text>
              </View>
            </View>

            {/* Lifetime Buy */}
            <View style={styles.metricCard}>
              <Text style={styles.metricLabel}>{t('dashboardEnergyReview.buy')}</Text>
              <View style={styles.metricValueContainer}>
                <Text style={[styles.metricValue, styles.buyColor]}>
                  {formatNumber(lifetimeData.buy)}
                </Text>
                <Text style={styles.metricUnit}>kWh</Text>
              </View>
            </View>
          </View>

          {/* Row 2: Sell (Month, Year, Lifetime) */}
          <View style={styles.row}>
            {/* Month Sell */}
            <View style={styles.metricCard}>
              <Text style={styles.metricLabel}>{t('dashboardEnergyReview.sell')}</Text>
              <View style={styles.metricValueContainer}>
                <Text style={[styles.metricValue, styles.sellColor]}>
                  {formatNumber(monthData.sell)}
                </Text>
                <Text style={styles.metricUnit}>kWh</Text>
              </View>
            </View>

            {/* Year Sell */}
            <View style={styles.metricCard}>
              <Text style={styles.metricLabel}>{t('dashboardEnergyReview.sell')}</Text>
              <View style={styles.metricValueContainer}>
                <Text style={[styles.metricValue, styles.sellColor]}>
                  {formatNumber(yearData.sell)}
                </Text>
                <Text style={styles.metricUnit}>kWh</Text>
              </View>
            </View>

            {/* Lifetime Sell */}
            <View style={styles.metricCard}>
              <Text style={styles.metricLabel}>{t('dashboardEnergyReview.sell')}</Text>
              <View style={styles.metricValueContainer}>
                <Text style={[styles.metricValue, styles.sellColor]}>
                  {formatNumber(lifetimeData.sell)}
                </Text>
                <Text style={styles.metricUnit}>kWh</Text>
              </View>
            </View>
          </View>

          {/* Column Headers - Bottom */}
          <View style={styles.columnHeadersRow}>
            <View style={styles.columnHeader}>
              <Text style={styles.columnHeaderText}>{t('dashboardGridPerformance.month')}</Text>
            </View>
            <View style={styles.columnHeader}>
              <Text style={styles.columnHeaderText}>{t('dashboardGridPerformance.year')}</Text>
            </View>
            <View style={styles.columnHeader}>
              <Text style={styles.columnHeaderText}>{t('dashboardGridPerformance.lifetime')}</Text>
            </View>
          </View>
        </View>
      )}
    </Animated.View>
  );
};

export default GridPerformanceCard;
