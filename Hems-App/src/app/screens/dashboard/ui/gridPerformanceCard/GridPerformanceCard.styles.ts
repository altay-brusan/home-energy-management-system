import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
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
    historyButton: {
      padding: SPACING.xs,
    },
    gridContainer: {
      gap: SPACING.sm,
    },
    columnHeadersRow: {
      flexDirection: 'row',
      gap: SPACING.sm,
      marginTop: 4,
      marginBottom: SPACING.md,
    },
    columnHeader: {
      flex: 1,
      alignItems: 'center',
    },
    columnHeaderText: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '600',
      color: COLORS.textSoft,
      textTransform: 'uppercase',
    },
    row: {
      flexDirection: 'row',
      gap: SPACING.sm,
    },
    metricCard: {
      flex: 1,
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: 90,
    },
    metricLabel: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginBottom: 4,
      textAlign: 'center',
      textTransform: 'uppercase',
    },
    metricValueContainer: {
      alignItems: 'center',
    },
    metricValue: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '700',
    },
    metricUnit: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '500',
      color: COLORS.textSoft,
      marginTop: 2,
    },
    periodLabel: {
      fontSize: 10,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.7,
      marginBottom: 4,
      textTransform: 'uppercase',
    },
    // Loading State
    loadingContainer: {
      height: 200,
      justifyContent: 'center',
      alignItems: 'center',
    },
    // Colors
    buyColor: {
      color: '#ef4444', // Red for grid buy
    },
    sellColor: {
      color: '#10b981', // Green for grid sell
    },
  });
};
