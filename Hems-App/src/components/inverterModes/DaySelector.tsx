/**
 * DaySelector Component
 * Hafta günü seçici (çoklu seçim)
 */

import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

interface ActiveDays {
  monday: boolean;
  tuesday: boolean;
  wednesday: boolean;
  thursday: boolean;
  friday: boolean;
  saturday: boolean;
  sunday: boolean;
}

interface DaySelectorProps {
  activeDays: ActiveDays;
  onChange: (days: ActiveDays) => void;
  disabled?: boolean;
}

const DAYS: { key: keyof ActiveDays; label: string; shortLabel: string }[] = [
  { key: 'monday', label: 'Pazartesi', shortLabel: 'Pzt' },
  { key: 'tuesday', label: 'Salı', shortLabel: 'Sal' },
  { key: 'wednesday', label: 'Çarşamba', shortLabel: 'Çar' },
  { key: 'thursday', label: 'Perşembe', shortLabel: 'Per' },
  { key: 'friday', label: 'Cuma', shortLabel: 'Cum' },
  { key: 'saturday', label: 'Cumartesi', shortLabel: 'Cmt' },
  { key: 'sunday', label: 'Pazar', shortLabel: 'Paz' },
];

const DaySelector: React.FC<DaySelectorProps> = ({
  activeDays,
  onChange,
  disabled = false,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const toggleDay = (day: keyof ActiveDays) => {
    if (disabled) return;
    onChange({
      ...activeDays,
      [day]: !activeDays[day],
    });
  };

  const selectAll = () => {
    if (disabled) return;
    const allSelected = DAYS.every((d) => activeDays[d.key]);
    const newValue = !allSelected;
    onChange({
      monday: newValue,
      tuesday: newValue,
      wednesday: newValue,
      thursday: newValue,
      friday: newValue,
      saturday: newValue,
      sunday: newValue,
    });
  };

  const selectWeekdays = () => {
    if (disabled) return;
    onChange({
      monday: true,
      tuesday: true,
      wednesday: true,
      thursday: true,
      friday: true,
      saturday: false,
      sunday: false,
    });
  };

  const selectWeekend = () => {
    if (disabled) return;
    onChange({
      monday: false,
      tuesday: false,
      wednesday: false,
      thursday: false,
      friday: false,
      saturday: true,
      sunday: true,
    });
  };

  const allSelected = DAYS.every((d) => activeDays[d.key]);

  return (
    <View style={styles.container}>
      {/* Quick Select Buttons */}
      <View style={styles.quickSelectRow}>
        <TouchableOpacity
          onPress={selectAll}
          disabled={disabled}
          style={[
            styles.quickButton,
            {
              backgroundColor: allSelected ? COLORS.accent : 'transparent',
              borderColor: COLORS.accent,
            },
          ]}
        >
          <Text
            style={[
              styles.quickButtonText,
              { color: allSelected ? '#fff' : COLORS.accent },
            ]}
          >
            Tümü
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          onPress={selectWeekdays}
          disabled={disabled}
          style={[
            styles.quickButton,
            { borderColor: COLORS.border },
          ]}
        >
          <Text style={[styles.quickButtonText, { color: COLORS.textSoft }]}>
            Hafta İçi
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          onPress={selectWeekend}
          disabled={disabled}
          style={[
            styles.quickButton,
            { borderColor: COLORS.border },
          ]}
        >
          <Text style={[styles.quickButtonText, { color: COLORS.textSoft }]}>
            Hafta Sonu
          </Text>
        </TouchableOpacity>
      </View>

      {/* Day Buttons */}
      <View style={styles.daysRow}>
        {DAYS.map((day) => {
          const isActive = activeDays[day.key];
          return (
            <TouchableOpacity
              key={day.key}
              onPress={() => toggleDay(day.key)}
              disabled={disabled}
              style={[
                styles.dayButton,
                {
                  backgroundColor: isActive
                    ? COLORS.accent
                    : isDark
                    ? COLORS.border
                    : COLORS.gray100,
                },
              ]}
            >
              <Text
                style={[
                  styles.dayText,
                  { color: isActive ? '#fff' : COLORS.text },
                ]}
              >
                {day.shortLabel}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    marginVertical: 12,
  },
  quickSelectRow: {
    flexDirection: 'row',
    marginBottom: 12,
    gap: 8,
  },
  quickButton: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    borderWidth: 1,
  },
  quickButtonText: {
    fontSize: 12,
    fontWeight: '600',
  },
  daysRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  dayButton: {
    width: 44,
    height: 44,
    borderRadius: 22,
    justifyContent: 'center',
    alignItems: 'center',
  },
  dayText: {
    fontSize: 11,
    fontWeight: '600',
  },
});

export default DaySelector;
