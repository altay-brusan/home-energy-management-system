import { StyleSheet } from 'react-native';
import { getDesignTokens, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getButtonStyles () {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    button: {
      paddingVertical: SPACING.sm + 4,
      paddingHorizontal: SPACING.lg,
      borderRadius: 8,
      alignItems: 'center',
      justifyContent: 'center',
    },
    primary: {
      backgroundColor: tokens.button.primaryBg,
      color: tokens.button.primaryText,
    },
    secondary: {
      backgroundColor: tokens.button.secondaryBg,
      borderWidth: 1,
      borderColor: tokens.button.secondaryBorder,
      color: tokens.button.secondaryText,
    },
    danger: {
      backgroundColor: tokens.button.dangerBg,
      color: tokens.button.dangerText,
    },
    disabled: { opacity: 0.5 },
    text: {
      color: tokens.button.primaryText,
      fontSize: 16,
      fontWeight: '600',
      lineHeight: 16 * 1.4,
      textAlign: 'center',
    },
  });
}
