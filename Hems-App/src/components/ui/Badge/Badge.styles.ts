import { StyleSheet } from 'react-native';
import { getDesignTokens, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getBadgeStyles() {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    badge: {
      paddingHorizontal: SPACING.sm,
      paddingVertical: SPACING.xs / 2,
      borderRadius: 12,
      alignSelf: 'flex-start',
      borderWidth: 1,
    },
    text: {
      fontSize: 12,
      fontWeight: '600',
    },
    default: {
      backgroundColor: tokens.badge.defaultBg,
      borderColor: tokens.badge.defaultBorder,
    },
    defaultText: {
      color: tokens.badge.defaultText,
    },
    primary: {
      backgroundColor: tokens.badge.primaryBg,
      borderColor: tokens.badge.primaryBorder,
    },
    primaryText: {
      color: tokens.badge.primaryText,
    },
    success: {
      backgroundColor: tokens.badge.successBg,
      borderColor: tokens.badge.successBorder,
    },
    successText: {
      color: tokens.badge.successText,
    },
    warning: {
      backgroundColor: tokens.badge.warningBg,
      borderColor: tokens.badge.warningBorder,
    },
    warningText: {
      color: tokens.badge.warningText,
    },
    error: {
      backgroundColor: tokens.badge.errorBg,
      borderColor: tokens.badge.errorBorder,
    },
    errorText: {
      color: tokens.badge.errorText,
    },
    info: {
      backgroundColor: tokens.badge.infoBg,
      borderColor: tokens.badge.infoBorder,
    },
    infoText: {
      color: tokens.badge.infoText,
    },
  });
}
