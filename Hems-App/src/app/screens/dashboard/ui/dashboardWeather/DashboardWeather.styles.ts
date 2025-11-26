import { StyleSheet, Dimensions } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

const { width } = Dimensions.get('window');
const isSmall = width <= 380;

export function getDashboardWeatherStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    widget: {
      width: '100%',
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      padding: isSmall ? SPACING.sm : SPACING.md,
      marginBottom: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    title: {
      fontSize: isSmall ? TYPOGRAPHY.md : TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: 6,
      color: COLORS.textSoft,
    },
    row: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      gap: isSmall ? SPACING.sm : SPACING.md,
      flexWrap: 'wrap',
    },
    contentWrapper: {
      flex: 1,
      minWidth: 0,
    },
    weatherIcon: {
      width: isSmall ? 36 : 48,
      height: isSmall ? 36 : 48,
    },
    icon: {
      fontSize: isSmall ? 28 : 36,
      marginRight: 10,
    },
    temp: {
      fontSize: isSmall ? TYPOGRAPHY.xl : TYPOGRAPHY.xxl,
      fontWeight: 'bold',
      color: COLORS.accent,
    },
    desc: {
      fontSize: isSmall ? TYPOGRAPHY.sm : TYPOGRAPHY.md,
      color: COLORS.textSoft,
    },
    meta: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.slate400,
      marginTop: 2,
      flexShrink: 1,
    },
    metricsContainer: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: SPACING.md,
      marginTop: SPACING.md,
    },
    metricCard: {
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 12,
      padding: SPACING.md,
      flex: 1,
      alignItems: 'center',
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    metricLabel: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.7,
      marginBottom: 4,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
    },
    metricValue: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: COLORS.accent,
      textAlign: 'center',
    },
  });
}
