/**
 * Stacked Bar Chart Component
 * Displays energy metrics as stacked bars using SVG
 */

import * as React from 'react';
import { View, Text, Dimensions, PanResponder } from 'react-native';
import Svg, { Rect, Line, Text as SvgText, G } from 'react-native-svg';
import type { AggregatedEnergyDataPoint, MetricVisibility, PowerMetricColors } from '../../api/types/powerMetrics.types';
import { styles } from './StackedBarChart.styles';

export interface StackedBarChartProps {
  data: AggregatedEnergyDataPoint[];
  colors: PowerMetricColors;
  visibility: MetricVisibility;
  width?: number;
  height?: number;
  cardBgColor?: string;
  textColor?: string;
}

const StackedBarChart: React.FC<StackedBarChartProps> = ({
  data,
  colors,
  visibility,
  width,
  height = 400,
  cardBgColor = '#1F2937',
  textColor = '#E5E7EB',
}) => {
  const [touchX, setTouchX] = React.useState<number | null>(null);
  const [selectedDataPoint, setSelectedDataPoint] = React.useState<AggregatedEnergyDataPoint | null>(null);

  // Format date for tooltip
  const formatTooltipDate = (timestamp: string, label: string) => {
    const date = new Date(timestamp);

    // Check if it's a yearly label (like "Jan", "Feb", "Mar")
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    if (monthNames.includes(label)) {
      // Yearly view: "November 2025"
      const fullMonthNames = ['January', 'February', 'March', 'April', 'May', 'June',
                              'July', 'August', 'September', 'October', 'November', 'December'];
      return `${fullMonthNames[date.getMonth()]} ${date.getFullYear()}`;
    }

    // For weekly and monthly views: "10 November 2025"
    const day = date.getDate();
    const monthName = monthNames[date.getMonth()];
    const year = date.getFullYear();

    return `${day} ${monthName} ${year}`;
  };

  const screenWidth = width || Dimensions.get('window').width;
  const padding = { top: 30, right: 10, bottom: 60, left: 10 };
  const chartWidth = screenWidth;
  const innerWidth = chartWidth - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;

  // Calculate Y-axis domain
  const calculateDomain = () => {
    let maxPositive = 0;
    let maxNegative = 0;

    data.forEach(point => {
      let positive = 0;
      let negative = 0;

      // Sum positive values (stacked)
      if (visibility.solar && point.solarEnergy > 0) positive += point.solarEnergy;
      if (visibility.battery && point.batteryEnergy > 0) positive += point.batteryEnergy;
      if (visibility.grid && point.gridEnergy > 0) positive += point.gridEnergy;

      // Sum negative values (stacked)
      if (visibility.load) negative += Math.abs(point.loadEnergy);
      if (visibility.battery && point.batteryEnergy < 0) negative += Math.abs(point.batteryEnergy);
      if (visibility.grid && point.gridEnergy < 0) negative += Math.abs(point.gridEnergy);

      maxPositive = Math.max(maxPositive, positive);
      maxNegative = Math.max(maxNegative, negative);
    });

    // Add 10% padding
    const paddedMax = maxPositive * 1.1 || 10;
    const paddedMin = -(maxNegative * 1.1 || 10);

    return { min: paddedMin, max: paddedMax, range: paddedMax - paddedMin };
  };

  const { min: yMin, max: yMax, range: yRange } = calculateDomain();

  // Zero line Y position
  const zeroY = padding.top + innerHeight - ((0 - yMin) / yRange) * innerHeight;

  // Bar width calculation with 6px spacing (3px on each side)
  const barSpacing = innerWidth / data.length;

  // Weekly and Yearly views get narrower bars for better appearance
  const baseBarWidth = Math.max(10, barSpacing - 6);
  const barWidth = data.length === 7 ? baseBarWidth * 0.35 :
                   data.length === 12 ? baseBarWidth * 0.45 :
                   baseBarWidth;

  // Dynamic border radius based on view type
  // Week view (7 days) gets larger radius for better appearance
  const borderRadius = data.length === 7 ? 12 : 4;

  // Convert value to Y coordinate
  const valueToY = (value: number) => {
    return padding.top + innerHeight - ((value - yMin) / yRange) * innerHeight;
  };

  // Find nearest data point from touch X position
  const findNearestDataPoint = (x: number) => {
    if (x < padding.left || x > padding.left + innerWidth) {
      return null;
    }

    // Calculate which bar was touched
    const relativeX = x - padding.left;
    const index = Math.floor(relativeX / barSpacing);

    if (index >= 0 && index < data.length) {
      return { point: data[index], index };
    }

    return null;
  };

  // PanResponder for touch interactions
  const panResponder = React.useMemo(
    () =>
      PanResponder.create({
        onStartShouldSetPanResponder: () => true,
        onMoveShouldSetPanResponder: () => true,
        onPanResponderGrant: (evt) => {
          const x = evt.nativeEvent.locationX;
          const result = findNearestDataPoint(x);
          if (result) {
            setTouchX(x);
            setSelectedDataPoint(result.point);
          }
        },
        onPanResponderMove: (evt) => {
          const x = evt.nativeEvent.locationX;
          const result = findNearestDataPoint(x);
          if (result) {
            setTouchX(x);
            setSelectedDataPoint(result.point);
          } else {
            setTouchX(null);
            setSelectedDataPoint(null);
          }
        },
        onPanResponderRelease: () => {
          setTouchX(null);
          setSelectedDataPoint(null);
        },
      }),
    [data, padding, innerWidth, barSpacing]
  );

  // Render stacked bars
  const renderBars = () => {
    return data.map((point, index) => {
      const x = padding.left + index * barSpacing + (barSpacing - barWidth) / 2;

      // Positive stack (from zero upwards)
      let positiveY = zeroY;
      const positiveBars: React.ReactElement[] = [];

      if (visibility.solar && point.solarEnergy > 0) {
        const barHeight = Math.abs(valueToY(point.solarEnergy) - zeroY);
        positiveBars.push(
          <Rect
            key={`solar-${index}`}
            x={x}
            y={positiveY - barHeight}
            width={barWidth}
            height={barHeight}
            fill={colors.solar}
            rx={borderRadius}
            ry={borderRadius}
          />
        );
        positiveY -= barHeight;
      }

      if (visibility.battery && point.batteryEnergy > 0) {
        const barHeight = Math.abs(valueToY(point.batteryEnergy) - zeroY);
        positiveBars.push(
          <Rect
            key={`battery-discharge-${index}`}
            x={x}
            y={positiveY - barHeight}
            width={barWidth}
            height={barHeight}
            fill={colors.battery}
            rx={borderRadius}
            ry={borderRadius}
          />
        );
        positiveY -= barHeight;
      }

      if (visibility.grid && point.gridEnergy > 0) {
        const barHeight = Math.abs(valueToY(point.gridEnergy) - zeroY);
        positiveBars.push(
          <Rect
            key={`grid-import-${index}`}
            x={x}
            y={positiveY - barHeight}
            width={barWidth}
            height={barHeight}
            fill={colors.grid}
            rx={borderRadius}
            ry={borderRadius}
          />
        );
        positiveY -= barHeight;
      }

      // Negative stack (from zero downwards)
      let negativeY = zeroY;
      const negativeBars: React.ReactElement[] = [];

      if (visibility.load) {
        const barHeight = Math.abs(valueToY(-point.loadEnergy) - zeroY);
        negativeBars.push(
          <Rect
            key={`load-${index}`}
            x={x}
            y={negativeY}
            width={barWidth}
            height={barHeight}
            fill={colors.load}
            rx={borderRadius}
            ry={borderRadius}
          />
        );
        negativeY += barHeight;
      }

      if (visibility.battery && point.batteryEnergy < 0) {
        const barHeight = Math.abs(valueToY(point.batteryEnergy) - zeroY);
        negativeBars.push(
          <Rect
            key={`battery-charge-${index}`}
            x={x}
            y={negativeY}
            width={barWidth}
            height={barHeight}
            fill="#16A34A"
            rx={borderRadius}
            ry={borderRadius}
          />
        );
        negativeY += barHeight;
      }

      if (visibility.grid && point.gridEnergy < 0) {
        const barHeight = Math.abs(valueToY(point.gridEnergy) - zeroY);
        negativeBars.push(
          <Rect
            key={`grid-export-${index}`}
            x={x}
            y={negativeY}
            width={barWidth}
            height={barHeight}
            fill="#8B5CF6"
            rx={borderRadius}
            ry={borderRadius}
          />
        );
        negativeY += barHeight;
      }

      return [...positiveBars, ...negativeBars];
    });
  };

  // Render Y-axis grid lines (before bars)
  const renderYAxisGridLines = () => {
    const numTicks = 5;
    const gridLines: React.ReactElement[] = [];

    for (let i = 0; i <= numTicks; i++) {
      const value = yMax - (i * yRange) / numTicks;
      const y = padding.top + (i * innerHeight) / numTicks;

      // Grid line
      gridLines.push(
        <Line
          key={`grid-${i}`}
          x1={padding.left}
          y1={y}
          x2={padding.left + innerWidth}
          y2={y}
          stroke="#333"
          strokeWidth={0.5}
          strokeDasharray="4,4"
          opacity={0.3}
        />
      );
    }

    // Zero line (prominent)
    gridLines.push(
      <Line
        key="zero-line"
        x1={padding.left}
        y1={zeroY}
        x2={padding.left + innerWidth}
        y2={zeroY}
        stroke="#666"
        strokeWidth={2}
        opacity={0.7}
      />
    );

    return gridLines;
  };

  // Render Y-axis labels (after bars, so they appear on top)
  const renderYAxisLabels = () => {
    const numTicks = 5;
    const labels: React.ReactElement[] = [];

    for (let i = 0; i <= numTicks; i++) {
      const value = yMax - (i * yRange) / numTicks;
      const y = padding.top + (i * innerHeight) / numTicks;

      // Label (inside chart with background stroke for readability)
      const displayValue = Math.abs(value) >= 1000
        ? `${(value / 1000).toFixed(1)}k`
        : value.toFixed(0);

      // Background stroke for better readability
      labels.push(
        <SvgText
          key={`label-bg-${i}`}
          x={padding.left + 8}
          y={y + 4}
          fontSize={10}
          fill={cardBgColor}
          textAnchor="start"
          stroke={cardBgColor}
          strokeWidth={4}
          opacity={0.9}
        >
          {displayValue}
        </SvgText>
      );

      // Actual label
      labels.push(
        <SvgText
          key={`label-${i}`}
          x={padding.left + 8}
          y={y + 4}
          fontSize={10}
          fill="#888"
          textAnchor="start"
        >
          {displayValue}
        </SvgText>
      );
    }

    return labels;
  };

  // Render X-axis labels
  const renderXAxis = () => {
    return data.map((point, index) => {
      const x = padding.left + index * barSpacing + barSpacing / 2;
      const y = padding.top + innerHeight + 20;

      return (
        <SvgText
          key={`x-label-${index}`}
          x={x}
          y={y}
          fontSize={10}
          fill="#888"
          textAnchor="middle"
        >
          {point.label}
        </SvgText>
      );
    });
  };

  return (
    <View style={styles.container}>
      <View {...panResponder.panHandlers}>
        <Svg width={chartWidth} height={height}>
          {/* Y-axis grid lines (behind bars) */}
          {renderYAxisGridLines()}

          {/* Bars */}
          <G>{renderBars()}</G>

          {/* Y-axis labels (on top of bars) */}
          {renderYAxisLabels()}

          {/* X-axis */}
          {renderXAxis()}

          {/* Touch indicator line */}
          {touchX !== null && selectedDataPoint && (
            <Line
              x1={touchX}
              y1={padding.top}
              x2={touchX}
              y2={padding.top + innerHeight}
              stroke={textColor}
              strokeWidth={1}
              opacity={0.5}
              strokeDasharray="4,4"
            />
          )}
        </Svg>

        {/* Floating tooltip */}
        {touchX !== null && selectedDataPoint && (
          <View
            style={{
              position: 'absolute',
              top: 10,
              left: Math.max(10, Math.min(touchX - 100, chartWidth - 210)),
              backgroundColor: cardBgColor,
              borderRadius: 12,
              padding: 14,
              shadowColor: '#000',
              shadowOffset: { width: 0, height: 2 },
              shadowOpacity: 0.25,
              shadowRadius: 3.84,
              elevation: 5,
              minWidth: 200,
              borderWidth: 1,
              borderColor: 'rgba(255, 255, 255, 0.1)',
            }}
          >
            <Text style={{ color: textColor, fontSize: 13, fontWeight: '700', marginBottom: 8 }}>
              {formatTooltipDate(selectedDataPoint.timestamp, selectedDataPoint.label)}
            </Text>
            {visibility.solar && selectedDataPoint.solarEnergy > 0 && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: colors.solar }} />
                  <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>Solar</Text>
                </View>
                <Text style={{ color: textColor, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.solarEnergy.toFixed(2)} kWh
                </Text>
              </View>
            )}
            {visibility.battery && selectedDataPoint.batteryEnergy > 0 && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: colors.battery }} />
                  <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>Battery Discharged</Text>
                </View>
                <Text style={{ color: textColor, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.batteryEnergy.toFixed(2)} kWh
                </Text>
              </View>
            )}
            {visibility.battery && selectedDataPoint.batteryEnergy < 0 && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: '#16A34A' }} />
                  <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>Battery Charged</Text>
                </View>
                <Text style={{ color: textColor, fontSize: 12, fontWeight: '700' }}>
                  {Math.abs(selectedDataPoint.batteryEnergy).toFixed(2)} kWh
                </Text>
              </View>
            )}
            {visibility.grid && selectedDataPoint.gridEnergy > 0 && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: colors.grid }} />
                  <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>Grid Imported</Text>
                </View>
                <Text style={{ color: textColor, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.gridEnergy.toFixed(2)} kWh
                </Text>
              </View>
            )}
            {visibility.grid && selectedDataPoint.gridEnergy < 0 && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: '#8B5CF6' }} />
                  <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>Grid Exported</Text>
                </View>
                <Text style={{ color: textColor, fontSize: 12, fontWeight: '700' }}>
                  {Math.abs(selectedDataPoint.gridEnergy).toFixed(2)} kWh
                </Text>
              </View>
            )}
            {visibility.load && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: colors.load }} />
                  <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>Load</Text>
                </View>
                <Text style={{ color: textColor, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.loadEnergy.toFixed(2)} kWh
                </Text>
              </View>
            )}
          </View>
        )}
      </View>

      {/* Legend */}
      <View style={styles.legend}>
        <View style={styles.legendRow}>
          {visibility.solar && (
            <View style={styles.legendItem}>
              <View style={[styles.legendColor, { backgroundColor: colors.solar }]} />
              <Text style={styles.legendText}>Solar Produced</Text>
            </View>
          )}
          {visibility.battery && (
            <>
              <View style={styles.legendItem}>
                <View style={[styles.legendColor, { backgroundColor: colors.battery }]} />
                <Text style={styles.legendText}>Battery Discharged</Text>
              </View>
              <View style={styles.legendItem}>
                <View style={[styles.legendColor, { backgroundColor: '#16A34A' }]} />
                <Text style={styles.legendText}>Battery Charged</Text>
              </View>
            </>
          )}
        </View>
        <View style={styles.legendRow}>
          {visibility.grid && (
            <>
              <View style={styles.legendItem}>
                <View style={[styles.legendColor, { backgroundColor: colors.grid }]} />
                <Text style={styles.legendText}>Grid Imported</Text>
              </View>
              <View style={styles.legendItem}>
                <View style={[styles.legendColor, { backgroundColor: '#8B5CF6' }]} />
                <Text style={styles.legendText}>Grid Exported</Text>
              </View>
            </>
          )}
          {visibility.load && (
            <View style={styles.legendItem}>
              <View style={[styles.legendColor, { backgroundColor: colors.load }]} />
              <Text style={styles.legendText}>Load Consumed</Text>
            </View>
          )}
        </View>
      </View>
    </View>
  );
};

export default StackedBarChart;
