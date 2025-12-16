/**
 * SettingSlider Component
 * Slider ayarı (+/- butonları ile)
 */

import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity, TextInput } from 'react-native';
import { Minus, Plus } from 'lucide-react-native';
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

  const handleDecrease = () => {
    if (disabled) return;
    const newValue = Math.max(min, value - step);
    onChange(newValue);
  };

  const handleIncrease = () => {
    if (disabled) return;
    const newValue = Math.min(max, value + step);
    onChange(newValue);
  };

  const handleTextChange = (text: string) => {
    if (disabled) return;
    const numValue = parseInt(text, 10);
    if (!isNaN(numValue)) {
      const clampedValue = Math.min(max, Math.max(min, numValue));
      onChange(clampedValue);
    }
  };

  // Calculate progress percentage for visual bar
  const progress = ((value - min) / (max - min)) * 100;

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

      {/* Progress Bar */}
      <View style={[styles.progressBarContainer, { backgroundColor: isDark ? '#333' : '#E0E0E0' }]}>
        <View
          style={[
            styles.progressBar,
            {
              width: `${progress}%`,
              backgroundColor: disabled ? COLORS.textSoft : COLORS.accent,
            },
          ]}
        />
      </View>

      {/* Controls Row */}
      <View style={styles.controlsRow}>
        <TouchableOpacity
          onPress={handleDecrease}
          disabled={disabled || value <= min}
          style={[
            styles.button,
            {
              backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
              opacity: disabled || value <= min ? 0.4 : 1,
            },
          ]}
        >
          <Minus size={18} color={COLORS.text} />
        </TouchableOpacity>

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

        <TouchableOpacity
          onPress={handleIncrease}
          disabled={disabled || value >= max}
          style={[
            styles.button,
            {
              backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
              opacity: disabled || value >= max ? 0.4 : 1,
            },
          ]}
        >
          <Plus size={18} color={COLORS.text} />
        </TouchableOpacity>
      </View>
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
  progressBarContainer: {
    height: 6,
    borderRadius: 3,
    marginTop: 8,
    marginBottom: 12,
    overflow: 'hidden',
  },
  progressBar: {
    height: '100%',
    borderRadius: 3,
  },
  controlsRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  button: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  rangeContainer: {
    flex: 1,
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
  },
  rangeText: {
    fontSize: 11,
  },
});

export default SettingSlider;
