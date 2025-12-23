import React, { useMemo, useState, useCallback } from 'react';
import { View, Text, Dimensions, PanResponder } from 'react-native';
import Svg, { G, Line, Text as SvgText, Rect, Circle } from 'react-native-svg';
import { useTranslation } from 'react-i18next';

interface EnergyTransaction {
  timestamp: number;
  buy: number;
  sell: number;
  price: number;
}

interface EnergyRevenueChartProps {
  data: EnergyTransaction[];
  height: number;
  range: 'Day' | 'Week' | 'Month' | 'Year';
  cardBgColor?: string;
  textColor?: string;
}

const EnergyRevenueChart: React.FC<EnergyRevenueChartProps> = ({
  data,
  height = 400,
  range,
  cardBgColor = '#1F2937',
  textColor = '#E5E7EB',
}) => {
  const { t } = useTranslation();
  const screenWidth = Dimensions.get('window').width;
  const padding = { top: 20, right: 20, bottom: 40, left: 30 };
  const chartWidth = screenWidth;
  const innerWidth = chartWidth - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;

  const [touchX, setTouchX] = useState<number | null>(null);
  const [selectedDataPoint, setSelectedDataPoint] = useState<EnergyTransaction | null>(null);

  // Check if data has any meaningful values (not all zeros)
  const hasValidData = useMemo(() => {
    if (!data || data.length === 0) return false;
    // Check if any transaction has non-zero buy or sell values
    return data.some(t => t.buy !== 0 || t.sell !== 0);
  }, [data]);

  // Use actual data (no mock data)
  const chartData = useMemo(() => {
    return data;
  }, [data]);

  // Calculate scale (centered at 0, like EnergyReviewCard)
  const totalItems = chartData.length;
  const barWidth = Math.max(3, (innerWidth / totalItems) - 2);
  const xScale = innerWidth / totalItems;

  const maxValue = useMemo(() => {
    const max = Math.max(...chartData.map(t => Math.max(t.buy, t.sell)));
    return max > 0 ? max : 1; // Prevent division by zero
  }, [chartData]);

  const centerY = innerHeight / 2;
  const yScale = (innerHeight / 2) / (maxValue * 1.1);

  // Generate bars
  const bars = useMemo(() => {
    if (innerWidth === 0 || innerHeight === 0) return [];

    return chartData.map((t, i) => {
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
  }, [chartData, xScale, innerWidth, innerHeight, yScale, centerY]);

  // Calculate which bar is at the touch position
  const getBarAtPosition = useCallback((x: number) => {
    const adjustedX = x - padding.left;
    const barIndex = Math.floor(adjustedX / xScale);

    if (barIndex >= 0 && barIndex < chartData.length) {
      // Bar'ın ortasındaki x pozisyonunu hesapla
      const barCenterX = (barIndex * xScale) + (xScale / 2);
      return { index: barIndex, data: chartData[barIndex], centerX: barCenterX };
    }
    return null;
  }, [xScale, chartData, padding.left]);

  // Pan responder for touch tracking
  const panResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => true,
    onMoveShouldSetPanResponder: () => true,
    onPanResponderGrant: (evt) => {
      const x = evt.nativeEvent.locationX;
      const bar = getBarAtPosition(x);
      if (bar) {
        // Bar'ın ortasına snap et
        setTouchX(bar.centerX);
        setSelectedDataPoint(bar.data);
      }
    },
    onPanResponderMove: (evt) => {
      const x = evt.nativeEvent.locationX;
      const bar = getBarAtPosition(x);
      if (bar) {
        // Bar'ın ortasına snap et
        setTouchX(bar.centerX);
        setSelectedDataPoint(bar.data);
      }
    },
    onPanResponderRelease: () => {
      setTouchX(null);
      setSelectedDataPoint(null);
    },
    onPanResponderTerminationRequest: () => true,
  }), [getBarAtPosition]);

  // Generate time labels
  const timeLabels = useMemo(() => {
    const labels: { x: number; label: string }[] = [];
    let step = 1;

    if (range === 'Day') {
      step = 3; // Show every 3 hours for day view
    } else if (range === 'Week') {
      step = 1; // Show all days
    } else if (range === 'Month') {
      step = 5; // Show every 5 days
    } else {
      step = 1; // Show all months for year
    }

    for (let i = 0; i < chartData.length; i += step) {
      const transaction = chartData[i];
      const date = new Date(transaction.timestamp);
      const x = (i + 0.5) * xScale;

      let label = '';
      if (range === 'Day') {
        // For day view, show hour (e.g., "00:00", "03:00", etc.)
        label = date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
      } else if (range === 'Week') {
        const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
        label = days[date.getDay() === 0 ? 6 : date.getDay() - 1];
      } else if (range === 'Month') {
        label = date.toLocaleDateString('en-US', { day: '2-digit', month: '2-digit' });
      } else {
        label = date.toLocaleDateString('en-US', { month: 'short' });
      }

      labels.push({ x, label });
    }
    return labels;
  }, [chartData, xScale, range]);

  // Y-axis labels (centered at 0)
  const yLabels = useMemo(() => {
    const labels: { y: number; value: string }[] = [];
    const step = maxValue / 3;

    // Positive values (sell - above center)
    for (let i = 1; i <= 3; i++) {
      const value = i * step;
      const y = centerY - value * yScale;
      if (y >= 0) {
        labels.push({ y, value: `+${value.toFixed(1)}` });
      }
    }

    // Zero line (center)
    labels.push({ y: centerY, value: '0' });

    // Negative values (buy - below center)
    for (let i = 1; i <= 3; i++) {
      const value = i * step;
      const y = centerY + value * yScale;
      if (y <= innerHeight) {
        labels.push({ y, value: `-${value.toFixed(1)}` });
      }
    }

    return labels;
  }, [maxValue, innerHeight, centerY, yScale]);

  // Show "No data available" if there's no data or all values are zero
  if (!hasValidData) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <Text style={{ color: textColor, fontSize: 15 }}>No data available</Text>
      </View>
    );
  }

  return (
    <View style={{ flex: 1, paddingHorizontal: 0 }}>
      <Svg width={chartWidth} height={height}>
        <G x={padding.left} y={padding.top}>
          {/* Grid lines */}
          {yLabels.map((label, i) => (
            <Line
              key={`grid-${i}`}
              x1={0}
              y1={label.y}
              x2={innerWidth}
              y2={label.y}
              stroke={label.value === '0' ? textColor : '#333'}
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
              fill={textColor}
              opacity={label.value === '0' ? 0.8 : 0.6}
              fontWeight={label.value === '0' ? '700' : '400'}
              textAnchor="end"
              alignmentBaseline="middle">
              {label.value}
            </SvgText>
          ))}

          {/* Bar Chart - Sell (positive/up) and Buy (negative/down) */}
          {bars.map((bar: any, idx: number) => {
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

          {/* Touch tracking line */}
          {touchX !== null && (
            <>
              <Line
                x1={touchX}
                y1={0}
                x2={touchX}
                y2={innerHeight}
                stroke={textColor}
                strokeWidth={1}
                strokeDasharray="4,3"
                opacity={0.7}
              />
              <Circle
                cx={touchX}
                cy={centerY}
                r={4}
                fill={textColor}
                opacity={0.9}
              />
              <Circle
                cx={touchX}
                cy={centerY}
                r={7}
                fill="transparent"
                stroke={textColor}
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
              y={innerHeight + 20}
              fontSize={10}
              fill={textColor}
              opacity={0.6}
              textAnchor="middle">
              {label.label}
            </SvgText>
          ))}

          {/* Transparent overlay for touch */}
          <Rect
            x={0}
            y={0}
            width={innerWidth}
            height={innerHeight}
            fill="transparent"
            {...panResponder.panHandlers}
          />
        </G>
      </Svg>

      {/* Floating info box */}
      {touchX !== null && selectedDataPoint && (
        <View style={{
          position: 'absolute',
          top: -85,
          left: Math.max(10, Math.min((touchX + padding.left) - 100, chartWidth - 210)),
          backgroundColor: cardBgColor,
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
          {/* Date/Time */}
          <Text style={{ color: textColor, fontSize: 13, fontWeight: '700', marginBottom: 8 }}>
            {range === 'Day'
              ? new Date(selectedDataPoint.timestamp).toLocaleString('en-US', {
                  day: '2-digit',
                  month: 'short',
                  hour: '2-digit',
                  minute: '2-digit',
                  hour12: false,
                })
              : new Date(selectedDataPoint.timestamp).toLocaleDateString('en-US', {
                  day: '2-digit',
                  month: 'short',
                })
            }
          </Text>

          {/* Buy Row */}
          <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
              <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: '#ef4444' }} />
              <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>
                {t('energyRevenueDetail.chart.gridBuy')}
              </Text>
            </View>
            <Text style={{ color: '#ef4444', fontSize: 12, fontWeight: '700' }}>
              {selectedDataPoint.buy.toFixed(1)} kWh
            </Text>
          </View>

          {/* Sell Row */}
          <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
              <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: '#10b981' }} />
              <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>
                {t('energyRevenueDetail.chart.gridSell')}
              </Text>
            </View>
            <Text style={{ color: '#10b981', fontSize: 12, fontWeight: '700' }}>
              {selectedDataPoint.sell.toFixed(1)} kWh
            </Text>
          </View>

          {/* Net Row */}
          <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 5 }}>
            <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>
              {t('energyRevenueDetail.chart.net')}
            </Text>
            <Text style={{ color: (selectedDataPoint.sell - selectedDataPoint.buy) >= 0 ? '#10b981' : '#ef4444', fontSize: 12, fontWeight: '700' }}>
              {(selectedDataPoint.sell - selectedDataPoint.buy) >= 0 ? '+' : ''}{(selectedDataPoint.sell - selectedDataPoint.buy).toFixed(1)} kWh
            </Text>
          </View>

          {/* Price Row */}
          <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
            <Text style={{ color: textColor, fontSize: 12, fontWeight: '500' }}>
              {t('energyRevenueDetail.chart.price')}
            </Text>
            <Text style={{ color: selectedDataPoint.price >= 0 ? '#10b981' : '#ef4444', fontSize: 12, fontWeight: '700' }}>
              {selectedDataPoint.price >= 0 ? '+' : ''}€{selectedDataPoint.price.toFixed(2)}
            </Text>
          </View>
        </View>
      )}

      {/* Legend */}
      <View style={{
        flexDirection: 'row',
        justifyContent: 'center',
        alignItems: 'center',
        gap: 20,
        marginTop: 10,
      }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
          <View style={{ width: 12, height: 12, borderRadius: 6, backgroundColor: '#ef4444' }} />
          <Text style={{ color: textColor, fontSize: 12, fontWeight: '600' }}>{t('energyRevenueDetail.chart.gridBuy')}</Text>
        </View>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
          <View style={{ width: 12, height: 12, borderRadius: 6, backgroundColor: '#10b981' }} />
          <Text style={{ color: textColor, fontSize: 12, fontWeight: '600' }}>{t('energyRevenueDetail.chart.gridSell')}</Text>
        </View>
      </View>
    </View>
  );
};

export default EnergyRevenueChart;
