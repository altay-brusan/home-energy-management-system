/**
 * SettingSection Component
 * Accordion tarzı ayar kategorisi başlığı
 */

import React, { useState } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, LayoutAnimation } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

interface SettingSectionProps {
  title: string;
  icon?: string;
  children: React.ReactNode;
  defaultExpanded?: boolean;
  disabled?: boolean;
}

const SettingSection: React.FC<SettingSectionProps> = ({
  title,
  icon,
  children,
  defaultExpanded = false,
  disabled = false,
}) => {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const [expanded, setExpanded] = useState(defaultExpanded);

  const toggleExpanded = () => {
    if (disabled) return;
    LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
    setExpanded(!expanded);
  };

  return (
    <View style={[styles.container, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}>
      <TouchableOpacity
        onPress={toggleExpanded}
        activeOpacity={disabled ? 1 : 0.7}
        style={styles.header}
      >
        <View style={styles.titleRow}>
          {icon && (
            <FontAwesome
              name={icon as any}
              size={18}
              color={COLORS.accent}
              style={styles.icon}
            />
          )}
          <Text style={[styles.title, { color: COLORS.text }]}>{title}</Text>
        </View>
        <FontAwesome
          name={expanded ? 'chevron-up' : 'chevron-down'}
          size={14}
          color={COLORS.textSoft}
        />
      </TouchableOpacity>

      {expanded && <View style={styles.content}>{children}</View>}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    borderRadius: 12,
    marginBottom: 12,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  icon: {
    marginRight: 12,
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
  },
  content: {
    paddingHorizontal: 16,
    paddingBottom: 16,
  },
});

export default SettingSection;
