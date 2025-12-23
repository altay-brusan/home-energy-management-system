import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export const getEnergyRevenueDetailStyles = (COLORS: any, isDark: boolean) => {
  return StyleSheet.create({
    loadingContainer: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: 20,
    },
    loadingText: {
      marginTop: 12,
      fontSize: 15,
      color: COLORS.textSoft,
      fontWeight: '500',
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: 16,
      paddingVertical: 4,
      marginBottom: 16,
    },
    title: {
      fontSize: 26,
      fontWeight: '700',
      color: COLORS.textSoft,
      letterSpacing: -0.5,
    },
    // New page header styles (Settings style)
    pageHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: 16,
      paddingVertical: 12,
    },
    headerBackButton: {
      padding: 8,
    },
    pageHeaderTitle: {
      fontSize: 18,
      fontWeight: '600',
      flex: 1,
      textAlign: 'center',
      marginHorizontal: 8,
    },
    timeSelectorContainer: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.sm,
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    dateSelector: {
      flex: 2,
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: COLORS.cardBg,
      borderRadius: 8,
      paddingHorizontal: SPACING.xs,
      height: 40,
    },
    arrowButton: {
      padding: 0,
      width: 32,
      height: 32,
      alignItems: 'center',
      justifyContent: 'center',
    },
    dateText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginHorizontal: SPACING.sm,
    },
    rangeSelector: {
      flex: 1,
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      gap: 8,
      backgroundColor: COLORS.cardBg,
      borderRadius: 8,
      paddingHorizontal: SPACING.md,
      height: 40,
    },
    rangeText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    modalOverlay: {
      flex: 1,
      backgroundColor: isDark ? 'rgba(0, 0, 0, 0.7)' : 'rgba(0, 0, 0, 0.5)',
      justifyContent: 'flex-end',
    },
    modalContent: {
      backgroundColor: COLORS.surface,
      borderTopLeftRadius: 20,
      borderTopRightRadius: 20,
      paddingBottom: 20,
      maxHeight: '80%',
    },
    modalHeader: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.md,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    modalTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    rangeOptions: {
      paddingHorizontal: SPACING.md,
      paddingTop: SPACING.sm,
    },
    rangeOption: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingVertical: SPACING.md,
      paddingHorizontal: SPACING.md,
      borderRadius: 12,
      marginBottom: SPACING.xs,
    },
    rangeOptionActive: {
      backgroundColor: isDark ? COLORS.cardBg : '#F5F5F5',
    },
    rangeOptionText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    rangeOptionTextActive: {
      color: COLORS.accent,
    },
  });
};
