/**
 * TimeOfUseScreen
 * Time of Use zaman programlama ekranı
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  SafeAreaView,
  Alert,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Save, Clock } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useInverterModeStore } from '../../../store/inverterModeStore';
import {
  SettingToggle,
  TimelineBar,
  PeriodCard,
  DaySelector,
} from '../../../components/inverterModes';
import { TimeOfUsePeriod, TimeOfUsePeriodId } from '../../../api/types/inverterModes.types';

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
    customConfig,
    updateTimeOfUseSettings,
    updateTimeOfUsePeriod,
  } = useInverterModeStore();

  const { timeOfUse } = customConfig;

  const handleUpdatePeriod = (periodId: TimeOfUsePeriodId, updates: Partial<TimeOfUsePeriod>) => {
    updateTimeOfUsePeriod(periodId, updates);
  };

  const handleSave = () => {
    // Validate periods don't overlap
    const sortedPeriods = [...timeOfUse.periods].sort((a, b) => {
      const aStart = parseInt(a.startTime.replace(':', ''));
      const bStart = parseInt(b.startTime.replace(':', ''));
      return aStart - bStart;
    });

    for (let i = 0; i < sortedPeriods.length - 1; i++) {
      const currentEnd = sortedPeriods[i].endTime;
      const nextStart = sortedPeriods[i + 1].startTime;

      const currentEndMinutes =
        parseInt(currentEnd.split(':')[0]) * 60 + parseInt(currentEnd.split(':')[1]);
      const nextStartMinutes =
        parseInt(nextStart.split(':')[0]) * 60 + parseInt(nextStart.split(':')[1]);

      if (currentEndMinutes > nextStartMinutes) {
        Alert.alert(
          t('timeOfUse.periodOverlap'),
          t('timeOfUse.overlapError', { period1: i + 1, period2: i + 2 })
        );
        return;
      }
    }

    Alert.alert(t('inverterModes.success'), t('timeOfUse.saved'), [
      { text: t('inverterModes.ok'), onPress: () => navigation.goBack() },
    ]);
  };

  return (
    <SafeAreaView
      style={[
        styles.container,
        { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 },
      ]}
    >
      {/* Header */}
      <View style={styles.header}>
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
          { paddingBottom: insets.bottom + 100 },
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
            value={timeOfUse.enabled}
            onChange={(value) => updateTimeOfUseSettings({ enabled: value })}
            description={t('timeOfUse.enableDescription')}
          />
        </View>

        {timeOfUse.enabled === 'on' && (
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
                activeDays={timeOfUse.activeDays}
                onChange={(days) => updateTimeOfUseSettings({ activeDays: days })}
              />
            </View>

            {/* Timeline Visualization */}
            {timeOfUse.periods.length > 0 && (
              <View
                style={[
                  styles.card,
                  { backgroundColor: isDark ? COLORS.surface : COLORS.white },
                ]}
              >
                <Text style={[styles.cardTitle, { color: COLORS.text }]}>
                  {t('timeOfUse.24HourView')}
                </Text>
                <TimelineBar periods={timeOfUse.periods} />
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
            {timeOfUse.periods.map((period, index) => (
              <PeriodCard
                key={period.id}
                period={period}
                index={index}
                onUpdate={(updates) => handleUpdatePeriod(period.id, updates)}
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
                • {t('timeOfUse.tips.targetSOC')}
              </Text>
            </View>
          </>
        )}
      </ScrollView>

      {/* Save Button */}
      {timeOfUse.enabled === 'on' && (
        <View
          style={[
            styles.footer,
            {
              backgroundColor: isDark ? COLORS.surface : COLORS.white,
              paddingBottom: insets.bottom + 16,
            },
          ]}
        >
          <TouchableOpacity
            onPress={handleSave}
            style={[styles.saveButton, { backgroundColor: COLORS.accent }]}
          >
            <Save size={20} color="#fff" />
            <Text style={styles.saveButtonText}>{t('timeOfUse.save')}</Text>
          </TouchableOpacity>
        </View>
      )}
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
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
  footer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    paddingHorizontal: 16,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: 'rgba(0,0,0,0.1)',
  },
  saveButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 16,
    borderRadius: 12,
    gap: 8,
  },
  saveButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default TimeOfUseScreen;
