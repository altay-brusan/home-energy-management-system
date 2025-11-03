import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function getForgotPasswordStyles(
  COLORS: Record<string, string>,
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

    regionWrap: { alignSelf: 'flex-end' },

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
      minHeight: 320,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 10 },
      shadowOpacity: 0.18,
      shadowRadius: 12,
      elevation: 20, // Oval görselin üstüne çıkması için artırıldı
      zIndex: 10, // iOS için z-index eklendi
    },

    scrollContent: {
      flexGrow: 1,
      justifyContent: 'center',
      paddingVertical: SPACING.md,
    },
    cardInner: { flex: 1 },

    brand: {
      fontSize: TYPOGRAPHY.xxl,
      fontWeight: '900',
      letterSpacing: 1,
      textAlign: 'center',
      marginBottom: SPACING.xs,
      color: isDark ? COLORS.white : COLORS.black,
      textShadowColor: COLORS.accentGlow,
      textShadowOffset: { width: 0, height: 0 },
      textShadowRadius: 8,
    },

    subtitle: {
      fontSize: TYPOGRAPHY.sm,
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      textAlign: 'center',
      marginBottom: SPACING.lg,
      paddingHorizontal: SPACING.md,
      lineHeight: 20,
    },

    formWrap: { flexGrow: 1, justifyContent: 'center' },
    form: { gap: SPACING.sm, paddingHorizontal: 4, alignSelf: 'stretch' },

    primaryBtn: {
      height: 56,
      borderRadius: 16,
    },
    loginBtn: {
      height: 56,
      borderRadius: 16,
      backgroundColor: COLORS.buttonBg,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    loginText: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      letterSpacing: 0.3,
      color: COLORS.textSoft,
    },

    bgTopShade: {
      position: 'absolute',
      top: -110,
      left: -80,
      width: 260,
      height: 260,
      borderRadius: 130,
    backgroundColor: isDark ? COLORS.bgShadeLight :COLORS.border,
      zIndex: 1, // Card'ın altında kalması için
    },
    bgBottomShade: {
      position: 'absolute',
      bottom: -140,
      right: -100,
      width: 300,
      height: 300,
      borderRadius: 150,
       backgroundColor: isDark ? COLORS.bgShadeLight :COLORS.border, // Üstteki oval ile aynı renk
      zIndex: 1, // Card'ın altında kalması için
    },
  });
}
