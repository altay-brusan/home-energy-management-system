import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';

export function getThemeToggleStyles(isDark: boolean) {
  const COLORS = getThemeColors(isDark);
  return StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 6,
      paddingHorizontal: 12,
      paddingVertical: 8,
      borderRadius: 12,
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)',
    },
    icon: {
      fontSize: 18,
    },
    text: {
      fontSize: 14,
      fontWeight: '500',
      color: COLORS.textSoft,
    },
  });
}
