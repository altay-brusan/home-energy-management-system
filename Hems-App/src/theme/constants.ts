import { getDesignTokens, DESIGN_TOKENS_LIGHT, DESIGN_TOKENS_DARK } from './design-tokens';

/**
 * Backward Compatible Color Export
 *
 * Bu fonksiyon design-tokens.ts dosyasından renkleri alır ve
 * mevcut kod ile uyumlu olması için flat yapıda export eder.
 *
 * Design tokens'ı değiştirmek için design-tokens.ts dosyasını düzenleyin.
 */
export function getThemeColors (isDark: boolean) {
  const tokens = getDesignTokens(isDark);

  // Flat export for backward compatibility
  return {
    // Brand colors
    accent: tokens.brand.accent,
    accentGlow: tokens.brand.accentGlow,

    // Background colors
    dark: tokens.background.page,
    surface: tokens.surface.card,
    cardBg: tokens.surface.card,
    buttonBg: tokens.button.secondaryBg,
    bgShadeLight: tokens.background.shade,
    bgShadeDark: tokens.background.shadeDark,

    // Text colors
    textSoft: tokens.text.secondary,

    // Border colors
    border: tokens.border.primary,

    // Semantic colors
    error: tokens.semantic.error,
    warning: tokens.semantic.warning,
    success: tokens.semantic.success,
    info: tokens.semantic.info,

    // State colors
    disabled: tokens.text.disabled,

    // Utility colors
    white: tokens.utility.white,
    black: tokens.utility.black,

    // Gray scale (backward compatibility)
    slate900: tokens.utility.slate900,
    slate700: tokens.utility.slate700,
    slate400: tokens.utility.slate400,
    gray100: tokens.utility.gray100,
    gray200: tokens.utility.gray200,
    gray50: tokens.utility.gray50,
  };
}

// Export for direct access to design tokens
export { getDesignTokens, DESIGN_TOKENS_LIGHT, DESIGN_TOKENS_DARK };

export const SPACING = {
  xs: 8,
  sm: 12,
  md: 16,
  lg: 24,
  xl: 32,
  xxl: 40,
};

export const TYPOGRAPHY = {
  xs: 12,
  sm: 14,
  md: 16,
  lg: 18,
  xl: 20,
  xxl: 24,
  xxxl: 32,
};

// Font families
export const FONTS = {
  regular: 'Montserrat-Regular',
  medium: 'Montserrat-Medium',
  semiBold: 'Montserrat-SemiBold',
  bold: 'Montserrat-Bold',
  light: 'Montserrat-Light',
};

