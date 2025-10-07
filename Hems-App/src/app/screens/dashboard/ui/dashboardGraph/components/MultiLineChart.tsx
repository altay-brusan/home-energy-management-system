import React, { useState } from 'react';
import { View, Text, Switch, processColor } from 'react-native';
import { CombinedChart } from 'react-native-charts-wrapper';
import { getPowerMetricsChartStyles } from './MultiLineChart.styles';

type Props = { COLORS: Record<string, string> };

export default function PowerMetricsChart({ COLORS }: Props) {
  const styles = getPowerMetricsChartStyles(COLORS);

  const [showSolar, setShowSolar] = useState(true);
  const [showBattery, setShowBattery] = useState(true);
  const [showGrid, setShowGrid] = useState(true);

  const generateMockData = (min: number, max: number, negative = false, untilHour = 12) => {
  const arr = [];
  for (let i = 0; i <= untilHour; i++) { 
    let y = Math.random() * (max - min) + min;
    if (i % 2 === 0) y += (Math.random() - 0.5) * 0.8;

    if (negative) y = -Math.abs(y);
    else y = Math.abs(y);

    arr.push({ x: i, y });
  }
  return arr;
};
const currentHour = 12; 
  const solarData   = generateMockData(0.2, 3.0, false);
  const batteryData = generateMockData(0.5, 2.5, true);
  const gridData    = generateMockData(0.3, 2.0, false);

  const lineDataSets = [
    showSolar && {
      label: 'Solar Produced',
      values: solarData,
      config: {
        color: processColor('#FACC15'),
        drawFilled: true,
        fillColor: processColor('#FACC15'),
        fillAlpha: 70,
        fillFormatter: { type: 'default', min: 0 },
        drawValues: false,
        drawCircles: false,
        mode: 'CUBIC_BEZIER',
        lineWidth: 1,
      },
    },
    showBattery && {
      label: 'Battery',
      values: batteryData,
      config: {
        color: processColor('#22C55E'),
        drawFilled: true,
        fillColor: processColor('#22C55E'),
        fillAlpha: 70,
        fillFormatter: { type: 'default', min: 0 },
        drawValues: false,
        drawCircles: false,
        mode: 'CUBIC_BEZIER',
       lineWidth: 1,
      },
    },
    showGrid && {
      label: 'Grid',
      values: gridData,
      config: {
        color: processColor('#3B82F6'),
        drawFilled: true,
        fillColor: processColor('#3B82F6'),
        fillAlpha: 60,
        fillFormatter: { type: 'default', min: 0 },
        drawValues: false,
        drawCircles: false,
        mode: 'CUBIC_BEZIER',
        lineWidth: 1,
      },
    },
  ].filter(Boolean) as any[];

  const data = { lineData: { dataSets: lineDataSets } };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Power Metrics Chart (kW)</Text>
      <CombinedChart
        style={styles.chart}
        data={data}
        chartDescription={{ text: '', enabled: false }}
xAxis={{
  position: 'BOTTOM',
  textColor: processColor(COLORS.textSoft),
  granularityEnabled: true,
  granularity: 4,
  axisMinimum: 0,
  axisMaximum: 24,
  labelCount: 7,
   valueFormatter: Array.from({ length: 25 }, (_, i) =>
    i % 4 === 0
      ? (i === 24 ? "00:00" : `${i.toString().padStart(2, "0")}:00`)
      : ""
  ),
}}
        yAxis={{
          left: {
            drawGridLines: true,
            textColor: processColor(COLORS.textSoft),
            axisMinimum: -4,
            axisMaximum: 4,
            labelCount: 9,
            zeroLine: {
              enabled: true,
              lineWidth: 2,
              lineColor: processColor(COLORS.slate400 || '#edededff'),
            },
          },
          right: { enabled: false },
        }}
        legend={{ enabled: false }}
        touchEnabled
        dragEnabled
        scaleEnabled
        pinchZoom
      />

      <View style={{ marginTop: 12, flexDirection: 'row', justifyContent: 'space-between' }}>
        <View style={{ flexDirection: 'row' }}>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>Solar</Text>
            <Switch value={showSolar} onValueChange={setShowSolar}
              thumbColor="#FACC15" trackColor={{ true: '#FACC15', false: COLORS.border }} />
          </View>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>Battery</Text>
            <Switch value={showBattery} onValueChange={setShowBattery}
              thumbColor="#22C55E" trackColor={{ true: '#22C55E', false: COLORS.border }} />
          </View>
        </View>
        <View style={{ flexDirection: 'row' }}>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>Grid</Text>
            <Switch value={showGrid} onValueChange={setShowGrid}
              thumbColor="#3B82F6" trackColor={{ true: '#3B82F6', false: COLORS.border }} />
          </View>
        </View>
      </View>
    </View>
  );
}
