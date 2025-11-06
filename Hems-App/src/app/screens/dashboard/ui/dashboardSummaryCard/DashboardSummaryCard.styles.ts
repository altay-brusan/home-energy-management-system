import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      marginBottom: 12,
      width: '100%',
    },
    chartContainer: {
      alignItems: 'center',
      justifyContent: 'center',
      height: 260,
      position: 'relative',
      marginTop: SPACING.md,
      paddingTop: SPACING.lg,
      width: '100%',
    },
    metricsContainer: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: SPACING.md,
      marginTop: SPACING.md,
      width: '100%',
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
    statusMetricValue: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: COLORS.success,
      textAlign: 'center',
    },
    centerLabelContainer: {
      position: 'absolute',
      alignItems: 'center',
      justifyContent: 'center',
    },
    centerValue: {
      fontSize: 24,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    centerUnit: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      marginTop: 2,
    },
    legendContainer: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      justifyContent: 'center',
      marginTop: SPACING.md,
      gap: SPACING.sm,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: SPACING.xs,
    },
    legendColor: {
      width: 12,
      height: 12,
      borderRadius: 2,
      marginRight: SPACING.xs,
    },
    legendText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
    },
    legendValue: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      fontWeight: '600',
      marginLeft: 4,
    },
    loadingContainer: {
      height: 200,
      justifyContent: 'center',
      alignItems: 'center',
    },
    noDataContainer: {
      height: 200,
      justifyContent: 'center',
      alignItems: 'center',
    },
    noDataText: {
      fontSize: TYPOGRAPHY.md,
      color: COLORS.textSoft,
    },
    chartLabel: {
      fontSize: 12,
      fontWeight: '600',
    },
    tooltip: {
      position: 'absolute',
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      minWidth: 120,
      alignItems: 'center',
      borderWidth: 2,
      borderColor: COLORS.accent,
      shadowColor: COLORS.black,
      shadowOpacity: 0.3,
      shadowRadius: 8,
      shadowOffset: { width: 0, height: 4 },
      elevation: 8,
      zIndex: 1000,
    },
    tooltipLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginBottom: 4,
    },
    tooltipValue: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: COLORS.accent,
    },
  });
};