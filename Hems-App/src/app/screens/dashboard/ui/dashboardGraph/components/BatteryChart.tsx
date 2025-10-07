import React, { useMemo, useState } from 'react';
import { LayoutChangeEvent, Text, View } from 'react-native';
import Svg, { Defs, LinearGradient, Stop, Rect, Path, Circle } from 'react-native-svg';
import Animated, { FadeInUp, useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';
import { getBatteryChartStyles } from './BatteryChart.styles';

type Datum = { x: string | number; y: number };

type Props = {
  COLORS: Record<string, string>;
  selectedIndex: number | null;
  setSelectedIndex: (i: number | null) => void;
};

export default function BatteryChart({
  COLORS,
  selectedIndex,
  setSelectedIndex,
}: Props) {
  const styles = getBatteryChartStyles(COLORS);
  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const padding = { top: 10, right: 8, bottom: 18, left: 8 };

  const chartScale = useSharedValue(1);

  const batteryData: Datum[] = [
    { x: '00:00', y: 90 },
    { x: '04:00', y: 85 },
    { x: '08:00', y: 70 },
    { x: '12:00', y: 100 },
    { x: '16:00', y: 95 },
    { x: '20:00', y: 88 },
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

  const stepX = innerW / Math.max(batteryData.length, 1);
  const maxY = Math.max(...batteryData.map(d => d.y)) * 1.1 || 1;
  const yScale = (v: number) => innerH - (v / maxY) * innerH;

  const points = batteryData.map((d, i) => ({
    x: padding.left + i * stepX + stepX / 2,
    y: padding.top + yScale(d.y),
  }));

  const pathD = points.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
  const areaD = `${pathD} L ${padding.left + (batteryData.length - 1) * stepX + stepX / 2} ${
    padding.top + innerH
  } L ${padding.left + stepX / 2} ${padding.top + innerH} Z`;

  const avg = Math.round(batteryData.reduce((s, d) => s + d.y, 0) / Math.max(batteryData.length, 1));
  const peak = Math.max(...batteryData.map(d => d.y));

  return (
    <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
      <Text style={styles.title}>Battery (%)</Text>
      <Animated.View style={[styles.chartContainer, animatedChartStyle]} onLayout={onChartLayout}>
        <Svg width={chartSize.width} height={chartSize.height}>
          <Defs>
            <LinearGradient id="batteryGrad" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={COLORS.success || '#10B981'} stopOpacity={0.6} />
              <Stop offset="50%" stopColor={COLORS.success || '#10B981'} stopOpacity={0.3} />
              <Stop offset="100%" stopColor={COLORS.success || '#10B981'} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="batteryLineGrad" x1="0" y1="0" x2="1" y2="0">
              <Stop offset="0%" stopColor={COLORS.success || '#10B981'} stopOpacity={0.8} />
              <Stop offset="50%" stopColor={COLORS.success || '#10B981'} stopOpacity={1} />
              <Stop offset="100%" stopColor={COLORS.success || '#10B981'} stopOpacity={0.8} />
            </LinearGradient>
          </Defs>

          {Array.from({ length: 5 }).map((_, i) => {
            const y = padding.top + ((i + 1) * innerH) / 6;
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

          <Path d={areaD} fill="url(#batteryGrad)" />
          
          <Path 
            d={pathD} 
            stroke="url(#batteryLineGrad)" 
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
                fill={COLORS.success || '#10B981'}
                onPress={() => setSelectedIndex(i)} 
              />
            );
          })}

          {batteryData.map((_, i) => {
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
            stroke={COLORS.success || '#10B981'}
            strokeDasharray="6 4"
            strokeWidth={1}
            opacity={0.95}
          />
        </Svg>
      </Animated.View>
      
      <View style={styles.statsContainer}>
        <View style={styles.statItem}>
          <Text style={styles.statLabel}>Avg</Text>
          <Text style={styles.statValue}>{avg}%</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statLabel}>Peak</Text>
          <Text style={styles.statValue}>{peak}%</Text>
        </View>
        <View style={styles.statItem}>
          <Text style={styles.statLabel}>Current</Text>
          <Text style={styles.statValue}>{batteryData[batteryData.length - 1]?.y || 0}%</Text>
        </View>
      </View>
    </Animated.View>
  );
}
