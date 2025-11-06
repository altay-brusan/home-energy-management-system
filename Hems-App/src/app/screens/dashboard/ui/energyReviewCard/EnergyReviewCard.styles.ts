import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      padding: SPACING.lg,
      marginBottom: SPACING.sm,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOpacity: 0.06,
      shadowRadius: 6,
      shadowOffset: { width: 0, height: 2 },
      elevation: 1,
    },
    header: {
      marginBottom: SPACING.md,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.xs,
    },
    subtitle: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      opacity: 0.7,
    },
    chartContainer: {
      width: '100%',
      height: 280,
      marginTop: SPACING.sm,
      backgroundColor: 'transparent',
    },
    chartWrapper: {
      flex: 1,
    },
    legend: {
      flexDirection: 'row',
      justifyContent: 'center',
      alignItems: 'center',
      gap: SPACING.lg,
      marginTop: SPACING.md,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
    },
    legendDot: {
      width: 12,
      height: 12,
      borderRadius: 6,
    },
    legendLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    tooltipContainer: {
      position: 'absolute',
      backgroundColor: COLORS.surface,
      borderRadius: 8,
      padding: SPACING.sm,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOpacity: 0.15,
      shadowRadius: 8,
      shadowOffset: { width: 0, height: 2 },
      elevation: 4,
      minWidth: 160,
    },
    tooltipRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      marginBottom: 4,
    },
    tooltipLabel: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      opacity: 0.7,
    },
    tooltipValue: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginLeft: SPACING.sm,
    },
    tooltipTime: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      opacity: 0.5,
      marginTop: 4,
      textAlign: 'center',
    },
    loadingContainer: {
      height: 280,
      justifyContent: 'center',
      alignItems: 'center',
    },
    summaryContainer: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      paddingVertical: SPACING.sm,
      paddingHorizontal: SPACING.md,
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 12,
      marginBottom: SPACING.md,
      gap: SPACING.xs,
    },
    summaryItem: {
      alignItems: 'center',
      flex: 1,
    },
    summaryValue: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '700',
      marginBottom: 2,
    },
    summaryLabel: {
      fontSize: 10,
      color: COLORS.textSoft,
      opacity: 0.7,
      fontWeight: '600',
      textAlign: 'center',
    },
    buyColor: {
      color: '#ef4444', // Red for buying
    },
    sellColor: {
      color: '#10b981', // Green for selling
    },
  });
};
