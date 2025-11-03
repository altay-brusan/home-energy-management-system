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
import { useI18n } from "../../../../hooks/useI18n";

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
  onSave?: () => void;
  visible: boolean;
};



export default function ModeSelectionPanel ({
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
  const styles = getModeSelectionPanelStyles(COLORS, isDark);

  const height = useSharedValue(0);
  const opacity = useSharedValue(0);
  const { t } = useI18n();

  const getStoredModeValue = (displayValue: string): OperationMode => {
    if (displayValue === t("modeSelection.operation.batteryPriority.title")) {
      return "Battery Priority";
    } else if (displayValue === t("modeSelection.operation.loadPriority.title")) {
      return "Load Priority";
    } else if (displayValue === t("modeSelection.operation.custom.title")) {
      return "Custom";
    }
    return displayValue as OperationMode;
  };

  const MODES = [
    { key: t("modeSelection.operation.batteryPriority.title"), desc: t("modeSelection.operation.batteryPriority.desc"), storedValue: "Battery Priority" },
    { key: t("modeSelection.operation.loadPriority.title"), desc: t("modeSelection.operation.loadPriority.desc"), storedValue: "Load Priority" },
    { key: t("modeSelection.operation.custom.title"), desc: t("modeSelection.operation.custom.desc"), storedValue: "Custom" },
  ];

  const GRID_CODES = [
    t("modeSelection.grid.ieee"),
    t("modeSelection.grid.vde"),
    t("modeSelection.grid.as")
  ];

  const EXPORT_MODES = [
    { key: t("modeSelection.export.noExport.title"), desc: t("modeSelection.export.noExport.desc") },
    { key: t("modeSelection.export.limited.title"), desc: t("modeSelection.export.limited.desc") },
    { key: t("modeSelection.export.moderate.title"), desc: t("modeSelection.export.moderate.desc") },
    { key: t("modeSelection.export.maximum.title"), desc: t("modeSelection.export.maximum.desc") },
    { key: t("modeSelection.export.custom.title"), desc: t("modeSelection.export.custom.desc") },
  ];

const GENERATOR_MODES = [
  {
    key: t("modeSelection.generator.inputOnly.title"),
    desc: t("modeSelection.generator.inputOnly.desc"),
    storedValue: "Generator Input Only",
  },
  {
    key: t("modeSelection.generator.smartLoad.title"),
    desc: t("modeSelection.generator.smartLoad.desc"),
    storedValue: "Smart Load Output",
  },
  {
    key: t("modeSelection.generator.microInput.title"),
    desc: t("modeSelection.generator.microInput.desc"),
    storedValue: "Microinverter Input",
  },
];
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
        {
          borderColor: isActive
            ? COLORS.accent
            : isDark
              ? "#FFFFFF"
              : COLORS.border,
        },
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
          renderOption(m.key, m.desc, selectedMode === m.storedValue, () =>
            onSelectMode && onSelectMode(m.storedValue as OperationMode)
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
    renderOption(
      m.key,
      m.desc,
      selectedGeneratorMode === m.storedValue,
      () => onSelectGenerator && onSelectGenerator(m.storedValue as GeneratorMode)
    )
  )}

      {type !== "export" && (
        <TouchableOpacity
          onPress={onSave}
          style={[styles.saveButton, { backgroundColor: COLORS.accent }]}
          activeOpacity={0.8}
        >
          <Text style={styles.saveText}>{t('common.save')}</Text>
        </TouchableOpacity>
      )}
    </Animated.View>
  );
}
