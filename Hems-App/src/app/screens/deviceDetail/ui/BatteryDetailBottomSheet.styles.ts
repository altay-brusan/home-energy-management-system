import { Dimensions, StyleSheet } from "react-native";
import { SPACING, TYPOGRAPHY } from "../../../../theme/constants";

export function makeBatteryDetailStyles(
  COLORS: Record<string, string>,
  isDark: boolean
) {
  return StyleSheet.create({
    scrim: {
      ...StyleSheet.absoluteFillObject,
      backgroundColor: "rgba(0,0,0,0.35)",
    },
    modalContent: {
      position: "absolute",
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: COLORS.dark,
      borderTopLeftRadius: 24,
      borderTopRightRadius: 24,
      padding: SPACING.md,
      borderTopWidth: 1,
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: -6 },
      shadowOpacity: 0.12,
      shadowRadius: 14,
      elevation: 12,
      overflow: "hidden",
      height: Dimensions.get("window").height * 0.85, 
    },
    handleContainer: {
      alignItems: "center",
      marginBottom: 12,
      marginTop: 6,
    },
    handle: {
      width: 64,
      height: 6,
      borderRadius: 999,
      backgroundColor: COLORS.border,
    },

    sheetHeader: {
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: SPACING.md,
    },
    sheetHeaderLeft: {
      flex: 1,
    },
    sheetIconContainer: {
      width: 50,
      height: 50,
      borderRadius: 25,
      backgroundColor: COLORS.accent,
      alignItems: "center",
      justifyContent: "center",
      marginLeft: SPACING.md,
    },

    title: {
      fontSize: TYPOGRAPHY.lg,
      fontWeight: "800",
      color: COLORS.textSoft,
      marginBottom: 8,
    },
    subtitle: {
      fontSize: TYPOGRAPHY.sm,
      fontWeight: "700",
      color: COLORS.slate400,
      marginBottom: 8,
    },
    section: {
      marginBottom: SPACING.md,
    },
    row: {
      flexDirection: "row",
      justifyContent: "space-between",
      alignItems: "center",
      paddingVertical: 6,
    },
    label: {
      color: COLORS.slate400,
      fontSize: TYPOGRAPHY.sm,
    },
    value: {
      color: COLORS.textSoft,
      fontSize: TYPOGRAPHY.sm,
      fontWeight: "700",
    },

    buttonRow: {
      flexDirection: "row",
      justifyContent: "space-between",
      marginTop: SPACING.md,
       marginBottom: SPACING.lg, 
      paddingHorizontal: SPACING.sm,
    },
   actionBtn: {
  flex: 1,
  height: 72,          
  marginHorizontal: 4,
  borderRadius: 12,   
  justifyContent: "center",
},
  });
}
