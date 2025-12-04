import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { View, Text, LayoutChangeEvent, ActivityIndicator, PanResponder, TouchableOpacity } from 'react-native';
import Svg, { Line, Path, Text as SvgText, Rect, G } from 'react-native-svg';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { useNavigation } from '@react-navigation/native';
import { useI18n } from '../../../../../hooks/useI18n';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './EnergyReviewCard.styles';
import { EnergyTransaction, EnergyReviewData } from './EnergyReviewCard.types';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';

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

// Helper function to convert API response to component data
const transformApiDataToEnergyReviewData = (
  apiData: any
): EnergyReviewData => {
  const transactions: EnergyTransaction[] = apiData.data.map((item: any) => ({
    timestamp: new Date(item.timestamp).getTime(),
    buy: item.buy,
    sell: item.sell,
    price: item.price,
  }));

  // Calculate date range based on transactions
  const timestamps = transactions.map(t => t.timestamp);
  const weekStart = new Date(Math.min(...timestamps));
  const weekEnd = new Date(Math.max(...timestamps));

  // Use totals from API response
  const totalBuy = transactions.reduce((sum, t) => sum + t.buy, 0);
  const totalSell = transactions.reduce((sum, t) => sum + t.sell, 0);
  const netEnergy = totalSell - totalBuy;
  const totalBuyPrice = apiData.total.buyPrice;
  const totalSellPrice = apiData.total.sellPrice;
  const revenue = apiData.total.netPrice;
  const buyPricePerKWh = apiData.buyPricePerKWh;
  const sellPricePerKWh = apiData.sellPricePerKWh;

  return {
    transactions,
    weekStart,
    weekEnd,
    totalBuy,
    totalSell,
    netEnergy,
    totalBuyPrice,
    totalSellPrice,
    revenue,
    buyPricePerKWh,
    sellPricePerKWh,
  };
};

