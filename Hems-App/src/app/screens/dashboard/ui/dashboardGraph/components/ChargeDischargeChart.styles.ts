import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export function getChargeDischargeChartStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: 12,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'flex-start',
      marginBottom: SPACING.md,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    legendContainer: {
      flexDirection: 'row',
      gap: SPACING.md,
      marginTop: SPACING.xs,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
    },
    legendDot: {
      width: 10,
      height: 10,
      borderRadius: 5,
      marginRight: SPACING.xs,
    },
    legendText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      fontWeight: '600',
    },
    chartContainer: {
      width: '100%',
      height: 260,
      borderRadius: 12,
      overflow: 'hidden',
      backgroundColor: 'transparent',
    },
    statsContainer: {
      flexDirection: 'row',
      gap: SPACING.sm,
    },
    statItem: {
      alignItems: 'center',
      backgroundColor: COLORS.cardBg,
      borderRadius: 8,
      paddingVertical: SPACING.sm,
      paddingHorizontal: SPACING.xs,
      flex: 1,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    statLabel: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      fontWeight: '600',
      marginBottom: 4,
      textAlign: 'center',
      opacity: 0.8,
    },
    statValue: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      fontWeight: '700',
      textAlign: 'center',
    },
  });
}

