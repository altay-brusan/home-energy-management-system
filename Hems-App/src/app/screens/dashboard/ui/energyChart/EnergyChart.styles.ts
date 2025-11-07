/**
 * EnergyChart Component Styles
 */

import {StyleSheet} from 'react-native';
import {TYPOGRAPHY, SPACING} from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      padding: SPACING.md,
      marginBottom: SPACING.sm,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOpacity: 0.06,
      shadowRadius: 6,
      shadowOffset: {width: 0, height: 2},
      elevation: 1,
    },
    header: {
      marginBottom: SPACING.xs,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.xs,
    },
    subtitle: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      opacity: 0.7,
    },
    chartWrapper: {
      position: 'relative',
      marginTop: 40,
    },
    chartContainer: {
      width: '100%',
      backgroundColor: 'transparent',
    },

    // Info Box (Tooltip) Styles
    infoBox: {
      position: 'absolute',
      top: -40,
      left: 0,
      right: 0,
      zIndex: 10,
      backgroundColor: COLORS.surface,
      borderRadius: 8,
      padding: SPACING.xs,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    infoDate: {
      fontSize: 10,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.xs,
      textAlign: 'center',
    },
    infoSection: {
      marginBottom: 4,
    },
    infoSectionTitle: {
      fontSize: 11,
      fontWeight: '700',
      marginBottom: 2,
      flexDirection: 'row',
      alignItems: 'center',
    },
    inlineText: {
      fontSize: 9,
      color: COLORS.textSoft,
      opacity: 0.8,
      lineHeight: 14,
    },
    productionTitle: {
      color: '#10b981', // Green for production
    },
    consumptionTitle: {
      color: '#ef4444', // Red for consumption
    },
    infoRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingVertical: 2,
      paddingHorizontal: SPACING.xs,
    },
    infoLabel: {
      fontSize: 10,
      color: COLORS.textSoft,
      opacity: 0.8,
      fontWeight: '500',
    },
    infoValue: {
      fontSize: 10,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    infoDivider: {
      height: 1,
      backgroundColor: COLORS.border,
      marginVertical: 4,
      opacity: 0.5,
    },
    totalRow: {
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 6,
      marginTop: 2,
    },

    // Legend Styles
    legend: {
      flexDirection: 'row',
      justifyContent: 'center',
      alignItems: 'center',
      gap: SPACING.lg,
      marginTop: SPACING.md,
      marginBottom: SPACING.sm,
    },
    legendItem: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
    },
    legendDot: {
      width: 12,
      height: 12,
      borderRadius: 6,
    },
    legendLabel: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
    },
    productionLegend: {
      backgroundColor: '#10b981', // Green
    },
    consumptionLegend: {
      backgroundColor: '#ef4444', // Red
    },

    // Period Selector (Pill Tabs) Styles
    periodSelector: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: SPACING.xs,
      marginTop: SPACING.md,
      paddingHorizontal: SPACING.sm,
    },
    periodTab: {
      flex: 1,
      paddingVertical: SPACING.sm,
      paddingHorizontal: SPACING.xs,
      borderRadius: 18,
      borderWidth: 1,
      justifyContent: 'center',
      alignItems: 'center',
      marginHorizontal: 2,
    },
    periodTabText: {
      fontSize: 11,
      fontWeight: '500',
      textAlign: 'center',
    },

    // State Styles
    loadingContainer: {
      height: 320,
      justifyContent: 'center',
      alignItems: 'center',
    },
    loadingText: {
      marginTop: SPACING.sm,
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      opacity: 0.7,
    },
    errorContainer: {
      height: 320,
      justifyContent: 'center',
      alignItems: 'center',
      padding: SPACING.lg,
    },
    errorText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      textAlign: 'center',
      marginBottom: SPACING.md,
    },
    retryButton: {
      backgroundColor: COLORS.accent,
      paddingVertical: SPACING.sm,
      paddingHorizontal: SPACING.lg,
      borderRadius: 8,
    },
    retryButtonText: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.white,
    },
    emptyContainer: {
      height: 320,
      justifyContent: 'center',
      alignItems: 'center',
      padding: SPACING.lg,
    },
    emptyText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      textAlign: 'center',
      opacity: 0.7,
    },
  });
};
