import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: SPACING.sm,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: SPACING.lg,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    // 3 Columns Layout
    columnsContainer: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: SPACING.sm,
    },
    column: {
      flex: 1,
      alignItems: 'center',
    },
    valueBox: {
      width: '100%',
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      alignItems: 'center',
      borderWidth: 1,
      borderColor: COLORS.border,
      minHeight: 80,
      justifyContent: 'center',
    },
    valueLabel: {
      fontSize: 10,
      color: COLORS.textSoft,
      opacity: 0.7,
      marginBottom: SPACING.xs,
      fontWeight: '600',
      textTransform: 'uppercase',
    },
    valueNumber: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '700',
      marginBottom: 2,
    },
    valueUnit: {
      fontSize: 9,
      color: COLORS.textSoft,
      opacity: 0.6,
      fontWeight: '500',
    },
    divider: {
      width: '80%',
      height: 1,
      backgroundColor: COLORS.border,
      marginVertical: SPACING.sm,
      opacity: 0.5,
    },
    columnLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginTop: SPACING.md,
      textAlign: 'center',
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
