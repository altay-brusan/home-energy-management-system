import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function getResetPasswordStyles (
  COLORS: Record<string, string>,
  isDark: boolean
) {
  return StyleSheet.create({
    root: { flex: 1, backgroundColor: COLORS.dark },

    topBar: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
      marginTop: SPACING.sm,
      marginBottom: SPACING.xs,
    },

    themeToggle: { alignSelf: 'flex-start' },

    regionWrap: { alignSelf: 'flex-end' },

    centerWrap: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
    },

    card: {
      width: '92%',
      maxWidth: 560,
      borderRadius: 18,
      minHeight: 400,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 10 },
      shadowOpacity: 0.18,
      shadowRadius: 12,
      elevation: 20,
      zIndex: 10,
    },

    scrollContent: {
      flexGrow: 1,
      justifyContent: 'center',
      paddingVertical: SPACING.md,
    },
    cardInner: { flex: 1 },

    brand: {
      fontSize: TYPOGRAPHY.xxl,
      fontWeight: '900',
      letterSpacing: 1,
      textAlign: 'center',
      marginBottom: SPACING.xs,
      color: isDark ? COLORS.white : COLORS.black,
      textShadowColor: COLORS.accentGlow,
      textShadowOffset: { width: 0, height: 0 },
      textShadowRadius: 8,
    },

    subtitle: {
      fontSize: TYPOGRAPHY.sm,
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      textAlign: 'center',
      marginBottom: SPACING.lg,
      paddingHorizontal: SPACING.md,
      lineHeight: 20,
    },

    formWrap: { flexGrow: 1, justifyContent: 'center' },
    form: { gap: SPACING.sm, paddingHorizontal: 4, alignSelf: 'stretch' },

    primaryBtn: {
      height: 56,
      borderRadius: 16,
    },

    loginBtn: {
      height: 56,
      borderRadius: 16,
      backgroundColor: 'transparent',
      borderWidth: 1,
      borderColor: COLORS.accent,
    },

    loginText: {
      color: COLORS.accent,
      fontWeight: '600',
    },

    errorText: {
      color: COLORS.error || '#ef4444',
      fontSize: TYPOGRAPHY.sm,
      textAlign: 'center',
      marginTop: SPACING.sm,
      marginBottom: SPACING.xs,
    },

    successText: {
      color: COLORS.success || '#10b981',
      fontSize: TYPOGRAPHY.sm,
      textAlign: 'center',
      marginTop: SPACING.sm,
      marginBottom: SPACING.xs,
      fontWeight: '600',
    },

    bgTopShade: {
      position: 'absolute',
      top: -110,
      left: -80,
      width: 260,
      height: 260,
      borderRadius: 130,
      backgroundColor: COLORS.bgShadeLight,
      zIndex: -1,
    },

    bgBottomShade: {
      position: 'absolute',
      bottom: -140,
      right: -100,
      width: 300,
      height: 300,
      borderRadius: 150,
      backgroundColor: COLORS.bgShadeLight,
      zIndex: -1,
    },
  });
}

export type ResetPasswordStyles = ReturnType<typeof getResetPasswordStyles>;

