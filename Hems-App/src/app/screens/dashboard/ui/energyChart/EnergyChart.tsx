/**
 * EnergyChart Component
 * Displays energy production and consumption as stacked area chart
 * with interactive tooltip showing breakdown by source
 */

import React, {useState, useMemo, useCallback, useEffect} from 'react';
import {
  View,
  Text,
  LayoutChangeEvent,
  ActivityIndicator,
  PanResponder,
  TouchableOpacity,
  Platform,
  Vibration,
} from 'react-native';
import Svg, {
  Line,
  Path,
  Text as SvgText,
  G,
  Defs,
  LinearGradient,
  Stop,
  Rect,
} from 'react-native-svg';
import Animated, {FadeInUp} from 'react-native-reanimated';
import {useI18n} from '../../../../../hooks/useI18n';
import {useThemeStore} from '../../../../../store/themeStore';
import {getThemeColors} from '../../../../../theme/constants';
import {useSitesStore} from '../../../../../store/sitesStore';
import {useDashboardRefresh} from '../../../../../hooks/useDashboardRefresh';
import {mobileDashboardService} from '../../../../../api/services/mobileDashboardService';
import {createStyles} from './EnergyChart.styles';
import {
  EnergyChartProps,
  EnergyChartDataPoint,
  EnergyPeriod,
  TooltipData,
} from './EnergyChart.types';
import {
  transformPowerMetricsToChartData,
  dataPointToTooltip,
  formatEnergyValue,
  getMaxValue,
  generateYAxisTicks,
  formatXAxisLabel,
  formatTooltipDate,
  periodToApiParam,
  findDataPointAtX,
  createAreaPath,
} from './EnergyChart.utils';

/**
 * Safe vibration helper
 */
const safeVibrate = (duration: number) => {
  try {
    if (Platform.OS === 'android') {
      Vibration.vibrate(duration);
    } else if (Platform.OS === 'ios') {
      Vibration.vibrate();
    }
  } catch (error) {
    // Ignore vibration errors
  }
};

