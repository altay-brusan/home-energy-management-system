import React, { useState, useCallback, useMemo } from 'react';
import { View, Text, PanResponder, StyleSheet } from 'react-native';
import Svg, { Path, Line, Defs, LinearGradient, Stop, G, Text as SvgText } from 'react-native-svg';

type Range = 'Day' | 'Week' | 'Month' | 'Year';

interface DataPoint {
  x: string;
  y: number;
}

interface SolarLineChartProps {
  data: DataPoint[];
  range: Range;
  COLORS: Record<string, string>;
  width: number;
  height: number;
}

const SOLAR_COLOR = '#FACC15';
const SUNRISE_COLOR = '#FDB022';
const SUNSET_COLOR = '#F97316';
const NOW_COLOR = '#EF4444';

export const SolarLineChart: React.FC<SolarLineChartProps> = ({
  data,
  range,
  COLORS,
  width,
  height,
}) => {
  const [touchX, setTouchX] = useState<number | null>(null);
  const [selectedDataPoint, setSelectedDataPoint] = useState<DataPoint | null>(null);

  const padding = useMemo(() => ({ top: 30, right: 15, bottom: 40, left: 15 }), []);
  const innerWidth = Math.max(width - padding.left - padding.right, 1);
  const innerHeight = Math.max(height - padding.top - padding.bottom, 1);

  // Calculate Y scale
  const maxY = useMemo(() => Math.max(...data.map(d => d.y), 1) * 1.15, [data]);
  const minY = 0;
  const yRange = maxY - minY;

  // Map value to Y coordinate
  const valueToY = useCallback((value: number) => {
    return padding.top + innerHeight - ((value - minY) / yRange) * innerHeight;
  }, [innerHeight, yRange, minY, padding.top]);

  // Get X position for data point index
  const indexToX = useCallback((index: number) => {
    if (data.length <= 1) return padding.left + innerWidth / 2;
    return padding.left + (index / (data.length - 1)) * innerWidth;
  }, [data.length, innerWidth, padding.left]);

  // Create line path
  const createLinePath = useCallback((points: { x: number; y: number }[]): string => {
    if (points.length < 2) return '';
    let path = `M ${points[0].x},${points[0].y}`;
    for (let i = 1; i < points.length; i++) {
      path += ` L ${points[i].x},${points[i].y}`;
    }
    return path;
  }, []);

  // Create fill path (area to bottom)
  const createFillPath = useCallback((points: { x: number; y: number }[]): string => {
    if (points.length < 2) return '';
    const linePath = createLinePath(points);
    const bottomY = padding.top + innerHeight;
    return `${linePath} L ${points[points.length - 1].x},${bottomY} L ${points[0].x},${bottomY} Z`;
  }, [createLinePath, innerHeight, padding.top]);

  // Map data to chart points
  const chartPoints = useMemo(() => {
    return data.map((point, index) => ({
      x: indexToX(index),
      y: valueToY(point.y),
      value: point.y,
      label: point.x,
    }));
  }, [data, indexToX, valueToY]);

  // Find nearest data point
  const findNearestDataPoint = useCallback((x: number) => {
    if (data.length === 0) return;

    let nearestIndex = 0;
    let minDistance = Infinity;

    chartPoints.forEach((point, index) => {
      const distance = Math.abs(point.x - x);
      if (distance < minDistance) {
        minDistance = distance;
        nearestIndex = index;
      }
    });

    setSelectedDataPoint(data[nearestIndex]);
  }, [data, chartPoints]);

  // PanResponder for touch interaction
  const panResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => true,
    onMoveShouldSetPanResponder: () => true,
    onPanResponderGrant: (evt) => {
      const x = evt.nativeEvent.locationX;
      if (x >= padding.left && x <= padding.left + innerWidth) {
        setTouchX(x);
        findNearestDataPoint(x);
      }
    },
    onPanResponderMove: (evt) => {
      const x = evt.nativeEvent.locationX;
      if (x >= padding.left && x <= padding.left + innerWidth) {
        setTouchX(x);
        findNearestDataPoint(x);
      }
    },
    onPanResponderRelease: () => {
      setTouchX(null);
      setSelectedDataPoint(null);
    },
  }), [innerWidth, findNearestDataPoint, padding.left]);

  // X-axis labels based on range
  const getXLabels = useCallback((): { label: string; index: number; hour?: number }[] => {
    switch (range) {
      case 'Day':
        // Show 00:00, 06:00, 12:00, 18:00, 23:00 - use hour for positioning
        return [
          { label: '00:00', index: 0, hour: 0 },
          { label: '06:00', index: 6, hour: 6 },
          { label: '12:00', index: 12, hour: 12 },
          { label: '18:00', index: 18, hour: 18 },
          { label: '23:00', index: 23, hour: 23 },
        ];
      case 'Week':
        // Show all days
        return data.map((d, i) => ({ label: d.x, index: i }));
      case 'Month':
        // Show every 5th day
        return [0, 4, 9, 14, 19, 24, 29].filter(i => i < data.length).map(i => ({
          label: data[i]?.x || '',
          index: i,
        }));
      case 'Year':
        // Show all months
        return data.map((d, i) => ({ label: d.x, index: i }));
      default:
        return [];
    }
  }, [range, data]);

  // Get X position for label (handles Day view differently)
  const getLabelX = useCallback((item: { label: string; index: number; hour?: number }) => {
    if (range === 'Day' && item.hour !== undefined) {
      // For Day view, position based on 24-hour span (0-23)
      return padding.left + (item.hour / 23) * innerWidth;
    }
    // For other views, use data index
    return indexToX(item.index);
  }, [range, padding.left, innerWidth, indexToX]);

  // Y-axis labels
  const yLabels = useMemo(() => {
    const labels = [];
    const steps = 4;
    for (let i = 0; i <= steps; i++) {
      const value = maxY - (i * yRange) / steps;
      labels.push({
        value: value.toFixed(1),
        y: padding.top + (i * innerHeight) / steps,
      });
    }
    return labels;
  }, [maxY, yRange, innerHeight, padding.top]);

  // Day-specific markers
  const isDayView = range === 'Day';
  const sunriseX = isDayView ? padding.left + (6 / 23) * innerWidth : 0;
  const sunsetX = isDayView ? padding.left + (18 / 23) * innerWidth : 0;

  // NOW marker position
  const now = new Date();
  const currentHour = now.getHours() + now.getMinutes() / 60;
  const nowX = isDayView ? padding.left + (currentHour / 24) * innerWidth : 0;

  // Horizontal grid lines
  const gridLines = useMemo(() => {
    const lines = [];
    for (let i = 0; i <= 4; i++) {
      lines.push(padding.top + (i * innerHeight) / 4);
    }
    return lines;
  }, [innerHeight, padding.top]);

  if (data.length === 0) {
    return (
      <View style={[styles.container, { width, height }]}>
        <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>No data available</Text>
      </View>
    );
  }

  const linePath = createLinePath(chartPoints);
  const fillPath = createFillPath(chartPoints);
  const xLabels = getXLabels();

  return (
    <View style={[styles.container, { width, height }]}>
      {/* Floating Tooltip */}
      {touchX !== null && selectedDataPoint && (
        <View
          style={[
            styles.tooltip,
            {
              left: Math.max(10, Math.min(touchX - 70, width - 150)),
              top: -70,
              backgroundColor: COLORS.surface,
              borderColor: COLORS.border,
            },
          ]}
        >
          <Text style={[styles.tooltipTime, { color: COLORS.textSoft }]}>
            {selectedDataPoint.x}
          </Text>
          <View style={styles.tooltipRow}>
            <View style={[styles.tooltipDot, { backgroundColor: SOLAR_COLOR }]} />
            <Text style={[styles.tooltipLabel, { color: COLORS.textSoft }]}>Solar</Text>
            <Text style={[styles.tooltipValue, { color: COLORS.text }]}>
              {selectedDataPoint.y.toFixed(1)} {range === 'Day' ? 'kW' : 'kWh'}
            </Text>
          </View>
        </View>
      )}

      <View {...panResponder.panHandlers}>
        <Svg width={width} height={height}>
          <Defs>
            <LinearGradient id="solarGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={SOLAR_COLOR} stopOpacity={0.5} />
              <Stop offset="100%" stopColor={SOLAR_COLOR} stopOpacity={0.05} />
            </LinearGradient>
          </Defs>

          {/* Horizontal grid lines */}
          {gridLines.map((y, i) => (
            <Line
              key={`h-grid-${i}`}
              x1={padding.left}
              y1={y}
              x2={padding.left + innerWidth}
              y2={y}
              stroke={COLORS.border}
              strokeWidth={0.5}
              opacity={0.4}
            />
          ))}

          {/* Sunrise marker (Day view only) */}
          {isDayView && (
            <Line
              x1={sunriseX}
              y1={padding.top}
              x2={sunriseX}
              y2={padding.top + innerHeight}
              stroke={SUNRISE_COLOR}
              strokeWidth={1.5}
              strokeDasharray="4,4"
              opacity={0.6}
            />
          )}

          {/* Sunset marker (Day view only) */}
          {isDayView && (
            <Line
              x1={sunsetX}
              y1={padding.top}
              x2={sunsetX}
              y2={padding.top + innerHeight}
              stroke={SUNSET_COLOR}
              strokeWidth={1.5}
              strokeDasharray="4,4"
              opacity={0.6}
            />
          )}

          {/* NOW marker (Day view only) */}
          {isDayView && currentHour <= 24 && (
            <G>
              <Line
                x1={nowX}
                y1={padding.top}
                x2={nowX}
                y2={padding.top + innerHeight}
                stroke={NOW_COLOR}
                strokeWidth={1.5}
                opacity={0.8}
              />
              <SvgText
                x={nowX}
                y={padding.top - 8}
                fontSize={9}
                fill={NOW_COLOR}
                fontWeight="700"
                textAnchor="middle"
              >
                NOW
              </SvgText>
            </G>
          )}

          {/* Area fill */}
          {fillPath && (
            <Path d={fillPath} fill="url(#solarGradient)" />
          )}

          {/* Line */}
          {linePath && (
            <Path d={linePath} stroke={SOLAR_COLOR} strokeWidth={2} fill="none" />
          )}

          {/* Y-axis labels */}
          {yLabels.map((label, i) => (
            <G key={`y-label-${i}`}>
              <SvgText
                x={padding.left + 5}
                y={label.y + 4}
                fontSize={9}
                fill={COLORS.surface}
                textAnchor="start"
                stroke={COLORS.surface}
                strokeWidth={3}
              >
                {label.value}
              </SvgText>
              <SvgText
                x={padding.left + 5}
                y={label.y + 4}
                fontSize={9}
                fill={COLORS.textSoft}
                textAnchor="start"
              >
                {label.value}
              </SvgText>
            </G>
          ))}

          {/* X-axis labels */}
          {xLabels.map((item, i) => {
            const x = getLabelX(item);
            const textAnchor = i === 0 ? 'start' : i === xLabels.length - 1 ? 'end' : 'middle';
            return (
              <SvgText
                key={`x-label-${i}`}
                x={x}
                y={padding.top + innerHeight + 20}
                fontSize={10}
                fill={COLORS.textSoft}
                textAnchor={textAnchor}
              >
                {item.label}
              </SvgText>
            );
          })}

          {/* Touch vertical line */}
          {touchX !== null && (
            <Line
              x1={touchX}
              y1={padding.top}
              x2={touchX}
              y2={padding.top + innerHeight}
              stroke={COLORS.textSoft}
              strokeWidth={1}
              opacity={0.8}
            />
          )}
        </Svg>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'relative',
  },
  tooltip: {
    position: 'absolute',
    borderRadius: 10,
    padding: 12,
    borderWidth: 1,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.2,
    shadowRadius: 4,
    elevation: 5,
    minWidth: 140,
    zIndex: 100,
  },
  tooltipTime: {
    fontSize: 12,
    fontWeight: '700',
    marginBottom: 8,
  },
  tooltipRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  tooltipDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  tooltipLabel: {
    fontSize: 11,
    fontWeight: '500',
    flex: 1,
  },
  tooltipValue: {
    fontSize: 12,
    fontWeight: '700',
  },
});

export default SolarLineChart;
