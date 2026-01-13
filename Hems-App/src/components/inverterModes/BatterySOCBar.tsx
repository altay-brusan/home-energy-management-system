import React from "react";
import { View, Text } from "react-native";

interface BatterySOCBarProps {
  barHeight: number;
  COLORS: Record<string, string>;
  isDark: boolean;
}

export default function BatterySOCBar({ barHeight, COLORS, isDark }: BatterySOCBarProps) {
  const labelValues = [100, 80, 60, 40, 20, 0];

  const batteryBodyColor = isDark
    ? "#3b3f46"
    : "#e4e7eb";
  const gridColor = isDark ? "rgba(255,255,255,0.12)" : "rgba(0,0,0,0.2)";
  const textColor = isDark ? COLORS.textSoft : COLORS.slate700;

  return (
    <View
      style={{
        width: 70,
        height: barHeight,
        borderRadius: 24,
        overflow: "hidden",
        justifyContent: "flex-end",
        alignItems: "center",
        borderWidth: 2,
        borderColor: isDark ? COLORS.border : "#cfd3d8",
        backgroundColor: batteryBodyColor,
        position: "relative",
        shadowColor: isDark ? "#000" : "#999",
        shadowOpacity: isDark ? 0.35 : 0.15,
        shadowRadius: 8,
        elevation: 4,
      }}
    >
      {/* Background fill */}
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

      {/* Glossy highlight */}
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

      {/* Inner border */}
      <View
        style={{
          position: "absolute",
          top: 2,
          left: 2,
          right: 2,
          bottom: 2,
          borderRadius: 22,
          borderWidth: 1,
          borderColor: isDark
            ? "rgba(255,255,255,0.05)"
            : "rgba(0,0,0,0.06)",
        }}
      />

      {/* Grid lines and labels */}
      {labelValues.map((val, i) => {
        const topPos = (barHeight / (labelValues.length - 1)) * i;
        const labelOffset = val === 100 ? 10 : 0;
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
                width: "60%",
                height: 1,
                backgroundColor: gridColor,
                opacity: 0.5,
              }}
            />
            <Text
              style={{
                position: "absolute",
                right: 6,
                color: textColor,
                fontSize: 10,
                fontWeight: "600",
                opacity: 0.75,
              }}
            >
              {val}%
            </Text>
          </View>
        );
      })}

      {/* Bottom indicator */}
      <View
        style={{
          position: "absolute",
          bottom: 0,
          width: "65%",
          height: 3,
          borderRadius: 2,
          backgroundColor: COLORS.accent,
          opacity: 0.8,
        }}
      />
    </View>
  );
}
