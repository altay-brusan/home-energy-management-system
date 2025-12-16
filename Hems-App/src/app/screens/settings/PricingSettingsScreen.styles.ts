import { StyleSheet } from 'react-native';

export const makePricingSettingsScreenStyles = (COLORS: Record<string, string>, isDark: boolean) => {
  return StyleSheet.create({
    container: {
      flex: 1,
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: 16,
      paddingVertical: 12,
    },
    backButton: {
      padding: 8,
    },
    title: {
      fontSize: 18,
      fontWeight: '600',
      flex: 1,
      textAlign: 'center',
      marginHorizontal: 8,
    },
    scrollContent: {
      paddingHorizontal: 16,
      paddingTop: 16,
    },
    card: {
      borderRadius: 12,
      marginBottom: 12,
      padding: 16,
    },
    cardHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 4,
    },
    cardTitle: {
      fontSize: 16,
      fontWeight: '600',
      marginBottom: 4,
    },
    cardDesc: {
      fontSize: 13,
      marginBottom: 16,
      lineHeight: 18,
    },
    addButton: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 4,
    },
    addButtonText: {
      fontSize: 14,
      fontWeight: '600',
    },
    periodCard: {
      borderRadius: 12,
      marginBottom: 12,
      padding: 16,
    },
    periodHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 16,
    },
    periodBadge: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 6,
    },
    periodBadgeText: {
      fontSize: 14,
      fontWeight: '600',
    },
    deleteButton: {
      padding: 6,
    },
    timeRow: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 16,
    },
    timeButton: {
      flex: 1,
      padding: 12,
      borderRadius: 10,
      alignItems: 'center',
    },
    timeLabel: {
      fontSize: 11,
      marginBottom: 4,
    },
    timeValue: {
      fontSize: 18,
      fontWeight: '600',
    },
    timeSeparator: {
      fontSize: 18,
      marginHorizontal: 12,
    },
    pricesRow: {
      flexDirection: 'row',
      gap: 12,
    },
    priceColumn: {
      flex: 1,
    },
    priceLabel: {
      fontSize: 12,
      marginBottom: 6,
    },
    priceInputContainer: {
      flexDirection: 'row',
      alignItems: 'center',
      borderRadius: 10,
      paddingHorizontal: 12,
      paddingVertical: 10,
    },
    currencySymbol: {
      fontSize: 16,
      fontWeight: '600',
      marginRight: 6,
    },
    priceInput: {
      flex: 1,
      fontSize: 16,
      padding: 0,
    },
    // Modal styles
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0,0,0,0.5)',
      justifyContent: 'center',
      alignItems: 'center',
      padding: 24,
    },
    modalContent: {
      width: '100%',
      maxWidth: 320,
      borderRadius: 16,
      padding: 24,
    },
    modalTitle: {
      fontSize: 18,
      fontWeight: '600',
      textAlign: 'center',
      marginBottom: 24,
    },
    timePickerRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
      marginBottom: 24,
    },
    timePickerColumn: {
      alignItems: 'center',
    },
    timePickerLabel: {
      fontSize: 12,
      marginBottom: 8,
    },
    timePickerInput: {
      width: 80,
      height: 60,
      borderRadius: 12,
      fontSize: 28,
      fontWeight: '600',
      textAlign: 'center',
    },
    timePickerColon: {
      fontSize: 28,
      fontWeight: '600',
      marginHorizontal: 12,
      marginTop: 20,
    },
    modalButtons: {
      flexDirection: 'row',
      gap: 12,
    },
    modalButton: {
      flex: 1,
      paddingVertical: 14,
      borderRadius: 10,
      alignItems: 'center',
    },
    modalButtonText: {
      fontSize: 16,
      fontWeight: '600',
    },
  });
};
