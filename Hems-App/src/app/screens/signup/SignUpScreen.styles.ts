import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function getSignUpStyles (
  COLORS: Record<string, string>,
  isDark: boolean
) {
  return StyleSheet.create({
    root: {
      flex: 1,
      backgroundColor: COLORS.dark,
    },

    topBar: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
      paddingTop: SPACING.sm,
      paddingBottom: SPACING.xs,
    },

    themeToggle: {
      alignSelf: 'flex-start',
    },

    regionWrap: {
      alignSelf: 'flex-end',
    },

    content: {
      flex: 1,
      paddingHorizontal: SPACING.md,
      position: 'relative',
    },

    keyboardAvoider: {
      flex: 1,
    },

    scrollContent: {
      paddingVertical: SPACING.lg,
      minHeight: '100%',
    },

    card: {
      width: '100%',
      maxWidth: 520,
      alignSelf: 'center',
      borderRadius: 20,
      backgroundColor: COLORS.cardBg,
      zIndex: 100,
      minHeight: 400,
    },

    cardInner: {
      flex: 1,
    },

    brand: {
      fontSize: TYPOGRAPHY.xxxl,
      fontWeight: '800',
      letterSpacing: 0.8,
      textAlign: 'center',
      marginBottom: SPACING.lg,
      color: isDark ? COLORS.white : COLORS.slate900,
    },

    formWrapper: {
      marginTop: SPACING.sm,
    },

    form: {
      alignSelf: 'stretch',
    },

    primaryButton: {
      marginTop: SPACING.md,
      minHeight: 54,
      borderRadius: 14,
    },

    secondaryButton: {
      marginTop: SPACING.sm,
      minHeight: 54,
      borderRadius: 14,
    },

    errorText: {
      color: COLORS.error || '#ef4444',
      fontSize: TYPOGRAPHY.sm,
      textAlign: 'left',
      marginTop: SPACING.xs,
      marginBottom: SPACING.xs,
    },

    tosRow: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      gap: SPACING.sm,
      marginTop: SPACING.md,
      paddingRight: SPACING.sm,
    },

    checkbox: {
      width: 20,
      height: 20,
      borderRadius: 6,
      borderWidth: 1.5,
      borderColor: isDark ? COLORS.border : '#D1D5DB',
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: 'transparent',
      marginTop: 2,
    },

    checkboxChecked: {
      backgroundColor: COLORS.accent,
      borderColor: COLORS.accent,
    },

    checkIcon: {
      color: '#fff',
      fontSize: 12,
      fontWeight: '700',
    },

    tosText: {
      color: COLORS.textSoft,
      flex: 1,
      flexWrap: 'wrap',
      fontSize: TYPOGRAPHY.sm,
      lineHeight: TYPOGRAPHY.sm * 1.5,
    },

    tosLink: {
      color: COLORS.accent,
      fontWeight: '600',
    },

    errorMessage: {
      marginBottom: SPACING.sm,
    },

    passwordRules: {
      marginTop: SPACING.sm,
      marginBottom: SPACING.md,
      padding: SPACING.sm,
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 8,
    },

    passwordRulesTitle: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginBottom: SPACING.xs,
    },

    passwordRule: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      marginBottom: 2,
    },

    bgTopShade: {
      position: 'absolute',
      top: -110,
      left: -80,
      width: 260,
      height: 260,
      borderRadius: 130,
  backgroundColor: isDark ? COLORS.bgShadeLight :COLORS.border,
      zIndex: -1,
    },

    bgBottomShade: {
      position: 'absolute',
      bottom: -140,
      right: -100,
      width: 300,
      height: 300,
      borderRadius: 150,
     backgroundColor: isDark ? COLORS.bgShadeLight :COLORS.border,
      zIndex: -1,
    },
  });
}

export type SignUpStyles = ReturnType<typeof getSignUpStyles>;
