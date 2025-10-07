import React, { useMemo, useState } from 'react';
import { LayoutChangeEvent, Text, View } from 'react-native';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { Button } from '../../../../../components';
import { getDashboardGraphStyles } from './DashboardGraph.styles';
import type { DashboardGraphProps } from './DashboardGraph.types';
import AreaChart from './components/AreaChart';
import BarChart from './components/BarChart';
import MultiLineChart from './components/MultiLineChart';
import SankeyChart from './components/SankeyChart';

export default function DashboardGraph({
  COLORS,
  isDark,
  selectedRange,
  setSelectedRange,
}: DashboardGraphProps) {
  const styles = getDashboardGraphStyles(COLORS);
  const [chartTypeProd, setChartTypeProd] = useState<'bar' | 'area'>('bar');
  const [chartTypeCons, setChartTypeCons] = useState<'bar' | 'area'>('area');
  const [selectedRangeProd, setSelectedRangeProd] = useState(selectedRange);
  const [selectedRangeCons, setSelectedRangeCons] = useState(selectedRange);

  const [chartSize, setChartSize] = useState({ width: 0, height: 0 });
  const [selectedIndexProd, setSelectedIndexProd] = useState<number | null>(
    null,
  );
  const [selectedIndexCons, setSelectedIndexCons] = useState<number | null>(
    null,
  );
  const padding = { top: 10, right: 8, bottom: 18, left: 8 };

  const datasets = {
    Day: [
      { x: '00:00', y: 12 },
      { x: '04:00', y: 18 },
      { x: '08:00', y: 62 },
      { x: '12:00', y: 75 },
      { x: '16:00', y: 48 },
      { x: '20:00', y: 30 },
      { x: '24:00', y: 16 },
    ],
    Week: [
      { x: 'Mon', y: 320 },
      { x: 'Tue', y: 420 },
      { x: 'Wed', y: 380 },
      { x: 'Thu', y: 460 },
      { x: 'Fri', y: 520 },
      { x: 'Sat', y: 610 },
      { x: 'Sun', y: 540 },
    ],
    Month: Array.from({ length: 12 }).map((_, i) => ({
      x: `M${i + 1}`,
      y: Math.round(200 + Math.random() * 600),
    })),
    Year: Array.from({ length: 12 }).map((_, i) => ({
      x: `${i + 1}`,
      y: Math.round(3000 + Math.random() * 2000),
    })),
  } as const;

  const baseProd = datasets[selectedRangeProd] ?? datasets.Week;
  const baseCons = datasets[selectedRangeCons] ?? datasets.Week;
  const productionData = baseProd.map((d, idx) => ({
    x: d.x,
    y: Math.max(0, Math.round(d.y * (0.9 + (idx % 3) * 0.05))),
  }));
  const consumptionData = baseCons.map((d, idx) => ({
    x: d.x,
    y: Math.max(0, Math.round(d.y * (0.7 + ((idx + 1) % 4) * 0.06))),
  }));

  const calcStats = (arr: { y: number }[]) => {
    const total = arr.reduce((s, d) => s + d.y, 0);
    const peak = Math.max(...arr.map(d => d.y));
    const avg = Math.round(total / Math.max(arr.length, 1));
    return { total, peak, avg };
  };

  const prodStats = calcStats(productionData);
  const consStats = calcStats(consumptionData);

  const prodColor = COLORS.success || COLORS.accent;
  const consColor = COLORS.info || COLORS.accent;

  const enableNativeCharts = false;

  let Charts: any = null;
  if (enableNativeCharts) {
    try {
      Charts = require('react-native-charts-wrapper');
    } catch (e) {
      Charts = null;
    }
  }

  const StatPill = ({
    label,
    value,
  }: {
    label: string;
    value: string | number;
  }) => (
    <View style={styles.statPillContainer}>
      <View style={styles.statPill}>
        <Text style={styles.statPillLabel}>{label}</Text>
        <Text style={styles.statPillValue}>{value}</Text>
      </View>
    </View>
  );

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
    data: typeof productionData,
    accentColor: string,
    selectedIndex: number | null,
    setSelectedIndex: (i: number | null) => void,
  ) =>
    (() => {
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
          avg={Math.round(
            data.reduce((s, d) => s + d.y, 0) / Math.max(data.length, 1),
          )}
          peak={Math.max(...data.map(d => d.y))}
          selectedIndex={selectedIndex}
          setSelectedIndex={setSelectedIndex}
        />
      );
    })();

  const renderAreaSvg = (
    data: typeof productionData,
    accentColor: string,
    selectedIndex: number | null,
    setSelectedIndex: (i: number | null) => void,
  ) =>
    (() => {
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
          avg={Math.round(
            data.reduce((s, d) => s + d.y, 0) / Math.max(data.length, 1),
          )}
          peak={Math.max(...data.map(d => d.y))}
          selectedIndex={selectedIndex}
          setSelectedIndex={setSelectedIndex}
        />
      );
    })();

  const renderChart = (
    data: typeof productionData,
    accentColor: string,
    selectedIndex: number | null,
    setSelectedIndex: (i: number | null) => void,
    type: 'bar' | 'area',
  ) => {
    if (!chartSize.width || !chartSize.height) return null;
    if (type === 'bar')
      return renderBarSvg(data, accentColor, selectedIndex, setSelectedIndex);
    if (type === 'area')
      return renderAreaSvg(data, accentColor, selectedIndex, setSelectedIndex);
    return null;
  };

  return (
    <>
      <Animated.View
        entering={FadeInUp.delay(250).duration(600)}
        style={styles.card}
      >
        <Text style={styles.title}>Energy Production</Text>
        <View style={styles.statPillsContainer}>
          <StatPill label="Total" value={prodStats.total} />
          <StatPill label="Peak" value={prodStats.peak} />
          <StatPill label="Avg" value={prodStats.avg} />
        </View>
        <View style={[styles.chartContainer]} onLayout={onChartLayout}>
          {renderChart(
            productionData,
            prodColor,
            selectedIndexProd,
            setSelectedIndexProd,
            chartTypeProd,
          )}
        </View>
        <View style={styles.chartTypeTabs}>
          {(['bar', 'area'] as const).map(type => (
            <Button
              key={type}
              title={type === 'bar' ? 'Bar' : 'Area'}
              variant={chartTypeProd === type ? 'primary' : 'secondary'}
              style={[
                styles.tabBtn,
                chartTypeProd === type && { backgroundColor: COLORS.accent },
              ]}
              textStyle={{
                color: chartTypeProd === type ? COLORS.white : COLORS.textSoft,
                fontWeight: chartTypeProd === type ? '700' : '500',
              }}
              onPress={() => setChartTypeProd(type)}
            />
          ))}
        </View>
        <View style={styles.tabs}>
          {(['Day', 'Week', 'Month', 'Year'] as const).map(range => (
            <Button
              key={range}
              title={range}
              variant={selectedRangeProd === range ? 'primary' : 'secondary'}
              style={[
                styles.tabBtn,
                selectedRangeProd === range && {
                  backgroundColor: COLORS.accent,
                },
              ]}
              textStyle={{
                color:
                  selectedRangeProd === range ? COLORS.white : COLORS.textSoft,
                fontWeight: selectedRangeProd === range ? '700' : '500',
              }}
              onPress={() => setSelectedRangeProd(range)}
            />
          ))}
        </View>
      </Animated.View>

      <Animated.View
        entering={FadeInUp.delay(350).duration(600)}
        style={styles.card}
      >
        <Text style={styles.title}>Energy Consumption</Text>
        <View style={styles.statPillsContainer}>
          <StatPill label="Total" value={consStats.total} />
          <StatPill label="Peak" value={consStats.peak} />
          <StatPill label="Avg" value={consStats.avg} />
        </View>
        <View style={[styles.chartContainer]} onLayout={onChartLayout}>
          {renderChart(
            consumptionData,
            consColor,
            selectedIndexCons,
            setSelectedIndexCons,
            chartTypeCons,
          )}
        </View>
        <View style={styles.chartTypeTabs}>
          {(['bar', 'area'] as const).map(type => (
            <Button
              key={type}
              title={type === 'bar' ? 'Bar' : 'Area'}
              variant={chartTypeCons === type ? 'primary' : 'secondary'}
              style={[
                styles.tabBtn,
                chartTypeCons === type && { backgroundColor: COLORS.accent },
              ]}
              textStyle={{
                color: chartTypeCons === type ? COLORS.white : COLORS.textSoft,
                fontWeight: chartTypeCons === type ? '700' : '500',
              }}
              onPress={() => setChartTypeCons(type)}
            />
          ))}
        </View>
        <View style={styles.tabs}>
          {(['Day', 'Week', 'Month', 'Year'] as const).map(range => (
            <Button
              key={range}
              title={range}
              variant={selectedRangeCons === range ? 'primary' : 'secondary'}
              style={[
                styles.tabBtn,
                selectedRangeCons === range && {
                  backgroundColor: COLORS.accent,
                },
              ]}
              textStyle={{
                color:
                  selectedRangeCons === range ? COLORS.white : COLORS.textSoft,
                fontWeight: selectedRangeCons === range ? '700' : '500',
              }}
              onPress={() => setSelectedRangeCons(range)}
            />
          ))}
        </View>
      </Animated.View>
      <MultiLineChart COLORS={COLORS} />
      
    </>
  );
}
