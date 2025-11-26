import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: SPACING.sm,
    },

    // Header
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: SPACING.sm,
    },
    title: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
    },

    // Grid (2 columns)
    grid: {
      flexDirection: 'row',
      gap: SPACING.xs,
    },

    // Benefit Card
    benefitCard: {
      flex: 1,
      backgroundColor: 'transparent',
      flexDirection: 'column',
    },

    // Value Row
    valueRow: {
      flexDirection: 'row',
      alignItems: 'baseline',
      gap: 4,
      marginBottom: 4,
    },
    value: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    unit: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '500',
      color: COLORS.textSoft,
      opacity: 0.5,
    },

    // Label
    label: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      opacity: 0.6,
    },

    // Loading
    loadingContainer: {
      height: 80,
      justifyContent: 'center',
      alignItems: 'center',
    },

    // Modal Styles
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0, 0, 0, 0.5)',
      justifyContent: 'center',
      alignItems: 'center',
      padding: SPACING.lg,
    },
    modalContent: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      padding: SPACING.lg,
      width: '100%',
      maxWidth: 400,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    modalTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.lg,
      textAlign: 'center',
    },
    modalSection: {
      marginBottom: SPACING.lg,
    },
    modalIconRow: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: SPACING.xs,
      gap: SPACING.xs,
    },
    modalIconBg: {
      width: 32,
      height: 32,
      borderRadius: 16,
      backgroundColor: COLORS.bgShadeLight,
      alignItems: 'center',
      justifyContent: 'center',
    },
    modalIcon: {
      fontSize: 18,
    },
    modalSectionTitle: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    modalText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      opacity: 0.8,
      lineHeight: 20,
      marginLeft: 40, // align with title
    },
    modalButton: {
      backgroundColor: COLORS.accent,
      borderRadius: 8,
      padding: SPACING.md,
      alignItems: 'center',
      marginTop: SPACING.sm,
    },
    modalButtonText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.white,
    },
  });
};
