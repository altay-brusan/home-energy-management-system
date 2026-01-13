import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export function getEnergyBalanceChartStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 20,
      padding: SPACING.lg,
      marginBottom: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.accent || '#000',
      shadowOffset: {
        width: 0,
        height: 6,
      },
      shadowOpacity: 0.15,
      shadowRadius: 12,
      elevation: 6,
    },
    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: SPACING.lg,
      paddingBottom: SPACING.md,
      borderBottomWidth: 2,
      borderBottomColor: COLORS.accent + '20',
    },
    title: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '800',
      color: COLORS.textSoft,
    },
    netProductionContainer: {
      alignItems: 'flex-end',
    },
    netProductionLabel: {
      fontSize: 12,
      color: COLORS.textSoft,
      fontWeight: '500',
      marginBottom: 2,
    },
    netProductionValue: {
      fontSize: 18,
      fontWeight: '700',
    },
    chartsRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'stretch',
      gap: SPACING.lg,
    },
    energyCard: {
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 18,
      padding: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,
      flex: 1,
      shadowColor: COLORS.accent || '#000',
      shadowOffset: {
        width: 0,
        height: 4,
      },
      shadowOpacity: 0.08,
      shadowRadius: 8,
      elevation: 3,
    },
    cardHeader: {
      alignItems: 'center',
      marginBottom: SPACING.lg,
      paddingBottom: SPACING.md,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    cardTitleRow: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: SPACING.sm,
    },
    cardIcon: {
      fontSize: 20,
      marginRight: SPACING.sm,
    },
    cardTitle: {
      fontSize: 18,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    cardTotal: {
      fontSize: 28,
      fontWeight: '800',
      color: COLORS.accent,
    },
    progressContainer: {
      gap: SPACING.md,
    },
    progressItem: {
      padding: SPACING.md,
      borderRadius: 16,
      backgroundColor: COLORS.surface,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.accent || '#000',
      shadowOffset: {
        width: 0,
        height: 2,
      },
      shadowOpacity: 0.05,
      shadowRadius: 4,
      elevation: 1,
    },
    progressItemSelected: {
      backgroundColor: COLORS.accent + '10',
      borderWidth: 2,
      borderColor: COLORS.accent,
      shadowColor: COLORS.accent,
      shadowOffset: {
        width: 0,
        height: 4,
      },
      shadowOpacity: 0.15,
      shadowRadius: 8,
      elevation: 4,
    },
    progressHeader: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: SPACING.sm,
    },
    progressLabelRow: {
      flexDirection: 'row',
      alignItems: 'center',
      flex: 1,
    },
    progressIcon: {
      fontSize: 16,
      marginRight: SPACING.sm,
    },
    progressLabel: {
      fontSize: 16,
      color: COLORS.textSoft,
      fontWeight: '600',
      flex: 1,
    },
    progressLabelSelected: {
      fontWeight: '700',
      color: COLORS.accent,
    },
    trendIndicator: {
      width: 24,
      height: 24,
      borderRadius: 12,
      alignItems: 'center',
      justifyContent: 'center',
      marginLeft: SPACING.sm,
    },
    trendIcon: {
      fontSize: 12,
      color: COLORS.white,
      fontWeight: '700',
    },
    progressValue: {
      fontSize: 14,
      color: COLORS.textSoft,
      fontWeight: '700',
    },
    progressValueSelected: {
      color: COLORS.accent,
    },
    progressBar: {
      height: 16,
      backgroundColor: COLORS.border,
      borderRadius: 8,
      marginBottom: SPACING.sm,
      overflow: 'hidden',
    },
    progressFill: {
      height: '100%',
      borderRadius: 8,
    },
    progressFooter: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
    },
    percentageText: {
      fontSize: 14,
      color: COLORS.textSoft,
      fontWeight: '600',
    },
    percentageTextSelected: {
      fontWeight: '700',
      color: COLORS.accent,
    },
    percentageBar: {
      width: 60,
      height: 4,
      borderRadius: 2,
      overflow: 'hidden',
    },
    percentageFill: {
      height: '100%',
      borderRadius: 2,
    },
  });
}
