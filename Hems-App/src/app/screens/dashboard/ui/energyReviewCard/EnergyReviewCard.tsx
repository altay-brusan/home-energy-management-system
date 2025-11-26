import React, { useState, useMemo, useCallback } from 'react';
import { View, Text, LayoutChangeEvent, ActivityIndicator, PanResponder, TouchableOpacity } from 'react-native';
import Svg, { Line, Circle, Path, Text as SvgText, Rect, G } from 'react-native-svg';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { useNavigation } from '@react-navigation/native';
import { useI18n } from '../../../../../hooks/useI18n';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './EnergyReviewCard.styles';
import { EnergyTransaction, EnergyReviewData, TooltipData } from './EnergyReviewCard.types';

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

// Generate mock data for 30 days with daily data points
const generateMockData = (): EnergyReviewData => {
  const transactions: EnergyTransaction[] = [];
  const now = new Date();
  const monthStart = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);

  let totalBuy = 0;
  let totalSell = 0;

  // Generate daily data for 30 days
  for (let day = 0; day < 30; day++) {
    const timestamp = new Date(monthStart.getTime() + day * 24 * 60 * 60 * 1000).getTime();

    // Realistic daily patterns:
    // - More buying in winter/cloudy days
    // - More selling in sunny days when solar produces excess
    // Add some variation to make it realistic
    const isSunnyDay = Math.random() > 0.4; // 60% sunny days

    const buy = isSunnyDay ? Math.random() * 20 + 10 : Math.random() * 40 + 30; // Less buy on sunny days
    const sell = isSunnyDay ? Math.random() * 50 + 40 : Math.random() * 15 + 5; // More sell on sunny days
    const price = 0.15 + Math.random() * 0.1; // Price per kWh between $0.15-$0.25

    totalBuy += buy;
    totalSell += sell;

    transactions.push({
      timestamp,
      buy,
      sell,
      price,
    });
  }

  const netEnergy = totalSell - totalBuy;
  const revenue = netEnergy * 0.18; // Average price

  return {
    transactions,
    weekStart: monthStart,
    weekEnd: now,
    totalBuy,
    totalSell,
    netEnergy,
    revenue,
  };
};

export default function EnergyReviewCard() {
  const navigation = useNavigation<any>();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t, language } = useI18n();
  const styles = createStyles(COLORS);

  // Get locale for date formatting based on current language
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });

  const data = useMemo(() => generateMockData(), []);

  // Set initial selected data to last day
  const [selectedData, setSelectedData] = useState<EnergyTransaction | null>(
    data.transactions[data.transactions.length - 1] || null
  );

  const onLayout = useCallback((event: LayoutChangeEvent) => {
    const { width, height } = event.nativeEvent.layout;
    setChartSize({ width, height });
  }, []);

  // Chart dimensions (fixed padding) - top increased for info box space
  const padding = useMemo(() => ({ top: 100, right: 20, bottom: 40, left: 30 }), []);
  const chartWidth = chartSize.width - padding.left - padding.right;
  const chartHeight = chartSize.height - padding.top - padding.bottom;

  // Calculate scale for X axis (time)
  // Show all 30 days on screen
  const totalDays = data.transactions.length; // 30 days
  const barWidth = Math.max(3, (chartWidth / totalDays) - 2); // Bar width with 2px gap between bars
  const xScale = chartWidth / totalDays;

  // Calculate scale for Y axis (energy) - centered at 0
  const maxValue = Math.max(
    ...data.transactions.map(t => Math.max(t.buy, t.sell))
  );
  const centerY = chartHeight / 2;
  const yScale = (chartHeight / 2) / (maxValue * 1.1); // Half height for each direction

  // Calculate which bar is at the touch position
  const getBarAtPosition = useCallback((touchX: number) => {
    // Adjust for padding
    const adjustedX = touchX - padding.left;

    // Calculate which bar index
    const barIndex = Math.floor(adjustedX / xScale);

    // Check if valid index
    if (barIndex >= 0 && barIndex < data.transactions.length) {
      return data.transactions[barIndex];
    }

    return null;
  }, [xScale, data.transactions, padding.left]);

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

  // Generate bars for chart (all 30 days visible)
  const visibleBars = useMemo(() => {
    if (chartWidth === 0 || chartHeight === 0) return [];

    return data.transactions.map((t, i) => {
      const x = i * xScale;

      // Sell bar goes up from center (positive)
      const sellHeight = t.sell * yScale;
      const sellY = centerY - sellHeight;

      // Buy bar goes down from center (negative)
      const buyHeight = t.buy * yScale;
      const buyY = centerY;

      return {
        index: i,
        x,
        sellY,
        sellHeight,
        buyY,
        buyHeight,
        transaction: t,
      };
    });
  }, [data.transactions, xScale, chartWidth, chartHeight, yScale, centerY]);

  // Generate time labels (show every 5 days for 30-day view)
  const timeLabels = useMemo(() => {
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
  }, [data.transactions, xScale, locale]);

  // Generate Y-axis labels (centered at 0)
  const yLabels = useMemo(() => {
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
  }, [maxValue, chartHeight, centerY, yScale]);

  const handleHistoryPress = () => {
    navigation.navigate('EnergyRevenueDetail');
  };

  return (
    <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <View style={{ flex: 1 }}>
          <Text style={styles.title}>{t('dashboardEnergyReview.title', 'Enerji Alış/Satış')}</Text>
          <Text style={styles.subtitle}>
            {t('dashboardEnergyReview.subtitle', 'Grid ile 30 günlük enerji ticareti')}
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
          <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.totalBuy', 'Toplam Alış')}</Text>
          <Text style={[styles.summaryCardValue, styles.buyColor]}>
            {data.totalBuy.toFixed(1)}
          </Text>
          <Text style={styles.summaryCardUnit}>kWh</Text>
        </View>

        <View style={styles.summaryCard}>
          <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.totalSell', 'Toplam Satış')}</Text>
          <Text style={[styles.summaryCardValue, styles.sellColor]}>
            {data.totalSell.toFixed(1)}
          </Text>
          <Text style={styles.summaryCardUnit}>kWh</Text>
        </View>

        <View style={styles.summaryCard}>
          <Text style={styles.summaryCardLabel}>{t('dashboardEnergyReview.net', 'Net')}</Text>
          <Text style={[styles.summaryCardValue, data.netEnergy >= 0 ? styles.sellColor : styles.buyColor]}>
            {data.netEnergy >= 0 ? '+' : ''}{data.netEnergy.toFixed(1)}
          </Text>
          <Text style={styles.summaryCardUnit}>kWh</Text>
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
