import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getErrorMessageStyles() {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return StyleSheet.create({
    container: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      padding: SPACING.lg,
    },
    icon: {
      fontSize: 48,
      marginBottom: SPACING.md,
    },
    message: {
      fontSize: 16,
      color: COLORS.textSoft,
      textAlign: 'center',
      marginBottom: SPACING.lg,
    },
    button: {
      backgroundColor: COLORS.accent,
      paddingHorizontal: SPACING.xl,
      paddingVertical: SPACING.md,
      borderRadius: 8,
    },
    buttonText: {
      color: COLORS.white,
      fontSize: 16,
      fontWeight: '600',
    },
  });
}
