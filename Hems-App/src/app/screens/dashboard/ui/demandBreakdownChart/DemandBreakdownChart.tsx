import React, { useState, useCallback } from 'react';
import { View, Text, TouchableOpacity, Dimensions } from 'react-native';
import Animated, { FadeInDown, useAnimatedStyle, useSharedValue, withSpring } from 'react-native-reanimated';
import Svg, { G, Path, Circle } from 'react-native-svg';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './DemandBreakdownChart.styles';

interface DemandSegment {
  id: string;
  label: string;
  value: number;
  percentage: number;
  color: string;
}

// Static demo data
const DEMO_DATA: DemandSegment[] = [
  { id: 'solar', label: 'Solar', value: 12.47, percentage: 45, color: '#FBBF24' },
  { id: 'battery', label: 'Battery', value: 6.09, percentage: 22, color: '#34D399' },
  { id: 'grid', label: 'Grid', value: 9.14, percentage: 33, color: '#60A5FA' },
];

const TOTAL_DEMAND = 27.7;

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

const DemandBreakdownChart: React.FC = () => {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = createStyles(COLORS, isDark);

  const [selectedSegment, setSelectedSegment] = useState<string | null>(null);

  const screenWidth = Dimensions.get('window').width;
  const chartSize = Math.min(screenWidth - 64, 280);
  const centerX = chartSize / 2;
  const centerY = chartSize / 2;
  const radius = chartSize / 2 - 30;
  const strokeWidth = 28;
  const expandedStrokeWidth = 36;

  // Calculate segment angles
  const getSegmentAngles = useCallback(() => {
    let currentAngle = 0;
    return DEMO_DATA.map(segment => {
      const startAngle = currentAngle;
      const sweepAngle = (segment.percentage / 100) * 360;
      currentAngle += sweepAngle;
      return {
        ...segment,
        startAngle,
        endAngle: startAngle + sweepAngle,
      };
    });
  }, []);

  const segments = getSegmentAngles();

  const handleSegmentPress = (segmentId: string) => {
    setSelectedSegment(prev => (prev === segmentId ? null : segmentId));
  };

  const getSelectedData = () => {
    if (!selectedSegment) return null;
    return DEMO_DATA.find(s => s.id === selectedSegment);
  };

  const selectedData = getSelectedData();

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
                {t(`demandBreakdown.sources.${selectedData.id}`)}
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
              <Text style={styles.centerValue}>{TOTAL_DEMAND}</Text>
              <Text style={styles.centerUnit}>kWh</Text>
            </>
          )}
        </View>
      </View>

      {/* Legend */}
      <View style={styles.legend}>
        {DEMO_DATA.map((segment) => (
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
              {t(`demandBreakdown.sources.${segment.id}`)} {segment.percentage}%
            </Text>
          </TouchableOpacity>
        ))}
      </View>
    </Animated.View>
  );
};

export default DemandBreakdownChart;
