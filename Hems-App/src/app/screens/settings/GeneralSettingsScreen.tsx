import React from 'react';
import { View, Text, ScrollView, TouchableOpacity } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft, Thermometer, Ruler, Clock } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { useSettingsStore, TemperatureUnit, MeasurementSystem, TimeFormat, DateFormat } from '../../../store/settingsStore';
import { getThemeColors } from '../../../theme/constants';
import { useI18n } from '../../../hooks/useI18n';
import { makeGeneralSettingsScreenStyles } from './GeneralSettingsScreen.styles';

interface SegmentedButtonProps {
  options: { label: string; value: string }[];
  selectedValue: string;
  onSelect: (value: string) => void;
  colors: Record<string, string>;
  isDark: boolean;
}

const SegmentedButton: React.FC<SegmentedButtonProps> = ({
  options,
  selectedValue,
  onSelect,
  colors,
  isDark,
}) => {
  return (
    <View
      style={{
        flexDirection: 'row',
        backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.06)',
        borderRadius: 8,
        padding: 3,
      }}
    >
      {options.map((option) => {
        const isSelected = option.value === selectedValue;
        return (
          <TouchableOpacity
            key={option.value}
            onPress={() => onSelect(option.value)}
            style={{
              paddingVertical: 8,
              paddingHorizontal: 12,
              borderRadius: 6,
              backgroundColor: isSelected ? colors.accent : 'transparent',
              minWidth: 70,
              alignItems: 'center',
            }}
          >
            <Text
              style={{
                fontSize: 13,
                fontWeight: isSelected ? '600' : '500',
                color: isSelected ? '#fff' : colors.textSoft,
              }}
            >
              {option.label}
            </Text>
          </TouchableOpacity>
        );
      })}
    </View>
  );
};

export default function GeneralSettingsScreen({ navigation }: any) {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeGeneralSettingsScreenStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();

  const {
    temperatureUnit,
    measurementSystem,
    timeFormat,
    dateFormat,
    setTemperatureUnit,
    setMeasurementSystem,
    setTimeFormat,
    setDateFormat,
  } = useSettingsStore();

  return (
    <View style={[styles.container, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]}>{t('settings.generalSettings')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 24 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Temperature Unit */}
        <View style={[styles.settingCard, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <View style={styles.settingRow}>
            <View style={styles.settingLabelRow}>
              <Thermometer size={20} color={COLORS.accent} style={{ marginRight: 10 }} />
              <Text style={[styles.settingLabel, { color: COLORS.text }]}>
                {t('settings.temperatureUnit')}
              </Text>
            </View>
            <SegmentedButton
              options={[
                { label: t('settings.celsius'), value: 'celsius' },
                { label: t('settings.fahrenheit'), value: 'fahrenheit' },
              ]}
              selectedValue={temperatureUnit}
              onSelect={(value) => setTemperatureUnit(value as TemperatureUnit)}
              colors={COLORS}
              isDark={isDark}
            />
          </View>
        </View>

        {/* Measurement System */}
        <View style={[styles.settingCard, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <View style={styles.settingRow}>
            <View style={styles.settingLabelRow}>
              <Ruler size={20} color={COLORS.accent} style={{ marginRight: 10 }} />
              <Text style={[styles.settingLabel, { color: COLORS.text }]}>
                {t('settings.metricSystem')}
              </Text>
            </View>
            <SegmentedButton
              options={[
                { label: t('settings.metric'), value: 'metric' },
                { label: t('settings.imperial'), value: 'imperial' },
              ]}
              selectedValue={measurementSystem}
              onSelect={(value) => setMeasurementSystem(value as MeasurementSystem)}
              colors={COLORS}
              isDark={isDark}
            />
          </View>
        </View>

        {/* Time Format */}
        <View style={[styles.settingCard, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <View style={styles.settingRow}>
            <View style={styles.settingLabelRow}>
              <Clock size={20} color={COLORS.accent} style={{ marginRight: 10 }} />
              <Text style={[styles.settingLabel, { color: COLORS.text }]}>
                {t('settings.timeFormat')}
              </Text>
            </View>
            <SegmentedButton
              options={[
                { label: t('settings.24hour'), value: '24h' },
                { label: t('settings.12hour'), value: '12h' },
              ]}
              selectedValue={timeFormat}
              onSelect={(value) => setTimeFormat(value as TimeFormat)}
              colors={COLORS}
              isDark={isDark}
            />
          </View>
        </View>

        {/* Date Format */}
        <View style={[styles.settingCard, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
          <View style={styles.settingRow}>
            <View style={styles.settingLabelRow}>
              <Clock size={20} color={COLORS.accent} style={{ marginRight: 10 }} />
              <Text style={[styles.settingLabel, { color: COLORS.text }]}>
                {t('settings.dateFormat')}
              </Text>
            </View>
            <SegmentedButton
              options={[
                { label: 'DD/MM', value: 'DD/MM/YYYY' },
                { label: 'MM/DD', value: 'MM/DD/YYYY' },
                { label: 'YYYY-MM', value: 'YYYY-MM-DD' },
              ]}
              selectedValue={dateFormat}
              onSelect={(value) => setDateFormat(value as DateFormat)}
              colors={COLORS}
              isDark={isDark}
            />
          </View>
        </View>

      </ScrollView>
    </View>
  );
}
