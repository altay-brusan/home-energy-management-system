import { StyleSheet } from 'react-native';
import { getThemeColors, SPACING, TYPOGRAPHY } from '../../../theme/constants';
import { useThemeStore } from '../../../store/themeStore';

export function getGlobalAlertStyles () {
  const mode = useThemeStore.getState().mode;
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);

  return StyleSheet.create({
    overlay: {
      position: 'absolute',
      left: 0,
      right: 0,
      top: 0,
      bottom: 0,
      zIndex: 999,
      backgroundColor: 'rgba(0,0,0,0.35)',
      justifyContent: 'flex-end',
    },
    sheet: {
      backgroundColor: COLORS.dark,
      borderTopLeftRadius: 24,
      borderTopRightRadius: 24,
      borderTopWidth: 1,
      borderColor: COLORS.border,
      padding: SPACING.lg,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: -6 },
      shadowOpacity: 0.12,
      shadowRadius: 14,
      elevation: 12,
      position: 'relative',
      minHeight: 220,
    },
    accentBar: {
      position: 'absolute',
      left: 1,
      bottom: 20,
      height: "100%",
      width: 4,
      borderTopLeftRadius: 24,
      borderBottomLeftRadius: 8,
    },
    handle: {
      alignSelf: 'center',
      width: 64,
      height: 6,
      borderRadius: 999,
      backgroundColor: COLORS.border,
      marginBottom: 12,
      marginTop: 6,
    },
    titleRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
    },
    iconWrap: {
      width: 36,
      height: 36,
      borderRadius: 18,
      alignItems: 'center',
      justifyContent: 'center',
      marginRight: 10,
      borderWidth: 1,
      backgroundColor: isDark ? 'rgba(0,0,0,0.25)' : 'rgba(0,0,0,0.06)',
    },
    title: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '800',
      color: COLORS.textSoft,
    },
    msg: {
      marginTop: 8,
      color: COLORS.slate400,
      lineHeight: 22,
      fontSize: TYPOGRAPHY.md,
    },
    sectionTitle: {
      marginTop: SPACING.sm,
      color: COLORS.textSoft,
      fontWeight: '700',
      fontSize: TYPOGRAPHY.sm,
    },
    meta: {
      marginTop: 6,
      color: COLORS.slate400,
      fontSize: 12,
    },
    levelChip: {
      paddingVertical: 6,
      paddingHorizontal: 10,
      borderRadius: 999,
      marginLeft: 8,
      borderWidth: 1,
      backgroundColor: isDark ? 'rgba(0,0,0,0.2)' : 'rgba(0,0,0,0.04)',
    },
    levelChipText: {
      fontWeight: '700',
    },
    btnRow: {
      flexDirection: 'row',
      columnGap: SPACING.sm,
      marginTop: SPACING.md,
      flexWrap: 'wrap',
    },
    btnHalf: {
      flexGrow: 1,
      flexBasis: '48%',
    },
    closeBtn: {
      flexBasis: '100%',
      marginTop: SPACING.sm,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.12,
      shadowRadius: 6,
      elevation: 3,
      paddingVertical: SPACING.sm + 2,
    },
    pill: {
      paddingVertical: 8,
      paddingHorizontal: 12,
      borderRadius: 999,
      borderWidth: 1,
      borderColor: COLORS.border,
      backgroundColor: COLORS.bgShadeLight,
    },
    pillText: {
      color: COLORS.textSoft,
      fontWeight: '700',
    },
  });
}
