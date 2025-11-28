import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardWelcomeCardStyles (
  COLORS: Record<string, string>,
  isDark: boolean,
  width: number,
) {
  const isNarrow = width < 380;
  const isSmall = width < 340;
  const lastSyncSize = isSmall ? TYPOGRAPHY.xs : TYPOGRAPHY.sm;
  const metricLabelSize = isSmall ? 10 : 11;
  const metricValueSize = isSmall ? 12 : isNarrow ? TYPOGRAPHY.sm : 14;
  return StyleSheet.create({
    root: {
      width: '100%',
      alignSelf: 'stretch',
      alignItems: 'center',
      marginBottom: SPACING.xs,
      marginTop: SPACING.md,
      paddingVertical: SPACING.lg,
      backgroundColor: isDark ? COLORS.surface : COLORS.accentGlow,
      borderRadius: 22,
      flexDirection: 'row',
      paddingHorizontal: SPACING.lg,
      shadowColor: COLORS.accent,
      shadowOpacity: 0.14,
      shadowRadius: 16,
      shadowOffset: { width: 0, height: 6 },
      elevation: 3,
      overflow: 'hidden',
      borderWidth: isDark ? 1 : 0,
      borderColor: isDark ? COLORS.border : 'transparent',
    },
    logoContainer: {
      marginRight: SPACING.lg,
      justifyContent: 'center',
      alignItems: 'center',
    },
    statusRow: {
      flexDirection: 'row',
      gap: SPACING.sm,
      alignItems: 'center',
      justifyContent: 'flex-start',
      alignSelf: 'auto',
      flexWrap: 'wrap',
      marginBottom: SPACING.xs,
    },
    lastSync: {
      color: COLORS.textSoft,
      fontSize: lastSyncSize,
      marginLeft: SPACING.xs,
    },
    content: {
      flex: 1,
      minWidth: 0,
      alignSelf: 'stretch',
      alignItems: 'flex-start',
      justifyContent: 'center',
    },
    metricsRow: {
      flexDirection: 'row',
      justifyContent: 'flex-start',
      flexWrap: 'wrap',
    },
    metricPill: {
      backgroundColor: isDark ? COLORS.buttonBg : COLORS.white,
      borderRadius: 14,
      paddingVertical: 6,
      paddingHorizontal: 10,
      borderWidth: 1,
      borderColor: COLORS.border,
      marginRight: SPACING.sm,
      marginBottom: SPACING.xs,
    },
    metricLabel: {
      fontSize: metricLabelSize,
      color: COLORS.slate400,
    },
    metricValue: {
      fontSize: metricValueSize,
      color: COLORS.textSoft,
      fontWeight: '700',
    },
  });
}
