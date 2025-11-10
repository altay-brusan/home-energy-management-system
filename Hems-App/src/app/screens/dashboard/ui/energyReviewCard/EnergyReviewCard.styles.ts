import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: SPACING.sm,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'flex-start',
      marginBottom: SPACING.md,
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
    chartContainer: {
      width: '100%',
      height: 350,
      backgroundColor: 'transparent',
    },
    chartWrapper: {
      position: 'relative',
      marginTop: SPACING.sm,
    },
    legend: {
      flexDirection: 'row',
      justifyContent: 'center',
      alignItems: 'center',
      gap: SPACING.lg,
      marginTop: SPACING.md,
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
    infoBox: {
      position: 'absolute',
      top: -10,
      left: 0,
      right: 0,
      zIndex: 10,
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    infoDate: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.sm,
      textAlign: 'center',
    },
    infoGrid: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: SPACING.xs,
    },
    infoItem: {
      flex: 1,
      alignItems: 'center',
    },
    infoLabel: {
      fontSize: 9,
      color: COLORS.textSoft,
      opacity: 0.7,
      marginBottom: 4,
      fontWeight: '600',
    },
    infoValue: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    loadingContainer: {
      height: 350,
      justifyContent: 'center',
      alignItems: 'center',
    },
    summaryCardsContainer: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      marginTop: 0,
      gap: SPACING.sm,
    },
    summaryCard: {
      flex: 1,
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      alignItems: 'center',
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    summaryCardLabel: {
      fontSize: 10,
      color: COLORS.textSoft,
      opacity: 0.7,
      marginBottom: SPACING.xs,
      textAlign: 'center',
      fontWeight: '600',
    },
    summaryCardValue: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      marginBottom: 2,
    },
    summaryCardUnit: {
      fontSize: 9,
      color: COLORS.textSoft,
      opacity: 0.6,
      fontWeight: '500',
    },
    buyColor: {
      color: '#ef4444', // Red for buying
    },
    sellColor: {
      color: '#10b981', // Green for selling
    },
  });
};
