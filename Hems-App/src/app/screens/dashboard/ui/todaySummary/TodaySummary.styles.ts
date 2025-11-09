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
    row: {
      flexDirection: 'row',
      gap: SPACING.sm,
    },
    metricCard: {
      flex: 1,
      backgroundColor: COLORS.cardBg,
      borderRadius: 12,
      padding: SPACING.md,
      borderWidth: 1,
      borderColor: COLORS.border,
      minHeight: 80,
      justifyContent: 'center',
    },
    metricLabel: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.7,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
      marginBottom: SPACING.xs,
    },
    metricValueContainer: {
      flexDirection: 'row',
      alignItems: 'baseline',
      gap: 4,
    },
    metricValue: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '700',
    },
    metricUnit: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.6,
    },
  });
};
