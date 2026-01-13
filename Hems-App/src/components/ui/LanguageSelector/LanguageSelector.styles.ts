import { StyleSheet, Dimensions } from 'react-native';

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

export function makeLanguageSelectorStyles(
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
    primary: string;
    text: string;
    textSecondary: string;
  },
  isDark: boolean
) {
  return StyleSheet.create({
    buttonContainer: { flexDirection: 'row', alignItems: 'center' },
    button: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.04)',
      borderRadius: 12,
      paddingHorizontal: 12,
      paddingVertical: 8,
    },
    buttonContent: { flexDirection: 'row', alignItems: 'center', gap: 6 },
    emoji: { fontSize: 18 },
    textContainer: { alignItems: 'center' },
    languageText: {
      color: COLORS.textSoft,
      fontWeight: '500',
      fontSize: 14,
    },
    descriptionText: {
      color: isDark ? 'rgba(255,255,255,0.6)' : '#6b7280',
      fontSize: 12,
      marginTop: -1,
    },
    chevron: {
      marginLeft: 4,
      paddingHorizontal: 2,
    },
    chevronText: {
      color: isDark ? 'rgba(255,255,255,0.4)' : 'rgba(0,0,0,0.4)',
      fontSize: 10,
    },

    modalContainer: {
      flex: 1,
    },
    backdrop: {
      flex: 1,
    },
    backdropOverlay: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.6)',
    },
    modalContent: {
      position: 'absolute',
      bottom: 0,
      left: 0,
      right: 0,
    },
    sheet: {
      backgroundColor: isDark ? COLORS.dark : COLORS.white,
      borderTopLeftRadius: 24,
      borderTopRightRadius: 24,
      overflow: 'hidden',
    },
    handleContainer: { alignItems: 'center', paddingVertical: 12 },
    handle: {
      width: 40, height: 4, borderRadius: 2,
      backgroundColor: isDark ? 'rgba(255,255,255,0.2)' : 'rgba(0,0,0,0.2)',
    },
    header: { paddingHorizontal: 20, paddingBottom: 16 },
    title: {
      fontSize: 18, fontWeight: '600',
      color: COLORS.textSoft,
      marginBottom: 4,
    },
    subtitle: {
      fontSize: 14,
      color: isDark ? 'rgba(255,255,255,0.5)' : 'rgba(0,0,0,0.5)',
      fontWeight: '400',
    },
    scrollView: { paddingHorizontal: 0, paddingBottom: 32 },
    languagesList: { marginTop: 0 },
    languageItem: {
      borderRadius: 0,
      padding: 16,
      borderWidth: 0,
      borderBottomWidth: 1,
      borderBottomColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)',
      backgroundColor: isDark ? 'rgba(255,255,255,0.03)' : '#FFFFFF',
      marginBottom: 0,
    },
    languageItemSelected: {
      backgroundColor: isDark ? 'rgba(6,182,212,0.1)' : 'rgba(6,182,212,0.05)',
      borderBottomWidth: 1,
      borderBottomColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)',
    },
    languageItemPressed: {
      backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.02)',
    },
    languageItemContent: { flexDirection: 'row', alignItems: 'center' },
    languageEmoji: {
      width: 40, height: 40, borderRadius: 20,
      backgroundColor: 'transparent',
      alignItems: 'center', justifyContent: 'center', marginRight: 12,
    },
    languageEmojiText: { fontSize: 24 },
    languageInfo: { flex: 1, justifyContent: 'center' },
    languageHeader: { flexDirection: 'row', alignItems: 'center', gap: 8, minHeight: 24 },
    languageLabel: {
      fontSize: 15, fontWeight: '500',
      color: COLORS.textSoft,
      marginBottom: 0,
      flex: 1,
      lineHeight: 20,
    },
    languageLabelSelected: {
      color: COLORS.accent,
      fontWeight: '600',
    },
    languageDescription: {
      fontSize: 13,
      color: isDark ? 'rgba(255,255,255,0.5)' : 'rgba(0,0,0,0.5)',
      marginTop: 2,
    },
    languageDescriptionSelected: {
      color: isDark ? 'rgba(6,182,212,0.7)' : 'rgba(6,182,212,0.8)',
    },
    selectedIndicator: {
      width: 22, height: 22, borderRadius: 11,
      backgroundColor: COLORS.accent,
      alignItems: 'center',
      justifyContent: 'center',
    },
    selectedIcon: {
      color: COLORS.white,
      fontSize: 14,
      fontWeight: 'bold',
      lineHeight: 22,
      textAlign: 'center',
    },
  });
}
