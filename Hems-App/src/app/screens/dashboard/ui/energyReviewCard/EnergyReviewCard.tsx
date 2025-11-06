import React, { useState, useMemo, useCallback } from 'react';
import { View, Text, LayoutChangeEvent, ActivityIndicator, PanResponder } from 'react-native';
import Svg, { Line, Circle, Path, Text as SvgText, Rect, G } from 'react-native-svg';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './EnergyReviewCard.styles';
import { EnergyTransaction, EnergyReviewData, TooltipData } from './EnergyReviewCard.types';

// Generate mock data for 1 week (7 days) with hourly data points
const generateMockData = (): EnergyReviewData => {
  const transactions: EnergyTransaction[] = [];
  const now = new Date();
  const weekStart = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);

  let totalBuy = 0;
  let totalSell = 0;

  // Generate hourly data for 7 days (168 hours)
  for (let day = 0; day < 7; day++) {
    for (let hour = 0; hour < 24; hour++) {
      const timestamp = new Date(weekStart.getTime() + day * 24 * 60 * 60 * 1000 + hour * 60 * 60 * 1000).getTime();

      // Realistic patterns:
      // - More buying at night (18:00-06:00) when solar is not producing
      // - More selling during day (09:00-17:00) when solar produces excess
      const isBuyTime = hour >= 18 || hour <= 6;

      const buy = isBuyTime ? Math.random() * 5 + 2 : Math.random() * 1;
      const sell = !isBuyTime ? Math.random() * 6 + 3 : Math.random() * 0.5;
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
  }

  const netEnergy = totalSell - totalBuy;
  const revenue = netEnergy * 0.18; // Average price

  return {
    transactions,
    weekStart,
    weekEnd: now,
    totalBuy,
    totalSell,
    netEnergy,
    revenue,
  };
};

