import React, { useMemo, useState } from 'react';
import { LayoutChangeEvent, Text, View } from 'react-native';
import Svg, { Defs, LinearGradient, Stop, Rect, Path, Circle } from 'react-native-svg';
import Animated, { FadeInUp, useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';
import { getChargeDischargeChartStyles } from './ChargeDischargeChart.styles';

type Datum = { x: string | number; y: number };

type Props = {
  COLORS: Record<string, string>;
  selectedIndex: number | null;
  setSelectedIndex: (i: number | null) => void;
};

export default function ChargeDischargeChart({
  COLORS,
  selectedIndex,
  setSelectedIndex,
}: Props) {
  const styles = getChargeDischargeChartStyles(COLORS);
  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const padding = { top: 10, right: 8, bottom: 18, left: 8 };

  const chartScale = useSharedValue(1);

  const chargeDischargeData: Datum[] = [
    { x: '00:00', y: 0.2 },
    { x: '04:00', y: 0.5 },
    { x: '08:00', y: 0.1 },
    { x: '12:00', y: 4.5 },
    { x: '16:00', y: 2.8 },
    { x: '20:00', y: 0.3 },
  ];

  const { innerW, innerH } = useMemo(() => {
    const cw = Math.max(chartSize.width - padding.left - padding.right, 1);
    const ch = Math.max(chartSize.height - padding.top - padding.bottom, 1);
    return { innerW: cw, innerH: ch };
  }, [chartSize]);

  const onChartLayout = (e: LayoutChangeEvent) => {
    const { width: w, height: h } = e.nativeEvent.layout;
    if (w !== chartSize.width || h !== chartSize.height) {
      setChartSize({ width: w, height: h });
      chartScale.value = withSpring(1, { damping: 15, stiffness: 150 });
    }
  };

  const animatedChartStyle = useAnimatedStyle(() => ({
    transform: [{ scale: chartScale.value }],
  }));

  const stepX = innerW / Math.max(chargeDischargeData.length, 1);
  const maxY = Math.max(...chargeDischargeData.map(d => d.y)) * 1.1 || 1;
  const yScale = (v: number) => innerH - (v / maxY) * innerH;

  const points = chargeDischargeData.map((d, i) => ({
    x: padding.left + i * stepX + stepX / 2,
    y: padding.top + yScale(d.y),
  }));

  const pathD = points.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
  const areaD = `${pathD} L ${padding.left + (chargeDischargeData.length - 1) * stepX + stepX / 2} ${
    padding.top + innerH
  } L ${padding.left + stepX / 2} ${padding.top + innerH} Z`;

  const avg = Math.round(chargeDischargeData.reduce((s, d) => s + d.y, 0) / Math.max(chargeDischargeData.length, 1) * 10) / 10;
  const peak = Math.max(...chargeDischargeData.map(d => d.y));

  return (
    <Animated.View entering={FadeInUp.delay(300).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Charge & Discharge Power (kW)</Text>
        <View style={styles.legendContainer}>
          <View style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: COLORS.info || '#3B82F6' }]} />
            <Text style={styles.legendText}>Charge</Text>
          </View>
          <View style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: COLORS.warning || '#F59E0B' }]} />
            <Text style={styles.legendText}>Discharge</Text>
          </View>
        </View>
      </View>
      
      <Animated.View style={[styles.chartContainer, animatedChartStyle]} onLayout={onChartLayout}>
        <Svg width={chartSize.width} height={chartSize.height}>
          <Defs>
            <LinearGradient id="chargeGrad" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={COLORS.info || '#3B82F6'} stopOpacity={0.6} />
              <Stop offset="50%" stopColor={COLORS.info || '#3B82F6'} stopOpacity={0.3} />
              <Stop offset="100%" stopColor={COLORS.info || '#3B82F6'} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="chargeLineGrad" x1="0" y1="0" x2="1" y2="0">
              <Stop offset="0%" stopColor={COLORS.info || '#3B82F6'} stopOpacity={0.8} />
              <Stop offset="50%" stopColor={COLORS.info || '#3B82F6'} stopOpacity={1} />
              <Stop offset="100%" stopColor={COLORS.info || '#3B82F6'} stopOpacity={0.8} />
            </LinearGradient>
          </Defs>

          {Array.from({ length: 4 }).map((_, i) => {
            const y = padding.top + ((i + 1) * innerH) / 5;
            return (
              <Rect
                key={i}
                x={padding.left}
                y={y}
                width={innerW}
                height={1}
                fill={COLORS.slate200}
                opacity={0.3}
              />
            );
          })}

          <Rect x={0} y={0} width={chartSize.width} height={chartSize.height} fill="transparent" onPress={() => setSelectedIndex(null)} />

          <Path d={areaD} fill="url(#chargeGrad)" />
          
          <Path 
            d={pathD} 
            stroke="url(#chargeLineGrad)" 
            strokeWidth={3} 
            fill="none"
          />
          
          {points.map((p, i) => {
            const isSelected = selectedIndex === i;
            return (
              <Circle 
                key={i} 
                cx={p.x} 
                cy={p.y} 
                r={isSelected ? 6 : 4} 
                fill={COLORS.info || '#3B82F6'}
                onPress={() => setSelectedIndex(i)} 
              />
            );
          })}

          {chargeDischargeData.map((_, i) => {
            const hx = padding.left + i * stepX;
            return (
              <Rect
                key={`hit-${i}`}
                x={hx}
                y={padding.top}
                width={stepX}
                height={innerH}
                fill="transparent"
                onPress={() => setSelectedIndex(i)}
              />
            );
          })}

          <Path
            d={`M ${padding.left} ${padding.top + yScale(avg)} H ${padding.left + innerW}`}
            stroke={COLORS.slate400}
            strokeDasharray="4 4"
            strokeWidth={1}
            opacity={0.9}
          />
          <Path
            d={`M ${padding.left} ${padding.top + yScale(peak)} H ${padding.left + innerW}`}
            stroke={COLORS.info || '#3B82F6'}
            strokeDasharray="6 4"
            strokeWidth={1}
            opacity={0.95}
          />
        </Svg>
      </Animated.View>
      
      <View style={styles.statsContainer}>
        <View style={styles.statItem}>
          <Text style={styles.statLabel}>Avg</Text>
          <Text style={styles.statValue}>{avg} kW</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statLabel}>Peak</Text>
          <Text style={styles.statValue}>{peak} kW</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statLabel}>Current</Text>
          <Text style={styles.statValue}>{chargeDischargeData[chargeDischargeData.length - 1]?.y || 0} kW</Text>
        </View>
      </View>
    </Animated.View>
  );
}

