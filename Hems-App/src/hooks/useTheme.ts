import { useThemeStore } from '../store/themeStore';
import { getThemeColors, getDesignTokens } from '../theme/constants';

/**
 * Enhanced Theme Hook
 *
 * Provides access to theme mode and all design tokens.
 * Use tokens directly from this hook instead of calling getThemeColors manually.
 */
export function useTheme () {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';

  // Backward compatible colors (flat structure)
  const colors = getThemeColors(isDark);

  // Full design tokens for new components
  const tokens = getDesignTokens(isDark);

  return {
    mode,
    isDark,
    colors,      // Backward compatible: { accent, dark, surface, ... }
    tokens,      // Full design tokens: { brand, text, background, ... }
  };
}
