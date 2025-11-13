import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      marginBottom: 12,
    },
    scrollContent: {
      paddingHorizontal: SPACING.md,
    },
    slide: {
      // Let each slide take its natural height (don't force same height)
    },
    pagination: {
      flexDirection: 'row',
      justifyContent: 'center',
      alignItems: 'center',
      marginTop: 12,
      gap: 8,
    },
    dot: {
      width: 8,
      height: 8,
      borderRadius: 4,
    },
  });
};
