import React, { useState, useCallback, useEffect, useMemo } from 'react';
import { View, Text, TouchableOpacity, Dimensions, ActivityIndicator } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import Svg, { G, Path } from 'react-native-svg';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './DemandBreakdownChart.styles';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { LoadEnergySourceDistributionDto } from '../../../../../api/types/dashboard.types';

interface DemandSegment {
  id: string;
  label: string;
  value: number;
  percentage: number;
  color: string;
}

// Source colors mapping
const SOURCE_COLORS: Record<string, string> = {
  solar: '#FBBF24',
  battery: '#34D399',
  grid: '#60A5FA',
  generator: '#F97316',
};

// Get color for source
const getSourceColor = (source: string): string => {
  const normalizedSource = source.toLowerCase();
  return SOURCE_COLORS[normalizedSource] || '#94A3B8';
};

// Helper function to create arc path
const createArcPath = (
  centerX: number,
  centerY: number,
  radius: number,
  startAngle: number,
  endAngle: number,
  strokeWidth: number
): string => {
  const innerRadius = radius - strokeWidth / 2;
  const outerRadius = radius + strokeWidth / 2;

  // Convert angles to radians (start from top, -90 degrees offset)
  const startRad = ((startAngle - 90) * Math.PI) / 180;
  const endRad = ((endAngle - 90) * Math.PI) / 180;

  const x1Outer = centerX + outerRadius * Math.cos(startRad);
  const y1Outer = centerY + outerRadius * Math.sin(startRad);
  const x2Outer = centerX + outerRadius * Math.cos(endRad);
  const y2Outer = centerY + outerRadius * Math.sin(endRad);

  const x1Inner = centerX + innerRadius * Math.cos(endRad);
  const y1Inner = centerY + innerRadius * Math.sin(endRad);
  const x2Inner = centerX + innerRadius * Math.cos(startRad);
  const y2Inner = centerY + innerRadius * Math.sin(startRad);

  const largeArcFlag = endAngle - startAngle > 180 ? 1 : 0;

  return `
    M ${x1Outer} ${y1Outer}
    A ${outerRadius} ${outerRadius} 0 ${largeArcFlag} 1 ${x2Outer} ${y2Outer}
    L ${x1Inner} ${y1Inner}
    A ${innerRadius} ${innerRadius} 0 ${largeArcFlag} 0 ${x2Inner} ${y2Inner}
    Z
  `;
};

// Transform API response to component data
const transformApiData = (apiData: LoadEnergySourceDistributionDto): { segments: DemandSegment[]; totalDemand: number } => {
  const segments: DemandSegment[] = apiData.sources.map((source) => ({
    id: source.source.toLowerCase(),
    label: source.source,
    value: source.providedEnergyWh / 1000, // Convert Wh to kWh
    percentage: Math.round(source.contribution), // contribution is already 0-100
    color: getSourceColor(source.source),
  }));

  // Filter out segments with 0 contribution
  const validSegments = segments.filter(s => s.percentage > 0);

  return {
    segments: validSegments,
    totalDemand: apiData.loadDemandWh / 1000, // Convert Wh to kWh
  };
};

