import { StyleSheet } from "react-native";
import { SPACING } from "../../../../../theme/constants";

export const getSolarPriorityStyles = (COLORS: Record<string, string>, isDark: boolean) =>
  StyleSheet.create({
    headerRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      marginTop: 8,
      marginBottom: 16,
      paddingHorizontal: SPACING.lg,
    },

    backButton: { padding: 6 },

    headerTitle: {
      flex: 1,
      textAlign: "center",
      fontSize: 18,
      fontWeight: "700",
      color: COLORS.textSoft,
      marginRight: 28,
    },

    saveButton: { paddingHorizontal: 8, paddingVertical: 4 },
    saveText: { fontWeight: "700", fontSize: 15 },

    explanationCard: {
      backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
      borderRadius: 12,
      padding: 14,
      marginHorizontal: SPACING.lg,
      marginBottom: 8,
      borderWidth: 1,
      borderColor: isDark ? COLORS.border : COLORS.gray200,
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
    },

    explanationTitle: {
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      fontWeight: "700",
      fontSize: 16,
    },

    explanationContent: {
      backgroundColor: isDark ? COLORS.surface : COLORS.white,
      borderRadius: 12,
      marginHorizontal: SPACING.lg,
      marginBottom: 16,
      padding: 14,
      borderWidth: 1,
      borderColor: isDark ? COLORS.border : COLORS.gray200,
    },

    explanationText: {
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      fontSize: 14,
      lineHeight: 20,
      opacity: 0.9,
      marginBottom: 8,
    },

    learnMore: { color: COLORS.accent, fontSize: 14, fontWeight: "600" },

instructionText: {
  color: isDark ? COLORS.textSoft : COLORS.slate700,
  fontSize: 14,
  textAlign: "center",
  marginBottom: 12,
  opacity: 0.9,
  flexWrap: "wrap",
  width: "90%",
  alignSelf: "center",
  lineHeight: 20,
},

    priorityCard: {
      flexDirection: "row",
      alignItems: "center",
      backgroundColor: isDark ? COLORS.cardBg : COLORS.white,
      borderRadius: 18,
      paddingVertical: 12,
      paddingHorizontal: 10,
      marginBottom: 10,
      elevation: 4,
      shadowColor: "#000",
      shadowOpacity: 0.15,
      shadowRadius: 6,
    },

    numberCircle: {
      width: 28,
      height: 28,
      borderRadius: 14,
      backgroundColor: isDark ? COLORS.slate700 : COLORS.gray200,
      alignItems: "center",
      justifyContent: "center",
      marginRight: 12,
    },

    numberText: {
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      fontWeight: "700",
      fontSize: 14,
    },

    innerCard: { flexDirection: "row", alignItems: "center", flex: 1 },

    cardLabel: {
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      fontWeight: "600",
      fontSize: 15,
    },

    iconRight: {
      marginLeft: 8,
      width: 28,
      height: 28,
      alignItems: "center",
      justifyContent: "center",
      backgroundColor: isDark ? COLORS.slate700 : COLORS.gray200,
      borderRadius: 14,
    },

    innerCardLarge: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      borderRadius: 18,
      paddingVertical: 18,
      paddingHorizontal: 14,
      marginBottom: 14,
      elevation: 5,
      shadowColor: "#000",
      shadowOpacity: 0.15,
      shadowRadius: 6,
    },

    numberCircleLarge: {
      width: 34,
      height: 34,
      borderRadius: 17,
      backgroundColor: isDark ? "#9ca3af22" : COLORS.gray100,
      alignItems: "center",
      justifyContent: "center",
      marginRight: 12,
    },

    numberTextLarge: {
      color: isDark ? COLORS.textSoft : COLORS.slate700,
      fontWeight: "700",
      fontSize: 15,
    },

    cardLabelLarge: {
      fontSize: 17,
      fontWeight: "600",
      color: isDark ? COLORS.textSoft : COLORS.slate700,
    },

    iconTitleRow: {
      flexDirection: "row",
      alignItems: "center",
      flex: 1,
    },

    iconRightLarge: {
      width: 34,
      height: 34,
      borderRadius: 17,
      alignItems: "center",
      justifyContent: "center",
      backgroundColor: isDark ? "#9ca3af22" : COLORS.gray100,
    },
  });
