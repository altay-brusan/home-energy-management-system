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
      backgroundColor: isDark ? '#191F28' : COLORS.surface,
      borderWidth: 1.5,
      borderColor: COLORS.border,
      borderRadius: 20,
      paddingHorizontal: 16,
      paddingVertical: 12,
      shadowColor: COLORS.black,
      shadowOpacity: 0.15,
      shadowRadius: 6,
      shadowOffset: { width: 0, height: 2 },
      elevation: 4,
    },
    buttonContent: { flexDirection: 'row', alignItems: 'center', gap: 8 },
    emoji: { fontSize: 20 },
    textContainer: { alignItems: 'center' },
    languageText: {
      color: isDark ? COLORS.white : COLORS.black,
      fontWeight: '700',
      fontSize: 16,
    },
    descriptionText: {
      color: isDark ? 'rgba(255,255,255,0.6)' : '#6b7280',
      fontSize: 12,
      marginTop: -1,
    },
    chevron: {
      marginLeft: 8, 
      paddingHorizontal: 4,
    },
    chevronText: {
      color: isDark ? 'rgba(255,255,255,0.32)' : '#6b7280',
      fontSize: 12,
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
      backgroundColor: 'rgba(0,0,0,0.7)',
    },
    modalContent: {
      position: 'absolute',
      bottom: 0,
      left: 0,
      right: 0,
    },
    sheet: {
      backgroundColor: isDark ? COLORS.dark : COLORS.white,
      borderTopLeftRadius: 32,
      borderTopRightRadius: 32,
      overflow: 'hidden',
      borderWidth: 1,
      borderColor: isDark ? 'rgba(255,255,255,0.06)' : COLORS.border,
    },
    handleContainer: { alignItems: 'center', paddingVertical: 12 },
    handle: {
      width: 56, height: 6, borderRadius: 3,
      backgroundColor: isDark ? 'rgba(255,255,255,0.18)' : '#d1d5db',
    },
    header: { paddingHorizontal: 24, paddingBottom: 12 },
    title: {
      fontSize: 22, fontWeight: '700',
      color: isDark ? COLORS.white : COLORS.black,
      marginBottom: 2,
    },
    subtitle: {
      fontSize: 13,
      color: isDark ? 'rgba(255,255,255,0.6)' : '#6b7280',
    },
    scrollView: { paddingHorizontal: 16, paddingBottom: 32 },
    languagesList: { marginTop: 6 },
    languageItem: {
      borderRadius: 16,
      padding: 14,
      borderWidth: 2,
      borderColor: isDark ? 'rgba(255,255,255,0.08)' : COLORS.border,
      backgroundColor: isDark ? 'rgba(255,255,255,0.04)' : COLORS.white,
      marginBottom: 12,
    },
    languageItemSelected: {
      backgroundColor: isDark ? 'rgba(6,182,212,0.15)' : '#ecfeff',
      borderColor: isDark ? 'rgba(6,182,212,0.38)' : '#a5f3fc',
    },
    languageItemPressed: {
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : '#f3f4f6',
    },
    languageItemContent: { flexDirection: 'row', alignItems: 'center' },
    languageEmoji: {
      width: 48, height: 48, borderRadius: 12,
      backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : '#f3f4f6',
      alignItems: 'center', justifyContent: 'center', marginRight: 14,
    },
    languageEmojiText: { fontSize: 22 },
    languageInfo: { flex: 1 },
    languageHeader: { flexDirection: 'row', alignItems: 'center', gap: 12 },
    languageLabel: {
      fontSize: 16, fontWeight: '600',
      color: isDark ? COLORS.white : COLORS.black,
      marginBottom: 2,
      flex: 1,
    },
    languageLabelSelected: {
      color: isDark ? '#06b6d4' : '#0891b2',
    },
    languageDescription: {
      fontSize: 13,
      color: isDark ? 'rgba(255,255,255,0.6)' : '#6b7280',
    },
    languageDescriptionSelected: {
      color: isDark ? 'rgba(6,182,212,0.8)' : '#0891b2',
    },
    selectedIndicator: {
      width: 20, height: 20, borderRadius: 10,
      backgroundColor: isDark ? '#06b6d4' : '#0891b2',
      alignItems: 'center', justifyContent: 'center',
    },
    selectedIcon: {
      color: COLORS.white,
      fontSize: 12, fontWeight: 'bold',
    },
  });
}
