import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function getPVStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    // Screen
    screen: {
      flex: 1,
      backgroundColor: COLORS.dark,
    },

    // Header
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      backgroundColor: COLORS.dark,
    },
    backButton: {
      padding: 8,
    },
    headerTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    headerPlaceholder: {
      width: 40,
    },

    // Container
    container: {
      padding: SPACING.md,
      paddingBottom: SPACING.xl,
    },

    // Cards - Dashboard style
    card: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: SPACING.md,
    },

    // Card Header
    cardHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: SPACING.sm,
    },
    cardIconContainer: {
      width: 40,
      height: 40,
      borderRadius: 20,
      alignItems: 'center',
      justifyContent: 'center',
      marginRight: SPACING.sm,
    },
    cardTitle: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: COLORS.text,
    },
    cardSubtitle: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.xs,
      marginTop: 2,
    },

    // Metrics Row - TodaySummary style
    metricsRow: {
      flexDirection: 'row',
      justifyContent: 'space-around',
      paddingTop: SPACING.sm,
    },
    metricItem: {
      alignItems: 'center',
      flex: 1,
    },
    metricValue: {
      fontSize: TYPOGRAPHY.xxl,
      fontWeight: '700',
      color: COLORS.text,
    },
    metricUnit: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '400',
      color: COLORS.textSoft,
    },
    metricLabel: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      marginTop: 4,
    },
    metricDivider: {
      width: 1,
      backgroundColor: COLORS.border,
      marginVertical: 4,
    },

    // Chart Card
    chartCard: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: SPACING.md,
    },
    chartHeader: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: SPACING.sm,
    },
    chartTitle: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: COLORS.text,
    },
    chart: {
      width: '100%',
      height: 280,
      overflow: 'visible',
      backgroundColor: 'transparent',
    },

    // Range Tabs - Dashboard style
    rangeTabs: {
      flexDirection: 'row',
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 8,
      padding: 4,
      marginTop: SPACING.md,
    },
    rangeTab: {
      flex: 1,
      paddingVertical: SPACING.xs,
      borderRadius: 6,
      alignItems: 'center',
    },
    rangeTabActive: {
      backgroundColor: COLORS.surface,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 1 },
      shadowOpacity: 0.1,
      shadowRadius: 2,
      elevation: 2,
    },
    rangeTabText: {
      fontSize: TYPOGRAPHY.xs,
      fontWeight: '500',
      color: COLORS.textSoft,
    },
    rangeTabTextActive: {
      color: COLORS.text,
      fontWeight: '600',
    },

    // Info Card
    infoCard: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: SPACING.md,
    },
    infoTitle: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: COLORS.text,
      marginBottom: SPACING.sm,
    },
    infoRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingVertical: SPACING.sm,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    infoRowLast: {
      borderBottomWidth: 0,
    },
    infoLabel: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.sm,
    },
    infoValue: {
      color: COLORS.text,
      fontWeight: '600',
      fontSize: TYPOGRAPHY.sm,
    },

    // Empty state
    emptyContainer: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      paddingVertical: 60,
    },
    emptyText: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.sm,
    },
  });
}
