import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export const getOnboardingStyles = (
  COLORS: Record<string, string>,
  isDark: boolean
) =>
  StyleSheet.create({
    root: {
      flex: 1,
      backgroundColor: COLORS.dark,
    },
    topBar: {
      flexDirection: 'row',
      justifyContent: 'flex-end',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
      paddingTop: SPACING.sm,
      paddingBottom: SPACING.xs,
    },
    themeToggle: {
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
      borderWidth: isDark ? 0 : 1,
      borderColor: isDark ? 'transparent' : COLORS.border,
      elevation: 0,
      shadowOpacity: 0,
      shadowRadius: 0,
      shadowOffset: { width: 0, height: 0 },
      zIndex: 1,
      marginTop: -SPACING.md,
      paddingBottom: SPACING.lg,
    },
    inner: {
    },
    stepLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      letterSpacing: 0.4,
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      textTransform: 'uppercase',
    },
    title: {
      marginTop: SPACING.xs,
      fontSize: TYPOGRAPHY.xxxl,
      fontWeight: '800',
      color: isDark ? COLORS.white : COLORS.slate900,
    },
    subtitle: {
      marginTop: SPACING.sm,
      fontSize: TYPOGRAPHY.md,
      lineHeight: 22,
      color: isDark ? COLORS.textSoft : COLORS.slate700,
    },
    formWrapper: {
      marginTop: SPACING.lg,
    },
    dualRow: {
      gap: SPACING.md,
      flexDirection: 'row',
    },
    grow: {
      flex: 1,
    },
    primaryButton: {
      marginTop: SPACING.lg,
      minHeight: 54,
      borderRadius: 14,
      backgroundColor: COLORS.accent,
    },
    primaryButtonText: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      letterSpacing: 0.3,
      color: COLORS.white,
    },
    secondaryButton: {
      marginTop: SPACING.sm,
      minHeight: 50,
      borderRadius: 14,
      borderWidth: 1,
      borderColor: COLORS.border,
      backgroundColor: isDark ? COLORS.surface : COLORS.buttonBg,
    },
    secondaryButtonText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: isDark ? COLORS.textSoft : COLORS.slate700,
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
