import React from 'react';
import { Pressable, Text, View } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeToggleStyles } from './ThemeToggle.styles';
import { useTranslation } from "react-i18next";

interface ThemeToggleProps {
  style?: any;
}

export default function ThemeToggle({ style }: ThemeToggleProps) {
  const mode = useThemeStore(s => s.mode);
  const toggle = useThemeStore(s => s.toggle);
  const isDark = mode === 'dark';
  const styles = getThemeToggleStyles(isDark);
   const { t } = useTranslation();

  return (
    <Pressable onPress={toggle} style={style}>
      <View style={styles.container}>
        <Text style={styles.icon}>{isDark ? '🌙' : '☀️'}</Text>
      <Text style={styles.text}>
  {isDark ? t('themeToggle.dark') : t('themeToggle.light')}
</Text>

      </View>
    </Pressable>
  );
}
