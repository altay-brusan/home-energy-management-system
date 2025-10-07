import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardDevicesStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    list: {
      width: '100%',
      marginBottom: SPACING.lg,
    },
    sectionTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: 8,
      color: COLORS.textSoft,
    },
    item: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.lg,
      marginBottom: 20,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOpacity: 0.06,
      shadowRadius: 6,
      shadowOffset: { width: 0, height: 2 },
      elevation: 1,
    },
    deviceRow: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 8,
    },
    deviceIconWrap: {
      width: 40,
      height: 40,
      borderRadius: 20,
      backgroundColor: COLORS.bgShadeLight,
      alignItems: 'center',
      justifyContent: 'center',
      borderWidth: 1,
      borderColor: COLORS.border,
      marginRight: SPACING.md,
    },
    deviceIcon: {

    },
    deviceName: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.md,

    },
    badgeRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.lg,
      marginBottom: SPACING.xs,
      marginTop: SPACING.xs,
    },
    metaRow: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: SPACING.sm,
    },
    meta: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,

    },
  });
}
