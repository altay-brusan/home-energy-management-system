import { ViewStyle } from 'react-native';
import { getThemeColors } from '../../../theme/constants';

type Variant = 'elevated' | 'outlined' | 'ghost' | 'surfaceDark';

export function getCardVariantStyle(
  variant: Variant,
  radius: number,
  COLORS: ReturnType<typeof getThemeColors>,
  isDark: boolean
): ViewStyle {
  switch (variant) {
    case 'outlined':
      return {
        backgroundColor: isDark ? COLORS.dark : COLORS.surface,
        borderColor: COLORS.border,
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
        backgroundColor: isDark ? COLORS.dark : COLORS.cardBg ?? COLORS.white,
        borderColor: COLORS.border,
        borderWidth: 1,
        borderRadius: radius,
        shadowColor: COLORS.black,
        shadowOpacity: 0.18,
        shadowRadius: 12,
        shadowOffset: { width: 0, height: 4 },
        elevation: 6,
      };
    case 'elevated':
    default:
      return {
        backgroundColor: isDark ? COLORS.dark : COLORS.cardBg ?? COLORS.white,
        borderColor: COLORS.border,
        borderWidth: 1,
        borderRadius: radius,
        shadowColor: COLORS.black,
        shadowOpacity: isDark ? 0.18 : 0.08,
        shadowRadius: isDark ? 12 : 8,
        shadowOffset: { width: 0, height: 4 },
        elevation: isDark ? 6 : 3,
      };
  }
}
