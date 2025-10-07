import { useThemeStore } from '../store/themeStore';

export function useTheme () {
  const mode = useThemeStore((s) => s.mode);
  return {
    mode,
    isDark: mode === 'dark',
  };
}
