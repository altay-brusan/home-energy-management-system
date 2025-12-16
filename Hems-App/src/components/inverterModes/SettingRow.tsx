/**
 * SettingRow Component
 * Ayar satırı (label + value) - Read-only görüntüleme için
 */

import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

interface SettingRowProps {
  label: string;
  value: string | number;
  unit?: string;
  highlight?: boolean;
  status?: 'on' | 'off' | null;
}

const SettingRow: React.FC<SettingRowProps> = ({
  label,
  value,
  unit,
  highlight = false,
  status,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  const getValueColor = () => {
    if (status === 'on') return '#28A745';
    if (status === 'off') return '#DC3545';
    if (highlight) return COLORS.accent;
    return COLORS.text;
  };

  const displayValue = () => {
    if (status === 'on') return 'Açık';
    if (status === 'off') return 'Kapalı';
    return `${value}${unit ? ` ${unit}` : ''}`;
  };

  return (
    <View
      style={[
        styles.container,
        { borderBottomColor: isDark ? COLORS.border : COLORS.gray200 },
      ]}
    >
      <Text style={[styles.label, { color: COLORS.textSoft }]}>{label}</Text>
      <Text style={[styles.value, { color: getValueColor() }]}>
        {displayValue()}
      </Text>
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
  label: {
    fontSize: 14,
    flex: 1,
  },
  value: {
    fontSize: 14,
    fontWeight: '600',
    textAlign: 'right',
  },
});

export default SettingRow;
