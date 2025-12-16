import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function makeLoginStyles(
  COLORS: {
    dark: string;
    surface: string;
    border: string;
    textSoft: string;
    white: string;
    black: string;
    accent: string;
    accentGlow: string;
    buttonBg: string;
    bgShadeLight: string;
    bgShadeDark: string;
  },
  isDark: boolean
) {
  return StyleSheet.create({
    safeArea: {
      flex: 1,
      backgroundColor: isDark ? 'rgba(28, 34, 43, 1)' : 'rgba(243, 243, 245, 1)',
    },

    gradient: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      height: 350,
      zIndex: 0,
    },

    root: {
      flex: 1,
      backgroundColor: 'transparent',
    },

    topBar: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.lg,
      paddingTop: SPACING.md,
      paddingBottom: SPACING.xs,
    },

    themeToggle: { alignSelf: 'flex-start' },
    regionWrap: { alignSelf: 'flex-start' },

    scrollContent: {
      flexGrow: 1,
      paddingHorizontal: SPACING.lg,
      paddingBottom: SPACING.xl,
    },

    logoContainer: {
      alignItems: 'center',
      marginTop: SPACING.xl,
      marginBottom: SPACING.xl,
    },

    formCard: {
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderRadius: 20,
      padding: SPACING.lg,
      borderWidth: 1,
      borderColor: isDark ? COLORS.border : 'rgba(0,0,0,0.05)',
    },

    welcomeText: {
      fontSize: TYPOGRAPHY.xxl,
      fontWeight: '700',
      color: isDark ? COLORS.white : COLORS.black,
      textAlign: 'center',
      marginBottom: SPACING.xs,
    },

    subtitleText: {
      fontSize: TYPOGRAPHY.md,
      color: COLORS.textSoft,
      textAlign: 'center',
      marginBottom: SPACING.lg,
    },

    form: {
      gap: SPACING.sm,
    },

    forgotContainer: {
      alignSelf: 'flex-start',
      marginTop: SPACING.xs,
    },

    forgot: {
      color: COLORS.accent,
      fontWeight: '600',
      fontSize: TYPOGRAPHY.sm,
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

    primaryBtn: {
      marginTop: SPACING.lg,
      minHeight: 52,
      borderRadius: 14,
    },

    loginBtnDisabled: {
      opacity: 0.5,
    },

    signupContainer: {
      alignItems: 'center',
      marginTop: SPACING.lg,
      paddingBottom: SPACING.sm,
    },

    signupLink: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.sm,
    },

    signupHighlight: {
      color: COLORS.accent,
      fontWeight: '700',
    },
  });
}
