import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { LayoutChangeEvent, Pressable, ScrollView, Text, View, RefreshControl, ActivityIndicator, Modal } from 'react-native';
import { ArrowLeft, Sun, ChevronLeft, ChevronRight, ChevronDown } from 'lucide-react-native';
import { Calendar, LocaleConfig } from 'react-native-calendars';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getPVStyles } from './PVPanelDetailScreen.styles';
import { SolarLineChart } from './ui/SolarLineChart';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { useI18n } from '../../../hooks/useI18n';
import { useSelectedSiteDevice } from '../../../hooks/useSelectedSiteDevice';
import { useI18nStore } from '../../../store/i18nStore';
import { dashboardService } from '../../../api/services/dashboardService';

type Range = 'Day' | 'Week' | 'Month' | 'Year';

// Configure calendar locales
LocaleConfig.locales['tr'] = {
  monthNames: ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'],
  monthNamesShort: ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'],
  dayNames: ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'],
  dayNamesShort: ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt'],
};

LocaleConfig.locales['nl'] = {
  monthNames: ['Januari', 'Februari', 'Maart', 'April', 'Mei', 'Juni', 'Juli', 'Augustus', 'September', 'Oktober', 'November', 'December'],
  monthNamesShort: ['Jan', 'Feb', 'Mrt', 'Apr', 'Mei', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dec'],
  dayNames: ['Zondag', 'Maandag', 'Dinsdag', 'Woensdag', 'Donderdag', 'Vrijdag', 'Zaterdag'],
  dayNamesShort: ['Zo', 'Ma', 'Di', 'Wo', 'Do', 'Vr', 'Za'],
};

LocaleConfig.locales['en'] = {
  monthNames: ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'],
  monthNamesShort: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
  dayNames: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
  dayNamesShort: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
};

const SOLAR_COLOR = '#FACC15';

// Helper to get week number
const getWeekNumber = (date: Date): number => {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  const dayNum = d.getUTCDay() || 7;
  d.setUTCDate(d.getUTCDate() + 4 - dayNum);
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
};

// Helper to format date for display
const formatDateDisplay = (date: Date, range: Range, t: any, language: string): string => {
  const months = language === 'tr'
    ? ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık']
    : language === 'nl'
    ? ['Januari', 'Februari', 'Maart', 'April', 'Mei', 'Juni', 'Juli', 'Augustus', 'September', 'Oktober', 'November', 'December']
    : ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  switch (range) {
    case 'Day':
      return `${date.getDate()} ${months[date.getMonth()]} ${date.getFullYear()}`;
    case 'Week':
      const weekNum = getWeekNumber(date);
      return t('powerMetrics24H.dateFormat.week', { weekNumber: weekNum, year: date.getFullYear() });
    case 'Month':
      return `${months[date.getMonth()]} ${date.getFullYear()}`;
    case 'Year':
      return `${date.getFullYear()}`;
    default:
      return '';
  }
};

export default function PVPanelDetailScreen() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getPVStyles(COLORS), [COLORS]);
  const { t } = useI18n();
  const language = useI18nStore(s => s.language);
  const insets = useSafeAreaInsets();

  const { device, hasDevice } = useSelectedSiteDevice();

  // State
  const [range, setRange] = useState<Range>('Day');
  const [selectedDate, setSelectedDate] = useState(new Date());
  const [size, setSize] = useState({ w: 0, h: 0 });
  const [refreshing, setRefreshing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [isDatePickerVisible, setIsDatePickerVisible] = useState(false);

  // Data state
  const [chartData, setChartData] = useState<{ x: string; y: number }[]>([]);
  const [statistics, setStatistics] = useState({ peak: 0, average: 0, total: 0 });

  // Set calendar locale
  useEffect(() => {
    LocaleConfig.defaultLocale = language;
  }, [language]);

  // Load solar data from API
  const loadSolarData = useCallback(async (isInitialLoad = false) => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }

    if (isInitialLoad) {
      setLoading(true);
    } else {
      setRefreshing(true);
    }

    try {
      let data: { x: string; y: number }[] = [];
      let peak = 0;
      let average = 0;
      let total = 0;

      const dayLabels = language === 'tr'
        ? ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz']
        : language === 'nl'
        ? ['Ma', 'Di', 'Wo', 'Do', 'Vr', 'Za', 'Zo']
        : ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

      const monthLabels = language === 'tr'
        ? ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara']
        : language === 'nl'
        ? ['Jan', 'Feb', 'Mrt', 'Apr', 'Mei', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dec']
        : ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

      switch (range) {
        case 'Day': {
          const dateString = selectedDate.toISOString().split('T')[0];
          const response = await dashboardService.getPowerAndEnergyMetrics(device.id, 'day', dateString);

          if (response.data && response.data.length > 0) {
            // Extract solar power data (convert W to kW)
            data = response.data.map((point: any) => {
              const timestamp = point.timestamp;
              // Handle both "HH:mm" format and full ISO date format
              let label: string;
              if (timestamp.includes('T')) {
                const date = new Date(timestamp);
                label = `${date.getHours().toString().padStart(2, '0')}:00`;
              } else {
                label = timestamp;
              }
              return {
                x: label,
                y: (point.solarPower || 0) / 1000, // W to kW
              };
            });

            // Calculate statistics
            const solarPowers = response.data.map((p: any) => p.solarPower || 0);
            peak = Math.max(...solarPowers) / 1000; // kW
            average = solarPowers.reduce((a: number, b: number) => a + b, 0) / solarPowers.length / 1000; // kW

            // Calculate total energy (sum of solar energy if available, otherwise estimate)
            const firstPoint = response.data[0] as any;
            if (firstPoint?.solarEnergyKWh !== undefined) {
              total = response.data.reduce((sum: number, p: any) => sum + (p.solarEnergyKWh || 0), 0);
            } else {
              // Estimate: average power * hours (assuming 15-min intervals)
              const intervalHours = 0.25;
              total = solarPowers.reduce((sum: number, p: number) => sum + (p / 1000) * intervalHours, 0);
            }
          }
          break;
        }

        case 'Week': {
          const year = selectedDate.getFullYear().toString();
          const weekNumber = getWeekNumber(selectedDate);
          const response = await dashboardService.getPowerAndEnergyMetrics(device.id, 'week', year, weekNumber);

          if (response.data && response.data.length > 0) {
            // Create a map by day of week
            const dataByDay = new Map<number, number>();
            response.data.forEach((point: any) => {
              const date = new Date(point.timestamp);
              const dayOfWeek = date.getDay(); // 0 = Sunday
              dataByDay.set(dayOfWeek, point.solarEnergyKWh || 0);
            });

            // Create 7 data points (Monday to Sunday)
            data = [];
            for (let i = 0; i < 7; i++) {
              const dayOfWeek = i === 6 ? 0 : i + 1; // 0=Mon, 1=Tue, ..., 6=Sun
              const energy = dataByDay.get(dayOfWeek) || 0;
              data.push({
                x: dayLabels[i],
                y: energy,
              });
            }

            // Calculate statistics
            const energies = data.map(d => d.y);
            peak = Math.max(...energies);
            total = energies.reduce((a, b) => a + b, 0);
            average = total / energies.filter(e => e > 0).length || 0;
          } else {
            data = dayLabels.map(label => ({ x: label, y: 0 }));
          }
          break;
        }

        case 'Month': {
          const year = selectedDate.getFullYear().toString();
          const month = selectedDate.getMonth() + 1;
          const response = await dashboardService.getPowerAndEnergyMetrics(device.id, 'month', year, month);

          if (response.data && response.data.length > 0) {
            // Create a map by day of month
            const dataByDay = new Map<number, number>();
            response.data.forEach((point: any) => {
              const date = new Date(point.timestamp);
              dataByDay.set(date.getDate(), point.solarEnergyKWh || 0);
            });

            // Get days in month
            const daysInMonth = new Date(selectedDate.getFullYear(), selectedDate.getMonth() + 1, 0).getDate();
            data = [];
            for (let i = 1; i <= daysInMonth; i++) {
              data.push({
                x: `${i}`,
                y: dataByDay.get(i) || 0,
              });
            }

            // Calculate statistics
            const energies = data.map(d => d.y);
            peak = Math.max(...energies);
            total = energies.reduce((a, b) => a + b, 0);
            average = total / energies.filter(e => e > 0).length || 0;
          } else {
            const daysInMonth = new Date(selectedDate.getFullYear(), selectedDate.getMonth() + 1, 0).getDate();
            data = Array.from({ length: daysInMonth }, (_, i) => ({ x: `${i + 1}`, y: 0 }));
          }
          break;
        }

        case 'Year': {
          const year = selectedDate.getFullYear().toString();
          const response = await dashboardService.getPowerAndEnergyMetrics(device.id, 'year', year);

          if (response.data && response.data.length > 0) {
            // Create a map by month
            const dataByMonth = new Map<number, number>();
            response.data.forEach((point: any) => {
              const date = new Date(point.timestamp);
              dataByMonth.set(date.getMonth(), point.solarEnergyKWh || 0);
            });

            // Create 12 month data points
            data = monthLabels.map((label, i) => ({
              x: label,
              y: dataByMonth.get(i) || 0,
            }));

            // Calculate statistics
            const energies = data.map(d => d.y);
            peak = Math.max(...energies);
            total = energies.reduce((a, b) => a + b, 0);
            average = total / energies.filter(e => e > 0).length || 0;
          } else {
            data = monthLabels.map(label => ({ x: label, y: 0 }));
          }
          break;
        }
      }

      setChartData(data);
      setStatistics({ peak, average, total });

    } catch (error) {
      console.error('Failed to load solar data:', error);
      setChartData([]);
      setStatistics({ peak: 0, average: 0, total: 0 });
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [device?.id, hasDevice, range, selectedDate, language]);

  // Load data on mount and when dependencies change
  useEffect(() => {
    loadSolarData(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    loadSolarData(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [range, selectedDate]);

  const onRefresh = () => {
    loadSolarData(false);
  };

  const onLayout = (e: LayoutChangeEvent) => setSize({ w: e.nativeEvent.layout.width, h: e.nativeEvent.layout.height });

  // Date navigation
  const canNavigateNext = useMemo(() => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const selected = new Date(selectedDate);
    selected.setHours(0, 0, 0, 0);

    switch (range) {
      case 'Day':
        return selected < today;
      case 'Week':
        return getWeekNumber(selected) < getWeekNumber(today) || selected.getFullYear() < today.getFullYear();
      case 'Month':
        return selected.getMonth() < today.getMonth() || selected.getFullYear() < today.getFullYear();
      case 'Year':
        return selected.getFullYear() < today.getFullYear();
      default:
        return false;
    }
  }, [selectedDate, range]);

  const navigateDate = (direction: 'prev' | 'next') => {
    const newDate = new Date(selectedDate);

    switch (range) {
      case 'Day':
        newDate.setDate(newDate.getDate() + (direction === 'prev' ? -1 : 1));
        break;
      case 'Week':
        newDate.setDate(newDate.getDate() + (direction === 'prev' ? -7 : 7));
        break;
      case 'Month':
        newDate.setMonth(newDate.getMonth() + (direction === 'prev' ? -1 : 1));
        break;
      case 'Year':
        newDate.setFullYear(newDate.getFullYear() + (direction === 'prev' ? -1 : 1));
        break;
    }

    // Don't allow future dates
    const today = new Date();
    if (newDate <= today) {
      setSelectedDate(newDate);
    }
  };

  const handleDateSelect = (day: any) => {
    const newDate = new Date(day.dateString);
    setSelectedDate(newDate);
    setIsDatePickerVisible(false);
  };

  // Get unit for display
  const getUnit = () => range === 'Day' ? 'kW' : 'kWh';

  return (
    <View style={[styles.screen, { paddingTop: insets.top }]}>
      {/* Header */}
      <View style={styles.header}>
        <Pressable style={styles.backButton} onPress={() => navigation.goBack()}>
          <ArrowLeft size={24} color={COLORS.textSoft} strokeWidth={2} />
        </Pressable>
        <Text style={styles.headerTitle}>{t('deviceDetail.pvPanel.title')}</Text>
        <View style={styles.headerPlaceholder} />
      </View>

      <ScrollView
        contentContainerStyle={styles.container}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.accent} />
        }
      >
        {/* Summary Card */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <View style={[styles.cardIconContainer, { backgroundColor: `${SOLAR_COLOR}20` }]}>
              <Sun size={22} color={SOLAR_COLOR} strokeWidth={2} />
            </View>
            <View>
              <Text style={styles.cardTitle}>{t('deviceDetail.pvPanel.title')}</Text>
              <Text style={styles.cardSubtitle}>{t('deviceDetail.pvPanel.subtitle')}</Text>
            </View>
          </View>

          {/* Metrics Row */}
          <View style={styles.metricsRow}>
            <View style={styles.metricItem}>
              <Text style={styles.metricValue}>
                {statistics.peak.toFixed(1)}
                <Text style={styles.metricUnit}> {getUnit()}</Text>
              </Text>
              <Text style={styles.metricLabel}>{t('dashboardGraph.peak')}</Text>
            </View>
            <View style={styles.metricDivider} />
            <View style={styles.metricItem}>
              <Text style={styles.metricValue}>
                {statistics.average.toFixed(1)}
                <Text style={styles.metricUnit}> {getUnit()}</Text>
              </Text>
              <Text style={styles.metricLabel}>{t('dashboardGraph.average')}</Text>
            </View>
            <View style={styles.metricDivider} />
            <View style={styles.metricItem}>
              <Text style={styles.metricValue}>
                {statistics.total.toFixed(1)}
                <Text style={styles.metricUnit}> kWh</Text>
              </Text>
              <Text style={styles.metricLabel}>{t('dashboardGraph.total')}</Text>
            </View>
          </View>
        </View>

        {/* Chart Card */}
        <View style={styles.chartCard}>
          {/* Date Navigation */}
          <View style={styles.dateNavigation}>
            <Pressable
              style={styles.dateNavButton}
              onPress={() => navigateDate('prev')}
            >
              <ChevronLeft size={20} color={COLORS.textSoft} />
            </Pressable>

            <Pressable
              style={styles.dateSelector}
              onPress={() => setIsDatePickerVisible(true)}
            >
              <Text style={styles.dateText}>
                {formatDateDisplay(selectedDate, range, t, language)}
              </Text>
              <ChevronDown size={16} color={COLORS.textSoft} />
            </Pressable>

            <Pressable
              style={[styles.dateNavButton, !canNavigateNext && styles.dateNavButtonDisabled]}
              onPress={() => canNavigateNext && navigateDate('next')}
              disabled={!canNavigateNext}
            >
              <ChevronRight size={20} color={canNavigateNext ? COLORS.textSoft : COLORS.border} />
            </Pressable>
          </View>

          {/* Chart */}
          <View style={styles.chart} onLayout={onLayout}>
            {loading ? (
              <View style={styles.emptyContainer}>
                <ActivityIndicator size="large" color={SOLAR_COLOR} />
              </View>
            ) : size.w > 0 && size.h > 0 && chartData.length > 0 ? (
              <SolarLineChart
                data={chartData}
                range={range}
                COLORS={COLORS}
                width={size.w}
                height={size.h}
              />
            ) : (
              <View style={styles.emptyContainer}>
                <Text style={styles.emptyText}>{t('common.noData')}</Text>
              </View>
            )}
          </View>

          {/* Range Tabs */}
          <View style={styles.rangeTabs}>
            {(['Day', 'Week', 'Month', 'Year'] as const).map((r) => {
              const isActive = range === r;
              return (
                <Pressable
                  key={r}
                  style={[
                    styles.rangeTab,
                    isActive && styles.rangeTabActive,
                  ]}
                  onPress={() => setRange(r)}
                >
                  <Text style={[
                    styles.rangeTabText,
                    isActive && styles.rangeTabTextActive,
                  ]}>
                    {t(`dashboardGraph.range.${r.toLowerCase()}`)}
                  </Text>
                </Pressable>
              );
            })}
          </View>
        </View>
      </ScrollView>

      {/* Date Picker Modal */}
      <Modal
        visible={isDatePickerVisible}
        transparent
        animationType="fade"
        onRequestClose={() => setIsDatePickerVisible(false)}
      >
        <Pressable
          style={styles.modalOverlay}
          onPress={() => setIsDatePickerVisible(false)}
        >
          <View style={[styles.calendarContainer, { backgroundColor: COLORS.surface }]}>
            <Calendar
              current={selectedDate.toISOString().split('T')[0]}
              onDayPress={handleDateSelect}
              maxDate={new Date().toISOString().split('T')[0]}
              theme={{
                backgroundColor: COLORS.surface,
                calendarBackground: COLORS.surface,
                textSectionTitleColor: COLORS.textSoft,
                selectedDayBackgroundColor: SOLAR_COLOR,
                selectedDayTextColor: '#000',
                todayTextColor: SOLAR_COLOR,
                dayTextColor: COLORS.text,
                textDisabledColor: COLORS.border,
                arrowColor: SOLAR_COLOR,
                monthTextColor: COLORS.text,
              }}
            />
          </View>
        </Pressable>
      </Modal>
    </View>
  );
}
