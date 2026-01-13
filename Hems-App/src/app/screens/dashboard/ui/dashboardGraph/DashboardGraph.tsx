import MaterialIcons from '@react-native-vector-icons/material-icons';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, LayoutChangeEvent, Platform, Text, TouchableOpacity, Vibration, View } from 'react-native';
import Animated, { FadeInUp, useAnimatedStyle, useSharedValue, withSpring } from 'react-native-reanimated';
import { EnergyConsumptionResponseDto, EnergyProductionResponseDto, mobileDashboardService, SOCResponseDto } from '../../../../../api/services/mobileDashboardService';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { getDashboardGraphStyles } from './DashboardGraph.styles';
import type { DashboardGraphProps } from './DashboardGraph.types';
import AreaChart from './components/AreaChart';
import BarChart from './components/BarChart';
import MultiLineChart from './components/MultiLineChart';
import PowerMetricsChart from './components/PowerMetricsChart';

export default function DashboardGraph ({
  COLORS,
  isDark,
  selectedRange,
  setSelectedRange,
}: DashboardGraphProps) {
  const { t } = useTranslation();
  const styles = getDashboardGraphStyles(COLORS);
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();

  const [chartTypeProd, setChartTypeProd] = useState<'bar' | 'area'>('bar');
  const [chartTypeCons, setChartTypeCons] = useState<'bar' | 'area'>('area');
  const [selectedRangeProd, setSelectedRangeProd] = useState(selectedRange);
  const [selectedRangeCons, setSelectedRangeCons] = useState(selectedRange);
  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [selectedIndexProd, setSelectedIndexProd] = useState<number | null>(null);
  const [selectedIndexCons, setSelectedIndexCons] = useState<number | null>(null);

  const segmentedIndicatorPosition = useSharedValue(chartTypeCons === 'bar' ? 0 : 1);

  const safeVibrate = useCallback((duration: number) => {
    try {
      if (Platform.OS === 'android') {
        Vibration.vibrate(duration);
      } else if (Platform.OS === 'ios') {
        Vibration.vibrate();
      }
    } catch (error) {
    }
  }, []);

  const [productionData, setProductionData] = useState<EnergyProductionResponseDto | null>(null);
  const [consumptionData, setConsumptionData] = useState<EnergyConsumptionResponseDto | null>(null);
  const [productionLoading, setProductionLoading] = useState(true);
  const [consumptionLoading, setConsumptionLoading] = useState(true);

  const [batterySOCData, setBatterySOCData] = useState<SOCResponseDto | null>(null);
  const [batterySOCLoading, setBatterySOCLoading] = useState(true);

  const padding = selectedRange === 'Week'
    ? { top: 20, right: 8, bottom: 80, left: 8 }
    : { top: 20, right: 8, bottom: 80, left: 8 };

  const getApiPeriod = (uiPeriod: string): string => {
    const periodMap: Record<string, string> = {
      'Day': 'daily',
      'Week': 'weekly',
      'Month': 'monthly',
      'Year': 'yearly'
    };
    return periodMap[uiPeriod] || 'daily';
  };

  const loadProductionData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setProductionLoading(false);
      return;
    }

    const apiPeriod = getApiPeriod(selectedRangeProd);
    setProductionLoading(true);
    try {
      const data = await mobileDashboardService.getEnergyProduction(device.id, apiPeriod);
      setProductionData(data);
    } catch (error) {
      setProductionData(null);
    } finally {
      setProductionLoading(false);
    }
  }, [hasDevice, device?.id, selectedRangeProd]);

  const loadConsumptionData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setConsumptionLoading(false);
      return;
    }

    const apiPeriod = getApiPeriod(selectedRangeCons);
    setConsumptionLoading(true);
    try {
      const data = await mobileDashboardService.getEnergyConsumption(device.id, apiPeriod);
      setConsumptionData(data);
    } catch (error) {
      setConsumptionData(null);
    } finally {
      setConsumptionLoading(false);
    }
  }, [hasDevice, device?.id, selectedRangeCons]);

  const loadBatterySOCData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setBatterySOCLoading(false);
      return;
    }

    setBatterySOCLoading(true);
    try {
      const data = await mobileDashboardService.getSOC(device.id);
      setBatterySOCData(data);
    } catch (error) {
      setBatterySOCData(null);
    } finally {
      setBatterySOCLoading(false);
    }
  }, [hasDevice, device?.id]);

  const chartProductionData = useMemo(() => {
    if (!productionData?.data || !Array.isArray(productionData.data)) {
      return [];
    }

    return productionData.data.map((point, index) => {
      try {
        let timeString = point.timestamp;

        if (selectedRangeProd === 'Day') {
          const date = new Date(point.timestamp);
          timeString = isNaN(date.getTime())
            ? point.timestamp
            : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
        } else if (selectedRangeProd === 'Week') {
          try {
            const date = new Date(point.timestamp);
            if (!isNaN(date.getTime())) {
              const dayNames = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
              const dayIndex = date.getDay();
              timeString = dayIndex >= 0 && dayIndex < 7 ? dayNames[dayIndex] : point.timestamp;
            } else {
              const dayMatch = point.timestamp.match(/(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday|Pazartesi|Salı|Çarşamba|Perşembe|Cuma|Cumartesi|Pazar)/i);
              if (dayMatch) {
                const dayMap: { [key: string]: string } = {
                  'Monday': 'Pzt', 'Tuesday': 'Sal', 'Wednesday': 'Çar', 'Thursday': 'Per',
                  'Friday': 'Cum', 'Saturday': 'Cmt', 'Sunday': 'Paz',
                  'Pazartesi': 'Pzt', 'Salı': 'Sal', 'Çarşamba': 'Çar', 'Perşembe': 'Per',
                  'Cuma': 'Cum', 'Cumartesi': 'Cmt', 'Pazar': 'Paz'
                };
                timeString = dayMap[dayMatch[0]] || point.timestamp;
              } else {
                timeString = point.timestamp;
              }
            }
          } catch (error) {
            timeString = point.timestamp;
          }
        } else if (selectedRangeProd === 'Month') {
          timeString = point.timestamp.replace('Day ', '');
        } else if (selectedRangeProd === 'Year') {
          timeString = point.timestamp;
        }

        return {
          x: timeString,
          y: typeof point.value === 'number' && !isNaN(point.value) ? point.value : 0
        };
      } catch (error) {
        return {
          x: point.timestamp || `${index.toString().padStart(2, '0')}:00`,
          y: typeof point.value === 'number' && !isNaN(point.value) ? point.value : 0
        };
      }
    });
  }, [productionData, selectedRangeProd]);

  const chartConsumptionData = useMemo(() => {
    if (!consumptionData?.data || !Array.isArray(consumptionData.data)) {
      return [];
    }

    return consumptionData.data.map((point, index) => {
      try {
        let timeString = point.timestamp;

        if (selectedRangeCons === 'Day') {
          const date = new Date(point.timestamp);
          timeString = isNaN(date.getTime())
            ? point.timestamp
            : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
        } else if (selectedRangeCons === 'Week') {
          try {
            const date = new Date(point.timestamp);
            if (!isNaN(date.getTime())) {
              const dayNames = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
              const dayIndex = date.getDay();
              timeString = dayIndex >= 0 && dayIndex < 7 ? dayNames[dayIndex] : point.timestamp;
            } else {
              const dayMatch = point.timestamp.match(/(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday|Pazartesi|Salı|Çarşamba|Perşembe|Cuma|Cumartesi|Pazar)/i);
              if (dayMatch) {
                const dayMap: { [key: string]: string } = {
                  'Monday': 'Pzt', 'Tuesday': 'Sal', 'Wednesday': 'Çar', 'Thursday': 'Per',
                  'Friday': 'Cum', 'Saturday': 'Cmt', 'Sunday': 'Paz',
                  'Pazartesi': 'Pzt', 'Salı': 'Sal', 'Çarşamba': 'Çar', 'Perşembe': 'Per',
                  'Cuma': 'Cum', 'Cumartesi': 'Cmt', 'Pazar': 'Paz'
                };
                timeString = dayMap[dayMatch[0]] || point.timestamp;
              } else {
                timeString = point.timestamp;
              }
            }
          } catch (error) {
            timeString = point.timestamp;
          }
        } else if (selectedRangeCons === 'Month') {
          timeString = point.timestamp.replace('Day ', '');
        } else if (selectedRangeCons === 'Year') {
          timeString = point.timestamp;
        }

        return {
          x: timeString,
          y: typeof point.value === 'number' && !isNaN(point.value) ? point.value : 0
        };
      } catch (error) {
        return {
          x: point.timestamp || `${index.toString().padStart(2, '0')}:00`,
          y: typeof point.value === 'number' && !isNaN(point.value) ? point.value : 0
        };
      }
    });
  }, [consumptionData, selectedRangeCons]);

  const chartBatterySOCData = useMemo(() => {
    if (!batterySOCData?.data || !Array.isArray(batterySOCData.data)) {
      return [];
    }

    return batterySOCData.data.map((point, index) => {
      try {
        let timeString = point.timestamp;

        if (selectedRange === 'Day') {
          const date = new Date(point.timestamp);
          timeString = isNaN(date.getTime())
            ? point.timestamp
            : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
        } else if (selectedRange === 'Week') {
          try {
            const date = new Date(point.timestamp);
            if (!isNaN(date.getTime())) {
              const dayNames = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
              const dayIndex = date.getDay();
              timeString = dayIndex >= 0 && dayIndex < 7 ? dayNames[dayIndex] : point.timestamp;
            } else {
              const dayMatch = point.timestamp.match(/(Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday|Pazartesi|Salı|Çarşamba|Perşembe|Cuma|Cumartesi|Pazar)/i);
              if (dayMatch) {
                const dayMap: { [key: string]: string } = {
                  'Monday': 'Pzt', 'Tuesday': 'Sal', 'Wednesday': 'Çar', 'Thursday': 'Per',
                  'Friday': 'Cum', 'Saturday': 'Cmt', 'Sunday': 'Paz',
                  'Pazartesi': 'Pzt', 'Salı': 'Sal', 'Çarşamba': 'Çar', 'Perşembe': 'Per',
                  'Cuma': 'Cum', 'Cumartesi': 'Cmt', 'Pazar': 'Paz'
                };
                timeString = dayMap[dayMatch[0]] || point.timestamp;
              } else {
                timeString = point.timestamp;
              }
            }
          } catch (error) {
            timeString = point.timestamp;
          }
        } else if (selectedRange === 'Month') {
          timeString = point.timestamp.replace('Day ', '');
        } else if (selectedRange === 'Year') {
          timeString = point.timestamp;
        }

        return {
          x: timeString,
          y: typeof point.value === 'number' && !isNaN(point.value) ? point.value : 0
        };
      } catch (error) {
        return {
          x: `${index.toString().padStart(2, '0')}:00`,
          y: typeof point.value === 'number' && !isNaN(point.value) ? point.value : 0
        };
      }
    });
  }, [batterySOCData, selectedRange]);

  const calcStats = (arr: { y: number }[]) => {
    const total = arr.reduce((s, d) => s + d.y, 0);
    const peak = Math.max(...arr.map(d => d.y));
    const avg = Math.round(total / Math.max(arr.length, 1));
    return { total, peak, avg };
  };

  const prodStats = productionData?.statistics ? {
    total: productionData.statistics.total,
    peak: productionData.statistics.peak,
    avg: productionData.statistics.average
  } : calcStats(chartProductionData);

  const consStats = consumptionData?.statistics ? {
    total: consumptionData.statistics.total,
    peak: consumptionData.statistics.peak,
    avg: consumptionData.statistics.average
  } : calcStats(chartConsumptionData);

  const batterySOCStats = batterySOCData?.statistics ? {
    total: batterySOCData.statistics.total,
    peak: batterySOCData.statistics.peak,
    avg: batterySOCData.statistics.average
  } : calcStats(chartBatterySOCData);

  useEffect(() => {
    if (hasDevice && device?.id) {
      setSelectedIndexProd(null);
      loadProductionData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDevice, device?.id, selectedRangeProd]);

  useEffect(() => {
    if (hasDevice && device?.id) {
      setSelectedIndexCons(null); 
      loadConsumptionData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDevice, device?.id, selectedRangeCons]);

  useEffect(() => {
    if (hasDevice && device?.id) {
      loadBatterySOCData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDevice, device?.id]);

  useEffect(() => {
    if (selectedIndexProd !== null && chartProductionData.length > 0 && selectedIndexProd >= chartProductionData.length) {
      setSelectedIndexProd(null);
    }
  }, [chartProductionData.length, selectedIndexProd]);

  useEffect(() => {
    if (selectedIndexCons !== null && chartConsumptionData.length > 0 && selectedIndexCons >= chartConsumptionData.length) {
      setSelectedIndexCons(null);
    }
  }, [chartConsumptionData.length, selectedIndexCons]);

  useEffect(() => {
    const unregisterProd = registerRefreshCallback(loadProductionData);
    const unregisterCons = registerRefreshCallback(loadConsumptionData);
    const unregisterBattery = registerRefreshCallback(loadBatterySOCData);

    return () => {
      unregisterProd();
      unregisterCons();
      unregisterBattery();
    };
  }, [registerRefreshCallback, loadProductionData, loadConsumptionData, loadBatterySOCData]);
  const prodColor = COLORS.success || COLORS.accent;
  const consColor = COLORS.info || COLORS.accent;

  const StatPill = ({ label, value }: { label: string; value: string | number }) => {
    const formatValue = (val: string | number) => {
      const num = typeof val === 'string' ? parseFloat(val) : val;
      if (num >= 1000000) {
        return `${(num / 1000000).toFixed(1)}M`;
      } else if (num >= 1000) {
        return `${(num / 1000).toFixed(1)}K`;
      }
      return num.toFixed(1);
    };

    return (
      <View style={styles.statPillContainer}>
        <View style={styles.statPill}>
          <Text style={styles.statPillLabel}>{label}</Text>
          <Text style={styles.statPillValue}>{formatValue(value)} %</Text>
        </View>
      </View>
    );
  };

  const onChartLayout = (e: LayoutChangeEvent) => {
    const { width, height } = e.nativeEvent.layout;
    if (width !== chartSize.width || height !== chartSize.height) {
      setChartSize({ width, height });
    }
  };

  const { innerW, innerH } = useMemo(() => {
    const cw = Math.max(chartSize.width - padding.left - padding.right, 1);
    const ch = Math.max(chartSize.height - padding.top - padding.bottom, 1);
    return { innerW: cw, innerH: ch };
  }, [chartSize]);

  const renderBarSvg = (
    data: { x: string; y: number }[],
    accentColor: string,
    selectedIndex: number | null,
    setSelectedIndex: (i: number | null) => void,
  ) => {
    if (!data || data.length === 0) return null;

    const stepX = innerW / Math.max(data.length, 1);
    const maxY = Math.max(...data.map(d => d.y)) * 1.1 || 1;
    const yScale = (v: number) => innerH - (v / maxY) * innerH;
    return (
      <BarChart
        width={chartSize.width}
        height={chartSize.height}
        padding={padding}
        innerW={innerW}
        innerH={innerH}
        stepX={stepX}
        yScale={yScale}
        data={data}
        COLORS={COLORS}
        accentColor={accentColor}
        avg={Math.round(data.reduce((s, d) => s + d.y, 0) / Math.max(data.length, 1))}
        peak={Math.max(...data.map(d => d.y))}
        selectedIndex={selectedIndex}
        setSelectedIndex={setSelectedIndex}
        selectedRange={selectedRange}
      />
    );
  };

  const renderAreaSvg = (
    data: { x: string; y: number }[],
    accentColor: string,
    selectedIndex: number | null,
    setSelectedIndex: (i: number | null) => void,
  ) => {
    if (!data || data.length === 0) return null;

    const stepX = innerW / Math.max(data.length, 1);
    const maxY = Math.max(...data.map(d => d.y)) * 1.1 || 1;
    const yScale = (v: number) => innerH - (v / maxY) * innerH;
    return (
      <AreaChart
        width={chartSize.width}
        height={chartSize.height}
        padding={padding}
        innerW={innerW}
        innerH={innerH}
        stepX={stepX}
        yScale={yScale}
        data={data}
        COLORS={COLORS}
        accentColor={accentColor}
        avg={Math.round(data.reduce((s, d) => s + d.y, 0) / Math.max(data.length, 1))}
        peak={Math.max(...data.map(d => d.y))}
        selectedIndex={selectedIndex}
        setSelectedIndex={setSelectedIndex}
        selectedRange={selectedRange}
      />
    );
  };

  const renderChart = (
    data: { x: string; y: number }[],
    accentColor: string,
    selectedIndex: number | null,
    setSelectedIndex: (i: number | null) => void,
    type: 'bar' | 'area',
  ) => {
    if (!chartSize.width || !chartSize.height) return null;
    if (data.length === 0) return null;
    
    const safeSelectedIndex = selectedIndex !== null && selectedIndex >= 0 && selectedIndex < data.length 
      ? selectedIndex 
      : null;
    
    if (type === 'bar') return renderBarSvg(data, accentColor, safeSelectedIndex, setSelectedIndex);
    if (type === 'area') return renderAreaSvg(data, accentColor, safeSelectedIndex, setSelectedIndex);
    return null;
  };

  return (
    <>
      {/* Energy Production Card - Commented out */}
      {/* <Animated.View entering={FadeInUp.delay(250).duration(600)} style={styles.card}>
        <Text style={styles.title}>{t('dashboardGraph.productionTitle')}</Text>
        <View style={styles.statPillsContainer}>
          <StatPill label={t('dashboardGraph.peak')} value={prodStats.peak} />
          <StatPill label={t('dashboardGraph.average')} value={prodStats.avg} />
        </View>
        <View style={[styles.chartContainer]} onLayout={onChartLayout}>
          {productionLoading ? (
            <View style={{ justifyContent: 'center', alignItems: 'center', height: 400 }}>
              <ActivityIndicator color={COLORS.accent} size="large" />
            </View>
          ) : !hasDevice || !device?.id ? (
            <View style={{ justifyContent: 'center', alignItems: 'center', height: 400 }}>
              <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
                {t('dashboardGraph.noDevice')}
              </Text>
            </View>
          ) : !productionData || chartProductionData.length === 0 ? (
            <View style={{ justifyContent: 'center', alignItems: 'center', height: 400 }}>
              <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
                {t('dashboardGraph.noData')}
              </Text>
            </View>
          ) : (
            renderChart(chartProductionData, prodColor, selectedIndexProd, setSelectedIndexProd, chartTypeProd)
          )}
        </View>
        <View style={styles.segmentedControl}>
          <View style={styles.segmentedTrack}>
            <Animated.View
              style={[
                styles.segmentedIndicator,
                {
                  backgroundColor: COLORS.accent,
                  left: chartTypeProd === 'bar' ? 2 : '50%',
                  right: chartTypeProd === 'bar' ? '50%' : 2
                }
              ]}
            />
            {(['bar', 'area'] as const).map((type, index) => (
              <TouchableOpacity
                key={type}
                style={styles.segmentedButton}
                onPress={() => {
                  safeVibrate(50);

                  segmentedIndicatorPosition.value = withSpring(index, {
                    damping: 15,
                    stiffness: 150,
                  });

                  setChartTypeProd(type);
                  setSelectedIndexProd(null);
                }}
                activeOpacity={0.7}
                accessibilityLabel={`${t(`dashboardGraph.chartType.${type}`)} chart type`}
                accessibilityHint={`Switch to ${type} chart view`}
                accessibilityRole="button"
              >
                <View style={styles.segmentedContent}>
                  <MaterialIcons
                    name={type === 'bar' ? 'bar-chart' : 'show-chart'}
                    size={16}
                    color={chartTypeProd === type ? COLORS.white : COLORS.textSoft}
                  />
                  <Text style={[
                    styles.segmentedText,
                    {
                      color: chartTypeProd === type ? COLORS.white : COLORS.textSoft,
                      fontWeight: chartTypeProd === type ? '600' : '500'
                    }
                  ]}>
                    {t(`dashboardGraph.chartType.${type}`)}
                  </Text>
                </View>
              </TouchableOpacity>
            ))}
          </View>
        </View>

        <View style={styles.pillTabs}>
          {(['Day', 'Week', 'Month', 'Year'] as const).map((range) => {
            const isActive = selectedRangeProd === range;

            return (
              <TouchableOpacity
                key={range}
                style={[
                  styles.pillTab,
                  {
                    backgroundColor: isActive ? COLORS.accent : COLORS.bgShadeLight,
                    borderColor: isActive ? COLORS.accent : COLORS.border,
                    shadowColor: isActive ? COLORS.accent : 'transparent',
                    shadowOffset: { width: 0, height: 2 },
                    shadowOpacity: isActive ? 0.2 : 0,
                    shadowRadius: 3,
                    elevation: isActive ? 2 : 0,
                  }
                ]}
                onPress={() => {
                  safeVibrate(30);
                  setSelectedRangeProd(range);
                }}
                activeOpacity={0.7}
                accessibilityLabel={`${t(`dashboardGraph.range.${range.toLowerCase()}`)} time range`}
                accessibilityHint={`Switch to ${range.toLowerCase()} view`}
                accessibilityRole="button"
                accessibilityState={{ selected: isActive }}
              >
                <Text style={[
                  styles.pillText,
                  {
                    color: isActive ? COLORS.white : COLORS.textSoft,
                    fontWeight: isActive ? '600' : '500',
                  }
                ]}>
                  {t(`dashboardGraph.range.${range.toLowerCase()}`)}
                </Text>
              </TouchableOpacity>
            );
          })}
        </View>
      </Animated.View> */}

      {/* Energy Consumption Card - Commented out */}
      {/* <Animated.View entering={FadeInUp.delay(350).duration(600)} style={styles.card}>
        <Text style={styles.title}>{t('dashboardGraph.consumptionTitle')}</Text>
        <View style={styles.statPillsContainer}>
          <StatPill label={t('dashboardGraph.peak')} value={consStats.peak} />
          <StatPill label={t('dashboardGraph.average')} value={consStats.avg} />
        </View>
        <View style={[styles.chartContainer]} onLayout={onChartLayout}>
          {consumptionLoading ? (
            <View style={{ justifyContent: 'center', alignItems: 'center', height: 400 }}>
              <ActivityIndicator color={COLORS.accent} size="large" />
            </View>
          ) : !hasDevice || !device?.id ? (
            <View style={{ justifyContent: 'center', alignItems: 'center', height: 400 }}>
              <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
                {t('dashboardGraph.noDevice')}
              </Text>
            </View>
          ) : !consumptionData || chartConsumptionData.length === 0 ? (
            <View style={{ justifyContent: 'center', alignItems: 'center', height: 400 }}>
              <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
                {t('dashboardGraph.noData')}
              </Text>
            </View>
          ) : (
            renderChart(chartConsumptionData, consColor, selectedIndexCons, setSelectedIndexCons, chartTypeCons)
          )}
        </View>
        <View style={styles.segmentedControl}>
          <View style={styles.segmentedTrack}>
            <Animated.View
              style={[
                styles.segmentedIndicator,
                {
                  backgroundColor: COLORS.accent,
                  left: chartTypeCons === 'bar' ? 2 : '50%',
                  right: chartTypeCons === 'bar' ? '50%' : 2
                }
              ]}
            />
            {(['bar', 'area'] as const).map((type, index) => (
              <TouchableOpacity
                key={type}
                style={styles.segmentedButton}
                onPress={() => {
                  safeVibrate(50);

                  segmentedIndicatorPosition.value = withSpring(index, {
                    damping: 15,
                    stiffness: 150,
                  });

                  setChartTypeCons(type);
                  setSelectedIndexCons(null);
                }}
                activeOpacity={0.7}
                accessibilityLabel={`${t(`dashboardGraph.chartType.${type}`)} chart type`}
                accessibilityHint={`Switch to ${type} chart view`}
                accessibilityRole="button"
              >
                <Animated.View
                  style={[
                    styles.segmentedContent,
                    useAnimatedStyle(() => ({
                      transform: [{ scale: withSpring(chartTypeCons === type ? 1.05 : 1) }]
                    }))
                  ]}
                >
                  <MaterialIcons
                    name={type === 'bar' ? 'bar-chart' : 'show-chart'}
                    size={16}
                    color={chartTypeCons === type ? COLORS.white : COLORS.textSoft}
                  />
                  <Text style={[
                    styles.segmentedText,
                    {
                      color: chartTypeCons === type ? COLORS.white : COLORS.textSoft,
                      fontWeight: chartTypeCons === type ? '600' : '500'
                    }
                  ]}>
                    {t(`dashboardGraph.chartType.${type}`)}
                  </Text>
                </Animated.View>
              </TouchableOpacity>
            ))}
          </View>
        </View>

        <View style={styles.pillTabs}>
          {(['Day', 'Week', 'Month', 'Year'] as const).map((range) => {
            const isActive = selectedRangeCons === range;

            return (
              <TouchableOpacity
                key={range}
                style={[
                  styles.pillTab,
                  {
                    backgroundColor: isActive ? COLORS.accent : COLORS.bgShadeLight,
                    borderColor: isActive ? COLORS.accent : COLORS.border,
                    shadowColor: isActive ? COLORS.accent : 'transparent',
                    shadowOffset: { width: 0, height: 2 },
                    shadowOpacity: isActive ? 0.2 : 0,
                    shadowRadius: 3,
                    elevation: isActive ? 2 : 0,
                  }
                ]}
                onPress={() => {
                  safeVibrate(30);
                  setSelectedRangeCons(range);
                }}
                activeOpacity={0.7}
                accessibilityLabel={`${t(`dashboardGraph.range.${range.toLowerCase()}`)} time range`}
                accessibilityHint={`Switch to ${range.toLowerCase()} view`}
                accessibilityRole="button"
                accessibilityState={{ selected: isActive }}
              >
                <Text style={[
                  styles.pillText,
                  {
                    color: isActive ? COLORS.white : COLORS.textSoft,
                    fontWeight: isActive ? '600' : '500',
                  }
                ]}>
                  {t(`dashboardGraph.range.${range.toLowerCase()}`)}
                </Text>
              </TouchableOpacity>
            );
          })}
        </View>
      </Animated.View> */}

      {/* <MultiLineChart COLORS={COLORS} selectedRange={selectedRange} /> */}

      <PowerMetricsChart COLORS={COLORS} />
    </>
  );
}
