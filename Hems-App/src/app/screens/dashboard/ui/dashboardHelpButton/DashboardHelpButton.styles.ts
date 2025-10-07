import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../../theme/constants';

export function getDashboardHelpButtonStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    menuRow: {
      width: '100%',
      marginTop: SPACING.lg,
      alignItems: 'center',
      justifyContent: 'center',
    },
    menuBtn: {
      backgroundColor: COLORS.accent,
      borderRadius: 8,
      paddingHorizontal: 24,
      paddingVertical: 12,
      minWidth: 120,
    },
    menuBtnText: {
      color: COLORS.white,
      fontWeight: '700',
      fontSize: 16,
    },
  });
}
