import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardFlowChartStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    card: {
      width: '100%',
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      padding: SPACING.md,
      marginBottom: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: SPACING.md,
      color: COLORS.textSoft,
      textAlign: 'center',
    },
    chartContainer: {
      width: '100%',
      alignItems: 'center',
      marginBottom: SPACING.md,
    },
    legend: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: SPACING.md,
      justifyContent: 'center',
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
    },
    legendColor: {
      width: 12,
      height: 12,
      borderRadius: 6,
    },
    legendText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      fontWeight: '500',
    },
  });
}
