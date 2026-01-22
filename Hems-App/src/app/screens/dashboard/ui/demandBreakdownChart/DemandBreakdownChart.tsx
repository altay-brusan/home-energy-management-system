import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, TouchableOpacity, Dimensions, ActivityIndicator } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import Svg, { G, Path } from 'react-native-svg';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../../../store/themeStore';
import { useSitesStore } from '../../../../../store/sitesStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './DemandBreakdownChart.styles';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { EnergySourceMixDto } from '../../../../../api/types/dashboard.types';

interface DemandSegment {
  id: string;
  label: string;
  value: number; // in kWh
  percentage: number;
  color: string;
}

// Color mapping for energy sources
const SOURCE_COLORS: Record<string, string> = {
  solar: '#FBBF24',
  battery: '#34D399',
  grid: '#60A5FA',
  generator: '#F472B6',
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

// Format today's date as YYYY-MM-DD
const getTodayDate = (): string => {
  const today = new Date();
  return today.toISOString().split('T')[0];
};

const DemandBreakdownChart: React.FC = () => {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = createStyles(COLORS, isDark);

  const { selectedSiteDevice } = useSitesStore();
  const deviceId = selectedSiteDevice?.device?.id;

  const [selectedSegment, setSelectedSegment] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [data, setData] = useState<EnergySourceMixDto | null>(null);
  const [segments, setSegments] = useState<DemandSegment[]>([]);
  const [totalDemand, setTotalDemand] = useState(0);

  const screenWidth = Dimensions.get('window').width;
  const chartSize = Math.min(screenWidth - 64, 280);
  const centerX = chartSize / 2;
  const centerY = chartSize / 2;
  const radius = chartSize / 2 - 30;
  const strokeWidth = 28;
  const expandedStrokeWidth = 36;

  // Fetch data from API
  useEffect(() => {
    const fetchData = async () => {
      if (!deviceId) {
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        setError(null);
        const result = await dashboardService.getEnergySourceMix(deviceId, getTodayDate());
        setData(result);

        // Convert Wh to kWh and map to segments
        const mappedSegments: DemandSegment[] = result.sources
          .filter(source => source.contribution > 0) // Only show sources with contribution
          .map(source => ({
            id: source.source.toLowerCase(),
            label: source.source,
            value: source.providedEnergyWh / 1000, // Convert Wh to kWh
            percentage: Math.round(source.contribution),
            color: SOURCE_COLORS[source.source.toLowerCase()] || '#94A3B8',
          }));

        setSegments(mappedSegments);
        setTotalDemand(result.loadDemandWh / 1000); // Convert Wh to kWh
      } catch (err: any) {
        console.log('[DemandBreakdown] Error fetching data:', err);
        setError(err?.message || 'Failed to load data');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [deviceId]);

  // Calculate segment angles
  const getSegmentAngles = useCallback(() => {
    let currentAngle = 0;
    return segments.map(segment => {
      const startAngle = currentAngle;
      const sweepAngle = (segment.percentage / 100) * 360;
      currentAngle += sweepAngle;
      return {
        ...segment,
        startAngle,
        endAngle: startAngle + sweepAngle,
      };
    });
  }, [segments]);

  const segmentsWithAngles = getSegmentAngles();

  const handleSegmentPress = (segmentId: string) => {
    setSelectedSegment(prev => (prev === segmentId ? null : segmentId));
  };

  const getSelectedData = () => {
    if (!selectedSegment) return null;
    return segments.find(s => s.id === selectedSegment);
  };

  const selectedData = getSelectedData();

  // Loading state
  if (loading) {
    return (
      <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.container}>
        <Text style={styles.title}>{t('demandBreakdown.title')}</Text>
        <View style={[styles.chartWrapper, { justifyContent: 'center', alignItems: 'center', minHeight: 200 }]}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      </Animated.View>
    );
  }

  // Error state
  if (error) {
    return (
      <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.container}>
        <Text style={styles.title}>{t('demandBreakdown.title')}</Text>
        <View style={[styles.chartWrapper, { justifyContent: 'center', alignItems: 'center', minHeight: 200 }]}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>{error}</Text>
        </View>
      </Animated.View>
    );
  }

  // No data state
  if (segments.length === 0) {
    return (
      <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.container}>
        <Text style={styles.title}>{t('demandBreakdown.title')}</Text>
        <View style={[styles.chartWrapper, { justifyContent: 'center', alignItems: 'center', minHeight: 200 }]}>
          <Text style={{ color: COLORS.textSoft, textAlign: 'center' }}>
            {t('demandBreakdown.noData', 'No data available for today')}
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
            {segmentsWithAngles.map((segment) => {
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
              <Text style={styles.centerValue}>{totalDemand.toFixed(1)}</Text>
              <Text style={styles.centerUnit}>kWh</Text>
            </>
          )}
        </View>
      </View>

      {/* Legend */}
      <View style={styles.legend}>
        {segments.map((segment) => (
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
