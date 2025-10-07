import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getButtonStyles () {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return StyleSheet.create({
    button: {
      paddingVertical: SPACING.sm + 4,
      paddingHorizontal: SPACING.lg,
      borderRadius: 8,
      alignItems: 'center',
      justifyContent: 'center',
    },
    primary: { backgroundColor: COLORS.accent, color: COLORS.white, },
    secondary: { 
      backgroundColor: isDark ? COLORS.surface : COLORS.gray200, 
      borderWidth: 1,
      borderColor: isDark ? COLORS.border : COLORS.slate400,
      color: isDark ? COLORS.white : COLORS.slate900, 
    },
    danger: { backgroundColor: '#dc3545' },
    disabled: { opacity: 0.5 },
    text: {
      color: COLORS.white,
      fontSize: 16,
      fontWeight: '600',
      lineHeight: 16 * 1.4,
      textAlign: 'center',
    },
  });
}
