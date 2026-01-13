import { StyleSheet, Dimensions } from "react-native";
import { SPACING } from "../../../../../theme/constants";

const { height } = Dimensions.get("window");

export const getBatteryLevelStyles = (COLORS: Record<string, string>, isDark: boolean) => {
  const { width } = Dimensions.get("window");
  const isSmall = width <= 380;
  const isVerySmall = width <= 340;
  
  return StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: COLORS.background,
      paddingHorizontal: SPACING.lg,
    },
    headerRow: {
      flexDirection: "row",
      alignItems: "center",
      justifyContent: "space-between",
      marginTop: 8,
      marginBottom: 12,
      paddingHorizontal: SPACING.lg,
    },
    backButton: { padding: 6 },
    headerTitle: {
      flex: 1,
      textAlign: "center",
      fontSize: 20,
      fontWeight: "700",
      color: COLORS.textSoft,
      marginRight: 28,
    },
    infoBanner: {
      backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
      borderRadius: 10,
      paddingVertical: 10,
      paddingHorizontal: 14,
      marginHorizontal: SPACING.lg,
      marginBottom: 24,
      borderWidth: 1,
      borderColor: isDark ? COLORS.border : COLORS.gray200,
      marginTop:10,
    },
    infoText: {
      color: COLORS.textSoft,
      textAlign: "center",
      fontSize: 14,
      opacity: 0.9,
    },
    cardContainer: {
      backgroundColor: isDark ? COLORS.cardBg : COLORS.white,
      borderRadius: 24,
      padding: isVerySmall ? 12 : isSmall ? 16 : 20,
      alignSelf: "center",
      flexDirection: "row",
      justifyContent: "center",
      alignItems: "center",
      width: isVerySmall ? "95%" : "90%",
      shadowColor: "#000",
      shadowOpacity: 0.15,
      shadowRadius: 10,
      elevation: 5,
    },
    barContainer: {
      width: 100,
      height: height * 0.55,
      borderRadius: 30,
      overflow: "hidden",
      justifyContent: "flex-end",
      alignItems: "center",
      borderWidth: 2,
      borderColor: isDark ? COLORS.border : COLORS.gray200,
      backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
      position: "relative",
    },
    gridLine: {
      position: "absolute",
      width: "75%",
      height: 1,
      backgroundColor: isDark ? "#3c3c3c" : "#8f8f8f",
      opacity: 0.35,
    },
    handleCard: {
      position: "absolute",
      width: isVerySmall ? 160 : isSmall ? 175 : 190,
      backgroundColor: "transparent",
      borderRadius: 0,
      paddingVertical: isVerySmall ? 8 : 10,
      paddingHorizontal: isVerySmall ? 10 : 12,
      shadowColor: "transparent",
      shadowOpacity: 0,
      shadowRadius: 0,
      elevation: 0,
      borderWidth: 0,
      borderColor: "transparent",
      maxWidth: isVerySmall ? "85%" : "100%",
    },
    handleText: {
      fontWeight: "700",
      fontSize: isVerySmall ? 13 : isSmall ? 14 : 15,
      color: COLORS.textSoft,
      flexShrink: 1,
    },
    handleSub: {
      fontSize: isVerySmall ? 11 : isSmall ? 12 : 13,
      fontWeight: "500",
      opacity: 0.8,
    },
    handleGroup: {
      position: "absolute",
      right: 30,
      height: height * 0.55,
      justifyContent: "space-between",
      alignItems: "flex-start",
    },
    saveButton: {
      paddingHorizontal: 10,
      paddingVertical: 6,
      borderRadius: 8,
      backgroundColor: isDark ? COLORS.surface : COLORS.gray100,
      borderWidth: 1,
      borderColor: isDark ? COLORS.border : COLORS.gray200,
    },
    saveText: {
      fontWeight: "700",
      fontSize: 14,
      color: COLORS.accent,
    },
  });
};
