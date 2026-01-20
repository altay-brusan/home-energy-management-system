import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      padding: SPACING.md,
      marginBottom: SPACING.sm,
    },
    header: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: SPACING.md,
    },
    headerRight: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: SPACING.sm,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    percentage: {
      fontSize: TYPOGRAPHY.xl,
      fontWeight: '700',
    },
    historyButton: {
      padding: 4,
    },
    progressBarContainer: {
      height: 8,
      backgroundColor: COLORS.bgShadeLight,
      borderRadius: 4,
      overflow: 'hidden',
      marginBottom: SPACING.sm,
    },
    progressBar: {
      height: '100%',
      borderRadius: 4,
    },
    statusText: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
      color: COLORS.textSoft,
      opacity: 0.7,
    },
    loadingContainer: {
      height: 100,
      justifyContent: 'center',
      alignItems: 'center',
    },
  });
};
