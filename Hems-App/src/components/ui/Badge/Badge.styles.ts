import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getBadgeStyles() {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return StyleSheet.create({
    badge: {
      paddingHorizontal: SPACING.sm,
      paddingVertical: SPACING.xs / 2,
      borderRadius: 12,
      alignSelf: 'flex-start',
    },
    text: {
      fontSize: 12,
      fontWeight: '600',
      color: COLORS.white,
    },
    success: {
      backgroundColor: '#10b981',
    },
    warning: {
      backgroundColor: '#f59e0b',
    },
    error: {
      backgroundColor: '#ef4444',
    },
    info: {
      backgroundColor: COLORS.accent,
    },
  });
}
