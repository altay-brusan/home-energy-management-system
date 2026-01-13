import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getGlobalErrorStyles() {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return StyleSheet.create({
    wrap: {
      position: 'absolute',
      top: SPACING.xl,
      left: 12,
      right: 12,
      zIndex: 999,
    },
    card: {
      backgroundColor: COLORS.dark,
      borderColor: COLORS.border,
      borderWidth: 1,
      borderRadius: 12,
      padding: SPACING.md,
    },
    title: { color: COLORS.textSoft, fontWeight: '800', marginBottom: 4 },
    msg: { color: COLORS.textSoft },
    meta: { color: 'rgba(255,255,255,0.7)', marginTop: 4, fontSize: 12 },
    retryText: { 
      color: COLORS.accent, 
      marginTop: 8, 
      fontSize: 14, 
      fontWeight: '600',
      textAlign: 'center'
    },
  });
}
