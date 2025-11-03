import { StyleSheet, Platform, Dimensions } from 'react-native';
import { getThemeColors } from '../../../theme/constants';

export const makeTrashScreenStyles = (isDark: boolean, COLORS: any) => {
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
    pageScroll: { paddingHorizontal: 16, paddingBottom: 16 },

    headerRow: {
      flexDirection: 'column',
      alignItems: 'flex-start',
      justifyContent: 'space-between',
      marginBottom: 12,
    },
    headerTop: {
      flexDirection: 'row',
      alignItems: 'center',
      width: '100%',
      marginBottom: 8,
    },
    backButton: {
      padding: 8,
      marginRight: 12,
      borderRadius: 8,
      backgroundColor: isDark ? COLORS.surface : COLORS.bgShadeDark,
    },
    backButtonText: {
      color: isDark ? COLORS.white : COLORS.textPrimary,
    },
    title: { fontSize: 22, fontWeight: '700', color: TEXT, flex: 1 },

    chipsScrollView: {
      marginBottom: 12,
      marginTop: 4,
    },
    filterRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
      paddingRight: 16,
    },
    chip: {
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: isVerySmall ? 10 : isSmall ? 12 : 14,
      paddingHorizontal: isVerySmall ? 16 : isSmall ? 18 : 20,
      marginHorizontal: 2,
      borderRadius: 999,
      backgroundColor: 'transparent',
      borderWidth: StyleSheet.hairlineWidth,
      borderColor: isDark ? COLORS.buttonBg : COLORS.black,
      minHeight: isVerySmall ? 42 : isSmall ? 46 : 50,
      flexShrink: 0,
    },
    chipActive: {
      backgroundColor: ACCENT_SOFT,
      borderColor: ACCENT,
    },
    chipText: {
      color: MUTED,
      fontWeight: '600',
      fontSize: chipFontSize,
      lineHeight: chipLineHeight,
      textAlign: 'center',
      includeFontPadding: false,
      ...(Platform.OS === 'android' && {
        textAlignVertical: 'center',
        paddingTop: 0,
        paddingBottom: 0,
      }),
    },
    chipTextActive: {
      color: isDark ? TEXT : ACCENT,
      fontWeight: '700',
      fontSize: chipFontSize,
      lineHeight: chipLineHeight,
      textAlign: 'center',
      includeFontPadding: false,
      ...(Platform.OS === 'android' && {
        textAlignVertical: 'center',
        paddingTop: 0,
        paddingBottom: 0,
      }),
    },

    groupHeader: { marginTop: 14, marginBottom: 6, color: MUTED, fontSize: 18, fontWeight: '700' },

    itemPress: { paddingVertical: 2 },
    itemRow: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      paddingVertical: 16,
      paddingHorizontal: 16,
      borderLeftWidth: 5,
      backgroundColor: APP_BG,
      minHeight: 80,
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
      fontSize: isVerySmall ? 15 : isSmall ? 16 : 17, 
      fontWeight: '600', 
      flex: 1,
      flexShrink: 1,
      marginRight: isVerySmall ? 6 : isSmall ? 8 : 10,
      lineHeight: isVerySmall ? 20 : isSmall ? 22 : 24,
    },

    itemMsg: { color: MUTED, fontSize: 15, marginBottom: 6, lineHeight: 20 },

    itemMetaRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginTop: 4,
    },
    metaLeft: { minWidth: 1 },
    itemMeta: { color: MUTED, fontSize: 13, textAlign: 'right' },
    criticalText: { fontSize: 13, fontWeight: '700', color: COLORS.error },

    statusPill: {
      paddingHorizontal: isVerySmall ? 8 : isSmall ? 9 : 10,
      paddingVertical: isVerySmall ? 3 : 4,
      borderRadius: 999,
      marginLeft: 0,
      alignSelf: 'center',
      flexShrink: 0,
      backgroundColor: isDark ? 'rgba(239,68,68,0.12)' : 'rgba(239,68,68,0.10)',
    },
    statusPillText: { 
      fontSize: isVerySmall ? 12 : isSmall ? 13 : 14, 
      fontWeight: '700', 
      color: '#EF4444',
    },

    swipeLeft: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: ACCENT,
      borderTopLeftRadius: 10,
      borderBottomLeftRadius: 10,
      minHeight: 80,
    },
    swipeButtonContent: {
      justifyContent: 'center',
      alignItems: 'center',
      gap: 4,
    },
    swipeIcon: {
      marginBottom: 2,
    },
    swipeLeftText: { 
      color: '#fff', 
      fontWeight: '700',
      fontSize: 14,
    },

    swipeRight: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: '#E63946',
      borderTopRightRadius: 10,
      borderBottomRightRadius: 10,
      minHeight: 80,
    },
    swipeRightText: { 
      color: '#fff', 
      fontWeight: '700',
      fontSize: 14,
    },

    separator: {
      height: 1,
      backgroundColor:COLORS.bgShadeDark,
      marginLeft: 12,
    },
emptyWrap: { 
  flex: 1, 
  alignItems: 'center', 
  justifyContent: 'center', 
  marginTop: 260,  
},
emptyIcon: { 
  fontSize: 64 
},
emptyText: { 
  textAlign: 'center', 
  color: MUTED, 
  marginTop: 12, 
  fontSize: 16, 
  fontWeight: '600' 
},
  });
};

