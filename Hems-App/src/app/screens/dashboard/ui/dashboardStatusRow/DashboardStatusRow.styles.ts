import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../../theme/constants';

export function getDashboardStatusRowStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    row: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingVertical: SPACING.md,
      paddingHorizontal: SPACING.md,
      justifyContent: 'center',
      flexWrap: 'wrap',
      marginHorizontal: -SPACING.xs,
      marginVertical: -SPACING.xs,
      width: '100%',
    },
    badgeContainer: {
      alignItems: 'center',
      marginHorizontal: SPACING.xs,
      marginVertical: SPACING.xs,
    },
  });
}
