/**
 * SettingToggle Component
 * On/Off switch ayarı
 */

import React from 'react';
import { View, Text, Switch, StyleSheet } from 'react-native';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { OnOffStatus } from '../../api/types/inverterModes.types';

interface SettingToggleProps {
  label: string;
  value: OnOffStatus;
  onChange: (value: OnOffStatus) => void;
  description?: string;
  disabled?: boolean;
}

const SettingToggle: React.FC<SettingToggleProps> = ({
  label,
  value,
  onChange,
  description,
  disabled = false,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const isOn = value === 'on';

  const handleChange = (newValue: boolean) => {
    onChange(newValue ? 'on' : 'off');
  };

  return (
    <View
      style={[
        styles.container,
        { borderBottomColor: isDark ? COLORS.border : COLORS.gray200 },
      ]}
    >
      <View style={styles.labelContainer}>
        <Text
          style={[
            styles.label,
            { color: disabled ? COLORS.textSoft : COLORS.text },
          ]}
        >
          {label}
        </Text>
        {description && (
          <Text style={[styles.description, { color: COLORS.textSoft }]}>
            {description}
          </Text>
        )}
      </View>
      <Switch
        value={isOn}
        onValueChange={handleChange}
        disabled={disabled}
        thumbColor={isOn ? COLORS.accent : isDark ? '#555' : '#ccc'}
        trackColor={{
          false: isDark ? '#333' : '#ccc',
          true: `${COLORS.accent}66`,
        }}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  labelContainer: {
    flex: 1,
    marginRight: 12,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
  },
  description: {
    fontSize: 12,
    marginTop: 2,
  },
});

export default SettingToggle;
