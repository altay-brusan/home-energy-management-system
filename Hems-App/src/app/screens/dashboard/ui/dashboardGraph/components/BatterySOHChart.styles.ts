import { StyleSheet, Platform } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export function makeBatterySOHChartStyles (
    isDark: boolean,
    COLORS: Record<string, string>
) {
    return StyleSheet.create({
        container: {
            width: '100%',
            backgroundColor: COLORS.surface,
            borderRadius: 14,
            padding: SPACING.md,
            marginBottom: SPACING.lg,
            borderWidth: 1,
            borderColor: COLORS.border,
        },
        header: {
            marginBottom: SPACING.lg,
        },
        title: {
            fontSize: TYPOGRAPHY.lg,
            fontWeight: '600',
            color: COLORS.textSoft,
            marginBottom: SPACING.md,
        },
        chartContainer: {
            height: 300, // Dikey yazılar için daha yüksek
            marginBottom: SPACING.md,
        },
        noDataText: {
            fontSize: 16,
            color: COLORS.textSoft,
            textAlign: 'center',
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
    });
}