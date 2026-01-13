import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getIconInputStyles() {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return StyleSheet.create({
    wrap: {
      flexDirection: 'row',
      alignItems: 'center',
      borderRadius: 16,
      backgroundColor: isDark ? 'rgba(19,25,34,0.7)' : COLORS.surface,
      borderWidth: 1,
      borderColor: isDark ? 'rgba(255,255,255,0.06)' : COLORS.border,
      paddingHorizontal: SPACING.md,
      height: 64,
    },
    side: { width: 28, alignItems: 'center', justifyContent: 'center' },
    input: {
      flex: 1,
      color: isDark ? COLORS.white : COLORS.black,
      fontSize: 16,
      paddingVertical: SPACING.sm + 2,
    },
  });
}
