import { StyleSheet, Dimensions } from "react-native";
import { SPACING } from "../../../theme/constants";

export const getAdminPageStyles = (COLORS: any, isDark: boolean) => {
  const { width } = Dimensions.get("window");
  const isSmall = width <= 380;
  const isVerySmall = width <= 340;
  
  return StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.background,
      paddingHorizontal: SPACING.lg,
    },

    scrollContent: {
      paddingBottom: 100,
    },

    welcomeText: {
      color: COLORS.textSoft,
      fontSize: 30,
      fontWeight: "700",
      marginTop: SPACING.lg,
      marginBottom: SPACING.md,
    },

    errorText: {
      color: COLORS.error || '#FF6B6B',
      fontSize: 16,
      fontWeight: "500",
      textAlign: "center",
      marginTop: SPACING.md,
    },

    selectorRow: {
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: SPACING.lg,
      gap: SPACING.sm,
    },
    homeCardAddressSmall: {
      color: COLORS.textSoft,
      fontSize: isVerySmall ? 11 : isSmall ? 11.5 : 12,
      fontWeight: "500",
      marginTop: SPACING.xs,
      lineHeight: isVerySmall ? 16 : isSmall ? 17 : 18,
      flexShrink: 1,
    },
    pageButton: {
      backgroundColor: COLORS.accent,
      paddingVertical: 8,
      paddingHorizontal: 16,
      borderRadius: 8,
      alignItems: "center",
    },

    pageButtonText: {
      color: "#fff",
      fontWeight: "600",
      fontSize: 14,
    },
    selectorBox: {
      flex: 1,
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      borderWidth: 1,
      borderColor: COLORS.border,
      backgroundColor: COLORS.surface,
      borderRadius: 12,
      paddingVertical: 12,
      paddingHorizontal: 14,
    },

    selectorText: {
      fontSize: 15,
      fontWeight: "500",
      color: COLORS.textSoft,
    },

    modalOverlay: {
      flex: 1,
      backgroundColor: "rgba(0,0,0,0.4)",
      justifyContent: "flex-end",
    },
    modalContainer: {
      backgroundColor: COLORS.surface,
      borderTopLeftRadius: 20,
      borderTopRightRadius: 20,
      padding: SPACING.md,
      minHeight: 320,
    },
    modalHeader: {
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
      paddingBottom: 8,
      marginBottom: 10,
    },
    modalTitle: {
      color: COLORS.textSoft,
      fontSize: 16,
      fontWeight: "600",
    },
    modalSearchInput: {
      marginTop: 8,
      backgroundColor: isDark ? COLORS.background : COLORS.card,
      color: COLORS.text,
      borderRadius: 10,
      paddingVertical: 8,
      paddingHorizontal: 12,
      fontSize: 14,
    },
    modalItem: {
      paddingVertical: 12,
      borderBottomWidth: 0.5,
      borderBottomColor: COLORS.border,
    },
    modalItemText: {
      color: COLORS.textSoft,
      fontSize: 15,
    },
    modalEmptyText: {
      color: COLORS.textSoft,
      fontSize: 14,
      textAlign: "center",
      marginTop: 20,
    },
    modalCloseButton: {
      alignSelf: "center",
      marginTop: 16,
      paddingVertical: 10,
      paddingHorizontal: 24,
      backgroundColor: COLORS.accent,
      borderRadius: 10,
    },
    modalCloseButtonText: {
      color: "#fff",
      fontWeight: "600",
      fontSize: 15,
    },

    bottomRow: {
      flexDirection: "row",
      justifyContent: "space-between",
      gap: SPACING.sm,
      marginBottom: SPACING.lg,
    },
    statCard: {
      flex: 1,
      borderRadius: 12,
      backgroundColor: COLORS.surface,
      paddingVertical: SPACING.md,
      alignItems: "center",
      justifyContent: "center",
    },
    statLabel: {
      fontSize: 14,
      fontWeight: "500",
      color: COLORS.textSoft,
      marginTop: 4,
    },
    statValue: {
      fontSize: 18,
      fontWeight: "700",
      color: COLORS.textSoft,
      marginTop: 2,
    },

    sectionTitle: {
      fontSize: 20,
      fontWeight: "600",
      color: COLORS.textSoft,
      marginBottom: SPACING.md,
      marginTop: SPACING.sm,
    },
    cardsWrapper: {
      gap: SPACING.md,
    },

    homeCard: {
      borderRadius: 14,
      backgroundColor: COLORS.surface,
      padding: SPACING.md,
      paddingRight: isVerySmall ? SPACING.md + 4 : isSmall ? SPACING.md + 6 : SPACING.md + 8,
      elevation: 2,
      shadowColor: "#000",
      shadowOpacity: 0.1,
      shadowRadius: 4,
      shadowOffset: { width: 0, height: 2 },
    },

    homeCardHeader: {
      flexDirection: "row",
      alignItems: "flex-start",
      justifyContent: "space-between",
      marginBottom: SPACING.sm,
      gap: 8,
      paddingTop: SPACING.xs,
    },
    imageWrapper: {
      width: 80,
      height: 80,
      justifyContent: "center",
      alignItems: "center",
      marginRight: 10,
    },
    homeCardImage: {
      width: "100%",
      height: "100%",
      borderRadius: 10,
    },
    homeCardContent: {
      flex: 1,
      flexShrink: 1,
      justifyContent: "center",
      paddingRight: isVerySmall ? 4 : isSmall ? 6 : 8,
      minWidth: 0,
    },

    homeCardTitle: {
      fontSize: isVerySmall ? 14 : isSmall ? 15 : 16,
      fontWeight: "700",
      color: COLORS.accent,
      flexShrink: 1,
    },

    homeCardAddress: {
      color: COLORS.textSoft,
      marginTop: 2,
      fontSize: isVerySmall ? 12 : isSmall ? 13 : 14,
      flexShrink: 1,
    },

    statusBadgeContainer: {
      marginLeft: isVerySmall ? 8 : isSmall ? 10 : 12,
    },
    statusBadge: {
      borderRadius: 20,
      paddingHorizontal: isVerySmall ? 8 : isSmall ? 9 : 10,
      paddingVertical: isVerySmall ? 3 : 4,
      alignSelf: "flex-start",
      marginTop: 0,
    },
    statusBadgeText: {
      color: "#fff",
      fontSize: isVerySmall ? 10 : isSmall ? 11 : 12,
      fontWeight: "600",
    },

    homeCardFooter: {
      flexDirection: "row",
      justifyContent: "flex-end",
      marginTop: 10,
    },
    ownerInfoRight: {
      alignItems: "flex-end",
    },
    homeOwnerText: {
      color: COLORS.accent,
      fontSize: 13,
      fontWeight: "800",
    },

    searchContainer: {
      marginBottom: SPACING.lg,
    },
    searchInputWrapper: {
      flexDirection: "row",
      alignItems: "center",
      borderWidth: 1,
      borderColor: COLORS.border,
      backgroundColor: COLORS.surface,
      borderRadius: 10,
      paddingHorizontal: 12,
      paddingVertical: 6,
    },
    searchInput: {
      flex: 1,
      color: COLORS.textSoft,
      fontSize: 15,
      paddingVertical: 6,
    },
    searchIconWrapper: {
      paddingLeft: 8,
    },
    searchResultList: {
      marginTop: 6,
      backgroundColor: COLORS.surface,
      borderRadius: 10,
      borderWidth: 1,
      borderColor: COLORS.border,
      maxHeight: 150,
    },
    searchResultItem: {
      paddingVertical: 8,
      paddingHorizontal: 12,
      borderBottomWidth: 0.5,
      borderBottomColor: COLORS.border,
    },
    searchResultText: {
      color: COLORS.textSoft,
      fontSize: 14,
    },
  });
};
