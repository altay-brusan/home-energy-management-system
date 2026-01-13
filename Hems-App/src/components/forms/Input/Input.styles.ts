import { StyleSheet } from 'react-native';
import { getDesignTokens, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

type Params = {
    focused: boolean;
    error: boolean;
};

export function getInputStyles ({ focused, error }: Params) {
    const mode = useThemeStore.getState().mode;
    const isDark = mode === 'dark';
    const tokens = getDesignTokens(isDark);

    return {
        ...StyleSheet.create({
            container: { marginBottom: SPACING.md },
            label: {
                fontSize: 14,
                fontWeight: '500',
                color: tokens.input.label,
                marginBottom: 4,
            },
            input: {
                height: 48,
                borderWidth: 1,
                borderColor: tokens.input.border,
                borderRadius: 10,
                paddingHorizontal: SPACING.md,
                paddingVertical: SPACING.sm,
                fontSize: 16,
                color: tokens.input.text,
                backgroundColor: tokens.input.bg,
            },
            inputFocused: {
                borderColor: tokens.input.borderFocus,
            },
            inputError: {
                borderColor: tokens.input.borderError,
            },
            error: {
                fontSize: 12,
                color: tokens.input.error,
                marginTop: 4,
            },
        }),
        placeholderColor: tokens.input.placeholder,
    };
}
