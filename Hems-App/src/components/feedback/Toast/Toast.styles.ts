import { StyleSheet, Platform, Dimensions } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

const { width: screenWidth } = Dimensions.get('window');

export const createToastStyles = (COLORS: Record<string, string>, isDark: boolean) => StyleSheet.create({
  container: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    zIndex: 9999,
    pointerEvents: 'box-none',
  },
  
  toastContainer: {
    position: 'absolute',
    top: 60,
    left: 0,
    right: 0,
    zIndex: 10000,
    alignItems: 'center',
    paddingHorizontal: SPACING.md,
  },
  
  toast: {
    width: '100%',
    maxWidth: screenWidth - SPACING.md * 2,
    backgroundColor: COLORS.cardBg,
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    borderBottomLeftRadius: 8,
    borderBottomRightRadius: 8,
    padding: SPACING.lg,
    flexDirection: 'row',
    alignItems: 'flex-start',
    shadowColor: COLORS.black,
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: isDark ? 0.4 : 0.15,
    shadowRadius: 20,
    elevation: 16,
    borderLeftWidth: 4,
    borderTopColor: COLORS.accent,
    borderTopWidth: 1,
    borderRightColor: COLORS.accent,
    borderRightWidth: 1,
    borderBottomColor: COLORS.accent,
    borderBottomWidth: 1,
    ...Platform.select({
      ios: {
        shadowColor: COLORS.accent,
        shadowOffset: { width: 0, height: 0 },
        shadowOpacity: 0.8,
        shadowRadius: 20,
      },
      android: {
        elevation: 20,
      },
    }),
  },
  
  iconContainer: {
    marginRight: SPACING.md,
    marginTop: 2,
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: COLORS.accent,
    justifyContent: 'center',
    alignItems: 'center',
  },
  
  contentContainer: {
    flex: 1,
    paddingRight: SPACING.sm,
  },
  
  title: {
    fontSize: TYPOGRAPHY.md,
    fontWeight: '700',
    color: COLORS.accent,
    marginBottom: 4,
    letterSpacing: 0.5,
  },
  
  message: {
    fontSize: TYPOGRAPHY.sm,
    color: COLORS.textSoft,
    lineHeight: 20,
    letterSpacing: 0.3,
  },
  
  progressBarContainer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    height: 3,
    backgroundColor: COLORS.bgShadeDark,
    borderBottomLeftRadius: 8,
    borderBottomRightRadius: 8,
    overflow: 'hidden',
  },
  
  progressBar: {
    height: '100%',
    backgroundColor: COLORS.accent,
  },
  
  // Toast type specific styles
  success: {
    borderLeftColor: COLORS.success,
    borderTopColor: COLORS.success,
    borderRightColor: COLORS.success,
    borderBottomColor: COLORS.success,
  },
  
  error: {
    borderLeftColor: COLORS.error,
    borderTopColor: COLORS.error,
    borderRightColor: COLORS.error,
    borderBottomColor: COLORS.error,
  },
  
  warning: {
    borderLeftColor: COLORS.warning,
    borderTopColor: COLORS.warning,
    borderRightColor: COLORS.warning,
    borderBottomColor: COLORS.warning,
  },
  
  info: {
    borderLeftColor: COLORS.info,
    borderTopColor: COLORS.info,
    borderRightColor: COLORS.info,
    borderBottomColor: COLORS.info,
  },
  
  // Icon background colors for different types
  successIcon: {
    backgroundColor: COLORS.success,
  },
  
  errorIcon: {
    backgroundColor: COLORS.error,
  },
  
  warningIcon: {
    backgroundColor: COLORS.warning,
  },
  
  infoIcon: {
    backgroundColor: COLORS.info,
  },
  
  // Progress bar colors for different types
  successProgress: {
    backgroundColor: COLORS.success,
  },
  
  errorProgress: {
    backgroundColor: COLORS.error,
  },
  
  warningProgress: {
    backgroundColor: COLORS.warning,
  },
  
  infoProgress: {
    backgroundColor: COLORS.info,
  },
});

export type ToastStyles = ReturnType<typeof createToastStyles>;
