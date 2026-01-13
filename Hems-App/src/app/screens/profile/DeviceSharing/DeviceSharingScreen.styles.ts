import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../theme/constants';

export const makeDeviceSharingStyles = (
  COLORS: Record<string, string>,
  isDark: boolean
) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: isDark ? COLORS.dark : COLORS.gray100,
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: 16,
      paddingVertical: 12,
    },
    backButton: {
      padding: 8,
    },
    headerTitle: {
      fontSize: 18,
      fontWeight: '600',
      flex: 1,
      textAlign: 'center',
    },
    list: {
      flex: 1,
    },
    listContent: {
      paddingHorizontal: SPACING.lg,
    },
    listHeaderContent: {
      paddingTop: SPACING.sm,
    },
    sectionTitle: {
      fontSize: 14,
      color: COLORS.textSoft,
      marginBottom: SPACING.lg,
      textAlign: 'center',
    },
    sharedWithTitle: {
      fontSize: 16,
      fontWeight: '600',
      marginTop: SPACING.lg,
      marginBottom: SPACING.md,
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
      paddingVertical: SPACING.md,
      paddingHorizontal: SPACING.lg,
      minHeight: 72,
      marginBottom: SPACING.sm,
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderRadius: 12,
    },
    leftCol: {
      flex: 1,
      justifyContent: 'center'
    },
    rightCol: {
      alignItems: 'flex-end',
      justifyContent: 'space-between',
      marginLeft: 12
    },
    email: {
      color: isDark ? COLORS.white : COLORS.text,
      fontWeight: '600',
      fontSize: 15,
      flexShrink: 1,
      flexWrap: 'nowrap',
      width: '80%',
    },
    status: {
      fontSize: 12,
      fontWeight: '600',
      marginTop: 4,
    },
    pending: { color: COLORS.warning },
    accepted: { color: COLORS.success },
    active: { color: COLORS.success },
    rejected: { color: COLORS.error },
    since: {
      fontSize: 11,
      fontWeight: '500',
      color: COLORS.textSoft,
      marginTop: 4,
      textAlign: 'right'
    },
    ownerBadge: {
      fontSize: 12,
      fontWeight: '700',
      color: COLORS.accent,
      marginBottom: 4
    },
    messageBox: {
      marginBottom: SPACING.sm,
      marginTop: 2,
      padding: SPACING.md,
      borderRadius: 10,
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      alignItems: 'center',
    },
    messageText: {
      fontSize: 14,
      fontWeight: '500'
    },
    errorText: { color: COLORS.error },
    successText: { color: COLORS.success },
    buttonRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: 12
    },
    saveBtn: {
      flex: 1,
      backgroundColor: COLORS.accent,
      borderRadius: 12,
      minHeight: 48,
    },
    saveBtnText: {
      color: COLORS.white,
      fontWeight: '600',
      fontSize: 15
    },
    cancelBtn: {
      flex: 1,
      backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : COLORS.gray200,
      borderRadius: 12,
      minHeight: 48,
    },
    cancelBtnText: {
      color: isDark ? COLORS.white : COLORS.text,
      fontWeight: '600',
      fontSize: 15
    },
    separator: {
      height: 8,
    },
    emptyText: {
      textAlign: 'center',
      color: COLORS.textSoft,
      fontSize: 14,
      fontWeight: '500',
      marginTop: 40,
      opacity: 0.7,
    },
    // Legacy styles kept for compatibility
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
    },
  });
