import React, { useState, useEffect, useCallback } from 'react';
import { View, Text, TouchableOpacity, ActivityIndicator, Modal, Dimensions } from 'react-native';
import { ArrowLeft, Calendar } from 'lucide-react-native';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTranslation } from 'react-i18next';
import { Calendar as RNCalendar } from 'react-native-calendars';
import Svg, { Path, Line, Text as SvgText, G, Circle, Defs, LinearGradient, Stop, Rect } from 'react-native-svg';
import { useSelectedSiteDevice } from '../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { createStyles } from './SocHistoryDetailScreen.styles';

interface HourlySocData {
  hour: string;
  soc: number;
}

// Demo data generator for a single day - generates realistic SOC curve
const generateDemoData = (date: string): HourlySocData[] => {
  const data: HourlySocData[] = [];
  const seed = date.split('-').reduce((acc, val) => acc + parseInt(val, 10), 0);

  // Start with a random SOC between 30-60%
  let currentSoc = 30 + ((seed * 17) % 30);

  for (let hour = 0; hour < 24; hour++) {
    // Simulate realistic SOC behavior:
    // - Night (0-6): slow discharge (powering home)
    // - Morning (6-10): solar starts, SOC increases
    // - Midday (10-14): peak solar, fast charging
    // - Afternoon (14-18): moderate solar, slower charging
    // - Evening (18-24): discharge (no solar, powering home)

    let socChange = 0;
    const randomFactor = ((seed * (hour + 1) * 7) % 100) / 100;

    if (hour >= 0 && hour < 6) {
      // Night discharge
      socChange = -2 - randomFactor * 2;
    } else if (hour >= 6 && hour < 10) {
      // Morning - solar starts
      socChange = 3 + randomFactor * 3;
    } else if (hour >= 10 && hour < 14) {
      // Peak solar
      socChange = 5 + randomFactor * 4;
    } else if (hour >= 14 && hour < 18) {
      // Afternoon solar
      socChange = 2 + randomFactor * 3;
    } else {
      // Evening discharge
      socChange = -4 - randomFactor * 3;
    }

    currentSoc = Math.max(10, Math.min(100, currentSoc + socChange));

    data.push({
      hour: `${hour.toString().padStart(2, '0')}:00`,
      soc: Math.round(currentSoc),
    });
  }

  return data;
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
  const [loading, setLoading] = useState(false);
  const [hourlyData, setHourlyData] = useState<HourlySocData[]>([]);
  const [minSoc, setMinSoc] = useState(0);
  const [maxSoc, setMaxSoc] = useState(0);
  const [avgSoc, setAvgSoc] = useState(0);

  // Single date selection
  const [selectedDate, setSelectedDate] = useState<string>('');
  const [showDatePicker, setShowDatePicker] = useState(false);
  const [markedDates, setMarkedDates] = useState<any>({});

  const screenWidth = Dimensions.get('window').width;
  const chartHeight = 280;
  const chartPadding = { top: 40, right: 16, bottom: 50, left: 12 };

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
    const selectedDateObj = new Date(selected);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

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

  // Load data
  const loadData = useCallback(async () => {
    if (!hasDevice || !device?.id || !selectedDate) {
      return;
    }

    setLoading(true);
    try {
      // Use demo data since backend is not available
      const data = generateDemoData(selectedDate);
      setHourlyData(data);

      // Calculate statistics
      const socValues = data.map(d => d.soc);
      setMinSoc(Math.min(...socValues));
      setMaxSoc(Math.max(...socValues));
      setAvgSoc(Math.round(socValues.reduce((a, b) => a + b, 0) / socValues.length));
    } catch (error) {
      console.error('Error loading SOC history:', error);
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

  // Render line chart
  const renderChart = () => {
    if (hourlyData.length === 0) return null;

    const chartWidth = screenWidth - 32;
    const innerWidth = chartWidth - chartPadding.left - chartPadding.right;
    const innerHeight = chartHeight - chartPadding.top - chartPadding.bottom;

    // Scale: Y axis is 0-100%
    const yScale = (value: number) => {
      return chartPadding.top + innerHeight - (value / 100) * innerHeight;
    };

    const xScale = (index: number) => {
      return chartPadding.left + (index / (hourlyData.length - 1)) * innerWidth;
    };

    // Create path for the line
    const linePath = hourlyData
      .map((point, index) => {
        const x = xScale(index);
        const y = yScale(point.soc);
        return `${index === 0 ? 'M' : 'L'} ${x} ${y}`;
      })
      .join(' ');

    // Create path for the gradient fill area
    const areaPath = `${linePath} L ${xScale(hourlyData.length - 1)} ${chartPadding.top + innerHeight} L ${chartPadding.left} ${chartPadding.top + innerHeight} Z`;

    return (
      <View style={styles.chartContainer}>
        <Text style={styles.chartTitle}>{t('socHistoryDetail.chartTitle')}</Text>
        <Svg width={chartWidth} height={chartHeight}>
          <Defs>
            <LinearGradient id="socGradient" x1="0%" y1="0%" x2="0%" y2="100%">
              <Stop offset="0%" stopColor={COLORS.accent} stopOpacity="0.3" />
              <Stop offset="100%" stopColor={COLORS.accent} stopOpacity="0.05" />
            </LinearGradient>
          </Defs>

          {/* Y-axis grid lines and labels */}
          {[0, 25, 50, 75, 100].map((value) => {
            const y = yScale(value);
            return (
              <G key={`grid-${value}`}>
                <Line
                  x1={chartPadding.left}
                  y1={y}
                  x2={chartWidth - chartPadding.right}
                  y2={y}
                  stroke={isDark ? '#333' : '#e5e5e5'}
                  strokeWidth={1}
                  strokeDasharray={value === 0 ? '0' : '4,4'}
                />
                <SvgText
                  x={chartPadding.left + 4}
                  y={y - 4}
                  fontSize={10}
                  fill={isDark ? '#666' : '#888'}
                  textAnchor="start"
                >
                  {value}%
                </SvgText>
              </G>
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

          {/* Data points */}
          {hourlyData.map((point, index) => {
            // Only show points every 4 hours to avoid clutter
            if (index % 4 !== 0 && index !== hourlyData.length - 1) return null;
            const x = xScale(index);
            const y = yScale(point.soc);
            return (
              <G key={`point-${index}`}>
                <Circle
                  cx={x}
                  cy={y}
                  r={4}
                  fill={COLORS.accent}
                  stroke={isDark ? COLORS.surface : '#fff'}
                  strokeWidth={2}
                />
              </G>
            );
          })}

          {/* X-axis labels (every 4 hours) */}
          {[0, 4, 8, 12, 16, 20, 23].map((hour) => {
            const index = hour;
            if (index >= hourlyData.length) return null;
            const x = xScale(index);
            return (
              <SvgText
                key={`x-label-${hour}`}
                x={x}
                y={chartHeight - 15}
                fontSize={10}
                fill={isDark ? '#888' : '#666'}
                textAnchor="middle"
              >
                {hourlyData[index]?.hour || `${hour.toString().padStart(2, '0')}:00`}
              </SvgText>
            );
          })}
        </Svg>
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
        {!selectedDate ? (
          <View style={styles.emptyStateContainer}>
            <Calendar size={48} color={COLORS.textSoft} strokeWidth={1.5} />
            <Text style={styles.emptyStateText}>{t('socHistoryDetail.selectDatePrompt')}</Text>
            <Text style={styles.emptyStateSubtext}>
              {t('socHistoryDetail.selectDateDescription')}
            </Text>
          </View>
        ) : loading ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color={COLORS.accent} />
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
              maxDate={new Date().toISOString().split('T')[0]}
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
