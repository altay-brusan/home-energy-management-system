import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, ActivityIndicator, Image, TouchableOpacity, Modal } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import Svg, { G, Path, Text as SvgText, Rect } from 'react-native-svg';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService } from '../../../../../api/services/mobileDashboardService';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { useSelectedSiteUtils } from '../../../../../hooks/useSelectedSite';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './DashboardSummaryCard.styles';
import { DashboardSummaryCardProps, DashboardSummaryData, EnergySource } from './DashboardSummaryCard.types';

// Energy source color mapping
const ENERGY_COLORS = {
  pv: '#FDB52E',
  solar: '#FDB52E',
  battery: '#12C6A9',
  grid: '#9D76F2',
  generator: '#FF9500',
  load: '#FF6B6B',
};

const DashboardSummaryCard: React.FC<DashboardSummaryCardProps> = ({ deviceId }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);
  const { selectedSite } = useSelectedSiteUtils();

  const [loading, setLoading] = useState(true);
  const [summaryData, setSummaryData] = useState<DashboardSummaryData | null>(null);
  const [weather, setWeather] = useState<any>(null);
  const [tooltipData, setTooltipData] = useState<{
    segment: EnergySource;
    x: number;
    y: number;
  } | null>(null);
  const [pressedSegmentIndex, setPressedSegmentIndex] = useState<number | null>(null);

  const fetchSummaryData = useCallback(async () => {
    if (!deviceId) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);

      // Get today's date range
      const today = new Date();
      const startDate = new Date(today);
      startDate.setHours(0, 0, 0, 0);
      const endDate = new Date(today);
      endDate.setHours(23, 59, 59, 999);

      // Fetch energy flow data and weather in parallel
      const [energyFlow, weatherData] = await Promise.all([
        mobileDashboardService.getEnergyFlow(
          deviceId,
          startDate.toISOString(),
          endDate.toISOString()
        ),
        selectedSite?.id ? dashboardService.getWeatherForecast(selectedSite.id).catch(() => null) : Promise.resolve(null)
      ]);

      // Set weather data
      if (weatherData && weatherData.length > 0) {
        setWeather(weatherData[0]);
      } else {
        setWeather(null);
      }

      if (!energyFlow || !energyFlow.sources) {
        setSummaryData(null);
        return;
      }

      // Process energy sources
      const sources: EnergySource[] = energyFlow.sources
        .filter((source: any) => source.value > 0)
        .map((source: any) => {
          // Normalize label names
          let normalizedLabel = source.label;
          if (normalizedLabel.toLowerCase().includes('battery')) {
            normalizedLabel = 'Battery';
          } else if (normalizedLabel.toLowerCase() === 'pv' || normalizedLabel.toLowerCase().includes('solar')) {
            normalizedLabel = 'Solar';
          }

          return {
            id: source.id,
            label: normalizedLabel,
            value: source.value,
            percentage: source.percentage || 0,
            color: ENERGY_COLORS[source.id.toLowerCase() as keyof typeof ENERGY_COLORS] || COLORS.accent,
          };
        });

      // Calculate total energy
      const totalEnergy = sources.reduce((sum, source) => sum + source.value, 0);

      // Find grid consumption for savings calculation
      const gridSource = sources.find(s => s.id.toLowerCase() === 'grid');
      const gridEnergy = gridSource ? gridSource.value : 0;

      // Calculate savings percentage
      const savingPercentage = totalEnergy > 0
        ? Math.round(100 * (1 - gridEnergy / totalEnergy))
        : 0;

      // Determine status (simplified logic - can be enhanced based on actual business rules)
      let status: 'onGrid' | 'off' | 'hybrid' = 'onGrid';
      if (gridEnergy === 0) {
        status = 'off';
      } else if (gridEnergy > 0 && sources.some(s => s.id.toLowerCase() === 'battery' && s.value > 0)) {
        status = 'hybrid';
      }

      setSummaryData({
        totalEnergy: Math.round(totalEnergy * 10) / 10, // Round to 1 decimal
        sources,
        status,
        savingPercentage,
      });
    } catch (error) {
      console.error('Error fetching summary data:', error);
      setSummaryData(null);
    } finally {
      setLoading(false);
    }
  }, [deviceId, selectedSite?.id, COLORS.accent]);

  const { registerRefreshCallback } = useDashboardRefresh();

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(fetchSummaryData);
    return () => {
      unregister();
    };
  }, [registerRefreshCallback, fetchSummaryData]);

  useEffect(() => {
    fetchSummaryData();
  }, [fetchSummaryData]);

  if (!deviceId) return null;

  if (loading) {
    return (
      <View style={styles.card}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      </View>
    );
  }

  if (!summaryData || summaryData.sources.length === 0) {
    return (
      <View style={styles.card}>
        <View style={styles.noDataContainer}>
          <Text style={styles.noDataText}>{t('dashboard.noDataAvailable')}</Text>
        </View>
      </View>
    );
  }

  // Create donut chart path segments
  const createDonutPath = (data: EnergySource[], innerRadius: number, outerRadius: number) => {
    const total = data.reduce((sum, item) => sum + item.value, 0);
    if (total === 0) return [];

    let angleOffset = -90; // Start from top
    const paths = [];

    for (const item of data) {
      const percentage = item.value / total;
      const angle = percentage * 360;
      const largeArcFlag = angle > 180 ? 1 : 0;

      const startAngle = angleOffset * (Math.PI / 180);
      const endAngle = (angleOffset + angle) * (Math.PI / 180);

      const x1 = 140 + outerRadius * Math.cos(startAngle);
      const y1 = 140 + outerRadius * Math.sin(startAngle);
      const x2 = 140 + outerRadius * Math.cos(endAngle);
      const y2 = 140 + outerRadius * Math.sin(endAngle);
      const x3 = 140 + innerRadius * Math.cos(endAngle);
      const y3 = 140 + innerRadius * Math.sin(endAngle);
      const x4 = 140 + innerRadius * Math.cos(startAngle);
      const y4 = 140 + innerRadius * Math.sin(startAngle);

      const pathData = [
        `M ${x1} ${y1}`,
        `A ${outerRadius} ${outerRadius} 0 ${largeArcFlag} 1 ${x2} ${y2}`,
        `L ${x3} ${y3}`,
        `A ${innerRadius} ${innerRadius} 0 ${largeArcFlag} 0 ${x4} ${y4}`,
        'Z'
      ].join(' ');

      const labelAngle = angleOffset + angle / 2;
      const labelRadius = (innerRadius + outerRadius) / 2 + 25;
      const labelX = 140 + labelRadius * Math.cos(labelAngle * (Math.PI / 180));
      const labelY = 140 + labelRadius * Math.sin(labelAngle * (Math.PI / 180));

      paths.push({
        path: pathData,
        color: item.color,
        label: item.label,
        labelX,
        labelY,
        sourceData: item,
      });

      angleOffset += angle;
    }

    return paths;
  };

  const chartPaths = createDonutPath(summaryData.sources, 70, 110);

  return (
    <View style={styles.container}>
      <View style={styles.chartContainer}>
        <Svg width={280} height={280} viewBox="0 0 280 280">
          <G>
            {chartPaths.map((segment, index) => {
              const isPressed = pressedSegmentIndex === index;

              return (
                <G key={index}>
                  <Path
                    d={segment.path}
                    fill={segment.color}
                    fillOpacity={isPressed ? 0.85 : 1}
                    strokeWidth={0}
                    onPressIn={() => {
                      setPressedSegmentIndex(index);
                      setTooltipData({
                        segment: segment.sourceData,
                        x: segment.labelX,
                        y: segment.labelY,
                      });
                    }}
                    onPressOut={() => {
                      setPressedSegmentIndex(null);
                      setTooltipData(null);
                    }}
                  />
                </G>
              );
            })}
          </G>
        </Svg>

        <View style={styles.centerLabelContainer}>
          <Text style={styles.centerValue}>{summaryData.totalEnergy} kWh</Text>
          <Text style={styles.centerUnit}>{t('dashboard.dailiyPower')}</Text>
        </View>

        {tooltipData && (
          <View
            style={[
              styles.tooltip,
              {
                left: tooltipData.x - 60,
                top: tooltipData.y - 80,
              },
            ]}
          >
            <Text style={styles.tooltipLabel}>{tooltipData.segment.label}</Text>
            <Text style={styles.tooltipValue}>{tooltipData.segment.value.toFixed(2)} kW</Text>
          </View>
        )}
      </View>

      {/* Legend */}
      <View style={styles.legendContainer}>
        {summaryData.sources.map((source, index) => (
          <View key={index} style={styles.legendItem}>
            <View style={styles.legendLeft}>
              <View style={[styles.legendColor, { backgroundColor: source.color }]} />
              <Text style={styles.legendText}>{source.label}</Text>
            </View>
            <Text style={styles.legendValue}>{source.value.toFixed(2)} kWh</Text>
            <Text style={styles.legendPercentage}>{source.percentage.toFixed(1)}%</Text>
          </View>
        ))}
      </View>

      <View style={styles.metricsContainer}>
        <View style={styles.metricCard}>
          <Text style={styles.metricLabel}>{t('dashboard.status')}</Text>
          <Text style={styles.statusMetricValue}>
            {t(`dashboard.${summaryData.status}`).toUpperCase()}
          </Text>
        </View>
        <View style={styles.metricCard}>
          <Text style={styles.metricLabel}>{t('dashboard.saving')}</Text>
          <Text style={styles.metricValue}>%{summaryData.savingPercentage}</Text>
        </View>
        <View style={styles.metricCard}>
          {weather ? (
            <>
              {weather.icon && (
                <Image
                  source={{ uri: weather.icon.startsWith('http') ? weather.icon : `https:${weather.icon}` }}
                  style={styles.weatherIcon}
                  resizeMode="contain"
                />
              )}
              <Text style={styles.weatherTemp}>{weather.temperature}°</Text>
            </>
          ) : (
            <Text style={styles.weatherTemp}>--°</Text>
          )}
        </View>
      </View>
    </View>
  );
};

export default DashboardSummaryCard;