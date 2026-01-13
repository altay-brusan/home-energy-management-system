import { StyleSheet } from 'react-native'
import { SPACING, TYPOGRAPHY } from '../../../theme/constants'

export function getHomeStyles (COLORS: Record<string, string>, isDark: boolean) {
  return StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: isDark ? COLORS.dark : COLORS.gray100,
    },
    scrollContent: {
      flexGrow: 1,
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.lg,
    },
    // Header Section
    headerSection: {
      marginBottom: SPACING.xl,
    },
    welcomeSection: {
      marginBottom: SPACING.lg,
    },
    welcomeRow: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
    },
    profileButton: {
      padding: 8,
      borderRadius: 12,
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    profileIcon: {
      color: COLORS.textSoft,
    },
    welcomeText: {
      fontSize: 24,
      fontWeight: '700',
      color: COLORS.text,
      marginBottom: 4,
    },
    subtitleText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      fontWeight: '400',
    },
    actionButtons: {
      flexDirection: 'row',
      justifyContent: 'flex-start',
      alignItems: 'center',
    },
    primaryButton: {
      backgroundColor: COLORS.accent,
      borderRadius: 12,
      paddingHorizontal: 16,
      paddingVertical: 12,
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'center',
    },
    primaryButtonText: {
      color: COLORS.white,
      fontWeight: '600',
      fontSize: TYPOGRAPHY.sm,
    },
    headerButton: {
      flexDirection: 'row',
      alignItems: 'center',
    },
    // Section Title
    sectionTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '600',
      color: COLORS.text,
      marginBottom: SPACING.md,
    },
    // Cards Wrapper
    cardsWrapper: {
      flexDirection: 'column',
      gap: SPACING.sm,
    },
    // Card Styles (new design)
    card: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderRadius: 16,
      padding: SPACING.md,
      borderWidth: 1,
      borderColor: COLORS.border,
      position: 'relative',
    },
    cardPressed: {
      opacity: 0.7,
    },
    cardIcon: {
      width: 44,
      height: 44,
      borderRadius: 12,
      alignItems: 'center',
      justifyContent: 'center',
      marginRight: SPACING.md,
    },
    cardContent: {
      flex: 1,
      marginRight: SPACING.sm,
    },
    cardTitleRow: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
      marginBottom: 4,
    },
    cardTitle: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.text,
      flex: 1,
    },
    cardSubtitle: {
      fontSize: TYPOGRAPHY.xs,
      color: COLORS.textSoft,
      marginLeft: 4,
      flex: 1,
    },
    addressRow: {
      flexDirection: 'row',
      alignItems: 'center',
    },
    noDeviceBadge: {
      backgroundColor: `${COLORS.warning}20`,
      paddingHorizontal: 8,
      paddingVertical: 2,
      borderRadius: 6,
    },
    noDeviceBadgeText: {
      fontSize: 10,
      fontWeight: '600',
      color: COLORS.warning,
    },
    cardActions: {
      flexDirection: 'row',
      alignItems: 'center',
    },
    menuButton: {
      padding: 4,
    },
    // Dropdown Menu
    dropdownMenu: {
      position: 'absolute',
      top: 50,
      right: 16,
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: COLORS.border,
      paddingVertical: 8,
      minWidth: 160,
      zIndex: 100,
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 4 },
      shadowOpacity: 0.15,
      shadowRadius: 12,
      elevation: 8,
    },
    dropdownItem: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingVertical: 10,
      paddingHorizontal: 14,
      gap: 10,
    },
    dropdownItemText: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.text,
      fontWeight: '500',
    },
    dropdownDivider: {
      height: 1,
      backgroundColor: COLORS.border,
      marginVertical: 4,
    },
    // Empty State
    emptyState: {
      alignItems: 'center',
      justifyContent: 'center',
      paddingVertical: SPACING.xxl,
      paddingHorizontal: SPACING.lg,
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderRadius: 16,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    emptyTitle: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: '600',
      color: COLORS.text,
      marginTop: SPACING.md,
      marginBottom: SPACING.xs,
    },
    emptySubtitle: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.textSoft,
      textAlign: 'center',
      lineHeight: 20,
      marginBottom: SPACING.lg,
    },
    emptyButton: {
      backgroundColor: COLORS.accent,
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.md,
      borderRadius: 12,
    },
    emptyButtonText: {
      color: COLORS.white,
      fontSize: TYPOGRAPHY.sm,
      fontWeight: '600',
    },
    // Loading States
    loadingOverlay: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: 'rgba(0, 0, 0, 0.7)',
      justifyContent: 'center',
      alignItems: 'center',
      zIndex: 9999,
    },
    loadingContainer: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      padding: SPACING.xl,
      alignItems: 'center',
      justifyContent: 'center',
      minWidth: 200,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 4 },
      shadowOpacity: 0.3,
      shadowRadius: 8,
      elevation: 8,
    },
    loadingText: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      marginTop: SPACING.md,
      textAlign: 'center',
    },
    // Legacy styles (kept for compatibility)
    cardWrapper: {
      marginBottom: SPACING.sm,
      position: 'relative',
    },
    pressable: {
      borderRadius: 16,
      overflow: 'hidden',
    },
    pressablePressed: {
      opacity: 0.9,
    },
    secondaryButton: {
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      paddingHorizontal: 16,
      paddingVertical: 10,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    secondaryButtonText: {
      color: COLORS.text,
      fontWeight: '500',
    },
  })
}
