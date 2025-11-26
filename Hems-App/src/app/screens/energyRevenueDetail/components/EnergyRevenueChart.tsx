import React, { useMemo, useState, useCallback } from 'react';
import { View, Text, Dimensions, PanResponder } from 'react-native';
import Svg, { G, Line, Text as SvgText, Rect, Circle } from 'react-native-svg';

interface EnergyTransaction {
  timestamp: number;
  buy: number;
  sell: number;
  price: number;
}

interface EnergyRevenueChartProps {
  data: EnergyTransaction[];
  height: number;
  range: 'Week' | 'Month' | 'Year';
  cardBgColor?: string;
  textColor?: string;
}

// Generate mock data - bugünden sonraki günleri dahil etme
const generateMockData = (range: 'Week' | 'Month' | 'Year'): EnergyTransaction[] => {
  const count = range === 'Week' ? 7 : range === 'Month' ? 30 : 12;
  const transactions: EnergyTransaction[] = [];
  const now = new Date();
  now.setHours(23, 59, 59, 999); // Bugünün sonuna set et

  // Başlangıç tarihini hesapla (geçmişe doğru)
  const startDate = new Date(now);
  if (range === 'Week' || range === 'Month') {
    startDate.setDate(startDate.getDate() - (count - 1)); // count-1 gün geriye git
  } else {
    startDate.setMonth(startDate.getMonth() - (count - 1)); // count-1 ay geriye git
  }
  startDate.setHours(0, 0, 0, 0);

  for (let i = 0; i < count; i++) {
    let timestamp: number;

    if (range === 'Week' || range === 'Month') {
      // Günlük data
      timestamp = new Date(startDate.getTime() + i * 24 * 60 * 60 * 1000).getTime();
    } else {
      // Aylık data (Year view için)
      const monthDate = new Date(startDate);
      monthDate.setMonth(monthDate.getMonth() + i);
      timestamp = monthDate.getTime();
    }

    // Bugünden sonraki tarihleri ekleme
    if (timestamp > now.getTime()) {
      continue;
    }

    const isSunnyDay = Math.random() > 0.4;

    const buy = isSunnyDay ? Math.random() * 20 + 10 : Math.random() * 40 + 30;
    const sell = isSunnyDay ? Math.random() * 50 + 40 : Math.random() * 15 + 5;
    const price = 0.15 + Math.random() * 0.1;

    transactions.push({
      timestamp,
      buy,
      sell,
      price,
    });
  }

  return transactions;
};

const EnergyRevenueChart: React.FC<EnergyRevenueChartProps> = ({
  data,
  height = 400,
  range,
  cardBgColor = '#1F2937',
  textColor = '#E5E7EB',
}) => {
  const screenWidth = Dimensions.get('window').width;
  const padding = { top: 80, right: 20, bottom: 40, left: 30 }; // Top increased for floating box
  const chartWidth = screenWidth;
  const innerWidth = chartWidth - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;

  const [touchX, setTouchX] = useState<number | null>(null);
  const [selectedDataPoint, setSelectedDataPoint] = useState<EnergyTransaction | null>(null);

  // Generate mock data
  const chartData = useMemo(() => {
    return data.length > 0 ? data : generateMockData(range);
  }, [range, data]);

  // Calculate scale (centered at 0, like EnergyReviewCard)
  const totalItems = chartData.length;
  const barWidth = Math.max(3, (innerWidth / totalItems) - 2);
  const xScale = innerWidth / totalItems;

  const maxValue = useMemo(() => {
    return Math.max(...chartData.map(t => Math.max(t.buy, t.sell)));
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
    const step = range === 'Week' ? 1 : range === 'Month' ? 5 : 1;

    for (let i = 0; i < chartData.length; i += step) {
      const transaction = chartData[i];
      const date = new Date(transaction.timestamp);
      const x = (i + 0.5) * xScale;

      let label = '';
      if (range === 'Week') {
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

  if (chartData.length === 0) {
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
                strokeWidth={1.5}
                strokeDasharray="4,4"
                opacity={0.5}
              />
              <Circle
                cx={touchX}
                cy={centerY}
                r={4}
                fill={textColor}
                opacity={0.8}
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
      {selectedDataPoint && (
        <View style={{
          position: 'absolute',
          top: 10,
          left: 20,
          right: 20,
          backgroundColor: cardBgColor,
          borderRadius: 12,
          padding: 12,
          shadowColor: '#000',
          shadowOffset: { width: 0, height: 2 },
          shadowOpacity: 0.25,
          shadowRadius: 8,
          elevation: 5,
        }}>
          <Text style={{ color: textColor, fontSize: 12, fontWeight: '600', marginBottom: 8 }}>
            {new Date(selectedDataPoint.timestamp).toLocaleDateString('en-US', {
              day: '2-digit',
              month: 'short',
              year: 'numeric',
            })}
          </Text>

          <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: 12 }}>
            <View style={{ flex: 1 }}>
              <Text style={{ color: '#ef4444', fontSize: 11, fontWeight: '500', marginBottom: 2 }}>
                Grid Buy
              </Text>
              <Text style={{ color: textColor, fontSize: 14, fontWeight: '700' }}>
                {selectedDataPoint.buy.toFixed(1)} kWh
              </Text>
            </View>

            <View style={{ flex: 1 }}>
              <Text style={{ color: '#10b981', fontSize: 11, fontWeight: '500', marginBottom: 2 }}>
                Grid Sell
              </Text>
              <Text style={{ color: textColor, fontSize: 14, fontWeight: '700' }}>
                {selectedDataPoint.sell.toFixed(1)} kWh
              </Text>
            </View>

            <View style={{ flex: 1 }}>
              <Text style={{ color: textColor, fontSize: 11, fontWeight: '500', marginBottom: 2, opacity: 0.7 }}>
                Net
              </Text>
              <Text style={{
                color: (selectedDataPoint.sell - selectedDataPoint.buy) >= 0 ? '#10b981' : '#ef4444',
                fontSize: 14,
                fontWeight: '700'
              }}>
                {(selectedDataPoint.sell - selectedDataPoint.buy) >= 0 ? '+' : ''}
                {(selectedDataPoint.sell - selectedDataPoint.buy).toFixed(1)} kWh
              </Text>
            </View>
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
          <Text style={{ color: textColor, fontSize: 12, fontWeight: '600' }}>Grid Buy</Text>
        </View>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
          <View style={{ width: 12, height: 12, borderRadius: 6, backgroundColor: '#10b981' }} />
          <Text style={{ color: textColor, fontSize: 12, fontWeight: '600' }}>Grid Sell</Text>
        </View>
      </View>
    </View>
  );
};

export default EnergyRevenueChart;
