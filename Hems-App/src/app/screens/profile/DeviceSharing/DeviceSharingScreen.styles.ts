import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../theme/constants';

export const makeDeviceSharingStyles = (
  COLORS: Record<string, string>,
  isDark: boolean
) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.dark,
    },
    pageScroll: {
      flexGrow: 1,
      paddingHorizontal: SPACING.lg,
      paddingBottom: SPACING.lg,
    },
    title: {
      fontSize: 22,
      fontWeight: '700',
      marginBottom: SPACING.md,
      textAlign: 'center',
      color: COLORS.accent,
      flexWrap: 'nowrap',
      includeFontPadding: false,
      lineHeight: 28,
    },
    inputGroup: {
      width: '100%',
      gap: 12,
      marginBottom: 18,
    },
    row: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingVertical: SPACING.lg,
      paddingHorizontal: SPACING.lg,
      minHeight: 88,
      marginBottom: SPACING.md,  
    },
    leftCol: { flex: 1, justifyContent: 'center' },
    rightCol: { alignItems: 'flex-end', justifyContent: 'space-between', marginLeft: 12 },
    email: {
  color: isDark ? COLORS.white : COLORS.textSoft,
  fontWeight: '700',
  fontSize: 16,
  flexShrink: 1,
  flexWrap: 'nowrap',
  width: '80%',
},
    status: { fontSize: 13, fontWeight: '700' },
    pending: { color: COLORS.error },
    accepted: { color: COLORS.info },
    rejected: { color: COLORS.error },
    since: { fontSize: 12, fontWeight: '600', color: COLORS.success, marginTop: 6, textAlign: 'right' },
    ownerBadge: { fontSize: 13, fontWeight: '800', color: COLORS.error, marginBottom: 4 },
    messageBox: {
      marginBottom: SPACING.sm,
      marginTop: 2,
      padding: SPACING.md,
      borderRadius: 8,
      backgroundColor: isDark ? COLORS.border : COLORS.surface,
      alignItems: 'center',
    },
    messageText: { fontSize: 15, fontWeight: '600' },
    errorText: { color: isDark ? COLORS.error : '#dc2626' },
    successText: { color: isDark ? COLORS.success : '#059669' },
    buttonRow: { flexDirection: 'row', justifyContent: 'space-between', gap: 12 },
    saveBtn: { flex: 1, backgroundColor: COLORS.accent },
    saveBtnText: { color: COLORS.white, fontWeight: '700', fontSize: 16 },
    cancelBtn: { flex: 1, backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : COLORS.bgShadeDark },
    cancelBtnText: { color: isDark ? COLORS.white : COLORS.textSoft, fontWeight: '700', fontSize: 16 },
    separator: {
      height: 0, 
    },
    emptyText: {
  textAlign: 'center',
  color: COLORS.textSoft,
  fontSize: 15,
  fontWeight: '500',
  marginTop: 32,
  opacity: 0.8,
},
  });
