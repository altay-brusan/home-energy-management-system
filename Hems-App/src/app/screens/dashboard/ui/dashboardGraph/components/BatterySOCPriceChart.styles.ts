import { StyleSheet, Platform } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../../theme/constants';

export function makeBatterySOCPriceChartStyles (
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
        switchContainer: {
            flexDirection: 'row',
            gap: 8,
            marginBottom: SPACING.sm,
            paddingHorizontal: SPACING.sm,
            justifyContent: 'center',
            alignItems: 'center',
        },
        switchItem: {
            flexDirection: 'row',
            alignItems: Platform.OS === 'android' ? 'flex-start' : 'center',
            justifyContent: 'space-between',
            paddingHorizontal: 6,
            paddingVertical: Platform.OS === 'android' ? 6 : 4,
            backgroundColor: isDark ? COLORS.dark : COLORS.gray50,
            borderRadius: 3,
            height: Platform.OS === 'android' ? 28 : 24,
            minWidth: 60,
        },
        switchLabel: {
            fontSize: TYPOGRAPHY.xs,
            color: COLORS.textSoft,
            fontWeight: '500',
        },
        chartContainer: {
            height: 300, // Eski yükseklik
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
    });
}
