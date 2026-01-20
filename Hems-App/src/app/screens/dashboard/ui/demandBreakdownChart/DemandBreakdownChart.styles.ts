import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>, isDark: boolean) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      padding: SPACING.md,
      marginBottom: SPACING.sm,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.md,
    },
    chartWrapper: {
      alignItems: 'center',
      justifyContent: 'center',
      position: 'relative',
    },
    centerContent: {
      position: 'absolute',
      alignItems: 'center',
      justifyContent: 'center',
    },
    centerLabel: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '500',
      color: COLORS.textSoft,
      marginBottom: 4,
      textTransform: 'uppercase',
      letterSpacing: 0.5,
    },
    centerValue: {
      fontSize: 36,
      fontWeight: '700',
      color: COLORS.text,
      lineHeight: 42,
    },
    centerUnit: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '500',
      color: COLORS.textSoft,
      marginTop: 2,
    },
    centerPercentage: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginTop: 4,
    },
    legend: {
      flexDirection: 'row',
      justifyContent: 'center',
      flexWrap: 'wrap',
      gap: SPACING.md,
      marginTop: SPACING.md,
      paddingTop: SPACING.sm,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: SPACING.sm,
      paddingVertical: 6,
      borderRadius: 8,
    },
    legendItemSelected: {
      backgroundColor: isDark ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.05)',
    },
    legendColor: {
      width: 12,
      height: 12,
      borderRadius: 3,
      marginRight: 8,
    },
    legendText: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '500',
      color: COLORS.textSoft,
    },
  });
};
