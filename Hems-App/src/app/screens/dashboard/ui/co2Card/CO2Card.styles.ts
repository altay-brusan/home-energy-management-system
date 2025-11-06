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
    metricsContainer: {
      flexDirection: 'row',
      gap: SPACING.md,
    },
    metricBox: {
      flex: 1,
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 12,
      padding: SPACING.md,
      alignItems: 'center',
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    metricIcon: {
      fontSize: 32,
      marginBottom: SPACING.xs,
    },
    metricValue: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '700',
      color: COLORS.success,
      marginBottom: 4,
    },
    metricLabel: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.7,
      textAlign: 'center',
    },
    loadingContainer: {
      height: 120,
      justifyContent: 'center',
      alignItems: 'center',
    },
  });
};
