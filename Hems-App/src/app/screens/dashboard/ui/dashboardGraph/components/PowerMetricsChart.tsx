import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, Text, Switch, ActivityIndicator, Platform, PanResponder, TouchableOpacity } from 'react-native';
import Svg, { Path, Line, Defs, LinearGradient, Stop, G, Text as SvgText, Rect, Circle } from 'react-native-svg';
import { useNavigation } from '@react-navigation/native';
import { getPowerMetricsChartStyles } from './PowerMetricsChart.styles';
import { useTranslation } from 'react-i18next';
import { useSelectedSiteDevice } from '../../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../../hooks/useDashboardRefresh';
import { powerMetrics24HService } from '../../../../../../api/services/powerMetrics24HService';

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

type Props = {
  COLORS: Record<string, string>;
};

// Power line colors
const POWER_COLORS = {
  solar: '#FACC15',
  battery: '#22C55E',
  grid: '#3B82F6',
  load: '#A855F7',
};

export default function PowerMetricsChart({ COLORS }: Props) {
  console.log('🎯 PowerMetricsChart RENDERED!');

  const { t } = useTranslation();
  const navigation = useNavigation<any>();
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();

  // Toggle states for each power metric
  const [showSolar, setShowSolar] = useState(true);
  const [showBattery, setShowBattery] = useState(true);
  const [showGrid, setShowGrid] = useState(true);
  const [showLoad, setShowLoad] = useState(true);

  const [powerMetricsData, setPowerMetricsData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [touchX, setTouchX] = useState<number | null>(null);
  const [selectedDataPoint, setSelectedDataPoint] = useState<any>(null);

  const styles = useMemo(() => getPowerMetricsChartStyles(COLORS), [COLORS]);

  // Load power metrics data
  const loadPowerMetricsData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      // Fetch 24H power metrics (15-minute intervals, up to now)
      const data = await powerMetrics24HService.getPowerMetrics24H(device.id);
      console.log('✅ Power Metrics loaded:', data.data.length, 'points');
      setPowerMetricsData(data);
    } catch (error) {
      console.error('Failed to load power metrics:', error);
      setPowerMetricsData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  useEffect(() => {
    loadPowerMetricsData();
  }, [loadPowerMetricsData]);

  useEffect(() => {
    const unregister = registerRefreshCallback(loadPowerMetricsData);
    return () => unregister();
  }, [registerRefreshCallback, loadPowerMetricsData]);

  // Auto-refresh every 15 minutes to get latest data
  useEffect(() => {
    const interval = setInterval(() => {
      console.log('🔄 Auto-refreshing PowerMetricsChart data (15 min)...');
      loadPowerMetricsData();
    }, 15 * 60 * 1000); // 15 minutes

    return () => clearInterval(interval);
  }, [loadPowerMetricsData]);

  // Calculate non-linear X-axis mapping
  const getTimeToXPosition = useCallback((hour: number, sunriseHour: number, sunsetHour: number, width: number): number => {
    // Allocate 60% of chart width to sunrise-sunset period, 40% to night hours
    const daytimeRatio = 0.6;
    const nighttimeRatio = 0.4;

    const dayLength = sunsetHour - sunriseHour;
    const nightLength = 24 - dayLength;

    if (hour >= sunriseHour && hour <= sunsetHour) {
      // Daytime hours - expanded
      const hoursFromSunrise = hour - sunriseHour;
      const positionInDay = hoursFromSunrise / dayLength;
      const nightBeforeWidth = (sunriseHour / nightLength) * nighttimeRatio * width;
      return nightBeforeWidth + positionInDay * daytimeRatio * width;
    } else if (hour < sunriseHour) {
      // Night hours before sunrise - compressed
      const positionInNight = hour / sunriseHour;
      return positionInNight * (sunriseHour / nightLength) * nighttimeRatio * width;
    } else {
      // Night hours after sunset - compressed
      const hoursAfterSunset = hour - sunsetHour;
      const nightAfterLength = 24 - sunsetHour;
      const positionInNight = hoursAfterSunset / nightAfterLength;
      const beforeWidth = (sunriseHour / nightLength) * nighttimeRatio * width + daytimeRatio * width;
      return beforeWidth + positionInNight * (nightAfterLength / nightLength) * nighttimeRatio * width;
    }
  }, []);

  // Parse hour from ISO timestamp
  const getHourFromTimestamp = (timestamp: string): number => {
    const date = new Date(timestamp);
    return date.getHours() + date.getMinutes() / 60;
  };

  // Format time for display
  const formatTime = (timestamp: string): string => {
    const date = new Date(timestamp);
    return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
  };

  // Create straight line path (no smoothing)
  const createStraightPath = useCallback((points: { x: number; y: number }[]): string => {
    if (points.length < 2) return '';

    let path = `M ${points[0].x},${points[0].y}`;

    for (let i = 1; i < points.length; i++) {
      path += ` L ${points[i].x},${points[i].y}`;
    }

    return path;
  }, []);

  // Render the iOS chart with SVG
  const renderChart = () => {
    const chartWidth = 350;
    const chartHeight = 280; // Reduced from 300
    const padding = { top: 20, right: 20, bottom: 30, left: 10 }; // Reduced bottom from 55 to 30
    const innerWidth = chartWidth - padding.left - padding.right;
    const innerHeight = chartHeight - padding.top - padding.bottom;

    // Find nearest data point based on touch X position
    const findNearestDataPoint = (x: number) => {
      if (!powerMetricsData?.data) return;

      const sunriseHour = getHourFromTimestamp(powerMetricsData.sunriseTime);
      const sunsetHour = getHourFromTimestamp(powerMetricsData.sunsetTime);

      let nearestPoint = null;
      let minDistance = Infinity;

      powerMetricsData.data.forEach((point: any) => {
        const hour = getHourFromTimestamp(point.timestamp);
        const pointX = padding.left + getTimeToXPosition(hour, sunriseHour, sunsetHour, innerWidth);
        const distance = Math.abs(pointX - x);

        if (distance < minDistance) {
          minDistance = distance;
          nearestPoint = point;
        }
      });

      setSelectedDataPoint(nearestPoint);
    };

    // Calculate max X position (last data point)
    const getMaxTouchX = () => {
      if (!powerMetricsData?.data || powerMetricsData.data.length === 0) return padding.left + innerWidth;

      const sunriseHour = getHourFromTimestamp(powerMetricsData.sunriseTime);
      const sunsetHour = getHourFromTimestamp(powerMetricsData.sunsetTime);
      const lastPoint = powerMetricsData.data[powerMetricsData.data.length - 1];
      const lastHour = getHourFromTimestamp(lastPoint.timestamp);

      return padding.left + getTimeToXPosition(lastHour, sunriseHour, sunsetHour, innerWidth);
    };

    const maxTouchX = getMaxTouchX();

    // PanResponder for touch interaction
    const panResponder = PanResponder.create({
      onStartShouldSetPanResponder: () => true,
      onMoveShouldSetPanResponder: () => true,
      onPanResponderGrant: (evt) => {
        const x = evt.nativeEvent.locationX;
        // Limit to data range (from left padding to last data point)
        if (x >= padding.left && x <= maxTouchX) {
          setTouchX(x);
          findNearestDataPoint(x);
        }
      },
      onPanResponderMove: (evt) => {
        const x = evt.nativeEvent.locationX;
        // Limit to data range (from left padding to last data point)
        if (x >= padding.left && x <= maxTouchX) {
          setTouchX(x);
          findNearestDataPoint(x);
        } else if (x > maxTouchX) {
          // If finger goes beyond max, reset
          setTouchX(null);
          setSelectedDataPoint(null);
        }
      },
      onPanResponderRelease: () => {
        setTouchX(null);
        setSelectedDataPoint(null);
      },
    });

    if (!powerMetricsData || !powerMetricsData.data.length) {
      return (
        <View style={styles.loadingContainer}>
          <Text style={styles.loadingText}>{t('common.noData', 'No data available')}</Text>
        </View>
      );
    }

    // Data is already filtered by service (up to current time)
    const dataToShow = powerMetricsData.data;

    // Get sunrise/sunset from service
    const sunriseHour = getHourFromTimestamp(powerMetricsData.sunriseTime);
    const sunsetHour = getHourFromTimestamp(powerMetricsData.sunsetTime);

    console.log('📊 Chart Data:', {
      totalPoints: dataToShow.length,
      sunrise: sunriseHour,
      sunset: sunsetHour
    });

    // Collect all values to determine Y-axis range
    const allValues: number[] = [];
    dataToShow.forEach(point => {
      if (showSolar) allValues.push(point.solarPower);
      if (showBattery) allValues.push(point.batteryPower);
      if (showGrid) allValues.push(point.gridPower);
      if (showLoad) allValues.push(point.loadPower);
    });

    const minValue = Math.min(...allValues, 0);
    const maxValue = Math.max(...allValues, 1);
    const range = maxValue - minValue || 1;

    // Add 10% margin to Y-axis
    const yMargin = range * 0.1;
    const yMin = minValue - yMargin;
    const yMax = maxValue + yMargin;
    const yRange = yMax - yMin;

    // Calculate Y position for zero line
    const zeroY = padding.top + innerHeight - ((0 - yMin) / yRange) * innerHeight;

    // Map data points to chart coordinates
    const mapDataToPoints = (data: typeof powerMetricsData.data, valueKey: keyof typeof data[0]) => {
      return data.map(point => {
        const hour = getHourFromTimestamp(point.timestamp);
        const x = padding.left + getTimeToXPosition(hour, sunriseHour, sunsetHour, innerWidth);
        const value = point[valueKey] as number;
        const y = padding.top + innerHeight - ((value - yMin) / yRange) * innerHeight;
        return { x, y, value };
      });
    };

    const solarPoints = showSolar ? mapDataToPoints(dataToShow, 'solarPower') : [];
    const batteryPoints = showBattery ? mapDataToPoints(dataToShow, 'batteryPower') : [];
    const gridPoints = showGrid ? mapDataToPoints(dataToShow, 'gridPower') : [];
    const loadPoints = showLoad ? mapDataToPoints(dataToShow, 'loadPower') : [];

    // Create fill path (area to zero line)
    const createFillPath = (points: { x: number; y: number; value: number }[]): string => {
      if (points.length < 2) return '';

      const straightPath = createStraightPath(points);

      // Close path to zero line (not to bottom of chart)
      return `${straightPath} L ${points[points.length - 1].x},${zeroY} L ${points[0].x},${zeroY} Z`;
    };

    // Calculate sunrise/sunset X positions for markers
    const sunriseX = padding.left + getTimeToXPosition(sunriseHour, sunriseHour, sunsetHour, innerWidth);
    const sunsetX = padding.left + getTimeToXPosition(sunsetHour, sunriseHour, sunsetHour, innerWidth);

    return (
      <View {...panResponder.panHandlers}>
        <Svg width={chartWidth} height={chartHeight}>
          <Defs>
            <LinearGradient id="solarGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.solar} stopOpacity={0.5} />
              <Stop offset="100%" stopColor={POWER_COLORS.solar} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="batteryGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.battery} stopOpacity={0.5} />
              <Stop offset="100%" stopColor={POWER_COLORS.battery} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="gridGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.grid} stopOpacity={0.4} />
              <Stop offset="100%" stopColor={POWER_COLORS.grid} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="loadGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.load} stopOpacity={0.5} />
              <Stop offset="100%" stopColor={POWER_COLORS.load} stopOpacity={0.05} />
            </LinearGradient>
          </Defs>

          {/* Horizontal grid lines */}
          {[0, 1, 2, 3, 4].map(i => {
            const y = padding.top + (i * innerHeight) / 4;
            return (
              <Line
                key={`h-grid-${i}`}
                x1={padding.left}
                y1={y}
                x2={padding.left + innerWidth}
                y2={y}
                stroke={COLORS.border}
                strokeWidth={0.5}
                opacity={0.3}
              />
            );
          })}

          {/* Zero line (Y=0) - Prominent horizontal line */}
          <Line
            x1={padding.left}
            y1={zeroY}
            x2={padding.left + innerWidth}
            y2={zeroY}
            stroke={COLORS.textSoft}
            strokeWidth={2}
            opacity={0.7}
          />

          {/* Vertical grid lines */}
          {[0, 6, 12, 18, 24].map(hour => {
            const x = padding.left + getTimeToXPosition(hour === 24 ? 23.99 : hour, sunriseHour, sunsetHour, innerWidth);
            return (
              <Line
                key={`v-grid-${hour}`}
                x1={x}
                y1={padding.top}
                x2={x}
                y2={padding.top + innerHeight}
                stroke={COLORS.border}
                strokeWidth={0.5}
                opacity={0.3}
              />
            );
          })}

          {/* Sunrise marker */}
          <Line
            x1={sunriseX}
            y1={padding.top}
            x2={sunriseX}
            y2={padding.top + innerHeight}
            stroke="#FDB022"
            strokeWidth={1.5}
            strokeDasharray="4,4"
            opacity={0.6}
          />

          {/* Sunset marker */}
          <Line
            x1={sunsetX}
            y1={padding.top}
            x2={sunsetX}
            y2={padding.top + innerHeight}
            stroke="#F97316"
            strokeWidth={1.5}
            strokeDasharray="4,4"
            opacity={0.6}
          />

          {/* Current time marker (now) - Red vertical line */}
          {(() => {
            const now = new Date();
            const currentTime = now.getHours() + now.getMinutes() / 60;
            const currentTimeX = padding.left + getTimeToXPosition(currentTime, sunriseHour, sunsetHour, innerWidth);
            return (
              <>
                <Line
                  x1={currentTimeX}
                  y1={padding.top}
                  x2={currentTimeX}
                  y2={padding.top + innerHeight}
                  stroke="#EF4444"
                  strokeWidth={1}
                  opacity={0.7}
                />
                {/* "Now" label */}
                <SvgText
                  x={currentTimeX}
                  y={padding.top - 5}
                  fontSize={9}
                  fill="#EF4444"
                  fontWeight="700"
                  textAnchor="middle"
                >
                  NOW
                </SvgText>
              </>
            );
          })()}

          {/* Render filled areas */}
          {showLoad && loadPoints.length > 0 && (
            <Path d={createFillPath(loadPoints)} fill="url(#loadGradient)" />
          )}
          {showGrid && gridPoints.length > 0 && (
            <Path d={createFillPath(gridPoints)} fill="url(#gridGradient)" />
          )}
          {showBattery && batteryPoints.length > 0 && (
            <Path d={createFillPath(batteryPoints)} fill="url(#batteryGradient)" />
          )}
          {showSolar && solarPoints.length > 0 && (
            <Path d={createFillPath(solarPoints)} fill="url(#solarGradient)" />
          )}

          {/* Render lines */}
          {showLoad && loadPoints.length > 0 && (
            <Path d={createStraightPath(loadPoints)} stroke={POWER_COLORS.load} strokeWidth={1} fill="none" />
          )}
          {showGrid && gridPoints.length > 0 && (
            <Path d={createStraightPath(gridPoints)} stroke={POWER_COLORS.grid} strokeWidth={1} fill="none" />
          )}
          {showBattery && batteryPoints.length > 0 && (
            <Path d={createStraightPath(batteryPoints)} stroke={POWER_COLORS.battery} strokeWidth={1} fill="none" />
          )}
          {showSolar && solarPoints.length > 0 && (
            <Path d={createStraightPath(solarPoints)} stroke={POWER_COLORS.solar} strokeWidth={1} fill="none" />
          )}

          {/* Y-axis labels - Inside the chart */}
          {[0, 1, 2, 3, 4].map(i => {
            const y = padding.top + (i * innerHeight) / 4;
            const value = yMax - (i * yRange) / 4;
            const displayValue = Math.abs(value) >= 1000 ? `${(value / 1000).toFixed(1)}k` : value.toFixed(0);

            return (
              <G key={`y-label-${i}`}>
                {/* Background for better readability */}
                <SvgText
                  x={padding.left + 5}
                  y={y + 4}
                  fontSize={10}
                  fill={COLORS.surface}
                  textAnchor="start"
                  opacity={0.8}
                  stroke={COLORS.surface}
                  strokeWidth={3}
                >
                  {displayValue}
                </SvgText>
                <SvgText
                  x={padding.left + 5}
                  y={y + 4}
                  fontSize={10}
                  fill={COLORS.textSoft}
                  textAnchor="start"
                >
                  {displayValue}
                </SvgText>
              </G>
            );
          })}

          {/* Zero label - Prominent, inside chart */}
          <G>
            {/* Background for better readability */}
            <SvgText
              x={padding.left + 5}
              y={zeroY + 4}
              fontSize={11}
              fill={COLORS.surface}
              fontWeight="700"
              textAnchor="start"
              opacity={0.9}
              stroke={COLORS.surface}
              strokeWidth={4}
            >
              0
            </SvgText>
            <SvgText
              x={padding.left + 5}
              y={zeroY + 4}
              fontSize={11}
              fill={COLORS.textSoft}
              fontWeight="700"
              textAnchor="start"
            >
              0
            </SvgText>
          </G>

          {/* Touch interaction - Vertical line tracking finger */}
          {touchX !== null && selectedDataPoint && (
            <G>
              {/* Vertical touch line - solid thin line */}
              <Line
                x1={touchX}
                y1={padding.top}
                x2={touchX}
                y2={padding.top + innerHeight}
                stroke={COLORS.textSoft}
                strokeWidth={1}
                opacity={0.8}
              />
            </G>
          )}

          {/* X-axis labels (selected hours) */}
          {[0, 6, 12, 18, 23].map(hour => {
            const x = padding.left + getTimeToXPosition(hour, sunriseHour, sunsetHour, innerWidth);
            return (
              <SvgText
                key={`x-label-${hour}`}
                x={x}
                y={padding.top + innerHeight + 15}
                fontSize={10}
                fill={COLORS.textSoft}
                textAnchor="middle"
              >
                {`${hour.toString().padStart(2, '0')}:00`}
              </SvgText>
            );
          })}
        </Svg>

        {/* Touch data display - Floating card above chart */}
        {touchX !== null && selectedDataPoint && (
          <View style={{
            position: 'absolute',
            top: -120,
            left: Math.max(10, Math.min(touchX - 100, 350 - 210)), // Center card on touch point with bounds
            backgroundColor: COLORS.cardBg,
            borderRadius: 12,
            padding: 14,
            shadowColor: '#000',
            shadowOffset: { width: 0, height: 2 },
            shadowOpacity: 0.25,
            shadowRadius: 3.84,
            elevation: 5,
            minWidth: 200,
          }}>
            <Text style={{ color: COLORS.textSoft, fontSize: 13, fontWeight: '700', marginBottom: 8 }}>
              {formatTime(selectedDataPoint.timestamp)}
            </Text>
            {showSolar && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.solar }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>Solar</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {(selectedDataPoint.solarPower / 1000).toFixed(2)} kW
                </Text>
              </View>
            )}
            {showBattery && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.battery }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>Battery</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {(selectedDataPoint.batteryPower / 1000).toFixed(2)} kW
                </Text>
              </View>
            )}
            {showGrid && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.grid }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>Grid</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {(selectedDataPoint.gridPower / 1000).toFixed(2)} kW
                </Text>
              </View>
            )}
            {showLoad && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.load }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>Load</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {(selectedDataPoint.loadPower / 1000).toFixed(2)} kW
                </Text>
              </View>
            )}
          </View>
        )}
      </View>
    );
  };

  console.log('PowerMetricsChart - loading:', loading, 'hasDevice:', hasDevice, 'device:', device?.id);

  if (loading) {
    return (
      <View style={styles.container}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.primary} />
          <Text style={styles.loadingText}>{t('common.loading', 'Loading...')}</Text>
        </View>
      </View>
    );
  }

  const navigateToDetail = () => {
    navigation.navigate('PowerMetricsDetail');
  };

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>{t('powerMetrics24H.title', 'Power Metrics')}</Text>
        <TouchableOpacity onPress={navigateToDetail} style={{ padding: 4 }}>
          <HistoryIcon color={COLORS.accent} />
        </TouchableOpacity>
      </View>

      {/* Chart */}
      <View style={styles.chartContainer}>
        {renderChart()}
      </View>

      {/* Toggle switches */}
      <View style={styles.togglesContainer}>
        <View style={styles.togglesRow}>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>
              {t('dashboardPowerMetricsChart.labels.solar', 'Solar')}
            </Text>
            <Switch
              value={showSolar}
              onValueChange={setShowSolar}
              trackColor={{ false: COLORS.border, true: POWER_COLORS.solar }}
              thumbColor={showSolar ? '#fff' : '#f4f3f4'}
            />
          </View>

          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>
              {t('dashboardPowerMetricsChart.labels.battery', 'Battery')}
            </Text>
            <Switch
              value={showBattery}
              onValueChange={setShowBattery}
              trackColor={{ false: COLORS.border, true: POWER_COLORS.battery }}
              thumbColor={showBattery ? '#fff' : '#f4f3f4'}
            />
          </View>
        </View>

        <View style={styles.togglesRow}>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>
              {t('dashboardPowerMetricsChart.labels.grid', 'Grid')}
            </Text>
            <Switch
              value={showGrid}
              onValueChange={setShowGrid}
              trackColor={{ false: COLORS.border, true: POWER_COLORS.grid }}
              thumbColor={showGrid ? '#fff' : '#f4f3f4'}
            />
          </View>

          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>
              {t('dashboardPowerMetricsChart.labels.load', 'Load')}
            </Text>
            <Switch
              value={showLoad}
              onValueChange={setShowLoad}
              trackColor={{ false: COLORS.border, true: POWER_COLORS.load }}
              thumbColor={showLoad ? '#fff' : '#f4f3f4'}
            />
          </View>
        </View>
      </View>
    </View>
  );
}
