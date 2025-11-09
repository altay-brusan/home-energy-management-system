import { StyleSheet } from 'react-native';
import { TYPOGRAPHY, SPACING } from '../../../../../theme/constants';

export const createStyles = (COLORS: Record<string, string>) => {
  return StyleSheet.create({
    card: {
      backgroundColor: COLORS.cardBg,
      borderRadius: 16,
      padding: SPACING.lg,
      marginBottom: 12,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '700',
      color: COLORS.textSoft,
      marginBottom: SPACING.md,
    },
    diagramContainer: {
      position: 'relative',
      alignSelf: 'center',
      marginVertical: SPACING.md,
    },
    svgContainer: {
      position: 'absolute',
      top: 0,
      left: 0,
      zIndex: 1,
    },
    loadingContainer: {
      height: 400,
      justifyContent: 'center',
      alignItems: 'center',
    },
    loadingText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      marginTop: SPACING.md,
    },
    emptyContainer: {
      height: 400,
      justifyContent: 'center',
      alignItems: 'center',
      paddingHorizontal: SPACING.xl,
    },
    emptyText: {
      fontSize: TYPOGRAPHY.md,
      color: COLORS.textSoft,
      textAlign: 'center',
    },
    timestamp: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      textAlign: 'center',
      marginTop: SPACING.sm,
      opacity: 0.7,
    },
  });
};
