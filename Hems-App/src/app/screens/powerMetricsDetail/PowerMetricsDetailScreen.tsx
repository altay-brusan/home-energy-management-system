import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { View, Text, Switch, ActivityIndicator, Platform, PanResponder, TouchableOpacity, ScrollView, SafeAreaView, Dimensions, Modal } from 'react-native';
import Svg, { Path, Line, Defs, LinearGradient, Stop, G, Text as SvgText, Rect, Circle } from 'react-native-svg';
import Icon from 'react-native-vector-icons/MaterialIcons';
import { ArrowLeft, Eye, EyeOff, ChevronDown } from 'lucide-react-native';
import { Calendar } from 'react-native-calendars';
import { useTranslation } from 'react-i18next';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { LocaleConfig } from 'react-native-calendars';
import { useSelectedSiteDevice } from '../../../hooks/useSelectedSiteDevice';
import { useDashboardRefresh } from '../../../hooks/useDashboardRefresh';
import { powerMetrics24HService } from '../../../api/services/powerMetrics24HService';
import { dashboardService } from '../../../api/services/dashboardService';
import { useThemeStore } from '../../../store/themeStore';
import { useI18nStore } from '../../../store/i18nStore';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { getPowerMetricsDetailStyles } from './PowerMetricsDetailScreen.styles';
import StackedBarChart from '../../../components/charts/StackedBarChart';
import { aggregateToHourly, aggregateToWeekDays, aggregateToMonthDays, aggregateToMonths } from '../../../utils/powerMetricsAggregation';
import type { AggregatedEnergyDataPoint } from '../../../api/types/powerMetrics.types';

// Power line colors
const POWER_COLORS = {
  solar: '#FACC15',
  battery: '#22C55E',
  grid: '#3B82F6',
  load: '#A855F7',
};

// Button colors matching TodaySummary design
const BUTTON_COLORS = {
  solar: '#FACC15',    // Solar yellow
  battery: '#22C55E',  // Battery green
  grid: '#3B82F6',     // Grid blue
  load: '#A855F7',     // Load purple
};

// Configure calendar locales
LocaleConfig.locales['tr'] = {
  monthNames: ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'],
  monthNamesShort: ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'],
  dayNames: ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'],
  dayNamesShort: ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt'],
  today: 'Bugün'
};

