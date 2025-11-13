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
      marginBottom: 16,
      paddingHorizontal: 16,
      paddingVertical: 4,
    },
    title: {
      fontSize: 26,
      fontWeight: '700',
      color: TEXT,
      letterSpacing: -0.5,
    },
    headerBtn: {
      paddingHorizontal: 14,
      paddingVertical: 8,
      borderRadius: 12,
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.05)',
      minHeight: 36,
      justifyContent: 'center',
      alignItems: 'center',
    },
    headerBtnText: {
      color: TEXT,
      fontSize: 14,
      fontWeight: '600',
      letterSpacing: -0.2,
    },

    filterSticky: {
      backgroundColor: APP_BG,
      paddingTop: 4,
      paddingBottom: 16,
      paddingHorizontal: 16,
    },
    chipsScrollView: {
      marginTop: 0,
    },
    chipsWrap: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 10,
      paddingHorizontal: 0,
    },
    chip: {
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: 10,
      paddingHorizontal: 18,
      borderRadius: 22,
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.04)',
      borderWidth: 0,
      flexShrink: 0,
    },
    chipActive: {
      backgroundColor: ACCENT,
      borderWidth: 0,
    },
    chipText: {
      color: isDark ? '#D1D5DB' : '#6B7280',
      fontWeight: '600',
      fontSize: 15,
      textAlign: 'center',
      includeFontPadding: false,
      letterSpacing: -0.2,
      ...(Platform.OS === 'android' && {
        textAlignVertical: 'center',
        paddingTop: 0,
        paddingBottom: 0,
      }),
    },
    chipTextActive: {
      color: '#FFFFFF',
      fontWeight: '700',
      fontSize: 15,
      textAlign: 'center',
      includeFontPadding: false,
      letterSpacing: -0.2,
      ...(Platform.OS === 'android' && {
        textAlignVertical: 'center',
        paddingTop: 0,
        paddingBottom: 0,
      }),
    },

    groupHeader: {
      marginTop: 24,
      marginBottom: 12,
      marginHorizontal: 16,
      color: TEXT,
      fontSize: 19,
      fontWeight: '700',
      letterSpacing: -0.5,
    },
    itemPress: {
      paddingVertical: 0,
      paddingHorizontal: 0,
    },
    itemRow: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      paddingVertical: 18,
      paddingHorizontal: 16,
      borderLeftWidth: 0,
      backgroundColor: isDark ? 'rgba(32, 40, 53, 1)' : 'rgba(249,250,251,0.9)',
      borderRadius: 0,
      minHeight: 96,
    },
    itemContent: { flex: 1 },

    itemHeader: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'flex-start',
      marginBottom: 6,
    },
    itemTitle: {
      color: TEXT,
      fontSize: 16,
      fontWeight: '700',
      flex: 1,
      flexShrink: 1,
      marginRight: 10,
      lineHeight: 22,
      marginBottom: 4,
      letterSpacing: -0.3,
    },
    itemTitleRead: {
      color: MUTED,
      opacity: 0.7,
    },

    itemMsg: {
      color: MUTED,
      fontSize: 14,
      marginBottom: 10,
      lineHeight: 20,
      fontWeight: '400',
      letterSpacing: -0.1,
    },
    itemMsgRead: {
      opacity: 0.6,
    },

    additionalDataContainer: {
      backgroundColor: isDark ? 'rgba(29,209,161,0.1)' : 'rgba(29,209,161,0.08)',
      borderRadius: 10,
      padding: 10,
      marginTop: 8,
      marginBottom: 4,
      borderLeftWidth: 3,
      borderLeftColor: ACCENT,
    },
    additionalDataText: {
      color: isDark ? '#E6EAF2' : '#374151',
      fontSize: 13,
      fontWeight: '600',
      fontFamily: 'monospace',
      letterSpacing: -0.2,
    },

    itemMetaRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginTop: 6,
    },
    metaLeft: { minWidth: 1 },
    itemMeta: {
      color: MUTED,
      fontSize: 12,
      textAlign: 'right',
      fontWeight: '500',
      letterSpacing: -0.1,
    },
    criticalText: {
      fontSize: 13,
      fontWeight: '700',
      color: '#EB5757',
      letterSpacing: -0.2,
    },

    statusPill: {
      paddingHorizontal: 14,
      paddingVertical: 6,
      borderRadius: 8,
      marginLeft: 10,
      alignSelf: 'center',
      flexShrink: 0,
      backgroundColor: ACCENT,
    },
    statusPillText: {
      fontSize: 11,
      fontWeight: '700',
      color: '#FFFFFF',
      textTransform: 'capitalize',
      letterSpacing: 0.3,
    },

    swipeLeft: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: ACCENT,
      borderRadius: 0,
      minHeight: 96,
      marginVertical: 0,
      marginLeft: 0,
    },
    swipeLeftText: {
      color: '#fff',
      fontWeight: '700',
      fontSize: 13,
      letterSpacing: -0.2,
    },

    swipeRight: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: '#E63946',
      borderRadius: 0,
      minHeight: 96,
      marginVertical: 0,
      marginRight: 0,
    },
    swipeRightText: {
      color: '#fff',
      fontWeight: '700',
      fontSize: 13,
      letterSpacing: -0.2,
    },

    emptyWrap: {
      flex: 1,
      alignItems: 'center',
      justifyContent: 'center',
      marginTop: 120,
      paddingHorizontal: 32,
    },
    emptyIcon: { fontSize: 72, opacity: 0.8 },
    emptyText: {
      textAlign: 'center',
      color: MUTED,
      marginTop: 16,
      fontSize: 17,
      fontWeight: '600',
      letterSpacing: -0.3,
    },

    offlineBox: {
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.06)',
      padding: 10,
      borderRadius: 10,
      marginTop: 10,
    },
    offlineText: {
      color: isDark ? '#E6EAF2' : '#111827',
      fontSize: 13,
      textAlign: 'center',
      fontWeight: '500',
      letterSpacing: -0.1,
    },

    flex1: { flex: 1 },

    separator: {
      height: 1,
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)',
      marginVertical: 0,
    },
  });
};
