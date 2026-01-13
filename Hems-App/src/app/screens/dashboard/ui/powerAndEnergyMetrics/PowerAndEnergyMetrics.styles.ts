import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getPowerAndEnergyMetricsStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    card: {
      width: '100%',
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      padding: SPACING.md,
      marginBottom: SPACING.lg,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: 8,
      color: COLORS.textSoft,
    },
    placeholder: {
      minHeight: 200,
      borderRadius: 10,
      alignItems: 'center',
      justifyContent: 'center',
      marginBottom: 10,
    },
    chartContainer: {
      width: '100%',
      height: 300,
      borderRadius: 12,
      overflow: 'hidden',
      backgroundColor: 'transparent',
      borderWidth: 1,
      borderColor: COLORS.border,
    },
  });
}
