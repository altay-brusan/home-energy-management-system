import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardNotificationsStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    notifications: {
      width: '100%',
      marginBottom: SPACING.lg,
    },
    sectionTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: 8,
      color: COLORS.textSoft,
    },
    noNotification: {
      color: COLORS.slate400,
      fontSize: TYPOGRAPHY.md,
      textAlign: 'center',
      marginVertical: 12,
    },
    notificationItem: {
      backgroundColor: COLORS.surface,
      borderRadius: 10,
      padding: SPACING.md,
      marginBottom: 10,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOpacity: 0.04,
      shadowRadius: 4,
      shadowOffset: { width: 0, height: 1 },
      elevation: 1,
    },
    notificationText: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.md,
    },
  });
}
