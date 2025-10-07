import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export function getBatteryChartStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      padding: SPACING.md,
      marginBottom: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,

    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: SPACING.md,
      color: COLORS.textSoft,
      textAlign: 'center',
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

