import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../theme/constants';

export const makeCompanyInfoStyles = (COLORS: any, isDark: boolean, insetTop: number) => {
  return StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.dark,
      paddingTop: insetTop + SPACING.md,
    },
    pageScroll: {
      paddingBottom: SPACING.lg,
      paddingHorizontal: SPACING.md,
    },
    title: {
      fontSize: 20,
      fontWeight: '700',
      marginBottom: SPACING.md,
      color: isDark ? COLORS.white : COLORS.textSoft,
    },
    inputGroup: {
      gap: SPACING.sm,
    },
    regionCard: {
  marginTop: SPACING.lg, 
  padding: SPACING.md,
},
regionTitle: {
  fontSize: 20,           
  fontWeight: '700',
  marginBottom: SPACING.md,
  marginTop: SPACING.lg,   
  color:  isDark ? COLORS.white : COLORS.textSoft,
},
    regionRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.sm,
    },
    regionEmoji: {
      fontSize: 24,
      marginRight: SPACING.sm,
    },
    regionLabel: {
      fontSize: 16,
      fontWeight: '600',
      color:  isDark ? COLORS.white : COLORS.textSoft,
    },
    regionMeta: {
      fontSize: 14,
      color:  isDark ? COLORS.white : COLORS.textSoft,
      marginTop: 2,
    },
    buttonRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      marginTop: SPACING.lg,
      gap: SPACING.md,
    },
    saveBtn: {
      flex: 1,
      backgroundColor: COLORS.accent,
      borderRadius: 8,
      paddingVertical: SPACING.sm,
    },
    saveBtnText: {
      color: COLORS.white,
      fontWeight: '600',
    },
    cancelBtn: {
      flex: 1,
      backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : COLORS.bgShadeDark,
      borderRadius: 8,
      paddingVertical: SPACING.sm,
    },
    cancelBtnText: {
      color: isDark ? COLORS.white : COLORS.textSoft,
      fontWeight: '600',
    },
  });
};
