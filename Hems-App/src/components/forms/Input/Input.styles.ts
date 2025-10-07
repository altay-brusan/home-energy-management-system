import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

type Params = {
    focused: boolean;
    error: boolean;
};

export function getInputStyles ({ focused, error }: Params) {
    const mode = useThemeStore.getState().mode;
    const isDark = mode === 'dark';
    const C = getThemeColors(isDark);

    const placeholderColor = isDark ? 'rgba(255,255,255,0.5)' : '#9CA3AF';

    return {
        ...StyleSheet.create({
            container: { marginBottom: SPACING.md },
            label: {
                fontSize: 14,
                fontWeight: '500',
                color: isDark ? C.white : C.textSoft,
                marginBottom: 4,
            },
            input: {
                height: 48,
                borderWidth: 1,
                borderColor: isDark ? C.border : '#CBD5E1',
                borderRadius: 10,
                paddingHorizontal: SPACING.md,
                paddingVertical: SPACING.sm,
                fontSize: 16,
                color: isDark ? C.white : '#111827',
                backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : C.surface,
            },
            inputFocused: {
                borderColor: C.accent,


            },
            inputError: {
                borderColor: '#EF4444',
            },
            error: {
                fontSize: 12,
                color: '#EF4444',
                marginTop: 4,
            },
        }),
        placeholderColor,
    };
}
