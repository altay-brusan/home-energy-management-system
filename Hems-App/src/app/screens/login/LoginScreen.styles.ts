import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function makeLoginStyles(
  COLORS: {
    dark: string;
    surface: string;
    border: string;
    textSoft: string;
    white: string;
    black: string;
    accent: string;
    accentGlow: string;
    buttonBg: string;
    bgShadeLight: string;
    bgShadeDark: string;
  },
  isDark: boolean
) {
  return StyleSheet.create({
    root: { flex: 1, backgroundColor: COLORS.dark },

    topBar: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
      marginTop: SPACING.sm,
      marginBottom: SPACING.xs,
    },

    themeToggle: { alignSelf: 'flex-start' },
    regionWrap: { alignSelf: 'flex-start' },
   
   signupLink: {
  marginTop: 8,
  textAlign: 'left',   
  color: COLORS.textSoft,
  fontSize: 14,
},
signupHighlight: {
  color: COLORS.accent,
  fontWeight: '700',
},

    regionPill: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
      paddingHorizontal: SPACING.sm,
      paddingVertical: SPACING.xs,
      borderRadius: 18,
      backgroundColor: COLORS.surface,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    pressed: { opacity: 0.85 },
    regionIcon: { fontSize: TYPOGRAPHY.md, color: COLORS.textSoft },
    regionText: { color: COLORS.textSoft, fontWeight: '600' },

    brand: {
      fontSize: TYPOGRAPHY.xxxl,
      fontWeight: '900',
      letterSpacing: 1,
      textAlign: 'center',
      marginBottom: SPACING.lg,
      color: isDark ? COLORS.white : COLORS.black,
      textShadowColor: COLORS.accentGlow,
      textShadowOffset: { width: 0, height: 0 },
      textShadowRadius: 12,
    },

    centerWrap: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
    },

    card: {
      width: '92%',
      maxWidth: 560,
      borderRadius: 18,
      minHeight: 420,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 10 },
      shadowOpacity: 0.35,
      shadowRadius: 18,
      elevation: 50, // Çok yüksek elevation değeri
      zIndex: 100, // Çok yüksek z-index değeri
    },

    scrollContent: {
      flexGrow: 1,
      justifyContent: 'center',
      paddingVertical: SPACING.md,
    },
    cardInner: { flex: 1 },

    formWrap: { flexGrow: 1, justifyContent: 'center' },
    form: { gap: SPACING.sm, paddingHorizontal: 4, alignSelf: 'stretch' },

    forgot: {
      color: COLORS.accent,
      fontWeight: '700',
      marginTop: SPACING.xs,
      marginBottom: SPACING.xs,
    },

    primaryBtn: {
      minHeight: 56,
      height: 'auto',
      borderRadius: 16,
      paddingVertical: SPACING.md,
    },
    loginBtn: {
      minHeight: 56,
      height: 'auto',
      borderRadius: 16,
      paddingVertical: SPACING.md,
    },
    loginText: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      lineHeight: TYPOGRAPHY.lg * 1.4,
      textAlign: 'center',
    },
    loginBtnDisabled: {
      opacity: 0.6,
    },

    tosRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.xs,
      paddingHorizontal: 4,
      marginTop: SPACING.md,
    },
    checkbox: {
      width: 18,
      height: 18,
      borderRadius: 4,
      borderWidth: 1.4,
      borderColor: isDark ? COLORS.border : '#111827',
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: 'transparent',
    },
    checkboxChecked: {
      backgroundColor: COLORS.accent,
      borderColor: COLORS.accent,
    },
    checkIcon: {
      color: '#fff',
      fontSize: 12,
      lineHeight: 12,
    },
    tosText: { color: COLORS.textSoft, flex: 1, flexWrap: 'wrap' },
    tosLink: { color: COLORS.accent, fontWeight: '700' },

    bgTopShade: {
      position: 'absolute',
      top: -110,
      left: -80,
      width: 260,
      height: 260,
      borderRadius: 130,
      backgroundColor: isDark ? COLORS.bgShadeLight :COLORS.border,
      zIndex: -1, // Card'ın çok altında kalması için
    },
    bgBottomShade: {
      position: 'absolute',
      bottom: -140,
      right: -100,
      width: 300,
      height: 300,
      borderRadius: 150,
      backgroundColor: isDark ? COLORS.bgShadeLight :COLORS.border,
      zIndex: -1, // Card'ın çok altında kalması için
    },
  });
}
