import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export const getPowerMetricsChartStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: 12,
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
    },
    chartContainer: {
      height: 320, // Reduced from 400
      minHeight: 280, // Reduced from 350
      width: '100%',
      position: 'relative',
      marginBottom: -SPACING.md, // Increased negative margin
    },
    chartAndHoursWrapper: {
      width: '100%',
      alignItems: 'center',
    },
    // Sunrise/Sunset indicator styles
    sunriseContainer: {
      flexDirection: 'row',
      justifyContent: 'space-around',
      paddingHorizontal: SPACING.md,
      marginTop: SPACING.md,
      paddingTop: SPACING.sm,
      borderTopWidth: 1,
      borderTopColor: COLORS.border,
    },
    sunIndicator: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
    },
    sunIconContainer: {
      width: 32,
      height: 32,
      borderRadius: 16,
      backgroundColor: COLORS.cardBg,
      justifyContent: 'center',
      alignItems: 'center',
    },
    sunIcon: {
      fontSize: 18,
    },
    sunLabel: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '500',
    },
    sunTime: {
      color: COLORS.primary,
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
    },
    // Hour labels with non-linear spacing
    hoursContainer: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      paddingHorizontal: SPACING.sm,
      marginTop: SPACING.xs,
      position: 'relative',
    },
    hourText: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      fontWeight: '500',
      position: 'absolute',
    },
    // Toggle switches for power metrics
    togglesContainer: {
      marginTop: SPACING.xs, // Reduced from md to xs
      gap: 0, // No gap between rows
    },
    togglesRow: {
      flexDirection: 'row',
      gap: SPACING.sm,
    },
    toggleItem: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      backgroundColor: COLORS.cardBg,
      borderRadius: 8,
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      flex: 1,
    },
    toggleLabel: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '500',
    },
    // Loading state
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
    // Legend
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
