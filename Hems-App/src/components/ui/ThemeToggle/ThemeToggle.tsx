import React from 'react';
import { Pressable, Text, View } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeToggleStyles } from './ThemeToggle.styles';

interface ThemeToggleProps {
  style?: any;
}

export default function ThemeToggle({ style }: ThemeToggleProps) {
  const mode = useThemeStore(s => s.mode);
  const toggle = useThemeStore(s => s.toggle);
  const isDark = mode === 'dark';
  const styles = getThemeToggleStyles(isDark);

  return (
    <Pressable onPress={toggle} style={style}>
      <View style={styles.container}>
        <Text style={styles.icon}>{isDark ? '🌙' : '☀️'}</Text>
        <Text style={styles.text}>{isDark ? 'Dark' : 'Light'}</Text>
      </View>
    </Pressable>
  );
}
