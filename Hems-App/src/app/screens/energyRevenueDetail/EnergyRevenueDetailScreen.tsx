import React, { useState, useMemo, useEffect } from 'react';
import { View, Text, TouchableOpacity, Dimensions, Modal } from 'react-native';
import { ArrowLeft, ChevronDown } from 'lucide-react-native';
import { Calendar, LocaleConfig } from 'react-native-calendars';
import { useTranslation } from 'react-i18next';
import { useNavigation } from '@react-navigation/native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { useI18nStore } from '../../../store/i18nStore';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { getEnergyRevenueDetailStyles } from './EnergyRevenueDetailScreen.styles';
import Icon from 'react-native-vector-icons/MaterialIcons';
import EnergyRevenueChart from './components/EnergyRevenueChart';

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

type Range = 'Week' | 'Month' | 'Year';

export default function EnergyRevenueDetailScreen() {
  const { t } = useTranslation();
  const navigation = useNavigation();
  const insets = useSafeAreaInsets();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getEnergyRevenueDetailStyles(COLORS, isDark), [COLORS, isDark]);
  const language = useI18nStore(s => s.language);

  // Set calendar locale based on current language
  useEffect(() => {
    LocaleConfig.defaultLocale = language;
  }, [language]);

  const [selectedDate, setSelectedDate] = useState(new Date());
  const [selectedRange, setSelectedRange] = useState<Range>('Week');
  const [isDatePickerVisible, setIsDatePickerVisible] = useState(false);
  const [isRangePickerVisible, setIsRangePickerVisible] = useState(false);

  const screenHeight = Dimensions.get('window').height;

  // Mock data - API entegrasyonu için hazır
  // Şimdilik chart component kendi mock data'sını üretiyor
  // API hazır olduğunda buraya eklenecek

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
      case 'Week':
        return t('energyRevenueDetail.dateFormat.week', {
          weekNumber: getWeekNumber(date),
          year: date.getFullYear()
        });
      case 'Month':
        return t('energyRevenueDetail.dateFormat.month', {
          monthName: t(`energyRevenueDetail.months.${monthKeys[date.getMonth()]}`),
          year: date.getFullYear()
        });
      case 'Year':
        return t('energyRevenueDetail.dateFormat.year', {
          year: date.getFullYear()
        });
      default:
        return `${date.getFullYear()}/${String(date.getMonth() + 1).padStart(2, '0')}/${String(date.getDate()).padStart(2, '0')}`;
    }
  };

  const canNavigateNext = () => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const newDate = new Date(selectedDate);

    if (selectedRange === 'Week') {
      newDate.setDate(newDate.getDate() + 7);
      // Haftanın başlangıcı bugünden sonraysa izin verme
      return newDate.getTime() <= today.getTime();
    } else if (selectedRange === 'Month') {
      newDate.setMonth(newDate.getMonth() + 1);
      // Ayın başlangıcı bugünden sonraysa izin verme
      return newDate.getTime() <= today.getTime();
    } else if (selectedRange === 'Year') {
      newDate.setFullYear(newDate.getFullYear() + 1);
      // Yılın başlangıcı bugünden sonraysa izin verme
      return newDate.getFullYear() <= today.getFullYear();
    }

    return false;
  };

  const navigateDate = (direction: 'prev' | 'next') => {
    if (direction === 'next' && !canNavigateNext()) {
      return;
    }

    const newDate = new Date(selectedDate);
    if (selectedRange === 'Week') {
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
    const diff = date.getDate() - day + (day === 0 ? -6 : 1);
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

    if (selectedRange === 'Week') {
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

  const chartHeight = screenHeight - insets.top - 60 - 60 - insets.bottom - SPACING.md - 40;

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
          <Text style={styles.title}>{t('energyRevenue.title', 'Energy Revenue')}</Text>
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
          <Text style={styles.rangeText}>{t(`energyRevenueDetail.ranges.${selectedRange.toLowerCase()}`)}</Text>
          <ChevronDown size={16} color={COLORS.textSoft} strokeWidth={2.5} />
        </TouchableOpacity>
      </View>

      {/* Chart */}
      <View style={{ flex: 1 }}>
        <EnergyRevenueChart
          data={[]}
          height={chartHeight}
          range={selectedRange}
          cardBgColor={COLORS.cardBg}
          textColor={COLORS.textSoft}
        />
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
              {(['Week', 'Month', 'Year'] as const).map((range) => (
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
                    {t(`energyRevenueDetail.ranges.${range.toLowerCase()}`)}
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
