import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardWeatherStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    widget: {
      width: '100%',
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
      marginBottom: 6,
      color: COLORS.textSoft,
    },
    row: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.md,
    },
    icon: {
      fontSize: 36,
      marginRight: 10,
    },
    temp: {
      fontSize: TYPOGRAPHY.xxl,
      fontWeight: 'bold',
      color: COLORS.accent,
    },
    desc: {
      fontSize: TYPOGRAPHY.md,
      color: COLORS.textSoft,
    },
    meta: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.slate400,
      marginTop: 2,
    },
  });
}
