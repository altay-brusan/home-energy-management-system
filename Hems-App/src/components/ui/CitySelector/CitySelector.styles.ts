import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function makeCitySelectorStyles(
  isDark: boolean,
  COLORS: Record<string, string>,
  error?: string,
  selectedCity?: any
) {
  return StyleSheet.create({
    container: {
      marginBottom: SPACING.md,
    },
    label: {
      fontSize: 14,
      fontWeight: '500',
      color: isDark ? COLORS.white : COLORS.textSoft,
      marginBottom: 4,
    },
    selector: {
      borderWidth: 1,
      borderColor: error ? '#EF4444' : (isDark ? COLORS.border : '#CBD5E1'),
      borderRadius: 10,
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : COLORS.surface,
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      height: 48,
    },
    selectorText: {
      fontSize: 16,
      color: selectedCity ? (isDark ? COLORS.white : '#111827') : (isDark ? 'rgba(255,255,255,0.5)' : '#9CA3AF'),
      flex: 1,
    },
    arrowIcon: {
      fontSize: TYPOGRAPHY.lg,
      color: COLORS.slate400,
    },
    errorText: {
      fontSize: 12,
      color: '#EF4444',
      marginTop: 4,
    },
    modal: {
      flex: 1,
      backgroundColor: COLORS.overlay,
      justifyContent: 'flex-end',
    },
    bottomSheet: {
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderTopLeftRadius: SPACING.lg,
      borderTopRightRadius: SPACING.lg,
      maxHeight: '70%',
      minHeight: SPACING.xxl * 10,
      paddingTop: SPACING.md,
    },
    searchContainer: {
      paddingHorizontal: SPACING.lg,
      paddingBottom: SPACING.md,
    },
    searchInput: {
      borderWidth: 1,
      borderColor: COLORS.border,
      borderRadius: SPACING.sm,
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
      fontSize: TYPOGRAPHY.md,
      color: COLORS.textSoft,
      backgroundColor: isDark ? COLORS.dark : COLORS.gray50,
    },
    debugText: {
      color: COLORS.textSoft,
      marginTop: SPACING.sm,
      fontSize: TYPOGRAPHY.sm,
    },
    listContainer: {
      flex: 1,
      minHeight: SPACING.xxl * 5,
    },
    cityItem: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.md,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    cityName: {
      fontSize: TYPOGRAPHY.md,
      color: COLORS.textSoft,
      flex: 1,
    },
    cityCode: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.slate400,
      marginRight: SPACING.sm,
    },
    cityCountry: {
      fontSize: TYPOGRAPHY.sm,
      color: COLORS.slate400,
    },
    selectedItem: {
      backgroundColor: isDark ? COLORS.accent + '20' : COLORS.accent + '10',
    },
    closeButton: {
      paddingHorizontal: SPACING.lg,
      paddingVertical: SPACING.md,
      alignItems: 'center',
      borderTopWidth: 1,
      borderTopColor: COLORS.border,
    },
    closeButtonText: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '600',
      color: COLORS.accent,
    },
    emptyContainer: {
      padding: SPACING.lg,
      alignItems: 'center',
    },
    emptyText: {
      color: COLORS.slate400,
      fontSize: TYPOGRAPHY.md,
    },
  });
}
