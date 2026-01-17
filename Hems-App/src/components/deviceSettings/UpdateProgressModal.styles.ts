import { StyleSheet, Dimensions } from 'react-native';
import { getDesignTokens } from '../../theme/design-tokens';
import { FONTS, SPACING } from '../../theme/constants';

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

export const makeUpdateProgressModalStyles = (isDark: boolean) => {
  const tokens = getDesignTokens(isDark);

  return StyleSheet.create({
    // Overlay
    overlay: {
      flex: 1,
      backgroundColor: tokens.background.overlay,
      justifyContent: 'flex-end',
    },

    // Modal container
    modalContent: {
      backgroundColor: tokens.surface.modal,
      borderTopLeftRadius: 24,
      borderTopRightRadius: 24,
      minHeight: 320,
      maxHeight: SCREEN_HEIGHT * 0.7,
    },

    // Handle bar
    handleContainer: {
      alignItems: 'center',
      paddingVertical: 12,
    },
    handle: {
      width: 40,
      height: 4,
      borderRadius: 2,
      backgroundColor: tokens.utility.gray300,
    },

    // Header
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: SPACING.lg,
      paddingBottom: SPACING.md,
    },
    title: {
      fontSize: 18,
      fontFamily: FONTS.semiBold,
      color: tokens.text.primary,
    },
    closeButton: {
      padding: 4,
    },

    // Content
    content: {
      paddingHorizontal: SPACING.lg,
      alignItems: 'center',
      flex: 1,
    },

    // Status icon area
    statusIconContainer: {
      marginBottom: SPACING.md,
      height: 64,
      justifyContent: 'center',
      alignItems: 'center',
    },

    // Status text
    statusText: {
      fontSize: 16,
      fontFamily: FONTS.medium,
      color: tokens.text.primary,
      marginBottom: SPACING.lg,
      textAlign: 'center',
    },

    // Progress bar
    progressContainer: {
      width: '100%',
      alignItems: 'center',
      marginBottom: SPACING.lg,
    },
    progressBar: {
      width: '100%',
      height: 8,
      borderRadius: 4,
      backgroundColor: tokens.utility.gray200,
      overflow: 'hidden',
    },
    progressFill: {
      height: '100%',
      borderRadius: 4,
      backgroundColor: tokens.brand.accent,
    },
    progressText: {
      marginTop: 8,
      fontSize: 14,
      fontFamily: FONTS.regular,
      color: tokens.text.secondary,
    },

    // Summary
    summaryContainer: {
      width: '100%',
      marginTop: SPACING.md,
      padding: SPACING.md,
      backgroundColor: isDark ? tokens.background.shade : tokens.utility.gray50,
      borderRadius: 12,
    },
    summaryRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
      marginBottom: 8,
    },
    summaryText: {
      fontSize: 14,
      fontFamily: FONTS.regular,
      color: tokens.text.primary,
    },
    summaryTextError: {
      fontSize: 14,
      fontFamily: FONTS.regular,
      color: tokens.semantic.error,
    },

    // Error container (when no specific failed parameters)
    errorContainer: {
      width: '100%',
      marginTop: SPACING.md,
      padding: SPACING.md,
      backgroundColor: isDark ? 'rgba(239, 68, 68, 0.1)' : 'rgba(239, 68, 68, 0.05)',
      borderRadius: 12,
      borderWidth: 1,
      borderColor: 'rgba(239, 68, 68, 0.2)',
    },
    errorMessage: {
      fontSize: 14,
      fontFamily: FONTS.regular,
      color: tokens.semantic.error,
      textAlign: 'center',
    },

    // Failed parameters list
    failedListContainer: {
      width: '100%',
      maxHeight: 150,
      marginTop: SPACING.md,
    },
    failedTitle: {
      fontSize: 14,
      fontFamily: FONTS.semiBold,
      color: tokens.semantic.error,
      marginBottom: 8,
    },
    failedItem: {
      marginBottom: 8,
      paddingLeft: 8,
      borderLeftWidth: 2,
      borderLeftColor: tokens.semantic.error,
    },
    failedParamName: {
      fontSize: 13,
      fontFamily: FONTS.medium,
      color: tokens.text.primary,
    },
    failedParamError: {
      fontSize: 12,
      fontFamily: FONTS.regular,
      color: tokens.text.secondary,
    },

    // Actions
    actionsContainer: {
      paddingHorizontal: SPACING.lg,
      paddingTop: SPACING.lg,
      paddingBottom: SPACING.md,
      gap: 12,
    },
    button: {
      width: '100%',
      paddingVertical: 14,
      borderRadius: 12,
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: 48,
    },
    primaryButton: {
      backgroundColor: tokens.brand.accent,
    },
    primaryButtonText: {
      fontSize: 16,
      fontFamily: FONTS.semiBold,
      color: tokens.utility.white,
    },
    secondaryButton: {
      backgroundColor: tokens.button.secondaryBg,
      borderWidth: 1,
      borderColor: tokens.border.primary,
    },
    secondaryButtonText: {
      fontSize: 16,
      fontFamily: FONTS.semiBold,
      color: tokens.text.primary,
    },
  });
};
