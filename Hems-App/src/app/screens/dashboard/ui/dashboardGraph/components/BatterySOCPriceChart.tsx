import React, { useState, useMemo, useEffect, useCallback } from 'react';
import { View, Text, Switch, LayoutChangeEvent, Platform, ActivityIndicator } from 'react-native';
import Svg, { Defs, LinearGradient, Stop, Rect, Path, Circle, Text as SvgText } from 'react-native-svg';
import Animated, { FadeInUp, useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';
import { makeBatterySOCPriceChartStyles } from './BatterySOCPriceChart.styles';
import { useTranslation } from 'react-i18next';
import { mobileDashboardService, SOCResponseDto, SOCDataPoint } from '../../../../../../api/services/mobileDashboardService';
import { useSelectedSiteDevice } from '../../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../../hooks/useDashboardRefresh';
import Tooltip from './Tooltip';

type Props = {
  COLORS: Record<string, string>;
  isDark: boolean;
};

type Datum = { x: string | number; y: number };

export default function BatterySOCChart ({ COLORS, isDark }: Props) {
  const { t } = useTranslation();
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const [batterySOCEnabled, setBatterySOCEnabled] = useState(true);
  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [socData, setSocData] = useState<SOCResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);

  useEffect(() => {
    if (selectedIndex !== null) {}
  }, [selectedIndex]);


  const styles = makeBatterySOCPriceChartStyles(isDark, COLORS);
  const chartScale = useSharedValue(1);
  const padding = { top: 20, right: 20, bottom: 80, left: 30 };

  const loadSOCData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }

    setLoading(true);
    try {
      const data = await mobileDashboardService.getSOC(device.id);
      setSocData(data);
    } catch (error) {setSocData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);


  useEffect(() => {
    if (hasDevice && device?.id) {
      loadSOCData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDevice, device?.id]);

  useEffect(() => {const unregisterSOC = registerRefreshCallback(loadSOCData);return () => {unregisterSOC();
    };
  }, [registerRefreshCallback, loadSOCData]);


  const batterySOCData: Datum[] = useMemo(() => {
    if (!socData?.data || !Array.isArray(socData.data)) {
      return [];
    }

    return socData.data.map((point, index) => {
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
          y: typeof point.value === 'number' ? point.value : 0
        };
      } catch (error) {return {
          x: `${index.toString().padStart(2, '0')}:00`,
          y: typeof point.value === 'number' ? point.value : 0
        };
      }
    });
  }, [socData]);

  const timeLabels = useMemo(() => {
    if (batterySOCData.length === 0) {
      return ['00:00', '04:00', '08:00', '12:00', '16:00', '20:00', '24:00'];
    }

    const labels = batterySOCData.map(d => d.x as string);

    if (labels.length <= 24) {
      const showHours = [0, 4, 8, 12, 16, 20, 23];
      return labels.filter((_, index) => showHours.includes(index));
    }

    const step = Math.max(1, Math.floor(labels.length / 7));
    return labels.filter((_, index) => index % step === 0);
  }, [batterySOCData]);

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

  const batteryValues = batterySOCEnabled && batterySOCData.length > 0
    ? batterySOCData.map(d => typeof d.y === 'number' ? d.y : 0)
    : [];

  const batteryMax = batteryValues.length > 0
    ? Math.min(Math.max(...batteryValues) * 1.1, 100)
    : 100;

  const batteryMin = batteryValues.length > 0 ? Math.min(...batteryValues) * 0.9 : 0;
  const batteryRange = Math.max(batteryMax - batteryMin, 1); // Prevent division by zero

  const yScaleBattery = (v: number) => innerH - ((v - batteryMin) / batteryRange) * innerH;
  const stepX = batterySOCData.length > 1 ? innerW / (batterySOCData.length - 1) : innerW;

  const createPath = (data: Datum[]) => {
    if (!data || data.length === 0) {
      return '';
    }

    const points = data.map((d, i) => {
      const offsetX = i === 0 ? 13 : (i === 1 ? 4 : 0);
      return {
        x: padding.left + i * stepX + offsetX,
        y: padding.top + yScaleBattery(typeof d.y === 'number' ? d.y : 0),
      };
    });

    if (points.length === 0) {
      return '';
    }

    return points.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
  };

  const batteryPath = batterySOCEnabled ? createPath(batterySOCData) : '';

  // Don't render if no device
  if (!hasDevice || !device?.id) {
    return null;
  }

  if (loading) {
    return (
      <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('dashboardBatteryPriceChart.title')}</Text>
        </View>
        <View style={[styles.chartContainer, { justifyContent: 'center', alignItems: 'center' }]}>
          <ActivityIndicator color={COLORS.accent} size="large" />
        </View>
      </Animated.View>
    );
  }

  if (!socData || batterySOCData.length === 0) {
    return (
      <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>{t('dashboardBatteryPriceChart.title')}</Text>
        </View>
        <View style={[styles.chartContainer, { justifyContent: 'center', alignItems: 'center' }]}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
            {t('dashboardBatteryPriceChart.noData')}
          </Text>
        </View>
      </Animated.View>
    );
  }

  return (
    <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{t('dashboardBatteryPriceChart.title')}</Text>
      </View>

      <Animated.View style={[styles.chartContainer, animatedChartStyle]} onLayout={onChartLayout} pointerEvents="box-none">
        <Svg width={chartSize.width} height={chartSize.height}>
          <Defs>
            <LinearGradient id="batteryGrad" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor="#3B82F6" stopOpacity={0.3} />
              <Stop offset="100%" stopColor="#3B82F6" stopOpacity={0.05} />
            </LinearGradient>
          </Defs>

          {/* Grid lines */}
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
            const dataIndex = batterySOCData.findIndex(d => d.x === label);
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

          {batterySOCEnabled && (
            <Path
              d={batteryPath}
              stroke="#3B82F6"
              strokeWidth={3}
              fill="none"
            />
          )}

          {batterySOCEnabled &&
            batterySOCData.map((_, i) => {
              const hx = padding.left + i * stepX;
              return (
                <Rect
                  key={`hit-${i}`}
                  x={hx}
                  y={padding.top}
                  width={stepX}
                  height={innerH}
                  fill="transparent"
                  onPress={() => {setSelectedIndex(selectedIndex === i ? null : i);
                  }}
                />
              );
            })}

          {batterySOCEnabled &&
            batterySOCData.map((d, i) => {
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
                  onPress={() => {setSelectedIndex(selectedIndex === i ? null : i);
                  }}
                />
              );
            })}

          {batterySOCEnabled && selectedIndex !== null && selectedIndex < batterySOCData.length && (() => {
            const i = selectedIndex;
            const d = batterySOCData[i];
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

      {/* <View style={styles.switchContainer}>
        <View style={styles.switchItem}>
          <Text style={styles.switchLabel}>{t('dashboardBatteryPriceChart.switch.soc')}</Text>
          <Switch
            value={batterySOCEnabled}
            onValueChange={setBatterySOCEnabled}
            trackColor={{ false: COLORS.border, true: '#3B82F6' }}
            thumbColor={batterySOCEnabled ? '#FFFFFF' : COLORS.textSoft}
            style={{
              transform: [
                { scaleX: Platform.OS === 'android' ? 0.8 : 0.5 },
                { scaleY: Platform.OS === 'android' ? 0.8 : 0.5 },
              ],
              alignSelf: 'center',
            }}
          />
        </View>
      </View> */}

      <View style={styles.legend}>
        {batterySOCEnabled && (
          <View style={styles.legendItem}>
            <View style={[styles.legendColor, { backgroundColor: '#3B82F6' }]} />
            <Text style={styles.legendText}>{t('dashboardBatteryPriceChart.legend.soc')}</Text>
          </View>
        )}

        {/* Power Metrics legend */}
        {/* <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#FACC15' }]} />
          <Text style={styles.legendText}>Solar Power</Text>
        </View> */}
        {/* <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#22C55E' }]} />
          <Text style={styles.legendText}>Battery Power</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#3B82F6' }]} />
          <Text style={styles.legendText}>Grid Power</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#A855F7' }]} />
          <Text style={styles.legendText}>Load Power</Text>
        </View> */}
      </View>
    </Animated.View>
  );
}