export default function EnergyReviewCard() {
  const navigation = useNavigation<any>();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t, language } = useI18n();
  const styles = createStyles(COLORS);
  const { device } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();

  // Get locale for date formatting based on current language
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<EnergyReviewData | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Set initial selected data to last day
  const [selectedData, setSelectedData] = useState<EnergyTransaction | null>(null);

  // Fetch energy revenue data
  const fetchEnergyRevenue = useCallback(async () => {
    if (!device?.id) return;

    try {
      setLoading(true);
      setError(null);

      // Get current year and month for monthly data
      const now = new Date();
      const year = now.getFullYear().toString();
      const month = now.getMonth() + 1; // JavaScript months are 0-indexed, API expects 1-12

      const response = await dashboardService.getEnergyRevenue(device.id, 'month', year, month);
      const transformedData = transformApiDataToEnergyReviewData(response);
      setData(transformedData);

      // Set initial selected data to last day
      if (transformedData.transactions.length > 0) {
        setSelectedData(transformedData.transactions[transformedData.transactions.length - 1]);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to fetch energy revenue data');
    } finally {
      setLoading(false);
    }
  }, [device?.id]);

  // Initial fetch
  useEffect(() => {
    fetchEnergyRevenue();
  }, [fetchEnergyRevenue]);

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(fetchEnergyRevenue);
    return () => {
      unregister();
    };
  }, [registerRefreshCallback, fetchEnergyRevenue]);

  const onLayout = useCallback((event: LayoutChangeEvent) => {
    const { width, height } = event.nativeEvent.layout;
    setChartSize({ width, height });
  }, []);

  // Chart dimensions (fixed padding) - top increased for info box space
  const padding = useMemo(() => ({ top: 100, right: 20, bottom: 40, left: 30 }), []);
  const chartWidth = chartSize.width - padding.left - padding.right;
  const chartHeight = chartSize.height - padding.top - padding.bottom;

  // Calculate chart parameters - memoize with data dependency
  const chartParams = useMemo(() => {
    if (!data || data.transactions.length === 0) {
      return { barWidth: 0, xScale: 0, maxValue: 0, centerY: 0, yScale: 0 };
    }

    // Calculate scale for X axis (time)
    const totalDays = data.transactions.length;
    const barWidth = Math.max(3, (chartWidth / totalDays) - 2);
    const xScale = chartWidth / totalDays;

    // Calculate scale for Y axis (energy) - centered at 0
    const maxValue = Math.max(
      ...data.transactions.map(transaction => Math.max(transaction.buy, transaction.sell))
    );
    const centerY = chartHeight / 2;
    const yScale = (chartHeight / 2) / (maxValue * 1.1);

    return { barWidth, xScale, maxValue, centerY, yScale };
  }, [data, chartWidth, chartHeight]);

  const { barWidth, xScale, maxValue, centerY, yScale } = chartParams;

  // Calculate which bar is at the touch position
  const getBarAtPosition = useCallback((touchX: number) => {
    if (!data) return null;

    // Adjust for padding
    const adjustedX = touchX - padding.left;

    // Calculate which bar index
    const barIndex = Math.floor(adjustedX / xScale);

    // Check if valid index
    if (barIndex >= 0 && barIndex < data.transactions.length) {
      return data.transactions[barIndex];
    }

    return null;
  }, [xScale, data, padding.left]);

  // Pan responder for interactive touch tracking
  const panResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => true,
    onMoveShouldSetPanResponder: () => true,
    onPanResponderGrant: (evt) => {
      const touchX = evt.nativeEvent.locationX;
      const transaction = getBarAtPosition(touchX);
      if (transaction) {
        setSelectedData(transaction);
      }
    },
    onPanResponderMove: (evt) => {
      const touchX = evt.nativeEvent.locationX;
      const transaction = getBarAtPosition(touchX);
      if (transaction) {
        setSelectedData(transaction);
      }
    },
    onPanResponderRelease: () => {
      // Keep the info box visible after release
    },
    onPanResponderTerminationRequest: () => true,
  }), [getBarAtPosition]);

  // Generate bars for chart (all days visible)
  const visibleBars = useMemo(() => {
    if (chartWidth === 0 || chartHeight === 0 || !data) return [];

    return data.transactions.map((transaction, i) => {
      const x = i * xScale;

      // Sell bar goes up from center (positive)
      const sellHeight = transaction.sell * yScale;
      const sellY = centerY - sellHeight;

      // Buy bar goes down from center (negative)
      const buyHeight = transaction.buy * yScale;
      const buyY = centerY;

      return {
        index: i,
        x,
        sellY,
        sellHeight,
        buyY,
        buyHeight,
        transaction,
      };
    });
  }, [data, xScale, chartWidth, chartHeight, yScale, centerY]);

  // Generate time labels (show every 5 days for monthly view)
  const timeLabels = useMemo(() => {
    if (!data) return [];
    const labels: { x: number; label: string }[] = [];
    for (let i = 0; i < data.transactions.length; i += 5) {
      const transaction = data.transactions[i];
      const date = new Date(transaction.timestamp);
      const x = (i + 0.5) * xScale; // Center of the bar

      const label = date.toLocaleDateString(locale, { day: '2-digit', month: '2-digit' });

      labels.push({
        x,
        label,
      });
    }
    return labels;
  }, [data, xScale, locale]);

  // Generate Y-axis labels (centered at 0)
  const yLabels = useMemo(() => {
    if (!data || maxValue === 0) return [];
    const labels: { y: number; value: string }[] = [];
    const step = maxValue / 3;

    // Positive values (sell - above center)
    for (let i = 1; i <= 3; i++) {
      const value = i * step;
      const y = centerY - value * yScale;
      if (y >= 0) {
        labels.push({
          y,
          value: `+${value.toFixed(1)}`,
        });
      }
    }

    // Zero line (center)
    labels.push({
      y: centerY,
      value: '0',
    });

    // Negative values (buy - below center)
    for (let i = 1; i <= 3; i++) {
      const value = i * step;
      const y = centerY + value * yScale;
      if (y <= chartHeight) {
        labels.push({
          y,
          value: `-${value.toFixed(1)}`,
        });
      }
    }

    return labels;
  }, [data, maxValue, chartHeight, centerY, yScale]);

  const handleHistoryPress = () => {
    navigation.navigate('EnergyRevenueDetail');
  };

  // Show loading state
  if (loading || !data) {
    return (
      <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
        <View style={styles.header}>
          <View style={{ flex: 1 }}>
            <Text style={styles.title}>{t('dashboardEnergyReview.title', 'Energy Revenue')}</Text>
            <Text style={styles.subtitle}>
              {t('dashboardEnergyReview.subtitle', 'Monthly grid energy trading costs and revenue')}
            </Text>
          </View>
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      </Animated.View>
    );
  }

  // Show error state
  if (error) {
    return (
      <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
        <View style={styles.header}>
          <View style={{ flex: 1 }}>
            <Text style={styles.title}>{t('dashboardEnergyReview.title', 'Energy Revenue')}</Text>
            <Text style={styles.subtitle}>
              {t('dashboardEnergyReview.subtitle', 'Monthly grid energy trading costs and revenue')}
            </Text>
          </View>
        </View>
        <View style={styles.loadingContainer}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
            {error}
          </Text>
        </View>
      </Animated.View>
    );
  }

  return (
    <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <View style={{ flex: 1 }}>
          <Text style={styles.title}>{t('dashboardEnergyReview.title', 'Energy Revenue')}</Text>
          <Text style={styles.subtitle}>
            {t('dashboardEnergyReview.subtitle', 'Monthly grid energy trading costs and revenue')}
          </Text>
          <Text style={styles.priceInfo}>
            {t('dashboardEnergyReview.buyPrice', 'Buy')}: {data.buyPricePerKWh.toFixed(2)} €/kWh • {t('dashboardEnergyReview.sellPrice', 'Sell')}: {data.sellPricePerKWh.toFixed(2)} €/kWh
          </Text>
        </View>
        <TouchableOpacity onPress={handleHistoryPress} style={{ padding: 4 }}>
          <HistoryIcon color={COLORS.accent} />
        </TouchableOpacity>
      </View>

      {/* Chart Wrapper - for absolute positioning of info box */}
      <View style={styles.chartWrapper}>
        {/* Info Box - Outside chart container but above it */}
        {selectedData && (
          <View style={styles.infoBox}>
            <Text style={styles.infoDate}>
              {new Date(selectedData.timestamp).toLocaleDateString(locale, {
                day: '2-digit',
                month: 'long',
                year: 'numeric',
              })}
            </Text>

            <View style={styles.infoGrid}>
              <View style={styles.infoItem}>
                <Text style={styles.infoLabel}>{t('dashboardEnergyReview.buy', 'Alış')}</Text>
                <Text style={[styles.infoValue, styles.buyColor]}>
                  {selectedData.buy.toFixed(1)} kWh
                </Text>
              </View>

              <View style={styles.infoItem}>
                <Text style={styles.infoLabel}>{t('dashboardEnergyReview.sell', 'Satış')}</Text>
                <Text style={[styles.infoValue, styles.sellColor]}>
                  {selectedData.sell.toFixed(1)} kWh
                </Text>
              </View>

              <View style={styles.infoItem}>
                <Text style={styles.infoLabel}>{t('dashboardEnergyReview.net', 'Net')}</Text>
                <Text style={[styles.infoValue, (selectedData.sell - selectedData.buy) >= 0 ? styles.sellColor : styles.buyColor]}>
                  {(selectedData.sell - selectedData.buy) >= 0 ? '+' : ''}{(selectedData.sell - selectedData.buy).toFixed(1)} kWh
                </Text>
              </View>

              <View style={styles.infoItem}>
                <Text style={styles.infoLabel}>{t('dashboardEnergyReview.price', 'Fiyat')}</Text>
                <Text style={styles.infoValue}>
                  ${(selectedData.price * Math.abs(selectedData.sell - selectedData.buy)).toFixed(2)}
                </Text>
              </View>
            </View>
          </View>
        )}

        {/* Chart */}
        <View style={styles.chartContainer} onLayout={onLayout} {...panResponder.panHandlers}>
          {chartSize.width === 0 || chartWidth <= 0 || chartHeight <= 0 ? (
            <View style={styles.loadingContainer}>
              <ActivityIndicator size="large" color={COLORS.accent} />
            </View>
          ) : (
            <Svg width={chartSize.width} height={chartSize.height}>
            <G x={padding.left} y={padding.top}>
              {/* Grid lines */}
              {yLabels.map((label, i) => (
                <Line
                  key={`grid-${i}`}
                  x1={0}
                  y1={label.y}
                  x2={chartWidth}
                  y2={label.y}
                  stroke={label.value === '0' ? COLORS.textSoft : COLORS.border}
                  strokeWidth={label.value === '0' ? 2 : 1}
                  strokeDasharray={label.value === '0' ? '0' : '4,4'}
                  opacity={label.value === '0' ? 0.4 : 0.2}
                />
              ))}

              {/* Y-axis labels */}
              {yLabels.map((label, i) => (
                <SvgText
                  key={`ylabel-${i}`}
                  x={-5}
                  y={label.y}
                  fontSize={9}
                  fill={COLORS.textSoft}
                  opacity={label.value === '0' ? 0.8 : 0.6}
                  fontWeight={label.value === '0' ? '700' : '400'}
                  textAnchor="end"
                  alignmentBaseline="middle"
                >
                  {label.value}
                </SvgText>
              ))}

              {/* Bar Chart - Sell (positive/up) and Buy (negative/down) */}
              {visibleBars.map((bar: any, idx: number) => {
                if (!bar) return null;

                return (
                  <G key={`bar-${bar.index}-${idx}`}>
                    {/* Sell bar (green, upward) */}
                    {bar.sellHeight > 0 && (
                      <Rect
                        x={bar.x}
                        y={bar.sellY}
                        width={barWidth}
                        height={bar.sellHeight}
                        fill="#10b981"
                        opacity={0.85}
                      />
                    )}

                    {/* Buy bar (red, downward) */}
                    {bar.buyHeight > 0 && (
                      <Rect
                        x={bar.x}
                        y={bar.buyY}
                        width={barWidth}
                        height={bar.buyHeight}
                        fill="#ef4444"
                        opacity={0.85}
                      />
                    )}
                  </G>
                );
              })}

              {/* X-axis labels */}
              {timeLabels.map((label, i) => (
                <SvgText
                  key={`xlabel-${i}`}
                  x={label.x}
                  y={chartHeight + 20}
                  fontSize={10}
                  fill={COLORS.textSoft}
                  opacity={0.6}
                  textAnchor="middle"
                >
                  {label.label}
                </SvgText>
              ))}

              {/* Transparent overlay for touch */}
              <Rect
                x={0}
                y={0}
                width={chartWidth}
                height={chartHeight}
                fill="transparent"
              />
            </G>
          </Svg>
        )}
        </View>
      </View>

      {/* Summary Cards */}
      <View style={styles.summaryCardsContainer}>
        <View style={styles.summaryCard}>
          <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.totalBuy', 'Total Buy')}</Text>
          <Text style={[styles.summaryCardValue, styles.buyColor]}>
            {data.totalBuyPrice.toFixed(2)} €
          </Text>
        </View>

        <View style={styles.summaryCard}>
          <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.totalSell', 'Total Sell')}</Text>
          <Text style={[styles.summaryCardValue, styles.sellColor]}>
            {data.totalSellPrice.toFixed(2)} €
          </Text>
        </View>

        <View style={styles.summaryCard}>
          <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.net', 'Net')}</Text>
          <Text style={[styles.summaryCardValue, data.revenue >= 0 ? styles.sellColor : styles.buyColor]}>
            {data.revenue >= 0 ? '+' : ''}{data.revenue.toFixed(2)} €
          </Text>
        </View>
      </View>

      {/* Legend */}
      <View style={styles.legend}>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: '#ef4444' }]} />
          <Text style={styles.legendLabel}>{t('dashboardEnergyReview.gridBuy', 'Grid\'den Alış')}</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: '#10b981' }]} />
          <Text style={styles.legendLabel}>{t('dashboardEnergyReview.gridSell', 'Grid\'e Satış')}</Text>
        </View>
      </View>
    </Animated.View>
  );
}
