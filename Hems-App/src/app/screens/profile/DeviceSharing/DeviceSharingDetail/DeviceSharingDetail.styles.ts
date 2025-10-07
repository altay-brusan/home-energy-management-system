import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../../theme/constants';

export const makeUserDetailStyles = (
  COLORS: Record<string, string>,
  isDark: boolean
) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.dark, 
      paddingHorizontal: SPACING.lg,
    },
    pageScroll: {
      paddingBottom: SPACING.xl, 
    },
    headerContainer: {
      flexGrow: 1,
    },
    title: {
      fontSize: 22,
      fontWeight: '700',
      color: COLORS.accent,
      textAlign: 'center',
      marginBottom: SPACING.lg,
    },
    label: {
      fontSize: 14,
      fontWeight: '600',
      color: COLORS.accent,
      marginTop: 6,
    },
    value: {
      fontSize: 16,
      fontWeight: '700',
      color: isDark ? COLORS.white : COLORS.slate900,
      marginBottom: 4,
    },
    subtitle: {
      fontSize: 18,
      fontWeight: '700',
      color: COLORS.accent,
      marginBottom: SPACING.md,
      marginTop: SPACING.lg,
    },
    logList: {
      gap: 6,
      marginBottom: SPACING.xl,
    },
    logItem: {
      fontSize: 14,
      color: isDark ? COLORS.white : COLORS.textSoft,
      backgroundColor: isDark
        ? 'rgba(255,255,255,0.05)'
        : COLORS.surface,
      padding: 10,
      borderRadius: 8,
      marginBottom: 10,
    },
    buttonRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      gap: 12,
      marginTop: SPACING.md,
      marginBottom: SPACING.md,
    },
    removeBtn: {
      flex: 1,
      backgroundColor: COLORS.error,
    },
    removeBtnText: {
      color: COLORS.white,
      fontWeight: '700',
      fontSize: 16,
    },
    cancelBtn: {
      flex: 1,
      backgroundColor: isDark
        ? 'rgba(255,255,255,0.05)'
        : COLORS.bgShadeDark,
    },
    cancelBtnText: {
      color: isDark ? COLORS.white : COLORS.textSoft,
      fontWeight: '700',
      fontSize: 16,
    },
  });
