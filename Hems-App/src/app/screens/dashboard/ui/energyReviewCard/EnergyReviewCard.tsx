import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { View, Text, LayoutChangeEvent, ActivityIndicator, PanResponder, TouchableOpacity, StyleSheet } from 'react-native';
import Svg, { Line, Path, Text as SvgText, Rect, G, Circle } from 'react-native-svg';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { useNavigation } from '@react-navigation/native';
import { BlurView } from '@react-native-community/blur';
import { useI18n } from '../../../../../hooks/useI18n';
import { useThemeStore } from '../../../../../store/themeStore';
import { useSettingsStore } from '../../../../../store/settingsStore';
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

// Currency symbols mapping
const CURRENCY_SYMBOLS: Record<string, string> = {
  EUR: '€',
  USD: '$',
  GBP: '£',
  TRY: '₺',
};

// Helper function to get currency symbol
const getCurrencySymbol = (currency: string): string => {
  return CURRENCY_SYMBOLS[currency?.toUpperCase()] || currency || '€';
};

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
  const currency = apiData.currency || 'EUR';

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
    currency,
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
  const hasPricingData = useSettingsStore(s => s.hasPricingData);

  // Get locale for date formatting based on current language
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<EnergyReviewData | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Set initial selected data to last day
  const [selectedData, setSelectedData] = useState<EnergyTransaction | null>(null);
  // Track touch position for vertical line indicator
  const [touchX, setTouchX] = useState<number | null>(null);

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

  // Chart dimensions (fixed padding)
  const padding = useMemo(() => ({ top: 20, right: 20, bottom: 40, left: 30 }), []);
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
  const getBarAtPosition = useCallback((touchPosition: number) => {
    if (!data) return null;

    // Adjust for padding
    const adjustedX = touchPosition - padding.left;

    // Calculate which bar index
    const barIndex = Math.floor(adjustedX / xScale);

    // Check if valid index
    if (barIndex >= 0 && barIndex < data.transactions.length) {
      // Calculate bar center X position for snapping the vertical line
      const barCenterX = padding.left + (barIndex * xScale) + (xScale / 2);
      return {
        transaction: data.transactions[barIndex],
        centerX: barCenterX,
        index: barIndex,
      };
    }

    return null;
  }, [xScale, data, padding.left]);

  // Pan responder for interactive touch tracking
  const panResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => true,
    onMoveShouldSetPanResponder: () => true,
    onPanResponderGrant: (evt) => {
      const touchPosition = evt.nativeEvent.locationX;
      const barData = getBarAtPosition(touchPosition);
      if (barData) {
        setSelectedData(barData.transaction);
        setTouchX(barData.centerX);
      }
    },
    onPanResponderMove: (evt) => {
      const touchPosition = evt.nativeEvent.locationX;
      const barData = getBarAtPosition(touchPosition);
      if (barData) {
        setSelectedData(barData.transaction);
        setTouchX(barData.centerX);
      }
    },
    onPanResponderRelease: () => {
      // Clear the touch indicator when finger is released
      setTouchX(null);
      // Keep the info box visible with last selected data
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
        </View>
        <TouchableOpacity onPress={handleHistoryPress} style={{ padding: 4 }} disabled={!hasPricingData}>
          <HistoryIcon color={hasPricingData ? COLORS.accent : COLORS.textSoft} />
        </TouchableOpacity>
      </View>

      {/* Content wrapper for blur overlay */}
      <View style={{ position: 'relative' }}>
        {/* Price Cards */}
        <View style={styles.priceCardsContainer}>
          <View style={styles.priceCard}>
            <View style={[styles.priceCardDot, { backgroundColor: '#ef4444' }]} />
            <Text style={styles.priceCardLabel}>{t('dashboardEnergyReview.buyPrice', 'Buy')}</Text>
            <Text style={[styles.priceCardValue, styles.buyColor]}>{data.buyPricePerKWh.toFixed(2)} {getCurrencySymbol(data.currency)}/kWh</Text>
          </View>
          <View style={styles.priceCard}>
            <View style={[styles.priceCardDot, { backgroundColor: '#10b981' }]} />
            <Text style={styles.priceCardLabel}>{t('dashboardEnergyReview.sellPrice', 'Sell')}</Text>
            <Text style={[styles.priceCardValue, styles.sellColor]}>{data.sellPricePerKWh.toFixed(2)} {getCurrencySymbol(data.currency)}/kWh</Text>
          </View>
        </View>

        {/* Chart Wrapper - for absolute positioning of info box */}
        <View style={styles.chartWrapper}>
        {/* Floating Info Box - Shows when touching the chart */}
        {touchX !== null && selectedData && (
          <View style={{
            position: 'absolute',
            top: -85,
            left: Math.max(10, Math.min(touchX - 100, chartSize.width - 210)),
            backgroundColor: COLORS.cardBg,
            borderRadius: 12,
            padding: 12,
            shadowColor: '#000',
            shadowOffset: { width: 0, height: 2 },
            shadowOpacity: 0.25,
            shadowRadius: 3.84,
            elevation: 5,
            minWidth: 180,
            zIndex: 100,
          }}>
            {/* Date */}
            <Text style={{ color: COLORS.textSoft, fontSize: 13, fontWeight: '700', marginBottom: 8 }}>
              {new Date(selectedData.timestamp).toLocaleDateString(locale, {
                day: '2-digit',
                month: 'short',
              })}
            </Text>

            {/* Buy Row */}
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
                <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: '#ef4444' }} />
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>
                  {t('dashboardEnergyReview.buy', 'Buy')}
                </Text>
              </View>
              <Text style={{ color: '#ef4444', fontSize: 12, fontWeight: '700' }}>
                {selectedData.buy.toFixed(1)} kWh
              </Text>
            </View>

            {/* Sell Row */}
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
                <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: '#10b981' }} />
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>
                  {t('dashboardEnergyReview.sell', 'Sell')}
                </Text>
              </View>
              <Text style={{ color: '#10b981', fontSize: 12, fontWeight: '700' }}>
                {selectedData.sell.toFixed(1)} kWh
              </Text>
            </View>

            {/* Net Row */}
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
              <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>
                {t('dashboardEnergyReview.net', 'Net')}
              </Text>
              <Text style={{ color: (selectedData.sell - selectedData.buy) >= 0 ? '#10b981' : '#ef4444', fontSize: 12, fontWeight: '700' }}>
                {(selectedData.sell - selectedData.buy) >= 0 ? '+' : ''}{(selectedData.sell - selectedData.buy).toFixed(1)} kWh
              </Text>
            </View>

            {/* Price Row */}
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
              <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>
                {t('dashboardEnergyReview.price', 'Tutar')}
              </Text>
              <Text style={{ color: selectedData.price >= 0 ? '#10b981' : '#ef4444', fontSize: 12, fontWeight: '700' }}>
                {selectedData.price >= 0 ? '+' : ''}{getCurrencySymbol(data.currency)}{selectedData.price.toFixed(2)}
              </Text>
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
            <G transform={`translate(${padding.left}, ${padding.top})`}>
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

              {/* Vertical line indicator when touching */}
              {touchX !== null && (
                <>
                  {/* Dashed vertical line */}
                  <Line
                    x1={touchX - padding.left}
                    y1={0}
                    x2={touchX - padding.left}
                    y2={chartHeight}
                    stroke={COLORS.accent}
                    strokeWidth={1}
                    strokeDasharray="4,3"
                    opacity={0.7}
                  />
                  {/* Circle indicator at center */}
                  <Circle
                    cx={touchX - padding.left}
                    cy={centerY}
                    r={4}
                    fill={COLORS.accent}
                    opacity={0.9}
                  />
                  {/* Outer circle ring */}
                  <Circle
                    cx={touchX - padding.left}
                    cy={centerY}
                    r={7}
                    fill="transparent"
                    stroke={COLORS.accent}
                    strokeWidth={1.5}
                    opacity={0.4}
                  />
                </>
              )}

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
              {data.totalBuyPrice.toFixed(2)} {getCurrencySymbol(data.currency)}
            </Text>
          </View>

          <View style={styles.summaryCard}>
            <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.totalSell', 'Total Sell')}</Text>
            <Text style={[styles.summaryCardValue, styles.sellColor]}>
              {data.totalSellPrice.toFixed(2)} {getCurrencySymbol(data.currency)}
            </Text>
          </View>

          <View style={styles.summaryCard}>
            <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.net', 'Net')}</Text>
            <Text style={[styles.summaryCardValue, data.revenue >= 0 ? styles.sellColor : styles.buyColor]}>
              {data.revenue >= 0 ? '+' : ''}{data.revenue.toFixed(2)} {getCurrencySymbol(data.currency)}
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

        {/* Blur overlay when no pricing data is set - only covers content, not header */}
        {!hasPricingData && (
          <View style={blurOverlayStyles.overlay}>
            <BlurView
              style={StyleSheet.absoluteFill}
              blurType={isDark ? 'dark' : 'light'}
              blurAmount={8}
              reducedTransparencyFallbackColor={isDark ? '#1a1a1a' : '#ffffff'}
            />
            <TouchableOpacity
              style={blurOverlayStyles.promptContainer}
              onPress={() => navigation.navigate('PricingSettings')}
              activeOpacity={0.8}
            >
              <Text style={[blurOverlayStyles.promptText, { color: COLORS.accent }]}>
                {t('dashboard.setPricingPrompt', 'Please enter energy prices')}
              </Text>
            </TouchableOpacity>
          </View>
        )}
      </View>
    </Animated.View>
  );
}

const blurOverlayStyles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    borderRadius: 16,
    overflow: 'hidden',
    justifyContent: 'center',
    alignItems: 'center',
  },
  promptContainer: {
    backgroundColor: 'rgba(255, 255, 255, 0.9)',
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderRadius: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.15,
    shadowRadius: 4,
    elevation: 3,
  },
  promptText: {
    fontSize: 15,
    fontWeight: '600',
    textAlign: 'center',
  },
});
