import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function getSignUpStyles(
  COLORS: Record<string, string>,
  isDark: boolean
) {
  return StyleSheet.create({
    root: {
      flex: 1,
      backgroundColor: COLORS.dark,
    },

    topBar: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: SPACING.md,
      paddingTop: SPACING.sm,
      paddingBottom: SPACING.xs,
    },

    themeToggle: {
      alignSelf: 'flex-start',
    },

    regionWrap: {
      alignSelf: 'flex-end',
    },

    content: {
      flex: 1,
      paddingHorizontal: SPACING.md,
      position: 'relative', // Oval görseller için relative positioning
    },

    keyboardAvoider: {
      flex: 1,
    },

    scrollContent: {
      paddingVertical: SPACING.lg,
      minHeight: '100%', // Minimum yükseklik
    },

    card: {
      width: '100%',
      maxWidth: 520,
      alignSelf: 'center',
      borderRadius: 20,
      backgroundColor: COLORS.cardBg,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 12 },
      shadowOpacity: isDark ? 0.24 : 0.12,
      shadowRadius: 20,
      elevation: 50, // Çok yüksek elevation değeri
      zIndex: 100, // Çok yüksek z-index değeri
      minHeight: 400, // Minimum yükseklik
    },

    cardInner: {
      flex: 1,
    },

    brand: {
      fontSize: TYPOGRAPHY.xxxl,
      fontWeight: '800',
      letterSpacing: 0.8,
      textAlign: 'center',
      marginBottom: SPACING.lg,
      color: isDark ? COLORS.white : COLORS.slate900,
    },

    formWrapper: {
      marginTop: SPACING.sm,
    },

    form: {
      alignSelf: 'stretch',
    },

    primaryButton: {
      marginTop: SPACING.md,
      minHeight: 54,
      borderRadius: 14,
    },

    secondaryButton: {
      marginTop: SPACING.sm,
      minHeight: 54,
      borderRadius: 14,
    },

    bgTopShade: {
      position: 'absolute',
      top: -110,
      left: -80,
      width: 260,
      height: 260,
      borderRadius: 130,
      backgroundColor: COLORS.bgShadeLight,
      zIndex: -1, // Card'ın çok altında kalması için
    },

    bgBottomShade: {
      position: 'absolute',
      bottom: -140,
      right: -100,
      width: 300,
      height: 300,
      borderRadius: 150,
      backgroundColor: COLORS.bgShadeLight, // Üstteki oval ile aynı renk
      zIndex: -1, // Card'ın çok altında kalması için
    },
  });
}

export type SignUpStyles = ReturnType<typeof getSignUpStyles>;
