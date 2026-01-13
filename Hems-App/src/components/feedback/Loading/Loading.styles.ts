import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getLoadingStyles() {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return {
    ...StyleSheet.create({
      container: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        padding: SPACING.lg,
      },
      message: {
        marginTop: SPACING.md - 4,
        fontSize: 16,
        color: COLORS.textSoft,
      },
    }),
    indicatorColor: COLORS.accent,
  };
}
