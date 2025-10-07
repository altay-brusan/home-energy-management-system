import { StyleSheet } from "react-native";
import { SPACING } from "../../../../theme/constants";

export const getModeSelectionPanelStyles = (COLORS: Record<string, string>) =>
  StyleSheet.create({
    container: {
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      paddingVertical: SPACING.md,
      paddingHorizontal: SPACING.lg,
      marginTop: SPACING.sm,
      marginBottom: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,
      overflow: "hidden",
    },
    option: {
      borderWidth: 1,
      borderColor: COLORS.border,
      borderRadius: 10,
      paddingVertical: SPACING.md,
      paddingHorizontal: SPACING.md,
      marginBottom: SPACING.sm,
    },
    optionRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
    },
    optionText: {
      fontSize: 16,
      fontWeight: "600",
    },
    optionDesc: {
      marginTop: 6,
      fontSize: 13,
      lineHeight: 18,
    },
    advancedBox: {
      marginTop: SPACING.md,
      borderTopWidth: 1,
      borderTopColor: COLORS.border,
      paddingTop: SPACING.md,
    },
    advancedTitle: {
      fontSize: 15,
      fontWeight: "700",
      marginBottom: 8,
    },
    toggleRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      marginBottom: 8,
    },
    toggleLabel: {
      fontSize: 14,
    },
    saveButton: {
      marginTop: SPACING.md,
      paddingVertical: 14,
      borderRadius: 10,
      alignItems: "center",
    },
    saveText: {
      color: COLORS.white,
      fontWeight: "700",
      fontSize: 16,
    },
  });
