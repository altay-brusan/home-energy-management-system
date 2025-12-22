import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../theme/constants';

export function makeLegalModalStyles(
  COLORS: {
    dark: string;
    surface: string;
    border: string;
    textSoft: string;
    white: string;
    black: string;
    accent: string;
  },
  isDark: boolean
) {
  return StyleSheet.create({
    overlay: {
      flex: 1,
      backgroundColor: 'rgba(0, 0, 0, 0.6)',
      justifyContent: 'center',
      alignItems: 'center',
      padding: SPACING.md,
    },

    container: {
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderRadius: 20,
      width: '100%',
      maxWidth: 500,
      height: '85%',
      borderWidth: 1,
      borderColor: isDark ? COLORS.border : 'rgba(0,0,0,0.08)',
      overflow: 'hidden',
    },

    scrollView: {
      flex: 1,
    },

    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.md,
      borderBottomWidth: 1,
      borderBottomColor: isDark ? COLORS.border : 'rgba(0,0,0,0.08)',
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
    },

    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: isDark ? COLORS.white : COLORS.black,
      flex: 1,
    },

    closeButton: {
      width: 36,
      height: 36,
      borderRadius: 18,
      backgroundColor: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.05)',
      alignItems: 'center',
      justifyContent: 'center',
    },

    closeButtonText: {
      fontSize: 18,
      color: isDark ? COLORS.white : COLORS.black,
      fontWeight: '600',
    },

    scrollContent: {
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.lg,
      paddingBottom: SPACING.xl,
    },

    lastUpdated: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      marginBottom: SPACING.lg,
      fontStyle: 'italic',
    },

    intro: {
      fontSize: TYPOGRAPHY.md,
      color: isDark ? COLORS.white : COLORS.black,
      lineHeight: TYPOGRAPHY.md * 1.6,
      marginBottom: SPACING.lg,
    },

    sectionTitle: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: isDark ? COLORS.white : COLORS.black,
      marginTop: SPACING.lg,
      marginBottom: SPACING.sm,
    },

    sectionContent: {
      fontSize: TYPOGRAPHY.sm,
      color: isDark ? 'rgba(255,255,255,0.85)' : 'rgba(0,0,0,0.75)',
      lineHeight: TYPOGRAPHY.sm * 1.7,
      marginBottom: SPACING.md,
    },

    footer: {
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.md,
      borderTopWidth: 1,
      borderTopColor: isDark ? COLORS.border : 'rgba(0,0,0,0.08)',
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
    },

    footerButton: {
      backgroundColor: COLORS.accent,
      paddingVertical: SPACING.md,
      borderRadius: 12,
      alignItems: 'center',
    },

    footerButtonText: {
      color: '#fff',
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
    },
  });
}
