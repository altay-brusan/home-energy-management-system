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
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.sm,
    },
    iconContainer: {
      width: 40,
      height: 40,
      borderRadius: 10,
      justifyContent: 'center',
      alignItems: 'center',
    },
    metricContent: {
      flex: 1,
    },
    metricLabel: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '500',
      color: COLORS.textSoft,
      marginTop: 2,
    },
    metricValueContainer: {
      flexDirection: 'row',
      alignItems: 'baseline',
      gap: 4,
    },
    metricValue: {
      fontSize: TYPOGRAPHY.xxl,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    metricUnit: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '500',
      color: COLORS.textSoft,
    },
  });
};
