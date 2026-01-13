import React from "react";
import { View, Text, Dimensions } from "react-native";

const { height: SCREEN_HEIGHT } = Dimensions.get("window");
const BAR_HEIGHT = SCREEN_HEIGHT * 0.6;

export default function BatteryBar({ COLORS, isDark }: any) {
  const labelValues = [100, 80, 60, 40, 20, 0];

  const batteryBodyColor = isDark
    ? "#3b3f46" 
    : "#e4e7eb"; 
  const gridColor = isDark ? "rgba(255,255,255,0.12)" : "rgba(0,0,0,0.2)";
  const textColor = isDark ? COLORS.textSoft : COLORS.slate700;

  return (
    <View
      style={{
        width: 90,
        height: BAR_HEIGHT,
        borderRadius: 32,
        overflow: "hidden",
        justifyContent: "flex-end",
        alignItems: "center",
        borderWidth: 2,
        borderColor: isDark ? COLORS.border : "#cfd3d8",
        backgroundColor: batteryBodyColor,
        position: "relative",
        shadowColor: isDark ? "#000" : "#999",
        shadowOpacity: isDark ? 0.35 : 0.15,
        shadowRadius: 10,
        elevation: 5,
      }}
    >
      <View
        style={{
          position: "absolute",
          bottom: 0,
          width: "100%",
          height: "100%",
          backgroundColor: COLORS.accent,
          opacity: 0.35,
        }}
      />

      <View
        style={{
          position: "absolute",
          top: 0,
          width: "100%",
          height: "28%",
          backgroundColor: isDark
            ? "rgba(255,255,255,0.08)"
            : "rgba(255,255,255,0.35)",
        }}
      />

      <View
        style={{
          position: "absolute",
          top: 2,
          left: 2,
          right: 2,
          bottom: 2,
          borderRadius: 30,
          borderWidth: 1,
          borderColor: isDark
            ? "rgba(255,255,255,0.05)"
            : "rgba(0,0,0,0.06)",
        }}
      />

      {labelValues.map((val, i) => {
        const topPos = (BAR_HEIGHT / (labelValues.length - 1)) * i;
        const labelOffset = val === 100 ? 12 : 0;
        return (
          <View
            key={val}
            style={{
              position: "absolute",
              top: topPos - labelOffset,
              width: "100%",
              flexDirection: "row",
              alignItems: "center",
              justifyContent: "center",
            }}
          >
            <View
              style={{
                width: "65%",
                height: 1,
                backgroundColor: gridColor,
                opacity: 0.5,
              }}
            />
            <Text
              style={{
                position: "absolute",
                right: 8,
                color: textColor,
                fontSize: 12,
                fontWeight: "600",
                opacity: 0.75,
              }}
            >
              {val}%
            </Text>
          </View>
        );
      })}

      <View
        style={{
          position: "absolute",
          bottom: 0,
          width: "70%",
          height: 3,
          borderRadius: 2,
          backgroundColor: COLORS.accent,
          opacity: 0.8,
        }}
      />
    </View>
  );
}