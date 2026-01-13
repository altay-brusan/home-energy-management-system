import React, { useMemo, useState, useEffect, useCallback } from 'react';
import { LayoutChangeEvent, Text, View, ActivityIndicator, TouchableOpacity } from 'react-native';
import Svg, { Defs, LinearGradient, Stop, Rect, Path, Circle, Text as SvgText } from 'react-native-svg';
import Animated, { FadeInUp, useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';
import { getChargeDischargeChartStyles } from './ChargeDischargeChart.styles';
import { useTranslation } from 'react-i18next';
import { useSelectedSiteDevice } from '../../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService, ChargeDischargeResponseDto } from '../../../../../../api/services/mobileDashboardService';
import { SPACING } from '../../../../../../theme/constants';
import Tooltip from './Tooltip';

// History Icon Component
const HistoryIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={20} height={20} viewBox="0 0 24 24" fill="none">
    <Path
      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
      stroke={color}
      strokeWidth={1}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </Svg>
);

type Datum = { x: string | number; charge: number; discharge: number };

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
   const { t } = useTranslation();
  const styles = getChargeDischargeChartStyles(COLORS);
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [chargeDischargeData, setChargeDischargeData] = useState<ChargeDischargeResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const padding = { top: 15, right: 15, bottom: 50, left: 35 };

  const chartScale = useSharedValue(1);

  // Load charge/discharge data
  const loadChargeDischargeData = useCallback(async () => {if (!hasDevice || !device?.id) {setLoading(false);
      return;
    }setLoading(true);
    try {
      const data = await mobileDashboardService.getChargeDischarge(device.id);setChargeDischargeData(data);
    } catch (error) {setChargeDischargeData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  // Load data on mount and when dependencies change
  useEffect(() => {
    if (hasDevice && device?.id) {
      loadChargeDischargeData();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDevice, device?.id]);

  // Register refresh callback
  useEffect(() => {const unregister = registerRefreshCallback(loadChargeDischargeData);return () => {unregister();
    };
  }, [registerRefreshCallback, loadChargeDischargeData]);

  // Transform API data to chart format
  const chartData: Datum[] = useMemo(() => {
    if (!chargeDischargeData?.data || !Array.isArray(chargeDischargeData.data)) {
      return [];
    }
    
    return chargeDischargeData.data.map((point: any, index: number) => {
      try {
        // Backend'ten gelen veri formatı: {timestamp: 'Hour-19:00', chargeValue: 14.378, dischargeValue: 4.426, unit: 'kW'}
        let timeString: string;
        
        if (point.timestamp && typeof point.timestamp === 'string') {
          // "Hour-19:00" formatından "19:00" çıkar
          if (point.timestamp.includes('Hour-')) {
            timeString = point.timestamp.replace('Hour-', '');
          } else {
            // Diğer formatlar için Date parsing dene
            const date = new Date(point.timestamp);
            timeString = isNaN(date.getTime()) 
              ? `${index.toString().padStart(2, '0')}:00`
              : date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
          }
        } else {
          timeString = `${index.toString().padStart(2, '0')}:00`;
        }
        
        return {
          x: timeString,
          charge: typeof point.chargeValue === 'number' ? point.chargeValue : 0,
          discharge: typeof point.dischargeValue === 'number' ? point.dischargeValue : 0
        };
      } catch (error) {return {
          x: `${index.toString().padStart(2, '0')}:00`,
          charge: typeof point.chargeValue === 'number' ? point.chargeValue : 0,
          discharge: typeof point.dischargeValue === 'number' ? point.dischargeValue : 0
        };
      }
    });
  }, [chargeDischargeData]);

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
  const maxY = Math.max(...chartData.map(d => Math.max(d.charge, d.discharge))) * 1.1 || 1;
  const yScale = (v: number) => innerH - (v / maxY) * innerH;

  // Charge line points
  const chargePoints = chartData.map((d, i) => ({
    x: padding.left + i * stepX + stepX / 2,
    y: padding.top + yScale(d.charge),
  }));

  // Discharge line points
  const dischargePoints = chartData.map((d, i) => ({
    x: padding.left + i * stepX + stepX / 2,
    y: padding.top + yScale(d.discharge),
  }));

  // Charge path
  const chargePathD = chargePoints.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
  const chargeAreaD = `${chargePathD} L ${padding.left + (chartData.length - 1) * stepX + stepX / 2} ${
    padding.top + innerH
  } L ${padding.left + stepX / 2} ${padding.top + innerH} Z`;

  // Discharge path
  const dischargePathD = dischargePoints.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
  const dischargeAreaD = `${dischargePathD} L ${padding.left + (chartData.length - 1) * stepX + stepX / 2} ${
    padding.top + innerH
  } L ${padding.left + stepX / 2} ${padding.top + innerH} Z`;

  // Calculate stats from API data or fallback to calculated values
  const stats = useMemo(() => {
    if (chargeDischargeData?.statistics) {
      return {
        charge: {
          avg: Math.round(chargeDischargeData.statistics.averageCharge * 10) / 10,
          peak: chargeDischargeData.statistics.peakCharge,
          current: chargeDischargeData.statistics.currentCharge
        },
        discharge: {
          avg: Math.round((chargeDischargeData.statistics.averageDischarge || 0) * 10) / 10,
          peak: chargeDischargeData.statistics.peakDischarge || 0,
          current: chargeDischargeData.statistics.currentDischarge || 0
        }
      };
    }
    
    if (chartData.length > 0) {
      return {
        charge: {
          avg: Math.round(chartData.reduce((s, d) => s + d.charge, 0) / chartData.length * 10) / 10,
          peak: Math.max(...chartData.map(d => d.charge)),
          current: chartData[chartData.length - 1]?.charge || 0
        },
        discharge: {
          avg: Math.round(chartData.reduce((s, d) => s + d.discharge, 0) / chartData.length * 10) / 10,
          peak: Math.max(...chartData.map(d => d.discharge)),
          current: chartData[chartData.length - 1]?.discharge || 0
        }
      };
    }
    
    return { 
      charge: { avg: 0, peak: 0, current: 0 },
      discharge: { avg: 0, peak: 0, current: 0 }
    };
  }, [chargeDischargeData, chartData]);

  const chargeAvg = stats.charge.avg;
  const chargePeak = stats.charge.peak;
  const dischargeAvg = stats.discharge.avg;
  const dischargePeak = stats.discharge.peak;

  // Timestamp labels logic - evenly spaced, max N labels (always include first/last)
  const timestampLabels = useMemo(() => {
    if (chartData.length === 0) return [];
    const maxLabels = 12;
    const dataLength = chartData.length;

    // If we can show all labels, do so
    if (dataLength <= maxLabels) {
      return chartData.map((d, i) => ({
        index: i,
        label: d.x.toString(),
        x: padding.left + i * stepX + stepX / 2,
      }));
    }

    // Compute even indices including first and last
    const count = maxLabels;
    const interval = (dataLength - 1) / (count - 1);
    const chosenIndexes: number[] = [];
    for (let k = 0; k < count; k++) {
      const idx = Math.round(k * interval);
      if (chosenIndexes[chosenIndexes.length - 1] !== idx) {
        chosenIndexes.push(idx);
      }
    }
    // Safety: ensure first and last present
    if (chosenIndexes[0] !== 0) chosenIndexes.unshift(0);
    if (chosenIndexes[chosenIndexes.length - 1] !== dataLength - 1) chosenIndexes.push(dataLength - 1);

    return chosenIndexes.map((dataIndex) => ({
      index: dataIndex,
      label: chartData[dataIndex].x.toString(),
      x: padding.left + dataIndex * stepX + stepX / 2,
    }));
  }, [chartData, stepX, padding.left]);

  // Y-axis labels logic
  const yAxisLabels = useMemo(() => {
    const labelCount = 5;
    const labels = [];
    
    for (let i = 0; i <= labelCount; i++) {
      const value = (maxY / labelCount) * i;
      const y = padding.top + innerH - (i * innerH / labelCount);
      labels.push({
        value: Math.round(value * 10) / 10,
        y: y
      });
    }
    
    return labels;
  }, [maxY, padding.top, innerH]);

  const handleHistoryPress = () => {
    // TODO: Navigate to historical data screen or show modal
    console.log('History pressed - ChargeDischargeChart');
  };

  return (
    <Animated.View entering={FadeInUp.delay(300).duration(600)} style={styles.container}>
      <View style={styles.header}>
        <View style={{ flex: 1 }}>
          <Text style={styles.title}>{t('dashboardChargeDischargeChart.title')}</Text>
          <View style={styles.legendContainer}>
            <View style={styles.legendItem}>
              <View style={[styles.legendDot, { backgroundColor: COLORS.accent }]} />
              <Text style={styles.legendText}>{t('dashboardChargeDischargeChart.legend.charge')}</Text>
            </View>
            <View style={styles.legendItem}>
              <View style={[styles.legendDot, { backgroundColor: '#EF4444' }]} />
              <Text style={styles.legendText}>{t('dashboardChargeDischargeChart.legend.discharge')}</Text>
            </View>
          </View>
        </View>
        <TouchableOpacity onPress={handleHistoryPress} style={{ padding: 4 }}>
          <HistoryIcon color={COLORS.accent} />
        </TouchableOpacity>
      </View>
      
      <Animated.View style={[styles.chartContainer, animatedChartStyle]} onLayout={onChartLayout}>
        {loading ? (
          <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
            <ActivityIndicator color={COLORS.accent} size="large" />
          </View>
        ) : !hasDevice || !device?.id ? (
          <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
            <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
              {t('dashboardChargeDischargeChart.noDevice')}
            </Text>
          </View>
        ) : !chargeDischargeData || chartData.length === 0 ? (
          <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
            <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
              {t('dashboardChargeDischargeChart.noData')}
            </Text>
          </View>
        ) : (
        <Svg width={chartSize.width} height={chartSize.height}>
          <Defs>
            {/* Charge gradients */}
            <LinearGradient id="chargeGrad" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={COLORS.accent} stopOpacity={0.4} />
              <Stop offset="50%" stopColor={COLORS.accent} stopOpacity={0.2} />
              <Stop offset="100%" stopColor={COLORS.accent} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="chargeLineGrad" x1="0" y1="0" x2="1" y2="0">
              <Stop offset="0%" stopColor={COLORS.accent} stopOpacity={0.8} />
              <Stop offset="50%" stopColor={COLORS.accent} stopOpacity={1} />
              <Stop offset="100%" stopColor={COLORS.accent} stopOpacity={0.8} />
            </LinearGradient>

            {/* Discharge gradients */}
            <LinearGradient id="dischargeGrad" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor="#EF4444" stopOpacity={0.4} />
              <Stop offset="50%" stopColor="#EF4444" stopOpacity={0.2} />
              <Stop offset="100%" stopColor="#EF4444" stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="dischargeLineGrad" x1="0" y1="0" x2="1" y2="0">
              <Stop offset="0%" stopColor="#EF4444" stopOpacity={0.8} />
              <Stop offset="50%" stopColor="#EF4444" stopOpacity={1} />
              <Stop offset="100%" stopColor="#EF4444" stopOpacity={0.8} />
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

          {/* Charge area and line */}
          <Path d={chargeAreaD} fill="url(#chargeGrad)" />
          <Path
            d={chargePathD}
            stroke="url(#chargeLineGrad)"
            strokeWidth={1}
            fill="none"
          />

          {/* Discharge area and line */}
          <Path d={dischargeAreaD} fill="url(#dischargeGrad)" />
          <Path
            d={dischargePathD}
            stroke="url(#dischargeLineGrad)"
            strokeWidth={1}
            fill="none"
          />


          {/* Interactive hit areas for selection */}
          {chartData.map((_, i) => {
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

          {/* Charge average line */}
          <Path
            d={`M ${padding.left} ${padding.top + yScale(chargeAvg)} H ${padding.left + innerW}`}
            stroke={COLORS.accent}
            strokeDasharray="4 4"
            strokeWidth={1}
            opacity={0.7}
          />

          {/* Discharge average line */}
          <Path
            d={`M ${padding.left} ${padding.top + yScale(dischargeAvg)} H ${padding.left + innerW}`}
            stroke="#EF4444"
            strokeDasharray="4 4"
            strokeWidth={1}
            opacity={0.7}
          />

          {/* Charge peak line */}
          <Path
            d={`M ${padding.left} ${padding.top + yScale(chargePeak)} H ${padding.left + innerW}`}
            stroke={COLORS.accent}
            strokeDasharray="6 4"
            strokeWidth={1}
            opacity={0.9}
          />

          {/* Discharge peak line */}
          <Path
            d={`M ${padding.left} ${padding.top + yScale(dischargePeak)} H ${padding.left + innerW}`}
            stroke="#EF4444"
            strokeDasharray="6 4"
            strokeWidth={1}
            opacity={0.9}
          />

          {/* X-axis timestamp labels - Vertical */}
          {timestampLabels.map((label, i) => (
            <SvgText
              key={`timestamp-${i}`}
              x={label.x}
              y={padding.top + innerH + 35}
              fontSize="10"
              fill={COLORS.textSoft || '#6B7280'}
              textAnchor="start"
              transform={`rotate(-90, ${label.x}, ${padding.top + innerH + 35})`}
            >
              {label.label}
            </SvgText>
          ))}

          {/* Y-axis value labels */}
          {yAxisLabels.map((label, i) => (
            <SvgText
              key={`y-axis-${i}`}
              x={padding.left - 8}
              y={label.y + 3}
              fontSize="10"
              fill={COLORS.textSoft || '#6B7280'}
              textAnchor="end"
            >
              {label.value}
            </SvgText>
          ))}

          {selectedIndex !== null && selectedIndex < chartData.length && (() => {
            const i = selectedIndex;
            const d = chartData[i];
            const cx = padding.left + i * stepX + stepX / 2;
            const chargeCy = padding.top + yScale(d.charge);
            const dischargeCy = padding.top + yScale(d.discharge);
            const timeLabel = d.x as string;
            const chargeLabel = `Charge: ${d.charge.toFixed(2)} kW`;
            const dischargeLabel = `Discharge: ${d.discharge.toFixed(2)} kW`;
            const labelText = `${timeLabel}\n${chargeLabel}\n${dischargeLabel}`;
            const tooltipY = chargeCy < dischargeCy ? chargeCy : dischargeCy;
            return (
              <Tooltip
                cx={cx}
                cy={tooltipY}
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
      
      {/* Stats Container - 2 rows */}
      <View style={{ marginTop: SPACING.md, gap: SPACING.sm }}>
        {/* First Row - Charge Stats */}
        <View style={styles.statsContainer}>
          <View style={styles.statItem}>
            <Text style={styles.statLabel}>{t('dashboardChargeDischargeChart.stats.chargeAvg')}</Text>
            <Text style={[styles.statValue, { color: COLORS.accent }]}>{chargeAvg} kW</Text>
          </View>
          <View style={styles.statItem}>
            <Text style={styles.statLabel}>{t('dashboardChargeDischargeChart.stats.chargePeak')}</Text>
            <Text style={[styles.statValue, { color: COLORS.accent }]}>{chargePeak} kW</Text>
          </View>
          <View style={styles.statItem}>
            <Text style={styles.statLabel}>{t('dashboardChargeDischargeChart.stats.chargeCurrent')}</Text>
            <Text style={[styles.statValue, { color: COLORS.accent }]}>{stats.charge.current} kW</Text>
          </View>
        </View>

        {/* Second Row - Discharge Stats */}
        <View style={styles.statsContainer}>
          <View style={styles.statItem}>
            <Text style={styles.statLabel}>{t('dashboardChargeDischargeChart.stats.dischargeAvg')}</Text>
            <Text style={[styles.statValue, { color: '#EF4444' }]}>{dischargeAvg} kW</Text>
          </View>
          <View style={styles.statItem}>
            <Text style={styles.statLabel}>{t('dashboardChargeDischargeChart.stats.dischargePeak')}</Text>
            <Text style={[styles.statValue, { color: '#EF4444' }]}>{dischargePeak} kW</Text>
          </View>
          <View style={styles.statItem}>
            <Text style={styles.statLabel}>{t('dashboardChargeDischargeChart.stats.dischargeCurrent')}</Text>
            <Text style={[styles.statValue, { color: '#EF4444' }]}>{stats.discharge.current} kW</Text>
          </View>
        </View>
      </View>
    </Animated.View>
  );
}

