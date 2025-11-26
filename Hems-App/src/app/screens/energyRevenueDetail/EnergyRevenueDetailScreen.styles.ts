import { StyleSheet } from 'react-native';
import { SPACING } from '../../../theme/constants';

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
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      height: 60,
    },
    title: {
      fontSize: 22,
      fontWeight: '700',
      color: COLORS.textSoft,
      letterSpacing: -0.5,
    },
    timeSelectorContainer: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      height: 60,
    },
    dateSelector: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 12,
    },
    arrowButton: {
      padding: 4,
    },
    dateText: {
      fontSize: 16,
      fontWeight: '600',
      color: COLORS.textSoft,
      minWidth: 160,
      textAlign: 'center',
    },
    rangeSelector: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 6,
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.05)',
      paddingHorizontal: 14,
      paddingVertical: 8,
      borderRadius: 10,
    },
    rangeText: {
      fontSize: 14,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0,0,0,0.7)',
      justifyContent: 'flex-end',
    },
    modalContent: {
      backgroundColor: COLORS.surface,
      borderTopLeftRadius: 20,
      borderTopRightRadius: 20,
      paddingBottom: 30,
      maxHeight: '80%',
    },
    modalHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: 20,
      paddingVertical: 16,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    modalTitle: {
      fontSize: 18,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    rangeOptions: {
      paddingHorizontal: 20,
      paddingTop: 10,
    },
    rangeOption: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingVertical: 16,
      paddingHorizontal: 16,
      borderRadius: 12,
      marginBottom: 8,
      backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.03)',
    },
    rangeOptionActive: {
      backgroundColor: isDark ? 'rgba(29,209,161,0.15)' : 'rgba(29,209,161,0.1)',
      borderWidth: 1,
      borderColor: COLORS.accent,
    },
    rangeOptionText: {
      fontSize: 16,
      fontWeight: '500',
      color: COLORS.textSoft,
    },
    rangeOptionTextActive: {
      fontWeight: '700',
      color: COLORS.accent,
    },
  });
};
