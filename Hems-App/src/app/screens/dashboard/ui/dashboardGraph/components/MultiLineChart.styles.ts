import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export const getPowerMetricsChartStyles = (COLORS: Record<string, string>) =>
  StyleSheet.create({
    container: {
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
    },
    chart: {
      height: 260,
    },
    togglesRow: {
  flexDirection: 'row',
  justifyContent: 'space-around',
  marginTop: 12,
},
toggleItem: {
  flexDirection: 'row',
  alignItems: 'center',
  backgroundColor: COLORS.surface,
  borderRadius: 8,
  paddingHorizontal: 12,
  paddingVertical: 6,
},
toggleLabel: {
  color: COLORS.textSoft,
  fontSize: 14,
  marginRight: 6,
},

  });
