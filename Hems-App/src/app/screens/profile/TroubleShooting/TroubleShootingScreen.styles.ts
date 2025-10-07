import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../theme/constants';

export const makeTroubleShootingStyles = (
  COLORS: Record<string, string>,
  isDark: boolean
) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.dark,
      paddingHorizontal: SPACING.lg,
    },

    headerRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      marginBottom: SPACING.lg,
      marginTop: SPACING.sm,
    },
    backButton: {
      padding: 8,
    },
    screenTitle: {
      fontSize: 20,
      fontWeight: '700',
      color: isDark ? COLORS.white : COLORS.black,
      textAlign: 'center',
      flex: 1,
    },

    title: {
      fontSize: 16,
      fontWeight: '600',
      marginVertical: SPACING.md,
      color: isDark ? COLORS.white : COLORS.black,
    },

    dropdownOverlay: {
      position: 'absolute',
      top: 52, 
      left: 0,
      right: 0,
      zIndex: 999,
    },

    dropdownList: {
      backgroundColor: isDark ? COLORS.surface : '#fff',
      borderRadius: 12,
      overflow: 'hidden',
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.2,
      shadowRadius: 4,
      elevation: 6,
    },

    dropdownItem: {
      paddingVertical: 14,
      paddingHorizontal: 16,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    dropdownItemText: {
      fontSize: 15,
      color: isDark ? COLORS.white : COLORS.black,
    },

    confirmBtn: {
      marginTop: SPACING.xl,
      backgroundColor: COLORS.accent,
      borderRadius: 8,
      alignItems: 'center',
      paddingVertical: SPACING.md,
    },
    confirmBtnText: {
      color: COLORS.white,
      fontWeight: '700',
      fontSize: 16,
    },
  });
