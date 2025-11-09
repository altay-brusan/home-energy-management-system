import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardModeCardStyles(COLORS: Record<string, string>) {
    return StyleSheet.create({
        widget: {
            backgroundColor: COLORS.surface,
            borderRadius: 12,
            padding: SPACING.md,
            marginBottom: 12,
            borderWidth: 1,
            borderColor: COLORS.border,
        },
        headerRow: {
            flexDirection: 'row',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: SPACING.md,
        },
        title: {
            fontSize: TYPOGRAPHY.lg,
            fontWeight: '500',
            color: COLORS.textSoft,
        },
        modeRow: {
            flexDirection: 'row',
            alignItems: 'center',
            marginBottom: 4,
        },
        mode: {
            fontSize: TYPOGRAPHY.xl,
            fontWeight: '700',
            color: COLORS.accent,
        },
        desc: {
            fontSize: TYPOGRAPHY.md,
            color: COLORS.textSoft,
        },
        check: {
            fontSize: 32,
            fontWeight: 'bold',
            color: COLORS.accent,
            marginLeft: 20,
        },
    });
}
