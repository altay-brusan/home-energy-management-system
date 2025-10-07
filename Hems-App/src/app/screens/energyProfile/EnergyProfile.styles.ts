import { StyleSheet } from "react-native";
import { SPACING } from "../../../theme/constants";

export const getEnergyProfileStyles = (
  COLORS: Record<string, string>,
  isDark: boolean
) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.background,
      paddingHorizontal: SPACING.lg,
    },

    headerRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      marginTop: SPACING.lg,
      marginBottom: SPACING.md,
    },
    backButton: { padding: 8 },
    screenTitle: {
      flex: 1,
      textAlign: "center",
      color: COLORS.textSoft,
      fontSize: 24,
      fontWeight: "700",
    },

    loadingOverlay: {
      position: "absolute",
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: "rgba(0,0,0,0.4)",
      zIndex: 100,
      justifyContent: "center",
      alignItems: "center",
    },
    loadingBox: {
      backgroundColor: COLORS.surface,
      paddingVertical: 28,
      paddingHorizontal: 36,
      borderRadius: 14,
      alignItems: "center",
      elevation: 8,
      shadowColor: COLORS.shadow,
      shadowOpacity: 0.3,
      shadowRadius: 8,
      shadowOffset: { width: 0, height: 4 },
    },
    loadingText: {
      marginTop: 12,
      color: COLORS.textSoft,
      fontSize: 16,
      fontWeight: "600",
    },

    mainCard: {
      backgroundColor: COLORS.surface,
      borderRadius: 16,
      marginBottom: 28,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    cardTitle: {
      fontSize: 22,
      fontWeight: "700",
      color: COLORS.textSoft,
      paddingHorizontal: 22,
      paddingTop: 14,
      paddingBottom: 8,
    },
    subCard: {
      borderTopWidth: 1,
      borderTopColor: COLORS.border,
      paddingHorizontal: 22,
      paddingVertical: 18,
    },
    subCardTitle: {
      color: COLORS.textSoft,
      fontWeight: "700",
      fontSize: 18,
      marginBottom: 12,
    },

    switchRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      marginVertical: 10,
    },
    label: {
      color: COLORS.textSoft,
      fontSize: 17,
      fontWeight: "600",
    },
    valueText: {
      color: COLORS.textSoft,
      fontSize: 17,
      marginTop: 4,
    },
    valueAccent: {
      fontWeight: "700",
      color: COLORS.accent,
    },
    subHeader: {
      color: COLORS.textSoft,
      fontSize: 17,
      fontWeight: "600",
      marginTop: 12,
      marginBottom: 6,
    },
    subNote: {
      fontStyle: "italic",
      color: COLORS.textSoft,
      fontSize: 15,
      opacity: 0.8,
    },

dropdownRow: {
  flexDirection: "row",
  alignItems: "center",
  justifyContent: "space-between",
  borderTopWidth: 1,
  borderTopColor: COLORS.border,
  paddingHorizontal: 22,
  paddingVertical: 16,
},

    dropdownList: {
      borderTopWidth: 1,
      borderTopColor: COLORS.border,
      paddingHorizontal: 22,
      paddingVertical: 14,
      backgroundColor: COLORS.bgShadeDark,
    },
    dropdownItem: {
      marginBottom: 10,
    },
    dropdownLabel: {
      fontSize: 17,
      fontWeight: "600",
      color: COLORS.textSoft,
    },
    dropdownDesc: {
      fontSize: 15,
      color: COLORS.textSoft,
      opacity: 0.8,
      marginTop: 2,
    },

    advancedBox: {
      borderTopWidth: 1,
      borderTopColor: COLORS.border,
      marginTop: 14,
      paddingTop: 14,
    },
    advancedTitle: {
      fontSize: 16,
      fontWeight: "700",
      color: COLORS.textSoft,
      marginBottom: 8,
    },
    exportOptionCard: {
      backgroundColor: COLORS.surface,
      borderWidth: 1.5,
      borderColor: COLORS.border,
      borderRadius: 10,
      paddingVertical: 12,
      paddingHorizontal: 16,
      marginBottom: 10,
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      elevation: 2,
      shadowColor: COLORS.shadow,
      shadowOpacity: 0.15,
      shadowRadius: 3,
      shadowOffset: { width: 0, height: 2 },
    },
    exportOptionText: {
      fontSize: 15,
      fontWeight: "600",
      color: COLORS.textSoft,
    },
    exportOptionActive: {
      borderColor: COLORS.accent,
      shadowOpacity: 0.25,
      shadowRadius: 5,
    },
    exportOptionTextActive: {
      color: COLORS.accent,
    },
    saveButton: {
  marginTop: 12,
  backgroundColor: COLORS.accent,
  paddingVertical: 12,
  borderRadius: 10,
  alignItems: "center",
  justifyContent: "center",
  shadowColor: COLORS.accent,
  shadowOpacity: 0.3,
  shadowRadius: 6,
  shadowOffset: { width: 0, height: 3 },
  elevation: 3,
},
saveButtonText: {
  color: COLORS.white,
  fontWeight: "700",
  fontSize: 16,
  letterSpacing: 0.5,
},

  });