LocaleConfig.locales['nl'] = {
  monthNames: ['Januari', 'Februari', 'Maart', 'April', 'Mei', 'Juni', 'Juli', 'Augustus', 'September', 'Oktober', 'November', 'December'],
  monthNamesShort: ['Jan', 'Feb', 'Mrt', 'Apr', 'Mei', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dec'],
  dayNames: ['Zondag', 'Maandag', 'Dinsdag', 'Woensdag', 'Donderdag', 'Vrijdag', 'Zaterdag'],
  dayNamesShort: ['Zo', 'Ma', 'Di', 'Wo', 'Do', 'Vr', 'Za'],
  today: 'Vandaag'
};

LocaleConfig.locales['en'] = {
  monthNames: ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'],
  monthNamesShort: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
  dayNames: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
  dayNamesShort: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
  today: 'Today'
};

export default function PowerMetricsDetailScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();
  const insets = useSafeAreaInsets();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getPowerMetricsDetailStyles(COLORS, isDark), [COLORS, isDark]);
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const language = useI18nStore(s => s.language);

  // Set calendar locale based on current language
  useEffect(() => {
    LocaleConfig.defaultLocale = language;
  }, [language]);

  // Toggle states for each power metric
  const [showSolar, setShowSolar] = useState(true);
  const [showBattery, setShowBattery] = useState(true);
  const [showGrid, setShowGrid] = useState(true);
  const [showLoad, setShowLoad] = useState(true);

  const [powerMetricsData, setPowerMetricsData] = useState<any>(null);
  const [aggregatedData, setAggregatedData] = useState<AggregatedEnergyDataPoint[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [touchX, setTouchX] = useState<number | null>(null);
  const [selectedDataPoint, setSelectedDataPoint] = useState<any>(null);
  const [selectedDate, setSelectedDate] = useState(new Date());
  const [selectedRange, setSelectedRange] = useState<'Day' | 'Week' | 'Month' | 'Year'>('Day');
  const [isDatePickerVisible, setIsDatePickerVisible] = useState(false);
  const [isRangePickerVisible, setIsRangePickerVisible] = useState(false);

  const screenHeight = Dimensions.get('window').height;

  // Load power metrics data
  const loadPowerMetricsData = useCallback(async (isInitialLoad = false) => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }

    // Only show full page loading on initial load
    if (isInitialLoad) {
      setLoading(true);
    } else {
      setRefreshing(true);
    }

    try {
      let aggregated: AggregatedEnergyDataPoint[] = [];

      switch (selectedRange) {
        case 'Day': {
          // Day view uses line chart with raw data
          // Format date as YYYY-MM-DD for API
          const dateString = selectedDate.toISOString().split('T')[0];

          const data = await dashboardService.getPowerAndEnergyMetrics(device.id, 'day', dateString);
          setPowerMetricsData(data);
          // No aggregation needed for day view
          console.log('Power Metrics Data (Day):', {
            totalDataPoints: data.data.length,
            range: data.range,
            selectedDate: dateString
          });
          break;
        }
        case 'Week': {
          const weekData = await powerMetrics24HService.getPowerMetricsForWeek(device.id, selectedDate);
          // Calculate Monday of the week
          const dayOfWeek = selectedDate.getDay();
          const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
          const monday = new Date(selectedDate);
          monday.setDate(selectedDate.getDate() + mondayOffset);
          monday.setHours(0, 0, 0, 0);

          aggregated = aggregateToWeekDays(weekData, monday);
          setPowerMetricsData(null); // No sunrise/sunset for week view
          console.log('Aggregated Data (Week):', {
            range: selectedRange,
            dataPoints: aggregated.length,
            selectedDate: selectedDate.toISOString(),
            monday: monday.toISOString()
          });
          break;
        }
        case 'Month': {
          const monthData = await powerMetrics24HService.getPowerMetricsForMonth(device.id, selectedDate);
          const year = selectedDate.getFullYear();
          const month = selectedDate.getMonth();

          aggregated = aggregateToMonthDays(monthData, year, month);
          setPowerMetricsData(null); // No sunrise/sunset for month view
          console.log('Aggregated Data (Month):', {
            range: selectedRange,
            dataPoints: aggregated.length,
            selectedDate: selectedDate.toISOString(),
            year,
            month
          });
          break;
        }
        case 'Year': {
          const yearData = await powerMetrics24HService.getPowerMetricsForYear(device.id, selectedDate);
          const year = selectedDate.getFullYear();

          aggregated = aggregateToMonths(yearData, year);
          setPowerMetricsData(null); // No sunrise/sunset for year view
          console.log('Aggregated Data (Year):', {
            range: selectedRange,
            dataPoints: aggregated.length,
            selectedDate: selectedDate.toISOString(),
            year
          });
          break;
        }
      }

      setAggregatedData(aggregated);
    } catch (error) {
      console.error('Failed to load power metrics:', error);
      setPowerMetricsData(null);
      setAggregatedData([]);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [hasDevice, device?.id, selectedDate, selectedRange]);

  useEffect(() => {
    loadPowerMetricsData(true); // Initial load
  }, []);

  useEffect(() => {
    loadPowerMetricsData(false); // Date or range change - don't show full page loading
  }, [selectedDate, selectedRange]);

  useEffect(() => {
    const refreshCallback = () => loadPowerMetricsData(false);
    const unregister = registerRefreshCallback(refreshCallback);
    return () => unregister();
  }, [registerRefreshCallback, loadPowerMetricsData]);

  // Auto-refresh every 15 minutes
  useEffect(() => {
    const interval = setInterval(() => {
      loadPowerMetricsData(false);
    }, 15 * 60 * 1000);
    return () => clearInterval(interval);
  }, [loadPowerMetricsData]);

  // Calculate non-linear X-axis mapping
  const getTimeToXPosition = useCallback((hour: number, sunriseHour: number, sunsetHour: number, width: number): number => {
    const daytimeRatio = 0.6;
    const nighttimeRatio = 0.4;

    const dayLength = sunsetHour - sunriseHour;
    const nightLength = 24 - dayLength;

    if (hour >= sunriseHour && hour <= sunsetHour) {
      const hoursFromSunrise = hour - sunriseHour;
      const positionInDay = hoursFromSunrise / dayLength;
      const nightBeforeWidth = (sunriseHour / nightLength) * nighttimeRatio * width;
      return nightBeforeWidth + positionInDay * daytimeRatio * width;
    } else if (hour < sunriseHour) {
      const positionInNight = hour / sunriseHour;
      return positionInNight * (sunriseHour / nightLength) * nighttimeRatio * width;
    } else {
      const hoursAfterSunset = hour - sunsetHour;
      const nightAfterLength = 24 - sunsetHour;
      const positionInNight = hoursAfterSunset / nightAfterLength;
      const beforeWidth = (sunriseHour / nightLength) * nighttimeRatio * width + daytimeRatio * width;
      return beforeWidth + positionInNight * (nightAfterLength / nightLength) * nighttimeRatio * width;
    }
  }, []);

  const getHourFromTimestamp = (timestamp: string): number => {
    // Handle both formats: "HH:mm" (new API) and full ISO date (old API)
    if (timestamp.includes(':') && !timestamp.includes('T')) {
      // New format: "04:18"
      const [hours, minutes] = timestamp.split(':').map(Number);
      return hours + minutes / 60;
    } else {
      // Old format: full ISO date
      const date = new Date(timestamp);
      return date.getHours() + date.getMinutes() / 60;
    }
  };

  const formatTime = (timestamp: string): string => {
    // Handle both formats: "HH:mm" (new API) and full ISO date (old API)
    if (timestamp.includes(':') && !timestamp.includes('T')) {
      // New format: "04:18" - already formatted
      return timestamp;
    } else {
      // Old format: full ISO date
      const date = new Date(timestamp);
      return `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
    }
  };

  const createStraightPath = useCallback((points: { x: number; y: number }[]): string => {
    if (points.length < 2) return '';
    let path = `M ${points[0].x},${points[0].y}`;
    for (let i = 1; i < points.length; i++) {
      path += ` L ${points[i].x},${points[i].y}`;
    }
    return path;
  }, []);

  const getWeekNumber = (date: Date) => {
    const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    const dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    const weekNo = Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
    return weekNo;
  };

  const formatDate = (date: Date) => {
    const monthKeys = ['january', 'february', 'march', 'april', 'may', 'june',
      'july', 'august', 'september', 'october', 'november', 'december'];

    switch (selectedRange) {
      case 'Day':
        return `${date.getFullYear()}/${String(date.getMonth() + 1).padStart(2, '0')}/${String(date.getDate()).padStart(2, '0')}`;
      case 'Week':
        return t('powerMetrics24H.dateFormat.week', {
          weekNumber: getWeekNumber(date),
          year: date.getFullYear()
        });
      case 'Month':
        return t('powerMetrics24H.dateFormat.month', {
          monthName: t(`powerMetrics24H.months.${monthKeys[date.getMonth()]}`),
          year: date.getFullYear()
        });
      case 'Year':
        return t('powerMetrics24H.dateFormat.year', {
          year: date.getFullYear()
        });
      default:
        return `${date.getFullYear()}/${String(date.getMonth() + 1).padStart(2, '0')}/${String(date.getDate()).padStart(2, '0')}`;
    }
  };

  // Check if we can navigate to next period (prevent future dates)
  const canNavigateNext = () => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const newDate = new Date(selectedDate);
    newDate.setHours(0, 0, 0, 0);

    if (selectedRange === 'Day') {
      newDate.setDate(newDate.getDate() + 1);
      // Allow navigating up to and including today
      return newDate.getTime() <= today.getTime();
    } else if (selectedRange === 'Week') {
      newDate.setDate(newDate.getDate() + 7);
      return newDate.getTime() <= today.getTime();
    } else if (selectedRange === 'Month') {
      newDate.setMonth(newDate.getMonth() + 1);
      return newDate.getTime() <= today.getTime();
    } else if (selectedRange === 'Year') {
      newDate.setFullYear(newDate.getFullYear() + 1);
      return newDate.getFullYear() <= today.getFullYear();
    }

    return true;
  };

  const navigateDate = (direction: 'prev' | 'next') => {
    // Don't allow navigation to future dates
    if (direction === 'next' && !canNavigateNext()) {
      return;
    }

    const newDate = new Date(selectedDate);
    if (selectedRange === 'Day') {
      newDate.setDate(newDate.getDate() + (direction === 'next' ? 1 : -1));
    } else if (selectedRange === 'Week') {
      newDate.setDate(newDate.getDate() + (direction === 'next' ? 7 : -7));
    } else if (selectedRange === 'Month') {
      newDate.setMonth(newDate.getMonth() + (direction === 'next' ? 1 : -1));
    } else if (selectedRange === 'Year') {
      newDate.setFullYear(newDate.getFullYear() + (direction === 'next' ? 1 : -1));
    }
    setSelectedDate(newDate);
  };

  const getWeekRange = (date: Date) => {
    const day = date.getDay();
    const diff = date.getDate() - day + (day === 0 ? -6 : 1); // Adjust when day is Sunday
    const monday = new Date(date);
    monday.setDate(diff);
    monday.setHours(0, 0, 0, 0);

    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);
    sunday.setHours(23, 59, 59, 999);

    return { start: monday, end: sunday };
  };

  const getMarkedDates = () => {
    const primaryColor = COLORS.accent || '#1169b1';
    const selectedTextColor = COLORS.white || '#FFFFFF';

    if (selectedRange === 'Day') {
      const dateString = selectedDate.toISOString().split('T')[0];
      return {
        [dateString]: {
          startingDay: true,
          endingDay: true,
          color: primaryColor,
          textColor: selectedTextColor,
        }
      };
    } else if (selectedRange === 'Week') {
      const { start, end } = getWeekRange(selectedDate);
      const marked: any = {};

      const currentDate = new Date(start);
      while (currentDate <= end) {
        const dateString = currentDate.toISOString().split('T')[0];
        const isStart = currentDate.toDateString() === start.toDateString();
        const isEnd = currentDate.toDateString() === end.toDateString();

        marked[dateString] = {
          startingDay: isStart,
          endingDay: isEnd,
          color: primaryColor,
          textColor: selectedTextColor,
        };

        currentDate.setDate(currentDate.getDate() + 1);
      }

      return marked;
    } else if (selectedRange === 'Month') {
      const year = selectedDate.getFullYear();
      const month = selectedDate.getMonth();
      const firstDay = new Date(year, month, 1);
      const lastDay = new Date(year, month + 1, 0);
      const marked: any = {};

      const currentDate = new Date(firstDay);
      while (currentDate <= lastDay) {
        const dateString = currentDate.toISOString().split('T')[0];
        const isStart = currentDate.getDate() === 1;
        const isEnd = currentDate.getDate() === lastDay.getDate();

        marked[dateString] = {
          startingDay: isStart,
          endingDay: isEnd,
          color: primaryColor,
          textColor: selectedTextColor,
        };

        currentDate.setDate(currentDate.getDate() + 1);
      }

      return marked;
    }

    return {};
  };

  const renderChart = () => {
    // Day view uses line chart, other views use stacked bar chart
    if (selectedRange === 'Day') {
      return renderLineChart();
    } else {
      return renderStackedBarChart();
    }
  };

  const renderStackedBarChart = () => {
    const chartWidth = Dimensions.get('window').width;
    // Header (60) + TimeSelector (60) + Bottom area (140) + top padding (insets.top + 8) + safe area bottom + extra padding
    const chartHeight = screenHeight - insets.top - 60 - 60 - 140 - insets.bottom - SPACING.md - 40;

    if (!aggregatedData || aggregatedData.length === 0) {
      return (
        <View style={styles.loadingContainer}>
          {refreshing ? (
            <>
              <ActivityIndicator size="large" color={COLORS.accent} />
              <Text style={styles.loadingText}>{t('common.loading', 'Loading...')}</Text>
            </>
          ) : (
            <Text style={styles.loadingText}>{t('common.noData', 'No data available')}</Text>
          )}
        </View>
      );
    }

    return (
      <View style={{ flex: 1 }}>
        <StackedBarChart
          data={aggregatedData}
          colors={POWER_COLORS}
          visibility={{
            solar: showSolar,
            battery: showBattery,
            grid: showGrid,
            load: showLoad
          }}
          width={chartWidth}
          height={chartHeight}
          cardBgColor={COLORS.cardBg}
          textColor={COLORS.textSoft}
        />
      </View>
    );
  };

  const renderLineChart = () => {
    const chartWidth = Dimensions.get('window').width;
    // Header (60) + TimeSelector (60) + Bottom area (140) + top padding (insets.top + 8) + safe area bottom + extra padding
    const chartHeight = screenHeight - insets.top - 60 - 60 - 140 - insets.bottom - SPACING.md - 40;
    const padding = { top: 20, right: 0, bottom: 56, left: 0 };
    const innerWidth = chartWidth - padding.left - padding.right;
    const innerHeight = chartHeight - padding.top - padding.bottom;

    const findNearestDataPoint = (x: number) => {
      if (!powerMetricsData?.data) return;

      const sunriseHour = powerMetricsData.sunriseTime ? getHourFromTimestamp(powerMetricsData.sunriseTime) : 6;
      const sunsetHour = powerMetricsData.sunsetTime ? getHourFromTimestamp(powerMetricsData.sunsetTime) : 18;

      let nearestPoint = null;
      let minDistance = Infinity;

      powerMetricsData.data.forEach((point: any) => {
        const hour = getHourFromTimestamp(point.timestamp);
        const pointX = padding.left + getTimeToXPosition(hour, sunriseHour, sunsetHour, innerWidth);
        const distance = Math.abs(pointX - x);

        if (distance < minDistance) {
          minDistance = distance;
          nearestPoint = point;
        }
      });

      setSelectedDataPoint(nearestPoint);
    };

    const getMaxTouchX = () => {
      if (!powerMetricsData?.data || powerMetricsData.data.length === 0) return padding.left + innerWidth;

      const sunriseHour = powerMetricsData.sunriseTime ? getHourFromTimestamp(powerMetricsData.sunriseTime) : 6;
      const sunsetHour = powerMetricsData.sunsetTime ? getHourFromTimestamp(powerMetricsData.sunsetTime) : 18;
      const lastPoint = powerMetricsData.data[powerMetricsData.data.length - 1];
      const lastHour = getHourFromTimestamp(lastPoint.timestamp);

      return padding.left + getTimeToXPosition(lastHour, sunriseHour, sunsetHour, innerWidth);
    };

    const maxTouchX = getMaxTouchX();

    const panResponder = PanResponder.create({
      onStartShouldSetPanResponder: () => true,
      onMoveShouldSetPanResponder: () => true,
      onPanResponderGrant: (evt) => {
        const x = evt.nativeEvent.locationX;
        if (x >= padding.left && x <= maxTouchX) {
          setTouchX(x);
          findNearestDataPoint(x);
        }
      },
      onPanResponderMove: (evt) => {
        const x = evt.nativeEvent.locationX;
        if (x >= padding.left && x <= maxTouchX) {
          setTouchX(x);
          findNearestDataPoint(x);
        } else if (x > maxTouchX) {
          setTouchX(null);
          setSelectedDataPoint(null);
        }
      },
      onPanResponderRelease: () => {
        setTouchX(null);
        setSelectedDataPoint(null);
      },
    });

    if (!powerMetricsData || !powerMetricsData.data.length) {
      return (
        <View style={styles.loadingContainer}>
          {refreshing ? (
            <>
              <ActivityIndicator size="large" color={COLORS.accent} />
              <Text style={styles.loadingText}>{t('common.loading', 'Loading...')}</Text>
            </>
          ) : (
            <Text style={styles.loadingText}>{t('common.noData', 'No data available')}</Text>
          )}
        </View>
      );
    }

    const dataToShow = powerMetricsData.data;
    // Use default sunrise/sunset times if not provided by API (6:00 and 18:00)
    const sunriseHour = powerMetricsData.sunriseTime ? getHourFromTimestamp(powerMetricsData.sunriseTime) : 6;
    const sunsetHour = powerMetricsData.sunsetTime ? getHourFromTimestamp(powerMetricsData.sunsetTime) : 18;

    const allValues: number[] = [];
    dataToShow.forEach((point: any) => {
      if (showSolar) allValues.push(point.solarPower);
      if (showBattery) allValues.push(point.batteryPower);
      if (showGrid) allValues.push(point.gridPower);
      // Load values should be negative for Y-axis range calculation
      if (showLoad) allValues.push(-Math.abs(point.loadPower));
    });

    const minValue = Math.min(...allValues, 0);
    const maxValue = Math.max(...allValues, 1);
    const range = maxValue - minValue || 1;

    const yMargin = range * 0.1;
    const yMin = minValue - yMargin;
    const yMax = maxValue + yMargin;
    const yRange = yMax - yMin;

    const zeroY = padding.top + innerHeight - ((0 - yMin) / yRange) * innerHeight;

    const mapDataToPoints = (data: typeof powerMetricsData.data, valueKey: string) => {
      return data.map((point: any) => {
        const hour = getHourFromTimestamp(point.timestamp);
        const x = padding.left + getTimeToXPosition(hour, sunriseHour, sunsetHour, innerWidth);
        let value = point[valueKey] as number;
        // Load values should be shown as negative (consumption below x-axis)
        if (valueKey === 'loadPower') {
          value = -Math.abs(value);
        }
        const y = padding.top + innerHeight - ((value - yMin) / yRange) * innerHeight;
        return { x, y, value };
      });
    };

    const solarPoints = showSolar ? mapDataToPoints(dataToShow, 'solarPower') : [];
    const batteryPoints = showBattery ? mapDataToPoints(dataToShow, 'batteryPower') : [];
    const gridPoints = showGrid ? mapDataToPoints(dataToShow, 'gridPower') : [];
    const loadPoints = showLoad ? mapDataToPoints(dataToShow, 'loadPower') : [];

    const createFillPath = (points: { x: number; y: number; value: number }[]): string => {
      if (points.length < 2) return '';
      const straightPath = createStraightPath(points);
      return `${straightPath} L ${points[points.length - 1].x},${zeroY} L ${points[0].x},${zeroY} Z`;
    };

    const sunriseX = padding.left + getTimeToXPosition(sunriseHour, sunriseHour, sunsetHour, innerWidth);
    const sunsetX = padding.left + getTimeToXPosition(sunsetHour, sunriseHour, sunsetHour, innerWidth);

    return (
      <View {...panResponder.panHandlers}>
        <Svg width={chartWidth} height={chartHeight}>
          <Defs>
            <LinearGradient id="solarGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.solar} stopOpacity={0.5} />
              <Stop offset="100%" stopColor={POWER_COLORS.solar} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="batteryGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.battery} stopOpacity={0.5} />
              <Stop offset="100%" stopColor={POWER_COLORS.battery} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="gridGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.grid} stopOpacity={0.4} />
              <Stop offset="100%" stopColor={POWER_COLORS.grid} stopOpacity={0.05} />
            </LinearGradient>
            <LinearGradient id="loadGradient" x1="0" y1="0" x2="0" y2="1">
              <Stop offset="0%" stopColor={POWER_COLORS.load} stopOpacity={0.5} />
              <Stop offset="100%" stopColor={POWER_COLORS.load} stopOpacity={0.05} />
            </LinearGradient>
          </Defs>

          {/* Grid lines */}
          {[0, 1, 2, 3, 4].map(i => {
            const y = padding.top + (i * innerHeight) / 4;
            return (
              <Line
                key={`h-grid-${i}`}
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

          <Line
            x1={padding.left}
            y1={zeroY}
            x2={padding.left + innerWidth}
            y2={zeroY}
            stroke={COLORS.textSoft}
            strokeWidth={2}
            opacity={0.7}
          />

          {[0, 6, 12, 18, 24].map(hour => {
            const x = padding.left + getTimeToXPosition(hour === 24 ? 23.99 : hour, sunriseHour, sunsetHour, innerWidth);
            return (
              <Line
                key={`v-grid-${hour}`}
                x1={x}
                y1={padding.top}
                x2={x}
                y2={padding.top + innerHeight}
                stroke={COLORS.border}
                strokeWidth={0.5}
                opacity={0.3}
              />
            );
          })}

          <Line x1={sunriseX} y1={padding.top} x2={sunriseX} y2={padding.top + innerHeight} stroke="#FDB022" strokeWidth={1.5} strokeDasharray="4,4" opacity={0.6} />
          <Line x1={sunsetX} y1={padding.top} x2={sunsetX} y2={padding.top + innerHeight} stroke="#F97316" strokeWidth={1.5} strokeDasharray="4,4" opacity={0.6} />

          {(() => {
            const now = new Date();
            const currentTime = now.getHours() + now.getMinutes() / 60;
            const currentTimeX = padding.left + getTimeToXPosition(currentTime, sunriseHour, sunsetHour, innerWidth);
            return (
              <>
                <Line x1={currentTimeX} y1={padding.top} x2={currentTimeX} y2={padding.top + innerHeight} stroke="#EF4444" strokeWidth={1} opacity={0.7} />
                <SvgText x={currentTimeX} y={padding.top - 5} fontSize={9} fill="#EF4444" fontWeight="700" textAnchor="middle">{t('powerMetrics24H.now')}</SvgText>
              </>
            );
          })()}

          {showLoad && loadPoints.length > 0 && <Path d={createFillPath(loadPoints)} fill="url(#loadGradient)" />}
          {showGrid && gridPoints.length > 0 && <Path d={createFillPath(gridPoints)} fill="url(#gridGradient)" />}
          {showBattery && batteryPoints.length > 0 && <Path d={createFillPath(batteryPoints)} fill="url(#batteryGradient)" />}
          {showSolar && solarPoints.length > 0 && <Path d={createFillPath(solarPoints)} fill="url(#solarGradient)" />}

          {showLoad && loadPoints.length > 0 && <Path d={createStraightPath(loadPoints)} stroke={POWER_COLORS.load} strokeWidth={1} fill="none" />}
          {showGrid && gridPoints.length > 0 && <Path d={createStraightPath(gridPoints)} stroke={POWER_COLORS.grid} strokeWidth={1} fill="none" />}
          {showBattery && batteryPoints.length > 0 && <Path d={createStraightPath(batteryPoints)} stroke={POWER_COLORS.battery} strokeWidth={1} fill="none" />}
          {showSolar && solarPoints.length > 0 && <Path d={createStraightPath(solarPoints)} stroke={POWER_COLORS.solar} strokeWidth={1} fill="none" />}

          {[0, 1, 2, 3, 4].map(i => {
            const y = padding.top + (i * innerHeight) / 4;
            const value = yMax - (i * yRange) / 4;
            const displayValue = Math.abs(value) >= 1000 ? `${(value / 1000).toFixed(1)}k` : value.toFixed(0);

            return (
              <G key={`y-label-${i}`}>
                <SvgText x={padding.left + 5} y={y + 4} fontSize={10} fill={COLORS.surface} textAnchor="start" opacity={0.8} stroke={COLORS.surface} strokeWidth={3}>{displayValue}</SvgText>
                <SvgText x={padding.left + 5} y={y + 4} fontSize={10} fill={COLORS.textSoft} textAnchor="start">{displayValue}</SvgText>
              </G>
            );
          })}

          <G>
            <SvgText x={padding.left + 5} y={zeroY + 4} fontSize={11} fill={COLORS.surface} fontWeight="700" textAnchor="start" opacity={0.9} stroke={COLORS.surface} strokeWidth={4}>0</SvgText>
            <SvgText x={padding.left + 5} y={zeroY + 4} fontSize={11} fill={COLORS.textSoft} fontWeight="700" textAnchor="start">0</SvgText>
          </G>

          {touchX !== null && selectedDataPoint && (
            <G>
              <Line x1={touchX} y1={padding.top} x2={touchX} y2={padding.top + innerHeight} stroke={COLORS.textSoft} strokeWidth={1} opacity={0.8} />
            </G>
          )}

          {/* X-axis labels (selected hours) */}
          {[0, 6, 12, 18, 23].map(hour => {
            const x = padding.left + getTimeToXPosition(hour, sunriseHour, sunsetHour, innerWidth);
            // For 00:00, use "start" anchor to prevent left edge cut-off
            const textAnchor = hour === 0 ? 'start' : 'middle';
            return (
              <SvgText
                key={`x-label-${hour}`}
                x={x}
                y={padding.top + innerHeight + 15}
                fontSize={10}
                fill={COLORS.textSoft}
                textAnchor={textAnchor}
              >
                {`${hour.toString().padStart(2, '0')}:00`}
              </SvgText>
            );
          })}
        </Svg>

        {touchX !== null && selectedDataPoint && (
          <View style={{
            position: 'absolute',
            top: -120,
            left: Math.max(10, Math.min(touchX - 100, 350 - 210)),
            backgroundColor: COLORS.cardBg,
            borderRadius: 12,
            padding: 14,
            shadowColor: '#000',
            shadowOffset: { width: 0, height: 2 },
            shadowOpacity: 0.25,
            shadowRadius: 3.84,
            elevation: 5,
            minWidth: 200,
          }}>
            <Text style={{ color: COLORS.textSoft, fontSize: 13, fontWeight: '700', marginBottom: 8 }}>
              {formatTime(selectedDataPoint.timestamp)}
            </Text>
            {showSolar && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.solar }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>{t('powerMetrics24H.labels.solar')}</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.solarPower?.toFixed(0) || '0'} W
                </Text>
              </View>
            )}
            {showBattery && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.battery }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>{t('powerMetrics24H.labels.battery')}</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.batteryPower?.toFixed(0) || '0'} W
                </Text>
              </View>
            )}
            {showGrid && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.grid }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>{t('powerMetrics24H.labels.grid')}</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.gridPower?.toFixed(0) || '0'} W
                </Text>
              </View>
            )}
            {showLoad && (
              <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: POWER_COLORS.load }} />
                  <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '500' }}>{t('powerMetrics24H.labels.load')}</Text>
                </View>
                <Text style={{ color: COLORS.textSoft, fontSize: 12, fontWeight: '700' }}>
                  {selectedDataPoint.loadPower?.toFixed(0) || '0'} W
                </Text>
              </View>
            )}
          </View>
        )}
      </View>
    );
  };

  if (loading) {
    return (
      <View style={{ flex: 1, backgroundColor: COLORS.dark, paddingTop: insets.top + 8 }}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
          <Text style={styles.loadingText}>{t('common.loading', 'Loading...')}</Text>
        </View>
      </View>
    );
  }

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
          <Text style={styles.title}>{t('powerMetrics24H.title', 'Power Metrics')}</Text>
        </View>
      </View>

      {/* Time Selector */}
      <View style={styles.timeSelectorContainer}>
        <View style={styles.dateSelector}>
          <TouchableOpacity onPress={() => navigateDate('prev')} style={styles.arrowButton}>
            <Icon name="chevron-left" size={24} color={COLORS.textSoft} />
          </TouchableOpacity>
          <TouchableOpacity onPress={() => setIsDatePickerVisible(true)}>
            <Text style={styles.dateText}>{formatDate(selectedDate)}</Text>
          </TouchableOpacity>
          <TouchableOpacity
            onPress={() => navigateDate('next')}
            style={styles.arrowButton}
            disabled={!canNavigateNext()}
          >
            <Icon
              name="chevron-right"
              size={24}
              color={canNavigateNext() ? COLORS.textSoft : (isDark ? '#4B5563' : '#D1D5DB')}
            />
          </TouchableOpacity>
        </View>
        <TouchableOpacity
          style={styles.rangeSelector}
          onPress={() => setIsRangePickerVisible(true)}
        >
          <Text style={styles.rangeText}>{t(`powerMetrics24H.ranges.${selectedRange.toLowerCase()}`)}</Text>
          <ChevronDown size={16} color={COLORS.textSoft} strokeWidth={2.5} />
        </TouchableOpacity>
      </View>

      {/* Chart */}
      <View style={{ flex: 1, opacity: refreshing ? 0.5 : 1 }}>
        {renderChart()}
        {refreshing && (
          <View style={{ position: 'absolute', top: '50%', left: '50%', transform: [{ translateX: -20 }, { translateY: -20 }] }}>
            <ActivityIndicator size="large" color={COLORS.accent} />
          </View>
        )}
      </View>

      {/* Bottom Area - Switches and Sunrise/Sunset */}
      <View style={[styles.bottomContainer, { paddingBottom: insets.bottom + SPACING.md }]}>
        {/* Toggle Buttons */}
        <View style={styles.togglesContainer}>
            <View style={styles.togglesRow}>
              <TouchableOpacity
                style={styles.toggleButton}
                onPress={() => setShowSolar(!showSolar)}
                activeOpacity={0.7}
              >
                <View style={[styles.iconContainer, { backgroundColor: `${BUTTON_COLORS.solar}15` }]}>
                  {showSolar ? (
                    <Eye size={16} color={BUTTON_COLORS.solar} strokeWidth={2.5} />
                  ) : (
                    <EyeOff size={16} color={BUTTON_COLORS.solar} strokeWidth={2.5} />
                  )}
                </View>
                <Text style={[styles.toggleButtonLabel, { color: COLORS.textSoft }]}>
                  {t('powerMetrics24H.labels.solar')}
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.toggleButton}
                onPress={() => setShowBattery(!showBattery)}
                activeOpacity={0.7}
              >
                <View style={[styles.iconContainer, { backgroundColor: `${BUTTON_COLORS.battery}15` }]}>
                  {showBattery ? (
                    <Eye size={16} color={BUTTON_COLORS.battery} strokeWidth={2.5} />
                  ) : (
                    <EyeOff size={16} color={BUTTON_COLORS.battery} strokeWidth={2.5} />
                  )}
                </View>
                <Text style={[styles.toggleButtonLabel, { color: COLORS.textSoft }]}>
                  {t('powerMetrics24H.labels.battery')}
                </Text>
              </TouchableOpacity>
            </View>

            <View style={styles.togglesRow}>
              <TouchableOpacity
                style={styles.toggleButton}
                onPress={() => setShowGrid(!showGrid)}
                activeOpacity={0.7}
              >
                <View style={[styles.iconContainer, { backgroundColor: `${BUTTON_COLORS.grid}15` }]}>
                  {showGrid ? (
                    <Eye size={16} color={BUTTON_COLORS.grid} strokeWidth={2.5} />
                  ) : (
                    <EyeOff size={16} color={BUTTON_COLORS.grid} strokeWidth={2.5} />
                  )}
                </View>
                <Text style={[styles.toggleButtonLabel, { color: COLORS.textSoft }]}>
                  {t('powerMetrics24H.labels.grid')}
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.toggleButton}
                onPress={() => setShowLoad(!showLoad)}
                activeOpacity={0.7}
              >
                <View style={[styles.iconContainer, { backgroundColor: `${BUTTON_COLORS.load}15` }]}>
                  {showLoad ? (
                    <Eye size={16} color={BUTTON_COLORS.load} strokeWidth={2.5} />
                  ) : (
                    <EyeOff size={16} color={BUTTON_COLORS.load} strokeWidth={2.5} />
                  )}
                </View>
                <Text style={[styles.toggleButtonLabel, { color: COLORS.textSoft }]}>
                  {t('powerMetrics24H.labels.load')}
                </Text>
              </TouchableOpacity>
            </View>
          </View>

        {/* Sunrise/Sunset Info */}
        {powerMetricsData && (
          <View style={styles.sunInfoContainer}>
            <View style={styles.sunInfoItem}>
              <Text style={styles.sunInfoLabel}>{t('powerMetrics24H.sunrise')}</Text>
              <Text style={styles.sunInfoTime}>
                {new Date(powerMetricsData.sunriseTime).toLocaleTimeString('en-US', {
                  hour: '2-digit',
                  minute: '2-digit',
                  hour12: true
                })}
              </Text>
            </View>
            <View style={styles.sunInfoItem}>
              <Text style={styles.sunInfoLabel}>{t('powerMetrics24H.sunset')}</Text>
              <Text style={styles.sunInfoTime}>
                {new Date(powerMetricsData.sunsetTime).toLocaleTimeString('en-US', {
                  hour: '2-digit',
                  minute: '2-digit',
                  hour12: true
                })}
              </Text>
            </View>
          </View>
        )}
      </View>

      {/* Date Picker Modal */}
      <Modal
        visible={isDatePickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setIsDatePickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setIsDatePickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>{t('modals.selectDate')}</Text>
              <TouchableOpacity onPress={() => setIsDatePickerVisible(false)}>
                <Icon name="close" size={24} color={COLORS.textSoft} />
              </TouchableOpacity>
            </View>
            <Calendar
              current={selectedDate.toISOString().split('T')[0]}
              markedDates={getMarkedDates()}
              markingType={'period'}
              maxDate={new Date().toISOString().split('T')[0]}
              onDayPress={(day) => {
                setSelectedDate(new Date(day.timestamp));
                setIsDatePickerVisible(false);
              }}
              theme={{
                backgroundColor: COLORS.surface,
                calendarBackground: COLORS.surface,
                textSectionTitleColor: COLORS.textSoft,
                selectedDayBackgroundColor: COLORS.accent || '#1169b1',
                selectedDayTextColor: COLORS.white || '#FFFFFF',
                todayTextColor: COLORS.accent || '#1169b1',
                dayTextColor: COLORS.textSoft,
                textDisabledColor: isDark ? '#4B5563' : '#D1D5DB',
                monthTextColor: COLORS.textSoft,
                arrowColor: COLORS.textSoft,
                textMonthFontWeight: '700',
                textDayFontSize: 14,
                textMonthFontSize: 16,
                textDayHeaderFontSize: 12,
              }}
            />
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Range Picker Modal */}
      <Modal
        visible={isRangePickerVisible}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setIsRangePickerVisible(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setIsRangePickerVisible(false)}
        >
          <View style={styles.modalContent}>
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>{t('modals.selectRange')}</Text>
              <TouchableOpacity onPress={() => setIsRangePickerVisible(false)}>
                <Icon name="close" size={24} color={COLORS.textSoft} />
              </TouchableOpacity>
            </View>
            <View style={styles.rangeOptions}>
              {(['Day', 'Week', 'Month', 'Year'] as const).map((range) => (
                <TouchableOpacity
                  key={range}
                  style={[
                    styles.rangeOption,
                    selectedRange === range && styles.rangeOptionActive
                  ]}
                  onPress={() => {
                    setSelectedRange(range);
                    setIsRangePickerVisible(false);
                  }}
                >
                  <Text style={[
                    styles.rangeOptionText,
                    selectedRange === range && styles.rangeOptionTextActive
                  ]}>
                    {t(`powerMetrics24H.ranges.${range.toLowerCase()}`)}
                  </Text>
                  {selectedRange === range && (
                    <Icon name="check" size={20} color={COLORS.accent} />
                  )}
                </TouchableOpacity>
              ))}
            </View>
          </View>
        </TouchableOpacity>
      </Modal>
    </View>
  );
}
