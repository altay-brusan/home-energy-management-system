import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export const getPowerMetricsChartStyles = (COLORS: Record<string, string>, dataLength: number = 24) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: 12,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: SPACING.md,
      color: COLORS.textSoft,
    },
    chartContainer: {
      height: 400,
      minHeight: 350,
      width: '100%',
    },
    chartAndHoursWrapper: {
      width: '100%',
      alignItems: 'center',
    },
    hoursContainer: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      paddingHorizontal: SPACING.sm,
      marginTop: SPACING.xs,
    },
    hourText: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      fontWeight: '500',
    },
    togglesContainer: {
      flexDirection: 'row',
      justifyContent: 'center',
      flexWrap: 'wrap',
    },
    togglesRow: {
      flexDirection: 'row',
      justifyContent: 'space-around',
      marginTop: 12,
    },
    toggleItem: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: COLORS.surface,
      borderRadius: 8,
      paddingHorizontal: 12,
      paddingVertical: 6,
    },
    toggleLabel: {
      color: COLORS.textSoft,
      fontSize: 14,
      marginRight: 6,
    },
    loadingContainer: {
      justifyContent: 'center',
      alignItems: 'center',
      height: 300,
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      marginVertical: SPACING.md,
    },
    loadingText: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.md,
      marginTop: SPACING.sm,
      fontWeight: '500',
    },
    legendContainer: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      justifyContent: 'center',
      marginVertical: SPACING.md,
      paddingHorizontal: SPACING.sm,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      marginHorizontal: SPACING.sm,
      marginVertical: SPACING.xs,
    },
    legendColor: {
      width: 12,
      height: 12,
      borderRadius: 6,
      marginRight: SPACING.xs,
    },
    legendText: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '500',
    },

  });
};