const DemandBreakdownChart: React.FC = () => {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = createStyles(COLORS, isDark);

  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();

  const [selectedSegment, setSelectedSegment] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [demandData, setDemandData] = useState<{ segments: DemandSegment[]; totalDemand: number } | null>(null);

  const screenWidth = Dimensions.get('window').width;
  const chartSize = Math.min(screenWidth - 64, 280);
  const centerX = chartSize / 2;
  const centerY = chartSize / 2;
  const radius = chartSize / 2 - 30;
  const strokeWidth = 28;
  const expandedStrokeWidth = 36;

  // Fetch demand breakdown data
  const fetchDemandBreakdown = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      // Get today's date in YYYY-MM-DD format
      const today = new Date();
      const dateString = today.toISOString().split('T')[0];

      const response = await dashboardService.getEnergySourceMix(device.id, dateString);
      const transformedData = transformApiData(response);
      setDemandData(transformedData);
    } catch (err: any) {
      console.error('Error fetching demand breakdown:', err);
      setError(err.message || 'Failed to fetch demand breakdown');
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  // Initial fetch
  useEffect(() => {
    fetchDemandBreakdown();
  }, [fetchDemandBreakdown]);

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(fetchDemandBreakdown);
    return () => unregister();
  }, [registerRefreshCallback, fetchDemandBreakdown]);

  // Calculate segment angles
  const segments = useMemo(() => {
    if (!demandData || demandData.segments.length === 0) return [];

    let currentAngle = 0;
    return demandData.segments.map(segment => {
      const startAngle = currentAngle;
      const sweepAngle = (segment.percentage / 100) * 360;
      currentAngle += sweepAngle;
      return {
        ...segment,
        startAngle,
        endAngle: startAngle + sweepAngle,
      };
    });
  }, [demandData]);

  const handleSegmentPress = (segmentId: string) => {
    setSelectedSegment(prev => (prev === segmentId ? null : segmentId));
  };

  const getSelectedData = () => {
    if (!selectedSegment || !demandData) return null;
    return demandData.segments.find(s => s.id === selectedSegment);
  };

  const selectedData = getSelectedData();

  // Show loading state
  if (loading) {
    return (
      <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.container}>
        <Text style={styles.title}>{t('demandBreakdown.title')}</Text>
        <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', minHeight: 200 }}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      </Animated.View>
    );
  }

  // Show error or no data state
  if (error || !demandData || demandData.segments.length === 0) {
    return (
      <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.container}>
        <Text style={styles.title}>{t('demandBreakdown.title')}</Text>
        <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', minHeight: 200 }}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
            {error || t('demandBreakdown.noData', 'No data available')}
          </Text>
        </View>
      </Animated.View>
    );
  }

  return (
    <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.container}>
      <Text style={styles.title}>{t('demandBreakdown.title')}</Text>

      <View style={styles.chartWrapper}>
        {/* SVG Chart */}
        <Svg width={chartSize} height={chartSize}>
          <G>
            {segments.map((segment) => {
              const isSelected = selectedSegment === segment.id;
              const currentStrokeWidth = isSelected ? expandedStrokeWidth : strokeWidth;
              const currentRadius = isSelected ? radius + 4 : radius;

              // Add small gap between segments
              const gapAngle = 2;
              const adjustedStartAngle = segment.startAngle + gapAngle / 2;
              const adjustedEndAngle = segment.endAngle - gapAngle / 2;

              if (adjustedEndAngle <= adjustedStartAngle) return null;

              const path = createArcPath(
                centerX,
                centerY,
                currentRadius,
                adjustedStartAngle,
                adjustedEndAngle,
                currentStrokeWidth
              );

              return (
                <Path
                  key={segment.id}
                  d={path}
                  fill={segment.color}
                  opacity={selectedSegment && !isSelected ? 0.5 : 1}
                  onPress={() => handleSegmentPress(segment.id)}
                />
              );
            })}
          </G>
        </Svg>

        {/* Center content */}
        <View style={styles.centerContent}>
          {selectedData ? (
            <>
              <Text style={[styles.centerLabel, { color: selectedData.color }]}>
                {t(`demandBreakdown.sources.${selectedData.id}`, selectedData.label)}
              </Text>
              <Text style={styles.centerValue}>{selectedData.value.toFixed(1)}</Text>
              <Text style={styles.centerUnit}>kWh</Text>
              <Text style={[styles.centerPercentage, { color: selectedData.color }]}>
                {selectedData.percentage}%
              </Text>
            </>
          ) : (
            <>
              <Text style={styles.centerLabel}>{t('demandBreakdown.totalDemand')}</Text>
              <Text style={styles.centerValue}>{demandData.totalDemand.toFixed(1)}</Text>
              <Text style={styles.centerUnit}>kWh</Text>
            </>
          )}
        </View>
      </View>

      {/* Legend */}
      <View style={styles.legend}>
        {demandData.segments.map((segment) => (
          <TouchableOpacity
            key={segment.id}
            style={[
              styles.legendItem,
              selectedSegment === segment.id && styles.legendItemSelected,
            ]}
            onPress={() => handleSegmentPress(segment.id)}
            activeOpacity={0.7}
          >
            <View style={[styles.legendColor, { backgroundColor: segment.color }]} />
            <Text style={styles.legendText}>
              {t(`demandBreakdown.sources.${segment.id}`, segment.label)} {segment.percentage}%
            </Text>
          </TouchableOpacity>
        ))}
      </View>
    </Animated.View>
  );
};

export default DemandBreakdownChart;
