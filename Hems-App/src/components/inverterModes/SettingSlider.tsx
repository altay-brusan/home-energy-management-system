/**
 * SettingSlider Component
 * Standart slider ile ayar komponenti
 */

import React from 'react';
import { View, Text, StyleSheet, TextInput } from 'react-native';
import Slider from '@react-native-community/slider';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

interface SettingSliderProps {
  label: string;
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
  step?: number;
  unit?: string;
  description?: string;
  disabled?: boolean;
  showMinMax?: boolean;
}

const SettingSlider: React.FC<SettingSliderProps> = ({
  label,
  value,
  onChange,
  min,
  max,
  step = 1,
  unit = '',
  description,
  disabled = false,
  showMinMax = true,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const handleSliderChange = (newValue: number) => {
    if (disabled) return;
    // Round to step
    const steppedValue = Math.round(newValue / step) * step;
    const clampedValue = Math.min(max, Math.max(min, steppedValue));
    if (clampedValue !== value) {
      onChange(clampedValue);
    }
  };

  const handleTextChange = (text: string) => {
    if (disabled) return;
    const numValue = parseInt(text, 10);
    if (!isNaN(numValue)) {
      const clampedValue = Math.min(max, Math.max(min, numValue));
      const steppedValue = Math.round(clampedValue / step) * step;
      onChange(steppedValue);
    }
  };

  return (
    <View
      style={[
        styles.container,
        { borderBottomColor: isDark ? COLORS.border : COLORS.gray200 },
      ]}
    >
      <View style={styles.header}>
        <Text
          style={[
            styles.label,
            { color: disabled ? COLORS.textSoft : COLORS.text },
          ]}
        >
          {label}
        </Text>
        <View style={styles.valueContainer}>
          <TextInput
            style={[
              styles.valueInput,
              {
                color: COLORS.accent,
                backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
              },
            ]}
            value={String(value)}
            onChangeText={handleTextChange}
            keyboardType="numeric"
            editable={!disabled}
            selectTextOnFocus
          />
          <Text style={[styles.unit, { color: COLORS.accent }]}>{unit}</Text>
        </View>
      </View>

      {description && (
        <Text style={[styles.description, { color: COLORS.textSoft }]}>
          {description}
        </Text>
      )}

      {/* Standard Slider */}
      <Slider
        style={styles.slider}
        minimumValue={min}
        maximumValue={max}
        step={step}
        value={value}
        onValueChange={handleSliderChange}
        disabled={disabled}
        minimumTrackTintColor={disabled ? COLORS.textSoft : COLORS.accent}
        maximumTrackTintColor={isDark ? '#444' : '#E0E0E0'}
        thumbTintColor={disabled ? COLORS.textSoft : COLORS.accent}
      />

      {/* Min/Max Labels */}
      {showMinMax && (
        <View style={styles.rangeContainer}>
          <Text style={[styles.rangeText, { color: COLORS.textSoft }]}>
            {min}{unit}
          </Text>
          <Text style={[styles.rangeText, { color: COLORS.textSoft }]}>
            {max}{unit}
          </Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
    flex: 1,
  },
  valueContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  valueInput: {
    fontSize: 14,
    fontWeight: '700',
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 6,
    minWidth: 50,
    textAlign: 'center',
  },
  unit: {
    fontSize: 14,
    fontWeight: '600',
    marginLeft: 2,
  },
  description: {
    fontSize: 12,
    marginBottom: 8,
  },
  slider: {
    width: '100%',
    height: 40,
    marginTop: 8,
  },
  rangeContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 4,
    marginTop: -4,
  },
  rangeText: {
    fontSize: 11,
  },
});

export default SettingSlider;
