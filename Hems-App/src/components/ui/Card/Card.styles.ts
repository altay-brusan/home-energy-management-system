import { ViewStyle } from 'react-native';
import { getDesignTokens } from '../../../theme/constants';

type Variant = 'elevated' | 'outlined' | 'ghost' | 'surfaceDark';

export function getCardVariantStyle(
  variant: Variant,
  radius: number,
  isDark: boolean
): ViewStyle {
  const tokens = getDesignTokens(isDark);

  switch (variant) {
    case 'outlined':
      return {
        backgroundColor: tokens.surface.card,
        borderColor: tokens.border.primary,
        borderWidth: 1,
        borderRadius: radius,
        shadowColor: undefined,
        shadowOpacity: 0,
        shadowRadius: 0,
        shadowOffset: { width: 0, height: 0 },
        elevation: 0,
      };
    case 'ghost':
      return {
        backgroundColor: 'transparent',
        borderColor: 'transparent',
        borderWidth: 0,
        borderRadius: radius,
        shadowColor: undefined,
        shadowOpacity: 0,
        shadowRadius: 0,
        shadowOffset: { width: 0, height: 0 },
        elevation: 0,
      };
    case 'surfaceDark':
      return {
        backgroundColor: tokens.surface.card,
        borderColor: tokens.border.primary,
        borderWidth: 1,
        borderRadius: radius,
        shadowColor: tokens.utility.black,
        shadowOpacity: isDark ? 0.18 : 0.12,
        shadowRadius: 12,
        shadowOffset: { width: 0, height: 4 },
        elevation: 6,
      };
    case 'elevated':
    default:
      return {
        backgroundColor: tokens.surface.elevated,
        borderColor: tokens.border.primary,
        borderWidth: 1,
        borderRadius: radius,
        shadowColor: tokens.utility.black,
        shadowOpacity: isDark ? 0.18 : 0.08,
        shadowRadius: isDark ? 12 : 8,
        shadowOffset: { width: 0, height: 4 },
        elevation: isDark ? 6 : 3,
      };
  }
}
