import { StyleSheet } from 'react-native';
import { getThemeColors } from '../../../theme/constants';

export const makeNotificationScreenStyles = (isDark: boolean) => {
  const C = getThemeColors(isDark) as any;

  const APP_BG = C?.appBg ?? (isDark ? '#1c222b' : '#F2F4F7');
  const TEXT = C?.text ?? (isDark ? '#E6EAF2' : '#111827');
  const MUTED = C?.textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280');
  const ACCENT = C?.accent ?? '#1DD1A1';
  const ACCENT_SOFT = C?.accentSoft ?? (isDark ? 'rgba(29,209,161,0.12)' : 'rgba(29,209,161,0.10)');

  return StyleSheet.create({
    screen: { flex: 1, backgroundColor: APP_BG },
    pageScroll: { flexGrow: 1, paddingHorizontal: 16, paddingBottom: 16 },

    headerRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: 12,
    },
    title: { fontSize: 22, fontWeight: '700', color: TEXT },
    headerBtn: {
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 999,
      backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)',
    },
    headerBtnText: { color: TEXT, fontSize: 15, fontWeight: '700' },

    filterSticky: { backgroundColor: APP_BG, paddingTop: 4, paddingBottom: 8, paddingHorizontal: 16 },
    filterRow: { flexDirection: 'row', alignItems: 'center' },
    chipsWrap: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      gap: 8,
      marginTop: 12,
    },
    chip: {
      flex: 1,
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: 10,
      borderRadius: 999,
      backgroundColor: 'transparent',
      borderWidth: StyleSheet.hairlineWidth,
      borderColor: isDark ? 'rgba(255,255,255,0.08)' : '#000',
    },
    chipActive: { backgroundColor: ACCENT_SOFT, borderColor: ACCENT },
    chipText: { color: MUTED, fontWeight: '700', fontSize: 15 },
    chipTextActive: { color: isDark ? TEXT : ACCENT, fontWeight: '800' },

    groupHeader: { marginTop: 14, marginBottom: 6, color: MUTED, fontSize: 18, fontWeight: '700' },
    itemPress: { paddingVertical: 2 },
    itemRow: {
      flexDirection: 'row',
      alignItems: 'flex-start',
      paddingVertical: 16,
      paddingHorizontal: 16,
      borderLeftWidth: 5,
    },
    itemContent: { flex: 1 },

    itemHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
    itemTitle: { color: TEXT, fontSize: 17, fontWeight: '600', marginBottom: 4 },

    itemMsg: { color: MUTED, fontSize: 15, marginBottom: 6, lineHeight: 20 },

    itemMetaRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginTop: 4,
    },
    metaLeft: { minWidth: 1 },
    itemMeta: { color: MUTED, fontSize: 13, textAlign: 'right' },
    criticalText: { fontSize: 13, fontWeight: '700', color: '#EB5757' },

    statusPill: {
      paddingHorizontal: 10,
      paddingVertical: 4,
      borderRadius: 999,
      marginLeft: 8,
      alignSelf: 'center',
    },
    statusPillText: { fontSize: 14, fontWeight: '700' },

    swipeLeft: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: ACCENT,
      borderTopLeftRadius: 10,
      borderBottomLeftRadius: 10,
    },
    swipeLeftText: { color: '#fff', fontWeight: '700' },

    swipeRight: {
      width: 80,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: '#E63946',
      borderTopRightRadius: 10,
      borderBottomRightRadius: 10,
    },
    swipeRightText: { color: '#fff', fontWeight: '700' },

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

    separator: { height: 1, backgroundColor: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)', marginLeft: 12 },
  });
};
