import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';

export function getDashboardModeCardStyles(COLORS: Record<string, string>) {
    return StyleSheet.create({
        widget: {
            backgroundColor: COLORS.surface,
            borderRadius: 12,
            padding: SPACING.lg,
            paddingVertical: SPACING.xl,
            marginBottom: 24,
            borderWidth: 1,
            borderColor: COLORS.border,
            shadowColor: COLORS.black,
            shadowOpacity: 0.06,
            shadowRadius: 6,
            shadowOffset: { width: 0, height: 2 },
            elevation: 1,
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
