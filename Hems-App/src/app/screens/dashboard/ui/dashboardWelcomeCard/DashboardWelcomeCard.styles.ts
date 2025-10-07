import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardWelcomeCardStyles (
  COLORS: Record<string, string>,
  isDark: boolean,
  width: number,
) {
  const isNarrow = width < 380;
  const isSmall = width < 340;
  const imageSize = isNarrow ? 72 : 88;
  const titleSize = isSmall ? TYPOGRAPHY.md : isNarrow ? TYPOGRAPHY.lg : TYPOGRAPHY.xxl;
  const subtitleSize = isSmall ? TYPOGRAPHY.xs : isNarrow ? TYPOGRAPHY.xs : TYPOGRAPHY.md;
  const lastSyncSize = isSmall ? TYPOGRAPHY.xs : TYPOGRAPHY.sm;
  const metricLabelSize = isSmall ? 10 : 11;
  const metricValueSize = isSmall ? 12 : isNarrow ? TYPOGRAPHY.sm : 14;
  return StyleSheet.create({
    root: {
      width: '100%',
      alignSelf: 'stretch',
      alignItems: isNarrow ? 'center' : 'center',
      marginBottom: SPACING.xs,
      marginTop: SPACING.md,
      paddingVertical: isNarrow ? SPACING.lg : SPACING.xl + 8,
      paddingTop: isNarrow ? SPACING.xl : SPACING.xxl,
      backgroundColor: isDark ? COLORS.surface : COLORS.accentGlow,
      borderRadius: 22,
      flexDirection: isNarrow ? 'column' : 'row',
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
    image: {
      width: imageSize,
      height: imageSize,
      borderRadius: 20,
      marginRight: isNarrow ? 0 : SPACING.lg,
      marginBottom: isNarrow ? SPACING.md : 0,
      borderWidth: 2,
      borderColor: COLORS.accent,
      backgroundColor: isDark ? COLORS.bgShadeDark : COLORS.gray100,
    },
    title: {
      fontSize: titleSize,
      fontWeight: 'bold',
      color: isDark ? COLORS.white : COLORS.slate900,
      marginBottom: 2,
      letterSpacing: 0.2,
      flexShrink: 1,
      flexWrap: 'wrap',
      textAlign: isNarrow ? 'center' : 'left',
      alignSelf: isNarrow ? 'center' : 'auto',
      lineHeight: titleSize + 2,
      maxWidth: '100%',
    },
    subtitle: {
      color: isDark ? COLORS.slate400 : COLORS.slate700,
      fontSize: subtitleSize,
      marginBottom: 6,
      flexShrink: 1,
      flexWrap: 'wrap',
      textAlign: isNarrow ? 'center' : 'left',
      lineHeight: subtitleSize + 2,
      maxWidth: '100%',
    },
    statusRow: {
      flexDirection: 'row',
      gap: SPACING.sm,
      alignItems: 'center',
      justifyContent: isNarrow ? 'center' : 'flex-start',
      alignSelf: isNarrow ? 'stretch' : 'auto',
      flexWrap: 'wrap',
      marginTop: 4,
    },
    lastSync: {
      color: COLORS.slate400,
      fontSize: lastSyncSize,
      marginLeft: SPACING.xs,
    },
    content: {
      flex: 1,
      minWidth: 0,
      alignSelf: 'stretch',
      paddingHorizontal: isNarrow ? SPACING.md : 0,
      alignItems: isNarrow ? 'center' : 'flex-start',
      marginTop: isNarrow ? SPACING.xs : 0,
    },
    metricsRow: {
      flexDirection: 'row',
      marginTop: 8,
      justifyContent: isNarrow ? 'center' : 'flex-start',
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
