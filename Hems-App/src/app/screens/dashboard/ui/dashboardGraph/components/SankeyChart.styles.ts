import { StyleSheet } from "react-native";

export const getSankeyChartStyles = () => {
  return {
    ...StyleSheet.create({
      container: {
        backgroundColor: "#1e293b", 
        padding: 16,
        marginBottom: 20,
        shadowColor: "#000",
        shadowOpacity: 0.1,
        shadowRadius: 8,
        shadowOffset: { width: 0, height: 4 },
      },
      title: {
        fontSize: 18,
        fontWeight: "700",
        color: "#fff",
        textAlign: "center",
        marginBottom: 8,
      },
      chart: {
        alignSelf: "center",
      },
      nodeLabel: {
        fontSize: 12,
        fontWeight: "600",
        color: "#fff",
      },
    }),

    nodeColors: {
      solar: "#FACC15",       
      battery: "#06B6D4",      
      grid: "#3B82F6",         
    },

    linkColors: {
      batteryToSolar: "rgba(6, 182, 212, 0.6)", 
      gridToSolar: "rgba(59, 130, 246, 0.6)",   
    },
  };
};
