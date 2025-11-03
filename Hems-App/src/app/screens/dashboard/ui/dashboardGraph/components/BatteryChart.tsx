import React, { useMemo, useState, useEffect, useCallback } from 'react';
import { LayoutChangeEvent, Text, View, ActivityIndicator } from 'react-native';
import Svg, { Defs, LinearGradient, Stop, Rect, Path, Circle, Text as SvgText } from 'react-native-svg';

import Animated, { FadeInUp, useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';
import { getBatteryChartStyles } from './BatteryChart.styles';
import { useTranslation } from 'react-i18next';
import { useSelectedSiteDevice } from '../../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService, BatteryResponseDto } from '../../../../../../api/services/mobileDashboardService';
import Tooltip from './Tooltip';

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

   const { t } = useTranslation();
  const styles = getBatteryChartStyles(COLORS);
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  
  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [batteryData, setBatteryData] = useState<BatteryResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  
  const padding = { top: 50, right: 8, bottom: 60, left: 8 };

  const chartScale = useSharedValue(1);

  // Load Battery data from API
  const loadBatteryData = useCallback(async () => {if (!hasDevice || !device?.id) {setLoading(false);
      return;
    }setLoading(true);
    try {
      const data = await mobileDashboardService.getBattery(device.id);setBatteryData(data);
    } catch (error) {setBatteryData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  // Load data on mount and when dependencies change
  useEffect(() => {
    if (hasDevice && device?.id) {
      loadBatteryData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDevice, device?.id]);

  // Register refresh callback
  useEffect(() => {const unregister = registerRefreshCallback(loadBatteryData);return () => {unregister();
    };
  }, [registerRefreshCallback, loadBatteryData]);

  // Transform API data to chart format
  const chartData: Datum[] = useMemo(() => {
    if (!batteryData?.data || !Array.isArray(batteryData.data)) {
      return [];
    }
    
    return batteryData.data.map((point: any, index: number) => {
      try {
        const date = new Date(point.timestamp);
        const timeString = isNaN(date.getTime()) 
          ? `${index.toString().padStart(2, '0')}:00`
          : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
        
        return {
          x: timeString,
          y: typeof point.value === 'number' ? point.value : 0
        };
      } catch (error) {return {
          x: `${index.toString().padStart(2, '0')}:00`,
          y: typeof point.value === 'number' ? point.value : 0
        };
      }
    });
  }, [batteryData]);

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

  const stepX = innerW / Math.max(chartData.length, 1);
  const maxY = Math.max(...chartData.map(d => d.y)) * 1.1 || 1;
  const yScale = (v: number) => innerH - (v / maxY) * innerH;

  const points = chartData.map((d, i) => ({
    x: padding.left + i * stepX + stepX / 2,
    y: padding.top + yScale(d.y),
  }));

  const pathD = points.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
  const areaD = `${pathD} L ${padding.left + (chartData.length - 1) * stepX + stepX / 2} ${
    padding.top + innerH
  } L ${padding.left + stepX / 2} ${padding.top + innerH} Z`;

  const stats = useMemo(() => {
    if (batteryData?.statistics) {
      return {
        avg: Math.round(batteryData.statistics.average),
        peak: Math.round(batteryData.statistics.peak),
        current: Math.round(batteryData.statistics.current)
      };
    }
    
    if (chartData.length > 0) {
      return {
        avg: Math.round(chartData.reduce((s, d) => s + d.y, 0) / chartData.length),
        peak: Math.max(...chartData.map(d => d.y)),
        current: chartData[chartData.length - 1]?.y || 0
      };
    }
    
    return { avg: 0, peak: 0, current: 0 };
  }, [batteryData, chartData]);

  const avg = stats.avg;
  const peak = stats.peak;
  const current = stats.current;

  return (
    <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
    <Text style={styles.title}>{t('dashboardBatteryPriceChart.title')}</Text>
      <Animated.View style={[styles.chartContainer, animatedChartStyle]} onLayout={onChartLayout}>
        {loading ? (
          <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
            <ActivityIndicator color={COLORS.accent} size="large" />
          </View>
        ) : !hasDevice || !device?.id ? (
          <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
            <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
              {t('dashboardBatteryChart.noDevice')}
            </Text>
          </View>
        ) : !batteryData || chartData.length === 0 ? (
          <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
            <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
              {t('dashboardBatteryChart.noData')}
            </Text>
          </View>
        ) : (
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

          {chartData.map((_: any, i: number) => {
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

          
          {chartData.map((d, i) => {
            // For day view, show specific hours like MultiLineChart
            const showHours = [0, 4, 8, 12, 16, 20, 23];
            
            // Only show labels for specific hours to avoid crowding
            if (chartData.length >= 20 && !showHours.includes(i)) {
              return null;
            }
            
            const labelX = padding.left + i * stepX + stepX / 2;
            const labelY = padding.top + innerH + 45;
            return (
              <SvgText
                key={`time-${i}`}
                x={labelX}
                y={labelY}
                fontSize="12"
                fill={COLORS.textSoft}
                textAnchor="start"
                fontWeight="bold"
                transform={`rotate(-90 ${labelX} ${labelY})`}
              >
                {d.x}
              </SvgText>
            );
          })}

          {selectedIndex !== null && selectedIndex < chartData.length && (() => {
            const i = selectedIndex;
            const d = chartData[i];
            const cx = padding.left + i * stepX + stepX / 2;
            const cy = padding.top + yScale(d.y);
            const timeLabel = d.x as string;
            const valueLabel = `${Math.round(d.y)}%`;
            const labelText = `${timeLabel}\n${valueLabel}`;
            return (
              <Tooltip
                cx={cx}
                cy={cy}
                label={labelText}
                padding={padding}
                innerW={innerW}
                COLORS={COLORS}
              />
            );
          })()}

        </Svg>
        )}
      </Animated.View>
      
      <View style={styles.statsContainer}>
        <View style={styles.statItem}>
          <Text style={styles.statLabel}>{t('dashboardBatteryChart.stats.avg')}</Text>
          <Text style={styles.statValue}>{avg}%</Text>
        </View>
        <View style={styles.statItem}>
         <Text style={styles.statLabel}>{t('dashboardBatteryChart.stats.peak')}</Text>
          <Text style={styles.statValue}>{peak}%</Text>
        </View>
        <View style={styles.statItem}>
         <Text style={styles.statLabel}>{t('dashboardBatteryChart.stats.current')}</Text>
          <Text style={styles.statValue}>{current}%</Text>
        </View>
      </View>
    </Animated.View>
  );
}
