import React from 'react';
import { StatusBar, StatusBarStyle, StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';

interface MainLayoutProps {
  children: React.ReactNode;
}

export default function MainLayout({ children }: MainLayoutProps) {
  const mode = useThemeStore(state => state.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const barStyle: StatusBarStyle = isDark ? 'light-content' : 'dark-content';

  return (
    <SafeAreaView
      style={[styles.container, { backgroundColor: COLORS.dark }]}
      edges={['top', 'left', 'right', 'bottom']}
    >
      <StatusBar barStyle={barStyle} backgroundColor={COLORS.dark} />
      {children}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});
