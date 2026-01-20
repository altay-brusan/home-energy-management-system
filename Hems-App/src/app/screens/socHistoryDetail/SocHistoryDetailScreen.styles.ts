import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>, isDark: boolean) => {
  return StyleSheet.create({
    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      height: 60,
    },
    title: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    dateContainer: {
      paddingHorizontal: SPACING.md,
      marginBottom: SPACING.md,
    },
    dateButton: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: COLORS.surface,
      borderRadius: 8,
      paddingVertical: SPACING.sm,
      paddingHorizontal: SPACING.md,
      gap: 8,
    },
    dateButtonText: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    content: {
      flex: 1,
      paddingHorizontal: SPACING.md,
    },
    loadingContainer: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
    },
    cardsContainer: {
      flexDirection: 'row',
      gap: SPACING.sm,
      marginBottom: SPACING.md,
    },
    card: {
      flex: 1,
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: 100,
    },
    cardLabel: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginBottom: 8,
      textAlign: 'center',
      textTransform: 'uppercase',
    },
    cardValueContainer: {
      alignItems: 'center',
    },
    cardValue: {
      fontSize: 32,
      fontWeight: '700',
    },
    cardUnit: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '500',
      color: COLORS.textSoft,
      marginTop: 4,
    },
    chargeColor: {
      color: '#16A34A',
    },
    dischargeColor: {
      color: COLORS.accent,
    },
    chartContainer: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.sm,
      paddingTop: SPACING.md,
    },
    chartTitle: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginBottom: SPACING.sm,
      marginLeft: SPACING.sm,
    },
    emptyStateContainer: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      paddingVertical: 60,
      gap: SPACING.md,
    },
    emptyStateText: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '600',
      color: COLORS.text,
      textAlign: 'center',
    },
    emptyStateSubtext: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '400',
      color: COLORS.textSoft,
      textAlign: 'center',
      maxWidth: 260,
    },
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0, 0, 0, 0.5)',
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: SPACING.lg,
    },
    modalContent: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      width: '100%',
      maxWidth: 400,
      overflow: 'hidden',
    },
    modalHeader: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.md,
      borderBottomWidth: 1,
      borderBottomColor: isDark ? COLORS.slate700 : COLORS.slate200,
    },
    modalTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.text,
    },
    modalCloseButton: {
      padding: 4,
    },
    modalCloseText: {
      fontSize: 24,
      color: COLORS.textSoft,
      fontWeight: '300',
    },
  });
};
