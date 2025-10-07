import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';

export function getThemeToggleStyles(isDark: boolean) {
  const COLORS = getThemeColors(isDark);
  return StyleSheet.create({
    container: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.sm,
      borderRadius: 20,
      borderWidth: 1.5,
      borderColor: COLORS.border,
      backgroundColor: isDark ?'#191F28' : COLORS.surface,
      shadowColor: COLORS.black,
      shadowOpacity: 0.15,
      shadowRadius: 6,
      shadowOffset: { width: 0, height: 2 },
      elevation: 4,
    },
    icon: {
      fontSize: 20,
    },
    text: {
      fontSize: 16,
      fontWeight: '700',
      color: isDark ? COLORS.white : COLORS.black,
    },
  });
}
