import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardDevicesStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    list: {
      width: '100%',
      marginBottom: SPACING.lg,
    },
    sectionTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: 8,
      color: COLORS.textSoft,
    },
    item: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      padding: SPACING.lg,
      marginBottom: 16,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOpacity: 0.08,
      shadowRadius: 8,
      shadowOffset: { width: 0, height: 4 },
      elevation: 2,
    },
    cardHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: SPACING.md,
    },
    deviceIconWrap: {
      width: 48,
      height: 48,
      borderRadius: 24,
      backgroundColor: COLORS.bgShadeLight,
      alignItems: 'center',
      justifyContent: 'center',
      borderWidth: 1,
      borderColor: COLORS.border,
      marginRight: SPACING.md,
    },
    deviceIcon: {
    },
    deviceInfo: {
      flex: 1,
    },
    statusBadge: {
      backgroundColor: COLORS.accent,
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 20,
    },
    statusText: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '600',
      color: COLORS.white,
      // textTransform: 'uppercase',
    },
    deviceName: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: 4,
    },
    componentName: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.8,
      marginBottom: 2,
    },
    inverterMetrics: {
      gap: SPACING.md,
    },
    topRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: SPACING.md,
    },
    metricsGrid: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      justifyContent: 'space-between',
      gap: SPACING.md,

    },
    metricItem: {
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 12,
      padding: SPACING.md,
      minWidth: '30%',
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
    totalMetricItem: {
      backgroundColor: COLORS.accent,
      borderRadius: 12,
      padding: SPACING.lg,
      alignItems: 'center',
      borderWidth: 1,
      borderColor: COLORS.accent,
    },
    totalMetricLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.white,
      opacity: 0.9,
      marginBottom: 4,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
    },
    totalMetricValue: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '800',
      color: COLORS.white,
      textAlign: 'center',
    },
    badgeRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.lg,
      marginBottom: SPACING.xs,
      marginTop: SPACING.xs,
    },
  });
}
