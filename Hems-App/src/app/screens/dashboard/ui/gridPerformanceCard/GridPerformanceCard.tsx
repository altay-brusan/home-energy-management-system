import React, { useState, useEffect, useCallback } from 'react';
import { View, Text, ActivityIndicator, TouchableOpacity } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import Svg, { Path } from 'react-native-svg';
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

      // Fetch all 3 periods in parallel
      const [monthResponse, yearResponse, lifetimeResponse] = await Promise.all([
        gridPerformanceService.getGridPerformance(deviceId, 'monthly'),
        gridPerformanceService.getGridPerformance(deviceId, 'yearly'),
        gridPerformanceService.getGridPerformance(deviceId, 'lifetime'),
      ]);

      setMonthData({
        buy: monthResponse.statistics.totalBuy,
        sell: monthResponse.statistics.totalSell,
      });

      setYearData({
        buy: yearResponse.statistics.totalBuy,
        sell: yearResponse.statistics.totalSell,
      });

      setLifetimeData({
        buy: lifetimeResponse.statistics.totalBuy,
        sell: lifetimeResponse.statistics.totalSell,
      });
    } catch (err: any) {
      console.error('Error fetching grid performance:', err);
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
    // TODO: Navigate to historical data screen or show modal
    console.log('History pressed - GridPerformanceCard');
  };

  if (!deviceId) {
    return null;
  }

  return (
    <Animated.View entering={FadeInDown.duration(600).delay(200)} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>{t('dashboardGridPerformance.title')}</Text>
        <TouchableOpacity onPress={handleHistoryPress} style={{ padding: 4 }}>
          <HistoryIcon color={COLORS.accent} />
        </TouchableOpacity>
      </View>

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      ) : (
        <View style={styles.columnsContainer}>
          {/* Month Column */}
          <View style={styles.column}>
            <View style={styles.valueBox}>
              <Text style={styles.valueLabel}>{t('dashboardEnergyReview.buy')}</Text>
              <Text style={[styles.valueNumber, styles.buyColor]}>
                {formatNumber(monthData.buy)}
              </Text>
              <Text style={styles.valueUnit}>kWh</Text>
            </View>

            <View style={styles.divider} />

            <View style={styles.valueBox}>
              <Text style={styles.valueLabel}>{t('dashboardEnergyReview.sell')}</Text>
              <Text style={[styles.valueNumber, styles.sellColor]}>
                {formatNumber(monthData.sell)}
              </Text>
              <Text style={styles.valueUnit}>kWh</Text>
            </View>

            <Text style={styles.columnLabel}>{t('dashboardGridPerformance.month')}</Text>
          </View>

          {/* Year Column */}
          <View style={styles.column}>
            <View style={styles.valueBox}>
              <Text style={styles.valueLabel}>{t('dashboardEnergyReview.buy')}</Text>
              <Text style={[styles.valueNumber, styles.buyColor]}>
                {formatNumber(yearData.buy)}
              </Text>
              <Text style={styles.valueUnit}>kWh</Text>
            </View>

            <View style={styles.divider} />

            <View style={styles.valueBox}>
              <Text style={styles.valueLabel}>{t('dashboardEnergyReview.sell')}</Text>
              <Text style={[styles.valueNumber, styles.sellColor]}>
                {formatNumber(yearData.sell)}
              </Text>
              <Text style={styles.valueUnit}>kWh</Text>
            </View>

            <Text style={styles.columnLabel}>{t('dashboardGridPerformance.year')}</Text>
          </View>

          {/* Lifetime Column */}
          <View style={styles.column}>
            <View style={styles.valueBox}>
              <Text style={styles.valueLabel}>{t('dashboardEnergyReview.buy')}</Text>
              <Text style={[styles.valueNumber, styles.buyColor]}>
                {formatNumber(lifetimeData.buy)}
              </Text>
              <Text style={styles.valueUnit}>kWh</Text>
            </View>

            <View style={styles.divider} />

            <View style={styles.valueBox}>
              <Text style={styles.valueLabel}>{t('dashboardEnergyReview.sell')}</Text>
              <Text style={[styles.valueNumber, styles.sellColor]}>
                {formatNumber(lifetimeData.sell)}
              </Text>
              <Text style={styles.valueUnit}>kWh</Text>
            </View>

            <Text style={styles.columnLabel}>{t('dashboardGridPerformance.lifetime')}</Text>
          </View>
        </View>
      )}
    </Animated.View>
  );
};

export default GridPerformanceCard;
