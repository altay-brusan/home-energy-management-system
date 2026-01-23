import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, Text, TouchableOpacity, ActivityIndicator, Modal, PanResponder, useWindowDimensions } from 'react-native';
import { ArrowLeft, Calendar } from 'lucide-react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTranslation } from 'react-i18next';
import { Calendar as RNCalendar } from 'react-native-calendars';
import Svg, { Path, Line, Text as SvgText, G, Circle, Defs, LinearGradient, Stop } from 'react-native-svg';
import { useSelectedSiteDevice } from '../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { createStyles } from './SocHistoryDetailScreen.styles';
import { dashboardService } from '../../../api/services/dashboardService';
import { SocHistoryDataPointDto } from '../../../api/types/dashboard.types';

interface SocDataPoint {
  timestamp: string;
  soc: number;
  isInterpolated: boolean;
}

// Get today's date in YYYY-MM-DD format
const getTodayString = (): string => {
  const today = new Date();
  return today.toISOString().split('T')[0];
};

export default function SocHistoryDetailScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();
  const insets = useSafeAreaInsets();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = createStyles(COLORS, isDark);
  const { device, hasDevice } = useSelectedSiteDevice();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [socData, setSocData] = useState<SocDataPoint[]>([]);
  const [minSoc, setMinSoc] = useState(0);
  const [maxSoc, setMaxSoc] = useState(0);
  const [avgSoc, setAvgSoc] = useState(0);

  // Single date selection - default to today
  const todayString = getTodayString();
  const [selectedDate, setSelectedDate] = useState<string>(todayString);
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [markedDates, setMarkedDates] = useState<any>({
    [todayString]: {
      selected: true,
      selectedColor: COLORS.accent,
      selectedTextColor: COLORS.white,
    },
  });

  // Touch interaction state
  const [touchX, setTouchX] = useState<number | null>(null);
  const [selectedPoint, setSelectedPoint] = useState<SocDataPoint | null>(null);

  const { width: screenWidth } = useWindowDimensions();

  // Responsive chart dimensions
  const isTablet = screenWidth >= 768;
  // Account for: content padding (16*2=32) + chartContainer padding (12*2=24) = 56
  const chartWidth = isTablet ? Math.min(screenWidth - 64, 700) : screenWidth - 56;
  const chartHeight = isTablet ? 320 : 260;
  const chartPadding = { top: 20, right: 0, bottom: 35, left: 0 };
  const innerWidth = chartWidth - chartPadding.left - chartPadding.right;
  const innerHeight = chartHeight - chartPadding.top - chartPadding.bottom;

  // Format date for display
  const formatDate = (dateString: string): string => {
    if (!dateString) {
      return t('socHistoryDetail.selectDate');
    }
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  };

  // Handle date selection
  const handleDateSelect = (day: any) => {
    const selected = day.dateString;
    const selectedDateObj = new Date(selected + 'T00:00:00');
    const today = new Date();
    today.setHours(23, 59, 59, 999); // End of today

    // Validate: Cannot select future dates
    if (selectedDateObj > today) {
      return;
    }

    setSelectedDate(selected);
    setMarkedDates({
      [selected]: {
        selected: true,
        selectedColor: COLORS.accent,
        selectedTextColor: COLORS.white,
      },
    });
    setShowDatePicker(false);
  };

  // Load data from API
  const loadData = useCallback(async () => {
    if (!hasDevice || !device?.id || !selectedDate) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const response = await dashboardService.getSocHistory(device.id, selectedDate);

      // Transform API data to component format
      const transformedData: SocDataPoint[] = response.data.map((point: SocHistoryDataPointDto) => ({
        timestamp: point.timestamp,
        soc: point.socPercent,
        isInterpolated: point.isInterpolated,
      }));

      setSocData(transformedData);

      // Calculate statistics
      if (transformedData.length > 0) {
        const socValues = transformedData.map(d => d.soc);
        setMinSoc(Math.round(Math.min(...socValues)));
        setMaxSoc(Math.round(Math.max(...socValues)));
        setAvgSoc(Math.round(socValues.reduce((a, b) => a + b, 0) / socValues.length));
      }
    } catch (err: any) {
      console.error('Error loading SOC history:', err);
      setError(err.message || 'Failed to load SOC history');
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id, selectedDate]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Get color based on SOC value
  const getSocColor = (soc: number): string => {
    if (soc < 20) return '#EF4444'; // Red
    if (soc < 50) return '#F59E0B'; // Orange/Yellow
    return '#10B981'; // Green
  };

  // Scale functions
  const yScale = useCallback((value: number) => {
    return chartPadding.top + innerHeight - (value / 100) * innerHeight;
  }, [innerHeight]);

  const xScale = useCallback((index: number) => {
    if (socData.length <= 1) return chartPadding.left;
    return chartPadding.left + (index / (socData.length - 1)) * innerWidth;
  }, [socData.length, innerWidth]);

  // Get point at touch position
  const getPointAtPosition = useCallback((touchPosition: number) => {
    if (socData.length === 0) return null;

    const adjustedX = touchPosition - chartPadding.left;
    const index = Math.round((adjustedX / innerWidth) * (socData.length - 1));
    const clampedIndex = Math.max(0, Math.min(socData.length - 1, index));

    return {
      point: socData[clampedIndex],
      index: clampedIndex,
      x: xScale(clampedIndex),
    };
  }, [socData, innerWidth, xScale]);

  // Pan responder for touch interaction
  const panResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => true,
    onMoveShouldSetPanResponder: () => true,
    onPanResponderGrant: (evt) => {
      const touchPosition = evt.nativeEvent.locationX;
      const result = getPointAtPosition(touchPosition);
      if (result) {
        setSelectedPoint(result.point);
        setTouchX(result.x);
      }
    },
    onPanResponderMove: (evt) => {
      const touchPosition = evt.nativeEvent.locationX;
      const result = getPointAtPosition(touchPosition);
      if (result) {
        setSelectedPoint(result.point);
        setTouchX(result.x);
      }
    },
    onPanResponderRelease: () => {
      setTouchX(null);
      setSelectedPoint(null);
    },
    onPanResponderTerminationRequest: () => true,
  }), [getPointAtPosition]);

  // Render line chart
  const renderChart = () => {
    if (socData.length === 0) return null;

    // Create path for the line
    const linePath = socData
      .map((point, index) => {
        const x = xScale(index);
        const y = yScale(point.soc);
        return `${index === 0 ? 'M' : 'L'} ${x} ${y}`;
      })
      .join(' ');

    // Create path for the gradient fill area
    const areaPath = `${linePath} L ${xScale(socData.length - 1)} ${chartPadding.top + innerHeight} L ${chartPadding.left} ${chartPadding.top + innerHeight} Z`;

    // Calculate step for X-axis labels (show ~5 labels)
    const labelStep = Math.max(1, Math.floor(socData.length / 5));

    return (
      <View style={styles.chartContainer}>
        <Text style={styles.chartTitle}>{t('socHistoryDetail.chartTitle')}</Text>

        {/* Floating Tooltip */}
        {touchX !== null && selectedPoint && (
          <View
            style={{
              position: 'absolute',
              top: 25,
              left: Math.max(8, Math.min(touchX - 55, chartWidth - 120)),
              backgroundColor: COLORS.cardBg,
              borderRadius: 10,
              padding: 10,
              shadowColor: '#000',
              shadowOffset: { width: 0, height: 2 },
              shadowOpacity: 0.25,
              shadowRadius: 3.84,
              elevation: 5,
              minWidth: 100,
              zIndex: 100,
            }}
          >
            <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700', marginBottom: 6 }}>
              {selectedPoint.timestamp}
            </Text>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
              <Text style={{ color: COLORS.textSoft, fontSize: 11, fontWeight: '500' }}>
                SOC
              </Text>
              <Text style={{ color: getSocColor(selectedPoint.soc), fontSize: 13, fontWeight: '700' }}>
                {selectedPoint.soc.toFixed(1)}%
              </Text>
            </View>
            {selectedPoint.isInterpolated && (
              <Text style={{ color: COLORS.textSoft, fontSize: 9, marginTop: 3, fontStyle: 'italic' }}>
                {t('socHistoryDetail.interpolated', 'Interpolated')}
              </Text>
            )}
          </View>
        )}

        <View {...panResponder.panHandlers}>
          <Svg width={chartWidth} height={chartHeight}>
            <Defs>
              <LinearGradient id="socGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <Stop offset="0%" stopColor={COLORS.accent} stopOpacity="0.3" />
                <Stop offset="100%" stopColor={COLORS.accent} stopOpacity="0.05" />
              </LinearGradient>
            </Defs>

            {/* Y-axis grid lines only (no labels) */}
            {[0, 25, 50, 75, 100].map((value) => {
              const y = yScale(value);
              return (
                <Line
                  key={`grid-${value}`}
                  x1={chartPadding.left}
                  y1={y}
                  x2={chartWidth - chartPadding.right}
                  y2={y}
                  stroke={isDark ? '#333' : '#e5e5e5'}
                  strokeWidth={1}
                  strokeDasharray={value === 0 ? '0' : '4,4'}
                />
              );
            })}

            {/* Gradient fill area */}
            <Path d={areaPath} fill="url(#socGradient)" />

            {/* Main line */}
            <Path
              d={linePath}
              stroke={COLORS.accent}
              strokeWidth={2.5}
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            {/* Touch indicator line */}
            {touchX !== null && selectedPoint && (
              <>
                <Line
                  x1={touchX}
                  y1={chartPadding.top}
                  x2={touchX}
                  y2={chartPadding.top + innerHeight}
                  stroke={COLORS.accent}
                  strokeWidth={1}
                  strokeDasharray="4,3"
                  opacity={0.7}
                />
                <Circle
                  cx={touchX}
                  cy={yScale(selectedPoint.soc)}
                  r={5}
                  fill={COLORS.accent}
                  stroke={isDark ? COLORS.surface : '#fff'}
                  strokeWidth={2}
                />
                <Circle
                  cx={touchX}
                  cy={yScale(selectedPoint.soc)}
                  r={8}
                  fill="transparent"
                  stroke={COLORS.accent}
                  strokeWidth={1.5}
                  opacity={0.4}
                />
              </>
            )}

            {/* X-axis labels */}
            {socData.map((point, index) => {
              if (index % labelStep !== 0 && index !== socData.length - 1) return null;
              let x = xScale(index);
              const isFirst = index === 0;
              const isLast = index === socData.length - 1;
              // Adjust x position for first and last labels to stay within bounds
              if (isFirst) x = 2;
              if (isLast) x = chartWidth - 2;
              const textAnchor = isFirst ? 'start' : isLast ? 'end' : 'middle';
              return (
                <SvgText
                  key={`x-label-${index}`}
                  x={x}
                  y={chartPadding.top + innerHeight + 15}
                  fontSize={10}
                  fill={isDark ? '#888' : '#666'}
                  textAnchor={textAnchor}
                >
                  {point.timestamp}
                </SvgText>
              );
            })}
          </Svg>
        </View>
      </View>
    );
  };

  return (
    <View style={{ flex: 1, backgroundColor: COLORS.dark, paddingTop: insets.top + 8 }}>
      {/* Header */}
      <View style={styles.header}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 16 }}>
          <TouchableOpacity
            onPress={() => navigation.goBack()}
            style={{ paddingVertical: 8, paddingRight: 8, marginLeft: -4 }}
          >
            <ArrowLeft size={26} color={COLORS.textSoft} strokeWidth={2.5} />
          </TouchableOpacity>
          <Text style={styles.title}>{t('socHistoryDetail.title')}</Text>
        </View>
      </View>

      {/* Date Selector */}
      <View style={styles.dateContainer}>
        <TouchableOpacity
          style={styles.dateButton}
          onPress={() => setShowDatePicker(true)}
        >
          <Calendar size={18} color={COLORS.textSoft} />
          <Text style={styles.dateButtonText}>{formatDate(selectedDate)}</Text>
        </TouchableOpacity>
      </View>

      {/* Content */}
      <View style={styles.content}>
        {loading ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color={COLORS.accent} />
          </View>
        ) : error ? (
          <View style={styles.emptyStateContainer}>
            <Text style={[styles.emptyStateText, { color: COLORS.error }]}>{error}</Text>
            <TouchableOpacity onPress={loadData} style={{ marginTop: 16 }}>
              <Text style={{ color: COLORS.accent }}>{t('common.retry', 'Retry')}</Text>
            </TouchableOpacity>
          </View>
        ) : socData.length === 0 ? (
          <View style={styles.emptyStateContainer}>
            <Text style={styles.emptyStateText}>{t('socHistoryDetail.noData', 'No data available for this date')}</Text>
          </View>
        ) : (
          <>
            {/* Summary Cards */}
            <View style={styles.cardsContainer}>
              <View style={styles.card}>
                <Text style={styles.cardLabel}>{t('socHistoryDetail.min')}</Text>
                <Text style={[styles.cardValue, { color: getSocColor(minSoc) }]}>
                  {minSoc}%
                </Text>
              </View>

              <View style={styles.card}>
                <Text style={styles.cardLabel}>{t('socHistoryDetail.avg')}</Text>
                <Text style={[styles.cardValue, { color: getSocColor(avgSoc) }]}>
                  {avgSoc}%
                </Text>
              </View>

              <View style={styles.card}>
                <Text style={styles.cardLabel}>{t('socHistoryDetail.max')}</Text>
                <Text style={[styles.cardValue, { color: getSocColor(maxSoc) }]}>
                  {maxSoc}%
                </Text>
              </View>
            </View>

            {/* Chart */}
            {renderChart()}
          </>
        )}
      </View>

      {/* Date Picker Modal */}
      <Modal
        visible={showDatePicker}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setShowDatePicker(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>{t('socHistoryDetail.selectDate')}</Text>
              <TouchableOpacity
                onPress={() => setShowDatePicker(false)}
                style={styles.modalCloseButton}
              >
                <Text style={styles.modalCloseText}>×</Text>
              </TouchableOpacity>
            </View>
            <RNCalendar
              onDayPress={handleDateSelect}
              markedDates={markedDates}
              maxDate={todayString}
              theme={{
                backgroundColor: COLORS.surface,
                calendarBackground: COLORS.surface,
                textSectionTitleColor: COLORS.textSoft,
                selectedDayBackgroundColor: COLORS.accent,
                selectedDayTextColor: COLORS.white,
                todayTextColor: COLORS.accent,
                dayTextColor: COLORS.text,
                textDisabledColor: isDark ? 'rgba(156, 163, 175, 0.3)' : 'rgba(107, 114, 128, 0.3)',
                disabledArrowColor: COLORS.border,
                monthTextColor: COLORS.text,
                arrowColor: COLORS.accent,
              }}
            />
          </View>
        </View>
      </Modal>
    </View>
  );
}