export default function EnergyReviewCard() {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [offsetX, setOffsetX] = useState(0);
  const [startOffsetX, setStartOffsetX] = useState(0);
  const [tooltip, setTooltip] = useState<TooltipData>({
    x: 0,
    y: 0,
    transaction: { timestamp: 0, buy: 0, sell: 0, price: 0 },
    visible: false,
  });

  const data = useMemo(() => generateMockData(), []);

  const onLayout = useCallback((event: LayoutChangeEvent) => {
    const { width, height } = event.nativeEvent.layout;
    setChartSize({ width, height });
  }, []);

  // Chart dimensions (fixed padding)
  const padding = useMemo(() => ({ top: 30, right: 20, bottom: 60, left: 30 }), []);
  const chartWidth = chartSize.width - padding.left - padding.right;
  const chartHeight = chartSize.height - padding.top - padding.bottom;

  // Calculate scale for X axis (time)
  // Show 24 hours at a time (1 day visible on screen)
  const hoursVisible = 24;
  const totalHours = data.transactions.length;
  const barWidth = Math.max(4, chartWidth / hoursVisible - 1); // Bar width with 1px gap
  const xScale = chartWidth / hoursVisible;
  const totalChartWidth = totalHours * xScale;
  const maxOffsetX = Math.max(0, totalChartWidth - chartWidth);

  // Calculate scale for Y axis (energy) - centered at 0
  const maxValue = Math.max(
    ...data.transactions.map(t => Math.max(t.buy, t.sell))
  );
  const centerY = chartHeight / 2;
  const yScale = (chartHeight / 2) / (maxValue * 1.1); // Half height for each direction

  // Pan responder for horizontal scrolling
  const panResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => false,
    onMoveShouldSetPanResponder: (evt, gestureState) => {
      // Only activate pan if horizontal movement is significant
      // This prevents interfering with vertical scrolling
      const isHorizontal = Math.abs(gestureState.dx) > Math.abs(gestureState.dy);
      const hasMovedEnough = Math.abs(gestureState.dx) > 5;
      return isHorizontal && hasMovedEnough;
    },
    onPanResponderGrant: (evt, gestureState) => {
      setStartOffsetX(offsetX);
      setTooltip(prev => ({ ...prev, visible: false }));
    },
    onPanResponderMove: (evt, gestureState) => {
      // Update offset based on drag distance
      const newOffset = Math.max(0, Math.min(maxOffsetX, startOffsetX - gestureState.dx));
      setOffsetX(newOffset);
    },
    onPanResponderRelease: () => {
      // Optional: Add momentum/snap behavior here if needed
    },
    onPanResponderTerminationRequest: () => true,
    onShouldBlockNativeResponder: () => false,
  }), [offsetX, startOffsetX, maxOffsetX]);

  // Generate bars for chart
  const visibleBars = useMemo(() => {
    if (chartWidth === 0 || chartHeight === 0) return [];

    return data.transactions.map((t, i) => {
      const x = i * xScale - offsetX;

      // Only render bars that are visible on screen
      if (x < -barWidth || x > chartWidth + barWidth) {
        return null;
      }

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
    }).filter(Boolean);
  }, [data.transactions, xScale, offsetX, chartWidth, chartHeight, barWidth, yScale, centerY]);

  // Generate time labels (show every 6 hours for 24h view)
  const timeLabels = useMemo(() => {
    const labels: { x: number; label: string }[] = [];
    for (let i = 0; i < data.transactions.length; i += 6) {
      const transaction = data.transactions[i];
      const date = new Date(transaction.timestamp);
      const x = i * xScale - offsetX;

      if (x >= -50 && x <= chartWidth + 50) {
        // Show time for current day, date for day boundaries
        const hour = date.getHours();
        const label = hour === 0
          ? date.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' })
          : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });

        labels.push({
          x,
          label,
        });
      }
    }
    return labels;
  }, [data.transactions, xScale, offsetX, chartWidth]);

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

  return (
    <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{t('dashboardEnergyReview.title', 'Enerji Alış/Satış')}</Text>
        <Text style={styles.subtitle}>
          {t('dashboardEnergyReview.subtitle', 'Grid ile 1 haftalık enerji ticareti')}
        </Text>
      </View>

      {/* Summary */}
      <View style={styles.summaryContainer}>
        <View style={styles.summaryItem}>
          <Text style={styles.summaryLabel}>{t('dashboardEnergyReview.totalBuy', 'Toplam Alış')}</Text>
          <Text style={[styles.summaryValue, styles.buyColor]}>
            {data.totalBuy.toFixed(1)}
          </Text>
          <Text style={[styles.summaryLabel, { fontSize: 9 }]}>kWh</Text>
        </View>

        <View style={styles.summaryItem}>
          <Text style={styles.summaryLabel}>{t('dashboardEnergyReview.totalSell', 'Toplam Satış')}</Text>
          <Text style={[styles.summaryValue, styles.sellColor]}>
            {data.totalSell.toFixed(1)}
          </Text>
          <Text style={[styles.summaryLabel, { fontSize: 9 }]}>kWh</Text>
        </View>

        <View style={styles.summaryItem}>
          <Text style={styles.summaryLabel}>{t('dashboardEnergyReview.net', 'Net')}</Text>
          <Text style={[styles.summaryValue, data.netEnergy >= 0 ? styles.sellColor : styles.buyColor]}>
            {data.netEnergy >= 0 ? '+' : ''}{data.netEnergy.toFixed(1)}
          </Text>
          <Text style={[styles.summaryLabel, { fontSize: 9 }]}>kWh</Text>
        </View>
      </View>

      {/* Chart */}
      <View style={styles.chartContainer} onLayout={onLayout} {...(chartSize.width > 0 && chartWidth > 0 ? panResponder.panHandlers : {})}>
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

        {/* Tooltip */}
        {chartSize.width > 0 && tooltip.visible && (
          <View
            style={[
              styles.tooltipContainer,
              {
                left: Math.max(10, Math.min(chartSize.width - 170, tooltip.x - 80)),
                top: Math.max(10, tooltip.y - 80),
              },
            ]}
          >
            <View style={styles.tooltipRow}>
              <Text style={styles.tooltipLabel}>{t('dashboardEnergyReview.buy', 'Alış')}:</Text>
              <Text style={[styles.tooltipValue, styles.buyColor]}>
                {tooltip.transaction.buy.toFixed(2)} kWh
              </Text>
            </View>
            <View style={styles.tooltipRow}>
              <Text style={styles.tooltipLabel}>{t('dashboardEnergyReview.sell', 'Satış')}:</Text>
              <Text style={[styles.tooltipValue, styles.sellColor]}>
                {tooltip.transaction.sell.toFixed(2)} kWh
              </Text>
            </View>
            <View style={styles.tooltipRow}>
              <Text style={styles.tooltipLabel}>{t('dashboardEnergyReview.price', 'Fiyat')}:</Text>
              <Text style={styles.tooltipValue}>
                ${tooltip.transaction.price.toFixed(3)}
              </Text>
            </View>
            <Text style={styles.tooltipTime}>
              {new Date(tooltip.transaction.timestamp).toLocaleString('tr-TR', {
                day: '2-digit',
                month: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </Text>
          </View>
        )}
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
