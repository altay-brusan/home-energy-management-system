import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: 12,
    },
    weatherContainer: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: COLORS.cardBg,
      borderRadius: 12,
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.xs,
      gap: SPACING.xs,
    },
    weatherIcon: {
      width: 20,
      height: 20,
    },
    weatherTemp: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '700',
      color: COLORS.accent,
    },
    statusContainer: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
    },
    statusLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.7,
    },
    statusValue: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '700',
      color: COLORS.success,
    },
  });
};
