import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export function getChargeDischargeChartStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      padding: SPACING.md,
      marginBottom: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: SPACING.md,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      flex: 1,
    },
    legendContainer: {
      flexDirection: 'row',
      gap: SPACING.md,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
    },
    legendDot: {
      width: 8,
      height: 8,
      borderRadius: 4,
      marginRight: SPACING.xs,
    },
    legendText: {
      fontSize: 12,
      color: COLORS.textSoft,
      fontWeight: '500',
    },
    chartContainer: {
      width: '100%',
      height: 200,
      borderRadius: 12,
      overflow: 'hidden',
      backgroundColor: 'transparent',
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    statsContainer: {
      flexDirection: 'row',
      justifyContent: 'space-around',
      marginTop: SPACING.md,
    },
    statItem: {
      alignItems: 'center',
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 12,
      paddingVertical: SPACING.sm,
      paddingHorizontal: SPACING.md,
      minWidth: 60,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    statLabel: {
      fontSize: 12,
      color: COLORS.textSoft,
      fontWeight: '500',
      marginBottom: 2,
    },
    statValue: {
      fontSize: 16,
      color: COLORS.textSoft,
      fontWeight: '700',
    },
  });
}

