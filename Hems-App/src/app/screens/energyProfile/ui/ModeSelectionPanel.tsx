import React, { useEffect } from "react";
import { View, Text, TouchableOpacity } from "react-native";
import MaterialIcons from "@react-native-vector-icons/material-icons";
import Animated, {
  useAnimatedStyle,
  useSharedValue,
  withTiming,
} from "react-native-reanimated";
import { useThemeStore } from "../../../../store/themeStore";
import { getThemeColors } from "../../../../theme/constants";
import { getModeSelectionPanelStyles } from "./ModeSelectionPanel.styles";

export type OperationMode = "Battery Priority" | "Load Priority" | "Custom";
export type GeneratorMode =
  | "Generator Input Only"
  | "Smart Load Output"
  | "Microinverter Input";

export type ModeType = "operation" | "grid" | "export" | "generator";

type Props = {
  type: ModeType;
  selectedMode?: OperationMode;
  selectedGrid?: string;
  selectedExport?: string;
  selectedGeneratorMode?: GeneratorMode;
  onSelectMode?: (mode: OperationMode) => void;
  onSelectGrid?: (grid: string) => void;
  onSelectExport?: (exportMode: string) => void;
  onSelectGenerator?: (mode: GeneratorMode) => void;
  onSave: () => void;
  visible: boolean;
};

const MODES: { key: OperationMode; desc: string }[] = [
  {
    key: "Battery Priority",
    desc: "PV → Battery → Load → Grid sequence for maximum storage.",
  },
  {
    key: "Load Priority",
    desc: "PV → Load → Battery → Grid for direct self-consumption.",
  },
  {
    key: "Custom",
    desc: "Manually configure advanced control logic.",
  },
];

const GRID_CODES = ["IEEE-1547", "VDE-AR-N-4105", "AS4777"];

const EXPORT_MODES = [
  { key: "No Export", desc: "Disable all power export to grid." },
  { key: "Limited", desc: "Export with limited capacity." },
  { key: "Moderate", desc: "Balanced export between load and grid." },
  { key: "Maximum", desc: "Export as much solar as possible." },
  { key: "Custom", desc: "Manually configure export rules." },
];

const GENERATOR_MODES: { key: GeneratorMode; desc: string }[] = [
  { key: "Generator Input Only", desc: "Use port for generator input only." },
  { key: "Smart Load Output", desc: "Enable as smart load output." },
  { key: "Microinverter Input", desc: "Use as microinverter input." },
];

export default function ModeSelectionPanel({
  type,
  selectedMode,
  selectedGrid,
  selectedExport,
  selectedGeneratorMode,
  onSelectMode,
  onSelectGrid,
  onSelectExport,
  onSelectGenerator,
  onSave,
  visible,
}: Props) {
  const isDark = useThemeStore((s) => s.mode) === "dark";
  const COLORS = getThemeColors(isDark);
  const styles = getModeSelectionPanelStyles(COLORS);

  const height = useSharedValue(0);
  const opacity = useSharedValue(0);

  useEffect(() => {
    if (visible) {
      height.value = withTiming(1, { duration: 350 });
      opacity.value = withTiming(1, { duration: 350 });
    } else {
      height.value = withTiming(0, { duration: 250 });
      opacity.value = withTiming(0, { duration: 200 });
    }
  }, [visible]);

  const animStyle = useAnimatedStyle(() => ({
    opacity: opacity.value,
    transform: [{ scaleY: height.value }],
  }));

  const renderOption = (
    key: string,
    desc: string,
    isActive: boolean,
    onSelect: () => void
  ) => (
    <TouchableOpacity
      key={key}
      style={[
        styles.option,
        { borderColor: isActive ? COLORS.accent : COLORS.border },
      ]}
      onPress={onSelect}
      activeOpacity={0.85}
    >
      <View style={styles.optionRow}>
        <Text
          style={[
            styles.optionText,
            { color: isActive ? COLORS.accent : COLORS.textSoft },
          ]}
        >
          {key}
        </Text>
        {isActive && (
          <MaterialIcons name="check" size={22} color={COLORS.accent} />
        )}
      </View>
      {desc ? (
        <Text style={[styles.optionDesc, { color: COLORS.textSoft }]}>{desc}</Text>
      ) : null}
    </TouchableOpacity>
  );

  return (
    <Animated.View style={[styles.container, animStyle]}>
      {type === "operation" &&
        MODES.map((m) =>
          renderOption(m.key, m.desc, selectedMode === m.key, () =>
            onSelectMode && onSelectMode(m.key)
          )
        )}

      {type === "grid" &&
        GRID_CODES.map((code) =>
          renderOption(code, "", selectedGrid === code, () =>
            onSelectGrid && onSelectGrid(code)
          )
        )}

      {type === "export" &&
        EXPORT_MODES.map((m) =>
          renderOption(m.key, m.desc, selectedExport === m.key, () =>
            onSelectExport && onSelectExport(m.key)
          )
        )}

      {type === "generator" &&
        GENERATOR_MODES.map((m) =>
          renderOption(m.key, m.desc, selectedGeneratorMode === m.key, () =>
            onSelectGenerator && onSelectGenerator(m.key)
          )
        )}

      <TouchableOpacity
        onPress={onSave}
        style={[styles.saveButton, { backgroundColor: COLORS.accent }]}
        activeOpacity={0.8}
      >
        <Text style={styles.saveText}>Save</Text>
      </TouchableOpacity>
    </Animated.View>
  );
}
