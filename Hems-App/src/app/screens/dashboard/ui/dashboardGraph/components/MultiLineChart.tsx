import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, Text, Switch, processColor, ActivityIndicator, TouchableOpacity, ScrollView, Platform, Dimensions } from 'react-native';
import { CombinedChart } from 'react-native-charts-wrapper';
import Svg, { Path, Circle, Text as SvgText, Line, Rect, Defs, LinearGradient, Stop } from 'react-native-svg';
import { getPowerMetricsChartStyles } from './MultiLineChart.styles';
import { useTranslation } from 'react-i18next';
import { useSelectedSiteDevice } from '../../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService, PowerMetricsResponseDto } from '../../../../../../api/services/mobileDashboardService';

type Props = { 
  COLORS: Record<string, string>;
  selectedRange?: string;
};

export default function PowerMetricsChart ({ COLORS, selectedRange }: Props) {
  const { t } = useTranslation();
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const { width: screenWidth } = Dimensions.get('window');

  const [showSolar, setShowSolar] = useState(true);
  const [showBattery, setShowBattery] = useState(true);
  const [showGrid, setShowGrid] = useState(true);
  const [showLoad, setShowLoad] = useState(true);

  const [powerMetricsData, setPowerMetricsData] = useState<PowerMetricsResponseDto | null>(null);
  const [loading, setLoading] = useState(true);

  const loadPowerMetricsData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const data = await mobileDashboardService.getPowerMetrics(device.id, 'daily');
      setPowerMetricsData(data);
    } catch (error) {
      setPowerMetricsData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  const chartData = useMemo(() => {
    if (!powerMetricsData?.data || !Array.isArray(powerMetricsData.data)) {
      return {
        solarData: [],
        batteryData: [],
        gridData: [],
        loadData: [],
        timestamps: [],
        yAxisMin: -10,
        yAxisMax: 10
      };
    }

    const solarData: { x: number; y: number }[] = [];
    const batteryData: { x: number; y: number }[] = [];
    const gridData: { x: number; y: number }[] = [];
    const loadData: { x: number; y: number }[] = [];
    const timestamps: string[] = [];

    const allValues: number[] = [];

    powerMetricsData.data.forEach((point, index) => {
      try {
        if (!point.timestamp) {
          timestamps.push(`Point ${index}`);
          solarData.push({ x: index, y: 0 });
          batteryData.push({ x: index, y: 0 });
          gridData.push({ x: index, y: 0 });
          loadData.push({ x: index, y: 0 });
          allValues.push(0, 0, 0, 0);
          return;
        }
        
        timestamps.push(point.timestamp);

        const solarValue = typeof point.solarPower === 'number' && !isNaN(point.solarPower) ? point.solarPower : 0;
        const batteryValue = typeof point.batteryPower === 'number' && !isNaN(point.batteryPower) ? point.batteryPower : 0;
        const gridValue = typeof point.gridPower === 'number' && !isNaN(point.gridPower) ? point.gridPower : 0;
        const loadValue = typeof point.loadPower === 'number' && !isNaN(point.loadPower) ? point.loadPower : 0;

        solarData.push({ x: index, y: solarValue });
        batteryData.push({ x: index, y: batteryValue });
        gridData.push({ x: index, y: gridValue });
        loadData.push({ x: index, y: loadValue });

        allValues.push(solarValue, batteryValue, gridValue, loadValue);
      } catch (error: any) {
        timestamps.push(`Point ${index}`);
        solarData.push({ x: index, y: 0 });
        batteryData.push({ x: index, y: 0 });
        gridData.push({ x: index, y: 0 });
        loadData.push({ x: index, y: 0 });
        allValues.push(0, 0, 0, 0);
      }
    });

    const minValue = Math.min(...allValues);
    const maxValue = Math.max(...allValues);

    const margin = Math.max(Math.abs(maxValue - minValue) * 0.1, 1);
    const yAxisMin = Math.floor(minValue - margin);
    const yAxisMax = Math.ceil(maxValue + margin);


    return {
      solarData,
      batteryData,
      gridData,
      loadData,
      timestamps,
      yAxisMin,
      yAxisMax
    };
  }, [powerMetricsData]);

  const styles = useMemo(() => getPowerMetricsChartStyles(COLORS, chartData.timestamps?.length || 24), [COLORS, chartData.timestamps?.length]);

  // Calculate chart width (screen width - container padding - card padding)
  const chartWidth = useMemo(() => screenWidth - (24 * 2) - (16 * 2), [screenWidth]);

  const getLabelInterval = (dataLength: number, timeRange?: string) => {
    if (timeRange === 'Week' && dataLength <= 7) return 1;
    
    if (dataLength <= 7) return 1;
    if (dataLength <= 14) return 2;
    if (dataLength <= 21) return 3;
    if (dataLength <= 31) return 4;
    return Math.ceil(dataLength / 8);
  };

  const labelInterval = getLabelInterval(chartData.solarData.length, selectedRange);

  const getShortDayName = (dayName: string) => {
    const dayMap: { [key: string]: string } = {
      'Monday': 'Pzt',
      'Tuesday': 'Sal', 
      'Wednesday': 'Çar',
      'Thursday': 'Per',
      'Friday': 'Cum',
      'Saturday': 'Cmt',
      'Sunday': 'Paz',
      'Pazartesi': 'Pzt',
      'Salı': 'Sal',
      'Çarşamba': 'Çar', 
      'Perşembe': 'Per',
      'Cuma': 'Cum',
      'Cumartesi': 'Cmt',
      'Pazar': 'Paz'
    };
    return dayMap[dayName] || dayName.substring(0, 3);
  };

  const lineDataSets = [
    showSolar && {
      label: t('dashboardPowerMetricsChart.labels.solar'),
      values: chartData.solarData,
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
        circleRadius: 3,
        circleColor: processColor('#FACC15'),
      },
    },
    showBattery && {
      label: t('dashboardPowerMetricsChart.labels.battery'),
      values: chartData.batteryData,
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
        circleRadius: 3,
        circleColor: processColor('#22C55E'),
      },
    },
    showGrid && {
      label: t('dashboardPowerMetricsChart.labels.grid'),
      values: chartData.gridData,
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
        circleRadius: 3,
        circleColor: processColor('#3B82F6'),
      },
    },
    showLoad && {
      label: t('dashboardPowerMetricsChart.labels.load'),
      values: chartData.loadData,
      config: {
        color: processColor('#A855F7'),
        drawFilled: true,
        fillColor: processColor('#A855F7'),
        fillAlpha: 70,
        fillFormatter: { type: 'default', min: 0 },
        drawValues: false,
        drawCircles: false,
        mode: 'CUBIC_BEZIER',
        lineWidth: 1,
        circleRadius: 3,
        circleColor: processColor('#A855F7'),
      },
    },
  ].filter(Boolean) as any[];

  const data = { 
    lineData: { 
      dataSets: lineDataSets.length > 0 ? lineDataSets : [
        {
          label: 'Fallback',
          values: [{ y: 0 }],
          config: {
            color: processColor('#TRANSPARENT'),
            drawFilled: false,
            drawValues: false,
            drawCircles: false,
            lineWidth: 0,
          },
        }
      ]
    } 
  };

  const renderIOSChart = () => {
    const chartHeight = 300;
    const padding = { top: 20, right: 20, bottom: 55, left: 40 };
    const innerWidth = chartWidth - padding.left - padding.right;
    const innerHeight = chartHeight - padding.top - padding.bottom;

    if (!powerMetricsData || chartData.solarData.length === 0) {
      return (
        <View style={{ height: chartHeight, justifyContent: 'center', alignItems: 'center' }}>
          <Text style={{ color: COLORS.textSoft }}>No data available</Text>
        </View>
      );
    }

    const allValues = [
      ...chartData.solarData.map(d => d.y),
      ...chartData.batteryData.map(d => d.y),
      ...chartData.gridData.map(d => d.y),
      ...chartData.loadData.map(d => d.y)
    ].filter(v => typeof v === 'number' && !isNaN(v));

    const minValue = Math.min(...allValues, 0);
    const maxValue = Math.max(...allValues, 1);
    const range = maxValue - minValue || 1;

    const createPath = (data: any[], color: string, gradientId: string) => {
      if (!data || data.length === 0) return null;

      const points = data.map((point, index) => {
        const x = padding.left + (index / Math.max(data.length - 1, 1)) * innerWidth;
        const normalizedY = typeof point.y === 'number' && !isNaN(point.y) ? point.y : 0;
        const y = padding.top + innerHeight - ((normalizedY - minValue) / range) * innerHeight;
        return { x: Math.round(x), y: Math.round(y) };
      });

      const linePathData = points.map((point, index) => 
        index === 0 ? `M ${point.x} ${point.y}` : `L ${point.x} ${point.y}`
      ).join(' ');

      const fillPathData = [
        `M ${points[0].x} ${padding.top + innerHeight}`,
        `L ${points[0].x} ${points[0].y}`,
        ...points.slice(1).map(point => `L ${point.x} ${point.y}`),
        `L ${points[points.length - 1].x} ${padding.top + innerHeight}`,
        'Z'
      ].join(' ');

      return (
        <React.Fragment key={color}>
          <Path
            d={fillPathData}
            fill={`url(#${gradientId})`}
            stroke="none"
          />
          <Path
            d={linePathData}
            stroke={color}
            strokeWidth={2}
            fill="none"
          />
        </React.Fragment>
      );
    };

    return (
      <View style={{ width: chartWidth }}>
        <Svg width={chartWidth} height={chartHeight}>
            <Defs>
              <LinearGradient id="solarGradient" x1="0" y1="0" x2="0" y2="1">
                <Stop offset="0%" stopColor="#FACC15" stopOpacity={0.7} />
                <Stop offset="100%" stopColor="#FACC15" stopOpacity={0.1} />
              </LinearGradient>
              <LinearGradient id="batteryGradient" x1="0" y1="0" x2="0" y2="1">
                <Stop offset="0%" stopColor="#22C55E" stopOpacity={0.7} />
                <Stop offset="100%" stopColor="#22C55E" stopOpacity={0.1} />
              </LinearGradient>
              <LinearGradient id="gridGradient" x1="0" y1="0" x2="0" y2="1">
                <Stop offset="0%" stopColor="#3B82F6" stopOpacity={0.6} />
                <Stop offset="100%" stopColor="#3B82F6" stopOpacity={0.1} />
              </LinearGradient>
              <LinearGradient id="loadGradient" x1="0" y1="0" x2="0" y2="1">
                <Stop offset="0%" stopColor="#A855F7" stopOpacity={0.7} />
                <Stop offset="100%" stopColor="#A855F7" stopOpacity={0.1} />
              </LinearGradient>
            </Defs>

            {[0, 1, 2, 3, 4].map(i => {
              const y = padding.top + (i * innerHeight) / 4;
              return (
                <Line
                  key={`grid-${i}`}
                  x1={padding.left}
                  y1={y}
                  x2={padding.left + innerWidth}
                  y2={y}
                  stroke={COLORS.border}
                  strokeWidth={0.5}
                  opacity={0.3}
                />
              );
            })}

            {[0, 1, 2, 3, 4].map(i => {
              const value = maxValue - (i * range) / 4;
              const y = padding.top + (i * innerHeight) / 4;
              return (
                <SvgText
                  key={`y-label-${i}`}
                  x={padding.left - 5}
                  y={y + 4}
                  fontSize="10"
                  fill={COLORS.textSoft}
                  textAnchor="end"
                >
                  {Math.round(value)}
                </SvgText>
              );
            })}

            {showSolar && createPath(chartData.solarData, '#FACC15', 'solarGradient')}
            {showBattery && createPath(chartData.batteryData, '#22C55E', 'batteryGradient')}
            {showGrid && createPath(chartData.gridData, '#3B82F6', 'gridGradient')}
            {showLoad && createPath(chartData.loadData, '#A855F7', 'loadGradient')}

            {chartData.solarData.map((_, index) => {
              if (index % Math.max(1, Math.floor(chartData.solarData.length / 6)) !== 0) return null;
              const x = padding.left + (index / Math.max(chartData.solarData.length - 1, 1)) * innerWidth;
              return (
                <SvgText
                  key={`x-label-${index}`}
                  x={x}
                  y={chartHeight - padding.bottom + 10}
                  fontSize="10"
                  fill={COLORS.textSoft}
                  textAnchor="start"
                  transform={`rotate(-90, ${x}, ${chartHeight - padding.bottom + 10})`}
                >
                  {selectedRange === 'Day' ? `${index.toString().padStart(2, '0')}:00` : `${index}`}
                </SvgText>
              );
            })}
          </Svg>
      </View>
    );
  };

  useEffect(() => {
    loadPowerMetricsData();
  }, [loadPowerMetricsData]);

  useEffect(() => {
    const unregister = registerRefreshCallback(loadPowerMetricsData);
    return () => {
      unregister();
    };
  }, [registerRefreshCallback, loadPowerMetricsData]);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>{t('dashboardPowerMetricsChart.title')}</Text>

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator color={COLORS.accent} size="large" />
          <Text style={styles.loadingText}>{t('dashboardGraph.loading')}</Text>
        </View>
      ) : !hasDevice || !device?.id ? (
        <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
            {t('dashboardGraph.noDevice')}
          </Text>
        </View>
      ) : !powerMetricsData || chartData.solarData.length === 0 ? (
        <View style={{ justifyContent: 'center', alignItems: 'center', height: 200 }}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
            {t('dashboardGraph.noData')}
          </Text>
        </View>
      ) : Platform.OS === 'ios' ? (
        <View style={[styles.chartContainer, { minHeight: 350 }]}>
          {renderIOSChart()}
        </View>
      ) : (
        <View style={[styles.chartContainer, { minHeight: 350 }]}>
          <View style={styles.chartAndHoursWrapper}>
            <CombinedChart
                  style={{ height: 400, width: chartWidth }}
                  data={data}
                  chartDescription={{ text: '', enabled: false }}
                  extraOffsets={{
                    left: 10,
                    top: 10,
                    right: 10,
                    bottom: 25,
                  }}
                  xAxis={{
                    position: 'BOTTOM',
                    textColor: processColor(COLORS.textSoft),
                    granularityEnabled: true,
                    granularity: 1,
                    axisMinimum: 0,
                    axisMaximum: chartData.solarData.length > 0 ? chartData.solarData.length - 1 : 23,
                    labelCount: Math.min(chartData.timestamps.length, 
                      (!selectedRange || selectedRange === 'daily' || selectedRange === 'Day') ? 12 : 
                      (selectedRange === 'weekly' || selectedRange === 'Week') ? 7 : 10),
                    valueFormatter: (() => {
                      const labels = chartData.timestamps.map((timestamp, i) => {
                        if (!timestamp) return `${i}`;
                        
                        try {
                          if (typeof timestamp === 'string' && timestamp.match(/^\d{2}:\d{2}$/)) {
                            return timestamp;
                          }
                          
                          const date = new Date(timestamp);
                          if (isNaN(date.getTime())) {
                            return `${i}`;
                          }
                          
                          let label = '';
                          if (!selectedRange || selectedRange === 'daily' || selectedRange === 'Day') {
                            const hours = date.getHours().toString().padStart(2, '0');
                            const minutes = date.getMinutes().toString().padStart(2, '0');
                            label = `${hours}:${minutes}`;
                          } else if (selectedRange === 'weekly' || selectedRange === 'Week') {
                            const dayNames = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt'];
                            const dayName = dayNames[date.getDay()];
                            const day = date.getDate().toString().padStart(2, '0');
                            label = `${dayName} ${day}`;
                          } else if (selectedRange === 'monthly' || selectedRange === 'Month') {
                            const day = date.getDate().toString().padStart(2, '0');
                            const month = (date.getMonth() + 1).toString().padStart(2, '0');
                            label = `${day}/${month}`;
                          } else {
                            const hours = date.getHours().toString().padStart(2, '0');
                            const minutes = date.getMinutes().toString().padStart(2, '0');
                            label = `${hours}:${minutes}`;
                          }
                          
                          return label;
                        } catch (error) {
                          return `${i}`;
                        }
                      });
                      
                      return labels;
                    })(),
                    textSize: (selectedRange === 'weekly' || selectedRange === 'Week') ? 12 : 11,
                    textAngle: -90,
                    labelRotationAngle: -90,
                    drawGridLines: true,
                    avoidFirstLastClipping: true,
                    spaceMin: 0.5,
                    spaceMax: 0.5,
                  }}
                  yAxis={{
                    left: {
                      drawGridLines: true,
                      textColor: processColor(COLORS.textSoft),
                      axisMinimum: chartData.yAxisMin,
                      axisMaximum: chartData.yAxisMax,
                      labelCount: Math.min(11, Math.ceil((chartData.yAxisMax - chartData.yAxisMin) / 2) + 1),
                      zeroLine: {
                        enabled: true,
                        lineWidth: 2,
                        lineColor: processColor(COLORS.slate400 || '#edededff'),
                      },
                    },
                    right: { enabled: false },
                  }}
                  legend={{ enabled: false }}
                  touchEnabled={false}
                  dragEnabled={false}
                  scaleEnabled={false}
                  pinchZoom={false}
                  doubleTapToZoomEnabled={false}
                  highlightPerTapEnabled={false}
                  highlightPerDragEnabled={false}
                  animation={{
                    durationX: 1000,
                    durationY: 1000,
                    easingX: 'Linear',
                    easingY: 'Linear',
                  }}
                />
          </View>
        </View>
      )}

      <View style={styles.togglesContainer}>
        <View style={styles.togglesRow}>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>{t('dashboardPowerMetricsChart.labels.solar')}</Text>
            <Switch
              value={showSolar}
              onValueChange={setShowSolar}
              thumbColor="#FACC15"
              trackColor={{ true: '#FACC15', false: COLORS.border }}
            />
          </View>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>{t('dashboardPowerMetricsChart.labels.battery')}</Text>
            <Switch
              value={showBattery}
              onValueChange={setShowBattery}
              thumbColor="#22C55E"
              trackColor={{ true: '#22C55E', false: COLORS.border }}
            />
          </View>
        </View>
        <View style={styles.togglesRow}>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>{t('dashboardPowerMetricsChart.labels.grid')}</Text>
            <Switch
              value={showGrid}
              onValueChange={setShowGrid}
              thumbColor="#3B82F6"
              trackColor={{ true: '#3B82F6', false: COLORS.border }}
            />
          </View>
          <View style={styles.toggleItem}>
            <Text style={styles.toggleLabel}>{t('dashboardPowerMetricsChart.labels.load')}</Text>
            <Switch
              value={showLoad}
              onValueChange={setShowLoad}
              thumbColor="#A855F7"
              trackColor={{ true: '#A855F7', false: COLORS.border }}
            />
          </View>
        </View>
      </View>
    </View>
  );
}
