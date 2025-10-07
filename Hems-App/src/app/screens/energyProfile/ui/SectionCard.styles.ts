import { StyleSheet } from "react-native";
import { SPACING } from "../../../../theme/constants";

export const getSectionCardStyles = (
  COLORS: Record<string, string>,
  isDark: boolean
) =>
  StyleSheet.create({
    card: {
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      paddingVertical: SPACING.md,
      paddingHorizontal: SPACING.lg,
      marginBottom: SPACING.lg,
      borderWidth: 1,
      borderColor: COLORS.border,
      shadowColor: isDark ? "#000" : "#222",
      shadowOpacity: 0.15,
      shadowRadius: 6,
      shadowOffset: { width: 0, height: 3 },
      elevation: 4,
    },
    headerRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      marginBottom: SPACING.sm,
    },
    title: {
      fontSize: 18,
      fontWeight: "700",
      color: COLORS.text,
    },
    row: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      paddingVertical: 10,
      borderBottomWidth: 0.6,
      borderBottomColor: COLORS.border,
    },
    label: {
      fontSize: 15,
      color: COLORS.text,
    },
    desc: {
      fontSize: 13,
      color: COLORS.textSoft,
      marginTop: 4,
      lineHeight: 18,
    },
    changeModeRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "center",
      marginTop: SPACING.sm,
    },
    changeModeText: {
      fontSize: 14,
      fontWeight: "600",
      color: COLORS.accent,
      marginRight: 4,
    },
  });