export default function EnergyChart({
  initialPeriod = 'weekly',
  chartHeight = 320,
}: EnergyChartProps) {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const {t, language} = useI18n();
  const styles = createStyles(COLORS);
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  // Get selected device
  const {device, hasDevice} = useSitesStore(s => ({
    device: s.selectedSiteDevice?.device,
    hasDevice: !!s.selectedSiteDevice?.device,
  }));

  // State
  const [selectedPeriod, setSelectedPeriod] = useState<EnergyPeriod>(initialPeriod);
  const [chartData, setChartData] = useState<EnergyChartDataPoint[]>([]);
  const [selectedPoint, setSelectedPoint] = useState<EnergyChartDataPoint | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [chartSize, setChartSize] = useState({width: 0, height: 0});

  // Chart dimensions
  const padding = useMemo(
    () => ({top: 100, right: 10, bottom: 50, left: 30}),
    [],
  );
  const chartWidth = chartSize.width - padding.left - padding.right;
  const chartInnerHeight = chartSize.height - padding.top - padding.bottom;

  // Data loading
  const {registerRefreshCallback} = useDashboardRefresh();

  const loadData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setError(null);
      setChartData([]);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const apiPeriod = periodToApiParam(selectedPeriod);
      const response = await mobileDashboardService.getPowerMetrics(
        device.id,
        apiPeriod,
      );

      const transformed = transformPowerMetricsToChartData(response);
      setChartData(transformed);

      // Set initial selected point to most recent data
      if (transformed.length > 0) {
        setSelectedPoint(transformed[transformed.length - 1]);
      }
    } catch (err) {
      console.error('Failed to load energy chart data:', err);
      setError(t('energyChart.noData'));
      setChartData([]);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id, selectedPeriod, t]);

  // Load data on mount and when dependencies change
  useEffect(() => {
    loadData();
  }, [loadData]);

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(loadData);
    return () => unregister();
  }, [registerRefreshCallback, loadData]);

  // Chart layout
  const onLayout = useCallback((event: LayoutChangeEvent) => {
    const {width, height} = event.nativeEvent.layout;
    setChartSize({width, height});
  }, []);

  // Calculate scales
  const maxValue = useMemo(() => {
    const max = getMaxValue(chartData);
    return max > 0 ? max * 1.1 : 100; // Add 10% padding
  }, [chartData]);

  const yAxisTicks = useMemo(() => generateYAxisTicks(maxValue, 5), [maxValue]);

  // Find data point at touch position
  const getDataPointAtPosition = useCallback(
    (touchX: number) => {
      return findDataPointAtX(touchX, chartData, chartWidth, padding.left);
    },
    [chartData, chartWidth, padding.left],
  );

  // Pan responder for touch interaction
  const panResponder = useMemo(
    () =>
      PanResponder.create({
        onStartShouldSetPanResponder: () => true,
        onMoveShouldSetPanResponder: () => true,
        onPanResponderGrant: evt => {
          const touchX = evt.nativeEvent.locationX;
          const point = getDataPointAtPosition(touchX);
          if (point) {
            setSelectedPoint(point);
          }
        },
        onPanResponderMove: evt => {
          const touchX = evt.nativeEvent.locationX;
          const point = getDataPointAtPosition(touchX);
          if (point) {
            setSelectedPoint(point);
          }
        },
        onPanResponderRelease: () => {
          // Keep tooltip visible
        },
        onPanResponderTerminationRequest: () => true,
      }),
    [getDataPointAtPosition],
  );

  // Create area paths
  const productionPath = useMemo(() => {
    if (chartWidth <= 0 || chartInnerHeight <= 0 || chartData.length === 0) {
      return '';
    }
    return createAreaPath(
      chartData,
      point => point.totalProduction,
      chartWidth,
      chartInnerHeight,
      maxValue,
    );
  }, [chartData, chartWidth, chartInnerHeight, maxValue]);

  const consumptionPath = useMemo(() => {
    if (chartWidth <= 0 || chartInnerHeight <= 0 || chartData.length === 0) {
      return '';
    }
    return createAreaPath(
      chartData,
      point => point.totalConsumption,
      chartWidth,
      chartInnerHeight,
      maxValue,
    );
  }, [chartData, chartWidth, chartInnerHeight, maxValue]);

  // X-axis labels (show subset to avoid crowding)
  const xAxisLabels = useMemo(() => {
    if (chartData.length === 0) return [];

    // For weekly/monthly, show unique days only (no duplicate dates)
    if (selectedPeriod === 'weekly' || selectedPeriod === 'monthly') {
      const seenDates = new Set<string>();
      const uniqueLabels: Array<{x: number; label: string}> = [];

      chartData.forEach((point, index) => {
        const date = new Date(point.timestamp);
        const dateKey = `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;

        if (!seenDates.has(dateKey)) {
          seenDates.add(dateKey);
          const x = (index / Math.max(1, chartData.length - 1)) * chartWidth;
          uniqueLabels.push({
            x,
            label: formatXAxisLabel(point.timestamp, selectedPeriod, locale),
          });
        }
      });

      // Limit to maxLabels by taking every Nth unique label
      const maxLabels = 7;
      if (uniqueLabels.length > maxLabels) {
        const step = Math.ceil(uniqueLabels.length / maxLabels);
        return uniqueLabels.filter((_, index) => index % step === 0 || index === uniqueLabels.length - 1);
      }

      return uniqueLabels;
    }

    // For yearly, use original logic
    const maxLabels = 7;
    const step = Math.ceil(chartData.length / maxLabels);

    return chartData
      .filter((_, index) => index % step === 0 || index === chartData.length - 1)
      .map((point, _, filtered) => {
        const originalIndex = chartData.indexOf(point);
        const x = (originalIndex / Math.max(1, chartData.length - 1)) * chartWidth;
        return {
          x,
          label: formatXAxisLabel(point.timestamp, selectedPeriod, locale),
        };
      });
  }, [chartData, chartWidth, selectedPeriod, locale]);

  // Render tooltip
  const renderTooltip = () => {
    if (!selectedPoint) return null;

    const tooltip = dataPointToTooltip(selectedPoint);
    const dateStr = formatTooltipDate(tooltip.timestamp, selectedPeriod, locale);

    return (
      <View style={styles.infoBox}>
        <Text style={styles.infoDate}>{dateStr}</Text>

        {/* Production Section - Inline */}
        <View style={styles.infoSection}>
          <Text style={[styles.infoSectionTitle, styles.productionTitle]}>
            {t('energyChart.production')}: {formatEnergyValue(tooltip.production.total)} {tooltip.unit}
          </Text>
          <Text style={styles.inlineText}>
            {t('energyChart.sources.solar')}: {formatEnergyValue(tooltip.production.solar)} {tooltip.unit} | {t('energyChart.sources.batteryDischarge')}: {formatEnergyValue(tooltip.production.batteryDischarge)} {tooltip.unit} | {t('energyChart.sources.generator')}: {formatEnergyValue(tooltip.production.generator)} {tooltip.unit} | {t('energyChart.sources.gridImport')}: {formatEnergyValue(tooltip.production.gridImport)} {tooltip.unit}
          </Text>
        </View>

        {/* Consumption Section - Inline */}
        <View style={styles.infoSection}>
          <Text style={[styles.infoSectionTitle, styles.consumptionTitle]}>
            {t('energyChart.consumption')}: {formatEnergyValue(tooltip.consumption.total)} {tooltip.unit}
          </Text>
          <Text style={styles.inlineText}>
            {t('energyChart.sources.load')}: {formatEnergyValue(tooltip.consumption.load)} {tooltip.unit} | {t('energyChart.sources.batteryCharge')}: {formatEnergyValue(tooltip.consumption.batteryCharge)} {tooltip.unit} | {t('energyChart.sources.gridExport')}: {formatEnergyValue(tooltip.consumption.gridExport)} {tooltip.unit}
          </Text>
        </View>
      </View>
    );
  };

  // Render period selector
  const renderPeriodSelector = () => {
    const periods: EnergyPeriod[] = ['weekly', 'monthly', 'yearly'];

    return (
      <View style={styles.periodSelector}>
        {periods.map(period => {
          const isActive = selectedPeriod === period;
          return (
            <TouchableOpacity
              key={period}
              style={[
                styles.periodTab,
                {
                  backgroundColor: isActive ? COLORS.accent : COLORS.bgShadeLight,
                  borderColor: isActive ? COLORS.accent : COLORS.border,
                  shadowColor: isActive ? COLORS.accent : 'transparent',
                  shadowOpacity: isActive ? 0.3 : 0,
                  shadowRadius: isActive ? 4 : 0,
                  shadowOffset: {width: 0, height: 2},
                  elevation: isActive ? 2 : 0,
                },
              ]}
              onPress={() => {
                safeVibrate(30);
                setSelectedPeriod(period);
              }}
              activeOpacity={0.7}>
              <Text
                style={[
                  styles.periodTabText,
                  {
                    color: isActive ? COLORS.white : COLORS.textSoft,
                  },
                ]}>
                {t(`energyChart.period.${period}`)}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>
    );
  };

  // Render chart
  const renderChart = () => {
    if (chartWidth <= 0 || chartInnerHeight <= 0) return null;

    return (
      <Svg width={chartSize.width} height={chartSize.height}>
        <Defs>
          {/* Production gradient (green) */}
          <LinearGradient id="productionGradient" x1="0" y1="0" x2="0" y2="1">
            <Stop offset="0%" stopColor="#10b981" stopOpacity="0.6" />
            <Stop offset="100%" stopColor="#10b981" stopOpacity="0.1" />
          </LinearGradient>

          {/* Consumption gradient (red) */}
          <LinearGradient id="consumptionGradient" x1="0" y1="0" x2="0" y2="1">
            <Stop offset="0%" stopColor="#ef4444" stopOpacity="0.6" />
            <Stop offset="100%" stopColor="#ef4444" stopOpacity="0.1" />
          </LinearGradient>
        </Defs>

        <G x={padding.left} y={padding.top}>
          {/* Y-axis grid lines */}
          {yAxisTicks.map((tick, index) => {
            const y =
              chartInnerHeight - (tick / maxValue) * chartInnerHeight;
            return (
              <G key={`grid-${index}`}>
                <Line
                  x1={0}
                  y1={y}
                  x2={chartWidth}
                  y2={y}
                  stroke={COLORS.border}
                  strokeWidth="1"
                  strokeDasharray="4,4"
                  opacity={0.3}
                />
                <SvgText
                  x={-10}
                  y={y + 4}
                  fontSize="10"
                  fill={COLORS.textSoft}
                  opacity={0.7}
                  textAnchor="end">
                  {tick.toFixed(0)}
                </SvgText>
              </G>
            );
          })}

          {/* Area paths */}
          {productionPath && (
            <Path
              d={productionPath}
              fill="url(#productionGradient)"
              stroke="#10b981"
              strokeWidth="2"
            />
          )}

          {consumptionPath && (
            <Path
              d={consumptionPath}
              fill="url(#consumptionGradient)"
              stroke="#ef4444"
              strokeWidth="2"
            />
          )}

          {/* X-axis labels */}
          {xAxisLabels.map((label, index) => (
            <SvgText
              key={`x-label-${index}`}
              x={label.x}
              y={chartInnerHeight + 20}
              fontSize="9"
              fill={COLORS.textSoft}
              opacity={0.7}
              textAnchor="middle">
              {label.label}
            </SvgText>
          ))}

          {/* Transparent touch overlay */}
          <Rect
            x={0}
            y={0}
            width={chartWidth}
            height={chartInnerHeight}
            fill="transparent"
          />
        </G>
      </Svg>
    );
  };

  // No device state
  if (!hasDevice) {
    return (
      <Animated.View
        entering={FadeInUp.delay(400).duration(600)}
        style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('energyChart.title')}</Text>
        </View>
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>{t('energyChart.noDevice')}</Text>
        </View>
      </Animated.View>
    );
  }

  // Loading state
  if (loading && chartData.length === 0) {
    return (
      <Animated.View
        entering={FadeInUp.delay(400).duration(600)}
        style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('energyChart.title')}</Text>
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
          <Text style={styles.loadingText}>{t('energyChart.loading')}</Text>
        </View>
        {renderPeriodSelector()}
      </Animated.View>
    );
  }

  // Error state
  if (error && chartData.length === 0) {
    return (
      <Animated.View
        entering={FadeInUp.delay(400).duration(600)}
        style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('energyChart.title')}</Text>
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={loadData}>
            <Text style={styles.retryButtonText}>{t('common.retry')}</Text>
          </TouchableOpacity>
        </View>
        {renderPeriodSelector()}
      </Animated.View>
    );
  }

  // Main render
  return (
    <Animated.View
      entering={FadeInUp.delay(400).duration(600)}
      style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{t('energyChart.title')}</Text>
      </View>

      <View style={styles.chartWrapper}>
        {/* Tooltip */}
        {renderTooltip()}

        {/* Chart */}
        <View
          style={[styles.chartContainer, {height: chartHeight}]}
          onLayout={onLayout}
          {...panResponder.panHandlers}>
          {renderChart()}
        </View>
      </View>

      {/* Legend */}
      <View style={styles.legend}>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, styles.productionLegend]} />
          <Text style={styles.legendLabel}>{t('energyChart.production')}</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, styles.consumptionLegend]} />
          <Text style={styles.legendLabel}>{t('energyChart.consumption')}</Text>
        </View>
      </View>

      {/* Period Selector */}
      {renderPeriodSelector()}
    </Animated.View>
  );
}
