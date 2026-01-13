import React, { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, LayoutChangeEvent, Text, View } from 'react-native';
import Animated, { FadeInUp, useAnimatedStyle, useSharedValue, withSpring } from 'react-native-reanimated';
import Svg, { Circle, Defs, LinearGradient, Path, Rect, Stop, Text as SvgText } from 'react-native-svg';
import { useSelectedSiteDevice } from '../../../../../../hooks/useSelectedSiteDevice';
import { useSOH } from '../../../../../../hooks/useSOH';
import { makeBatterySOHChartStyles } from './BatterySOHChart.styles';
import Tooltip from './Tooltip';

type Props = {
  COLORS: Record<string, string>;
  isDark: boolean;
};

type Datum = { x: string | number; y: number };

export default function BatterySOHChart ({ COLORS, isDark }: Props) {
  const { t } = useTranslation();
  const { device, hasDevice } = useSelectedSiteDevice();
  const { sohData, isLoading, error } = useSOH();
  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);

  const styles = makeBatterySOHChartStyles(isDark, COLORS);
  const chartScale = useSharedValue(1);
  const padding = { top: 20, right: 20, bottom: 80, left: 30 };

  const batterySOHData: Datum[] = useMemo(() => {
    if (!sohData?.data || !Array.isArray(sohData.data)) {
      return [];
    }

    return sohData.data.map((point: any, index: number) => {
      try {
        const date = new Date(point.timestamp);
        const timeString = isNaN(date.getTime())
          ? `${index.toString().padStart(2, '0')}:00`
          : date.toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit'
          });

        return {
          x: timeString,
          y: typeof point.value === 'number' ? point.value : 0,
        };
      } catch (error) {
        return {
          x: `${index.toString().padStart(2, '0')}:00`,
          y: typeof point.value === 'number' ? point.value : 0,
        };
      }
    });
  }, [sohData]);

  const timeLabels = useMemo(() => {
    const labels = batterySOHData.map(d => d.x as string);
    const total = labels.length;

    if (total <= 12) return labels;

    const selected: string[] = [];
    const step = Math.floor((total - 1) / 11);
    for (let i = 0; i < total; i += step) {
      selected.push(labels[i]);
      if (selected.length === 11) break;
    }

    if (selected[selected.length - 1] !== labels[total - 1]) {
      selected.push(labels[total - 1]);
    }

    return selected;
  }, [batterySOHData]);

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

  const batteryValues = batterySOHData.length > 0
    ? batterySOHData.map(d => (typeof d.y === 'number' ? d.y : 0))
    : [];

  const batteryMax = batteryValues.length > 0
    ? Math.min(Math.max(...batteryValues) * 1.1, 100)
    : 100;

  const batteryMin = batteryValues.length > 0
    ? Math.min(...batteryValues) * 0.9
    : 0;

  const batteryRange = Math.max(batteryMax - batteryMin, 1);
  const yScaleBattery = (v: number) => innerH - ((v - batteryMin) / batteryRange) * innerH;

  const stepX = batterySOHData.length > 1 ? innerW / (batterySOHData.length - 1) : innerW;

  const createPath = (data: Datum[]) => {
    if (!data || data.length === 0) return '';

    const points = data.map((d, i) => {
      const offsetX = i === 0 ? 13 : (i === 1 ? 4 : 0);
      return {
        x: padding.left + i * stepX + offsetX,
        y: padding.top + yScaleBattery(typeof d.y === 'number' ? d.y : 0),
      };
    });

    if (points.length === 0) return '';
    return points.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
  };

  const batteryPath = createPath(batterySOHData);

  if (!hasDevice || !device?.id) return null;

  if (isLoading) {
    return (
      <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('dashboardBatterySOHChart.title')}</Text>
        </View>
        <View style={[styles.chartContainer, { justifyContent: 'center', alignItems: 'center' }]}>
          <ActivityIndicator color={COLORS.accent} size="large" />
        </View>
      </Animated.View>
    );
  }

  if (error) {
    return (
      <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('dashboardBatterySOHChart.title')}</Text>
        </View>
        <View style={[styles.chartContainer, { justifyContent: 'center', alignItems: 'center' }]}>
          <Text style={{ color: COLORS.error || '#FF6B6B', textAlign: 'center' }}>{error}</Text>
        </View>
      </Animated.View>
    );
  }

  if (!sohData || batterySOHData.length === 0) {
    return (
      <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('dashboardBatterySOHChart.title')}</Text>
        </View>
        <View style={[styles.chartContainer, { justifyContent: 'center', alignItems: 'center' }]}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
            {t('dashboardBatterySOHChart.noData')}
          </Text>
        </View>
      </Animated.View>
    );
  }

  return (
    <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{t('dashboardBatterySOHChart.title')}</Text>
      </View>

      <Animated.View style={[styles.chartContainer, animatedChartStyle]} onLayout={onChartLayout} pointerEvents="box-none">
        <Svg width={chartSize.width} height={chartSize.height}>
          <Defs>
            <LinearGradient id="sohGrad" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor="#3B82F6" stopOpacity={0.3} />
              <Stop offset="100%" stopColor="#3B82F6" stopOpacity={0.05} />
            </LinearGradient>
          </Defs>

          {Array.from({ length: 5 }).map((_, i) => {
            const y = padding.top + (i * innerH) / 4;
            return (
              <Rect
                key={i}
                x={padding.left}
                y={y}
                width={innerW}
                height={1}
                fill={COLORS.border}
                opacity={0.3}
              />
            );
          })}

          {Array.from({ length: 5 }).map((_, i) => {
            const value = batteryMin + (batteryRange * (4 - i)) / 4;
            const y = padding.top + (i * innerH) / 4;
            const labelText = Math.round(value).toString() + '%';
            return (
              <SvgText
                key={`battery-${i}`}
                x={padding.left - 5}
                y={y + 4}
                fontSize="10"
                fill={COLORS.textSoft}
                textAnchor="end"
              >
                {labelText}
              </SvgText>
            );
          })}

          {timeLabels.map((label, labelIndex) => {
            const dataIndex = batterySOHData.findIndex(d => d.x === label);
            const x = padding.left + (dataIndex >= 0 ? dataIndex * stepX : labelIndex * (innerW / Math.max(timeLabels.length - 1, 1)));
            return (
              <SvgText
                key={labelIndex}
                x={x}
                y={padding.top + innerH + 55}
                fontSize="12"
                fill={COLORS.textSoft}
                textAnchor="start"
                fontWeight="bold"
                transform={`rotate(-90 ${x} ${padding.top + innerH + 55})`}
              >
                {label}
              </SvgText>
            );
          })}

          <Rect
            x={0}
            y={0}
            width={chartSize.width}
            height={chartSize.height}
            fill="transparent"
            onPress={() => setSelectedIndex(null)}
          />

          <Path d={batteryPath} stroke="#3B82F6" strokeWidth={3} fill="none" />

          {batterySOHData.map((_, i) => {
            const hx = padding.left + i * stepX;
            return (
              <Rect
                key={`hit-${i}`}
                x={hx}
                y={padding.top}
                width={stepX}
                height={innerH}
                fill="transparent"
                onPress={() => {
                  setSelectedIndex(selectedIndex === i ? null : i);
                }}
              />
            );
          })}

          {batterySOHData.map((d, i) => {
            const offsetX = i === 0 ? 13 : (i === 1 ? 4 : 0);
            const x = padding.left + i * stepX + offsetX;
            const y = padding.top + yScaleBattery(d.y);
            return (
              <Circle
                key={`battery-${i}`}
                cx={x}
                cy={y}
                r={selectedIndex === i ? 6 : 4}
                fill="#3B82F6"
                onPress={() => {
                  setSelectedIndex(selectedIndex === i ? null : i);
                }}
              />
            );
          })}

          {selectedIndex !== null && selectedIndex < batterySOHData.length && (() => {
            const i = selectedIndex;
            const d = batterySOHData[i];
            const offsetX = i === 0 ? 13 : (i === 1 ? 4 : 0);
            const cx = padding.left + i * stepX + offsetX;
            const cy = padding.top + yScaleBattery(d.y);
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
      </Animated.View>

      <View style={styles.legend}>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#3B82F6' }]} />
          <Text style={styles.legendText}>{t('dashboardBatterySOHChart.legend.soh')}</Text>
        </View>
      </View>
    </Animated.View>
  );
}