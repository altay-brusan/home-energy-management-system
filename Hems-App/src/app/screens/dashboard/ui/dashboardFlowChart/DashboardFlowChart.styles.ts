import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardFlowChartStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    card: {
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
      marginBottom: SPACING.md,
      color: COLORS.textSoft,
      textAlign: 'center',
    },
    filterContainer: {
      marginBottom: SPACING.md,
    },
    dateRangeContainer: {
      width: '100%',
    },
    dateLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      marginBottom: SPACING.xs,
    },
    dateRangeButton: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      backgroundColor: COLORS.background,
      borderRadius: 10,
      paddingVertical: 12,
      paddingHorizontal: 16,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    dateRangeButtonText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.textSoft,
      flex: 1,
    },
    dateRangeButtonIcon: {
      fontSize: 12,
      color: COLORS.textSoft,
      marginLeft: SPACING.xs,
    },
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0, 0, 0, 0.5)',
      justifyContent: 'flex-end',
    },
    bottomSheet: {
      backgroundColor: COLORS.surface,
      borderTopLeftRadius: 24,
      borderTopRightRadius: 24,
      paddingHorizontal: SPACING.lg,
      paddingBottom: SPACING.xl,
      paddingTop: SPACING.md,
      maxHeight: '65%',
    },
    bottomSheetSmall: {
      maxHeight: '40%',
    },
    bottomSheetHandle: {
      width: 40,
      height: 4,
      backgroundColor: COLORS.textSoft,
      opacity: 0.3,
      borderRadius: 2,
      alignSelf: 'center',
      marginBottom: SPACING.md,
    },
    bottomSheetTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.lg,
      textAlign: 'center',
    },
    rangeOptions: {
      gap: SPACING.md,
    },
    rangeOption: {
      paddingVertical: 16,
      paddingHorizontal: SPACING.lg,
      borderRadius: 12,
      alignItems: 'center',
      backgroundColor: COLORS.background,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    rangeOptionText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
    },
    chartContainer: {
      width: '100%',
      alignItems: 'center',
      marginBottom: SPACING.md,
    },
    legend: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: SPACING.md,
      justifyContent: 'center',
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
    },
    legendColor: {
      width: 12,
      height: 12,
      borderRadius: 6,
    },
    legendText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      fontWeight: '500',
    },
    // Date range picker styles
    selectionStatus: {
      backgroundColor: COLORS.background,
      borderRadius: 8,
      padding: SPACING.md,
      marginBottom: SPACING.md,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    selectionStatusText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.accent,
      textAlign: 'center',
    },
    actionButtons: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      marginBottom: SPACING.xl,
      marginTop: SPACING.xs,
    },
    actionButton: {
      flex: 1,
      paddingVertical: SPACING.md,
      borderRadius: 10,
      alignItems: 'center',
      justifyContent: 'center',
    },
    cancelButton: {
      backgroundColor: COLORS.background,
      borderWidth: 1,
      borderColor: COLORS.border,
      marginRight: SPACING.md,
    },
    cancelButtonText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    confirmButton: {
      backgroundColor: COLORS.accent,
      elevation: 2,
      shadowColor: '#000000',
      shadowOpacity: 0.1,
      shadowRadius: 4,
      shadowOffset: { width: 0, height: 2 },
    },
    confirmButtonText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.white,
    },
    disabledButton: {
      backgroundColor: COLORS.textSoft + '40',
      borderWidth: 0,
    },
    // Loading and no device styles
    loadingContainer: {
      height: 380,
      justifyContent: 'center',
      alignItems: 'center',
    },
    loadingText: {
      marginTop: 12,
      fontSize: TYPOGRAPHY.md,
      fontWeight: '500',
      color: COLORS.textSoft,
      textAlign: 'center',
    },
    noDeviceContainer: {
      height: 380,
      justifyContent: 'center',
      alignItems: 'center',
      padding: 20,
    },
    noDeviceText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '500',
      color: COLORS.textSoft,
      textAlign: 'center',
      opacity: 0.7,
    },
    noDataContainer: {
      height: 380,
      justifyContent: 'center',
      alignItems: 'center',
      padding: 20,
    },
    noDataText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '500',
      color: COLORS.textSoft,
      textAlign: 'center',
      opacity: 0.7,
    },
  });
}
