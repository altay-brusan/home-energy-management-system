import { StyleSheet, Platform, Dimensions } from 'react-native';
import { getThemeColors } from '../../../theme/constants';

export const makeNotificationScreenStyles = (isDark: boolean) => {
  const C = getThemeColors(isDark) as any;
  const width = Dimensions.get('window').width;
  const isSmall = width <= 380;
  const isVerySmall = width <= 340;

  const APP_BG = C?.appBg ?? (isDark ? '#1c222b' : '#F2F4F7');
  const TEXT = C?.text ?? (isDark ? '#E6EAF2' : '#111827');
  const MUTED = C?.textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280');
  const ACCENT = C?.accent ?? '#1DD1A1';
  const ACCENT_SOFT = C?.accentSoft ?? (isDark ? 'rgba(29,209,161,0.12)' : 'rgba(29,209,161,0.10)');

  const chipFontSize = isVerySmall ? 15 : isSmall ? 16 : 17;
  const chipLineHeight = Platform.OS === 'android' 
    ? (isVerySmall ? 22 : isSmall ? 24 : 26)
    : (isVerySmall ? 20 : isSmall ? 22 : 24);

  return StyleSheet.create({
    screen: { flex: 1, backgroundColor: APP_BG },
    pageScroll: { flexGrow: 1, paddingHorizontal: 0, paddingBottom: 16 },

    headerRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 12,
      paddingHorizontal: 16,
    },
    title: { fontSize: 22, fontWeight: '700', color: TEXT },
    headerBtn: {
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 999,
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)',
    },
    headerBtnText: { color: TEXT, fontSize: 15, fontWeight: '700' },

    filterSticky: { backgroundColor: APP_BG, paddingTop: 4, paddingBottom: 12, paddingHorizontal: 16 },
    chipsScrollView: {
      marginTop: 0,
    },
    chipsWrap: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
      paddingHorizontal: 0,
    },
    chip: {
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: 8,
      paddingHorizontal: 16,
      borderRadius: 20,
      backgroundColor: 'transparent',
      borderWidth: 0,
      flexShrink: 0,
    },
    chipActive: {
      backgroundColor: ACCENT,
      borderWidth: 0,
    },
    chipText: {
      color: isDark ? '#9CA3AF' : '#6B7280',
      fontWeight: '500',
      fontSize: 15,
      textAlign: 'center',
      includeFontPadding: false,
      ...(Platform.OS === 'android' && {
        textAlignVertical: 'center',
        paddingTop: 0,
        paddingBottom: 0,
      }),
    },
    chipTextActive: {
      color: '#FFFFFF',
      fontWeight: '600',
      fontSize: 15,
      textAlign: 'center',
      includeFontPadding: false,
      ...(Platform.OS === 'android' && {
        textAlignVertical: 'center',
        paddingTop: 0,
        paddingBottom: 0,
      }),
    },

    groupHeader: {
      marginTop: 20,
      marginBottom: 12,
      marginHorizontal: 0,
      color: TEXT,
      fontSize: 18,
      fontWeight: '700'
    },
    itemPress: {
      paddingVertical: 0,
      paddingHorizontal: 0,
    },
    itemRow: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      paddingVertical: 16,
      paddingHorizontal: 0,
      borderLeftWidth: 0,
      backgroundColor: isDark ? 'rgba(255,255,255,0.02)' : 'rgba(249,250,251,0.8)',
      borderRadius: 0,
      minHeight: 90,
    },
    itemContent: { flex: 1 },

    itemHeader: { 
      flexDirection: 'row', 
      justifyContent: 'space-between', 
      alignItems: 'flex-start',
      marginBottom: 4,
    },
    itemTitle: {
      color: TEXT,
      fontSize: 16,
      fontWeight: '700',
      flex: 1,
      flexShrink: 1,
      marginRight: 8,
      lineHeight: 22,
      marginBottom: 4,
    },
    itemTitleRead: {
      color: MUTED,
      opacity: 0.7,
    },

    itemMsg: {
      color: MUTED,
      fontSize: 14,
      marginBottom: 8,
      lineHeight: 20,
      fontWeight: '400',
    },
    itemMsgRead: {
      opacity: 0.6,
    },

    additionalDataContainer: {
      backgroundColor: isDark ? 'rgba(29,209,161,0.08)' : 'rgba(29,209,161,0.06)',
      borderRadius: 8,
      padding: 8,
      marginTop: 6,
      marginBottom: 4,
      borderLeftWidth: 3,
      borderLeftColor: ACCENT,
    },
    additionalDataText: {
      color: isDark ? '#E6EAF2' : '#374151',
      fontSize: 13,
      fontWeight: '500',
      fontFamily: 'monospace',
    },

    itemMetaRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginTop: 4,
    },
    metaLeft: { minWidth: 1 },
    itemMeta: {
      color: MUTED,
      fontSize: 12,
      textAlign: 'right',
      fontWeight: '400',
    },
    criticalText: { fontSize: 13, fontWeight: '700', color: '#EB5757' },

    statusPill: {
      paddingHorizontal: 12,
      paddingVertical: 5,
      borderRadius: 4,
      marginLeft: 8,
      alignSelf: 'center',
      flexShrink: 0,
      backgroundColor: ACCENT,
    },
    statusPillText: {
      fontSize: 11,
      fontWeight: '600',
      color: '#FFFFFF',
      textTransform: 'capitalize',
    },

    swipeLeft: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: ACCENT,
      borderRadius: 0,
      minHeight: 90,
      marginVertical: 0,
      marginLeft: 0,
    },
    swipeLeftText: {
      color: '#fff',
      fontWeight: '700',
      fontSize: 13,
    },

    swipeRight: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: '#E63946',
      borderRadius: 0,
      minHeight: 90,
      marginVertical: 0,
      marginRight: 0,
    },
    swipeRightText: {
      color: '#fff',
      fontWeight: '700',
      fontSize: 13,
    },

    emptyWrap: { flex: 1, alignItems: 'center', justifyContent: 'center', marginTop: 100 },
    emptyIcon: { fontSize: 64 },
    emptyText: { textAlign: 'center', color: MUTED, marginTop: 12, fontSize: 16, fontWeight: '600' },

    offlineBox: {
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)',
      padding: 8,
      borderRadius: 8,
      marginTop: 8,
    },
    offlineText: { color: isDark ? '#E6EAF2' : '#111827', fontSize: 13, textAlign: 'center' },

    flex1: { flex: 1 },

    separator: {
      height: 1,
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)',
      marginVertical: 0,
    },
  });
};
