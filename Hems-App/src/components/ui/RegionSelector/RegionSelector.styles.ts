import { StyleSheet } from 'react-native';


export function makeRegionStyles (
  COLORS: {
    dark: string; surface: string; border: string; textSoft: string; white: string; black: string;
    accent: string; accentGlow: string; buttonBg: string; bgShadeLight: string; bgShadeDark: string;
  },
  IN: {
    dark: string; surface: string; border: string; textSoft: string; white: string; black: string;
    accent: string; accentGlow: string; buttonBg: string; bgShadeLight: string; bgShadeDark: string;
  },
  isDark: boolean
) {
  return StyleSheet.create({
    buttonContainer: { flexDirection: 'row', alignItems: 'center' },
    button: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: isDark ? '#191F28' : IN.surface,
      borderWidth: 1,
borderColor: COLORS.border,
      borderRadius: 16,
      paddingHorizontal: 16,
      paddingVertical: 10,
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.08,
      shadowRadius: 6,
      elevation: 3,
    },
    buttonContent: { flexDirection: 'row', alignItems: 'center', gap: 8 },
    emoji: { fontSize: 18, marginRight: 8 },
    regionText: {
      color: isDark ? IN.white : IN.black,
      fontWeight: '600',
      fontSize: 14,
    },
    timezoneText: {
      color: isDark ? 'rgba(255,255,255,0.6)' : '#6b7280',
      fontSize: 12,
    },
    chevron: {
       marginLeft: 8, 
       paddingHorizontal: 4 ,
       

    },
    chevronText: {
      color: isDark ? 'rgba(255,255,255,0.32)' : '#6b7280',
      fontSize: 12,
    },

    modalContainer: { flex: 1 },
    backdrop: { flex: 1 },
    backdropOverlay: {
      position: 'absolute', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0,0,0,0.7)',
    },
    modalContent: {
      position: 'absolute', bottom: 0, left: 0, right: 0,

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
      color: isDark ? IN.white : IN.black,
      marginBottom: 2,
    },
    subtitle: {
      fontSize: 13,
      color: isDark ? 'rgba(255,255,255,0.6)' : '#6b7280',
    },

    scrollView: { paddingHorizontal: 16, paddingBottom: 32 },
    regionsList: { marginTop: 6 },

    regionItem: {
      borderRadius: 16,
      padding: 14,
      borderWidth: 2,
      borderColor: isDark ? 'rgba(255,255,255,0.08)' : COLORS.border,
      backgroundColor: isDark ? 'rgba(255,255,255,0.04)' : COLORS.white,
      marginBottom: 12,
    },
    regionItemSelected: {
      backgroundColor: isDark ? 'rgba(6,182,212,0.15)' : '#ecfeff',
      borderColor: isDark ? 'rgba(6,182,212,0.38)' : '#a5f3fc',
    },
    regionItemPressed: {
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : '#f3f4f6',
    },

    regionContent: { flexDirection: 'row', alignItems: 'center' },
    regionIcon: {
      width: 48, height: 48, borderRadius: 12,
      backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : '#f3f4f6',
      alignItems: 'center', justifyContent: 'center', marginRight: 14,
    },
    regionEmoji: { fontSize: 22 },

    regionInfo: { flex: 1 },
    regionHeader: { flexDirection: 'row', alignItems: 'center' },
    regionLabel: {
      fontSize: 17, fontWeight: '700',
      color: isDark ? IN.white : IN.black,
    },
    regionLabelSelected: { color: IN.accent },

    activeBadge: {
      marginLeft: 8, paddingHorizontal: 8, paddingVertical: 2,
      borderRadius: 999,
      backgroundColor: isDark ? 'rgba(6,182,212,0.18)' : '#cffafe',
      borderWidth: 1, borderColor: IN.accent,
    },
    activeBadgeText: {
      color: isDark ? IN.accent : '#0e7490',
      fontSize: 12, fontWeight: '600',
    },

    regionMeta: { flexDirection: 'row', alignItems: 'center', marginTop: 6 },
    regionMetaText: {
      fontSize: 12,
      color: isDark ? 'rgba(255,255,255,0.56)' : '#6b7280',
    },
    dot: {
      width: 6, height: 6, borderRadius: 3,
      backgroundColor: isDark ? 'rgba(255,255,255,0.28)' : '#9ca3af',
      marginHorizontal: 8,
    },

    checkmark: {
      width: 28, height: 28, borderRadius: 14,
      backgroundColor: IN.accent,
      alignItems: 'center', justifyContent: 'center', marginLeft: 8,
    },
    checkmarkText: { color: IN.white, fontSize: 14, fontWeight: '700' },

    infoCard: {
      marginTop: 18, marginBottom: 16, padding: 14,
      borderRadius: 14,
      backgroundColor: isDark ? 'rgba(6,182,212,0.08)' : '#f3f4f6',
      borderWidth: 1,
      borderColor: isDark ? 'rgba(6,182,212,0.16)' : COLORS.border,
      flexDirection: 'row',
    },
    infoIcon: { fontSize: 18, marginRight: 10 },
    infoContent: { flex: 1 },
    infoTitle: {
      color: isDark ? '#67e8f9' : IN.black,
      fontSize: 14, fontWeight: '600', marginBottom: 4,
    },
    infoText: {
      color: isDark ? 'rgba(165,243,252,0.75)' : '#6b7280',
      fontSize: 13, lineHeight: 20,
    },
  });
}
