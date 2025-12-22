/**
 * TimeOfUseScreen
 * Time of Use zaman programlama ekranı
 * API yapısına uyumlu - deviceSettingsStore kullanır
 */

import React from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Clock } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useTimeOfUseSection } from '../../../hooks/useDeviceSettings';
import {
  SettingToggle,
  TimelineBar,
  PeriodCard,
  DaySelector,
} from '../../../components/inverterModes';
import { TimeSlot } from '../../../api/types/inverterModes.types';

// Fixed 6 periods as per inverter manual
const FIXED_PERIOD_COUNT = 6;

const TimeOfUseScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const themeMode = useThemeStore((s) => s.mode);
  const isDark = themeMode === 'dark';
  const COLORS = getThemeColors(isDark);

  const {
    enabled,
    activeDays,
    timeSlots,
    toggleEnabled,
    toggleDay,
    updateTimeSlot,
    isLoading,
  } = useTimeOfUseSection();

  const handleUpdatePeriod = (index: number, updates: Partial<TimeSlot>) => {
    updateTimeSlot(index, updates);
  };

  // Loading state
  if (isLoading) {
    return (
      <View style={[styles.loadingContainer, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
        <ActivityIndicator size="large" color={COLORS.accent} />
      </View>
    );
  }

  return (
    <View
      style={[
        styles.container,
        { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 },
      ]}
    >
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={styles.backButton}
        >
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]}>
          {t('timeOfUse.title')}
        </Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[
          styles.scrollContent,
          { paddingBottom: insets.bottom + 20 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        {/* Enable Toggle */}
        <View
          style={[
            styles.card,
            { backgroundColor: isDark ? COLORS.surface : COLORS.white },
          ]}
        >
          <SettingToggle
            label={t('timeOfUse.enabled')}
            value={enabled}
            onChange={toggleEnabled}
            description={t('timeOfUse.enableDescription')}
          />
        </View>

        {enabled === 'on' && (
          <>
            {/* Day Selector */}
            <View
              style={[
                styles.card,
                { backgroundColor: isDark ? COLORS.surface : COLORS.white },
              ]}
            >
              <Text style={[styles.cardTitle, { color: COLORS.text }]}>
                {t('timeOfUse.activeDays')}
              </Text>
              <DaySelector
                activeDays={activeDays}
                onChange={(days) => {
                  Object.entries(days).forEach(([day, value]) => {
                    if (activeDays[day as keyof typeof activeDays] !== value) {
                      toggleDay(day as keyof typeof activeDays);
                    }
                  });
                }}
              />
            </View>

            {/* Timeline Visualization */}
            {timeSlots.length > 0 && (
              <View
                style={[
                  styles.card,
                  { backgroundColor: isDark ? COLORS.surface : COLORS.white },
                ]}
              >
                <Text style={[styles.cardTitle, { color: COLORS.text }]}>
                  {t('timeOfUse.24HourView')}
                </Text>
                <TimelineBar periods={timeSlots} />
              </View>
            )}

            {/* Periods Section Header */}
            <View style={styles.periodHeader}>
              <View style={styles.periodTitleRow}>
                <Clock size={18} color={COLORS.accent} />
                <Text style={[styles.sectionTitle, { color: COLORS.text }]}>
                  {t('timeOfUse.periods')}
                </Text>
              </View>
              <Text style={[styles.periodSubtitle, { color: COLORS.textSoft }]}>
                {t('timeOfUse.fixedPeriods', { count: FIXED_PERIOD_COUNT })}
              </Text>
            </View>

            {/* Period Cards - Fixed 6 Periods */}
            {timeSlots.map((period, index) => (
              <PeriodCard
                key={period.id}
                period={period}
                index={index}
                onUpdate={(updates) => handleUpdatePeriod(index, updates)}
                editable
              />
            ))}

            {/* Info Box */}
            <View
              style={[
                styles.infoBox,
                { backgroundColor: isDark ? `${COLORS.accent}15` : '#E3F2FD' },
              ]}
            >
              <Text style={[styles.infoTitle, { color: COLORS.text }]}>
                {t('timeOfUse.tips.title')}
              </Text>
              <Text style={[styles.infoText, { color: COLORS.textSoft }]}>
                • {t('timeOfUse.tips.fixedPeriods', { count: FIXED_PERIOD_COUNT })}{'\n'}
                • {t('timeOfUse.tips.fullCoverage')}{'\n'}
                • {t('timeOfUse.tips.colors')}{'\n'}
                • {t('timeOfUse.tips.minBatteryVoltage', 'Min Battery Voltage controls the discharge cutoff voltage')}
              </Text>
            </View>
          </>
        )}
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  backButton: {
    padding: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    flex: 1,
    textAlign: 'center',
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  card: {
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
  },
  cardTitle: {
    fontSize: 15,
    fontWeight: '600',
    marginBottom: 12,
  },
  periodHeader: {
    marginBottom: 12,
    marginTop: 8,
  },
  periodTitleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 4,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
  },
  periodSubtitle: {
    fontSize: 13,
    marginLeft: 26,
  },
  infoBox: {
    borderRadius: 12,
    padding: 16,
    marginTop: 8,
  },
  infoTitle: {
    fontSize: 14,
    fontWeight: '600',
    marginBottom: 8,
  },
  infoText: {
    fontSize: 13,
    lineHeight: 20,
  },
});

export default TimeOfUseScreen;
