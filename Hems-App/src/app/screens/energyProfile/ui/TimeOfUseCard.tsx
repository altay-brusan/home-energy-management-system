import React, { useState, useRef, useEffect } from "react";
import { View, Text, TouchableOpacity, Switch, ActivityIndicator } from "react-native";
import Animated, { useSharedValue, runOnJS } from "react-native-reanimated";
import { Gesture, GestureDetector } from "react-native-gesture-handler";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import ModeSelectionPanel from "./ModeSelectionPanel";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function TimeOfUseCard({ styles, COLORS, isDark, deviceParams, setIsLoading }: any) {
  const [collapsed, setCollapsed] = useState(false);
  const { t } = useI18n();
  const [activeBoxes, setActiveBoxes] = useState<boolean[]>(Array(6).fill(false));
  const [selectedPeriod, setSelectedPeriod] = useState<number | null>(null);

  const [periodSettings, setPeriodSettings] = useState(
    Array(6)
      .fill(null)
      .map(() => ({
        gridCharging: false,
        generatorCharging: false,
        exportMode: "Limited",
        pendingMode: "Limited",
        showExportPanel: false,
      }))
  );

  const [periodStats, setPeriodStats] = useState(
    Array(6)
      .fill(null)
      .map(() => ({
        dischargePower: "2.5",
        targetSOC: 75,
      }))
  );

  const toggleGrid = (index: number, val: boolean) => {
    const updated = [...periodSettings];
    updated[index].gridCharging = val;
    if (val) updated[index].generatorCharging = false;
    setPeriodSettings(updated);
  };

  const toggleGenerator = (index: number, val: boolean) => {
    const updated = [...periodSettings];
    updated[index].generatorCharging = val;
    if (val) updated[index].gridCharging = false;
    setPeriodSettings(updated);
  };

  const toggleExportPanel = (index: number) => {
    const updated = [...periodSettings];
    const isOpening = !updated[index].showExportPanel;
    updated[index].showExportPanel = isOpening;
    updated[index].pendingMode = updated[index].exportMode;
    setPeriodSettings(updated);
    
    if (!isOpening) {
      saveExportMode(index);
    }
  };

  const setPendingMode = (index: number, mode: string) => {
    const updated = [...periodSettings];
    updated[index].pendingMode = mode;
    setPeriodSettings(updated);
  };

  const saveExportMode = (index: number) => {
    const updated = [...periodSettings];
    updated[index].exportMode = updated[index].pendingMode;
    updated[index].showExportPanel = false;
    setPeriodSettings(updated);
  };

  const [boundaries, setBoundaries] = useState({
    row1: [0, 4, 8, 12],
    row2: [12, 16, 20, 24],
  });

  const [touParam, setTouParam] = useState<{id: string, valueId: string} | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [hasTouParam, setHasTouParam] = useState(false);

  const totalWidth = 300;
  const step = 0.5;
  const hourPerPixel = 12 / totalWidth;
  const activeRow = useSharedValue<"row1" | "row2" | null>(null);
  const activeIndex = useSharedValue<number | null>(null);
  const initialPos = useSharedValue(0);
  const lastTapRef = useRef<{ index: number; time: number } | null>(null);

  const formatHour = (h: number) => {
    const hh = Math.floor(h);
    const mm = (h % 1) * 60;
    return `${String(hh).padStart(2, "0")}:${mm === 0 ? "00" : "30"}`;
  };

  const saveToBackend = async () => {
    if (!deviceParams?.setupDeviceId || !touParam || isSaving) return;

    setIsSaving(true);
    try {
      const periods = Array.from({ length: 6 }).map((_, index) => {
        const row = index < 3 ? "row1" : "row2";
        const offset = row === "row1" ? 0 : 3;
        const i = index - offset;
        const start = boundaries[row][i];
        const end = boundaries[row][i + 1];

        return {
          periodIndex: index,
          startTime: start,
          endTime: end,
          startTimeFormatted: formatHour(start),
          endTimeFormatted: formatHour(end),
          isActive: activeBoxes[index],
          settings: {
            gridCharging: periodSettings[index].gridCharging,
            generatorCharging: periodSettings[index].generatorCharging,
            exportMode: periodSettings[index].exportMode,
          },
          statistics: {
            dischargePower: parseFloat(periodStats[index].dischargePower),
            targetSOC: periodStats[index].targetSOC,
            unit: "kW",
          },
        };
      });

      const touData = {
        enabled: true,
        periods,
        boundaries: {
          row1: boundaries.row1,
          row2: boundaries.row2,
        },
      };

      await setupDeviceParameterValuesService.update(touParam.valueId, {
        id: touParam.valueId,
        value: JSON.stringify(touData),
        sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      } finally {
      setIsSaving(false);
    }
  };

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    const touFound = deviceParams.findParameterByName("Time-of-use (TOU) Scheduling");
    if (touFound?.setupDeviceParameterValue) {
      setTouParam({
        id: touFound.id,
        valueId: touFound.setupDeviceParameterValue.id,
      });
      setHasTouParam(true);

      try {
        const touData = JSON.parse(touFound.setupDeviceParameterValue.value);
        
        if (touData.periods && Array.isArray(touData.periods)) {
          const loadedSettings = touData.periods.map((p: any) => ({
            gridCharging: p.settings?.gridCharging || false,
            generatorCharging: p.settings?.generatorCharging || false,
            exportMode: p.settings?.exportMode || "Limited",
            pendingMode: p.settings?.exportMode || "Limited",
            showExportPanel: false,
          }));
          setPeriodSettings(loadedSettings);

          const loadedStats = touData.periods.map((p: any) => ({
            dischargePower: p.statistics?.dischargePower?.toFixed(1) || "2.5",
            targetSOC: p.statistics?.targetSOC || 75,
          }));
          setPeriodStats(loadedStats);

          const loadedActiveBoxes = touData.periods.map((p: any) => p.isActive || false);
          setActiveBoxes(loadedActiveBoxes);
        }

        if (touData.boundaries) {
          setBoundaries({
            row1: touData.boundaries.row1 || [0, 4, 8, 12],
            row2: touData.boundaries.row2 || [12, 16, 20, 24],
          });
        }
      } catch (error) {
      }
    } else {
      setHasTouParam(false);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const getPeriodRange = (index: number) => {
    const row = index < 3 ? "row1" : "row2";
    const offset = row === "row1" ? 0 : 3;
    const i = index - offset;
    const start = boundaries[row][i];
    const end = boundaries[row][i + 1];
    return `${formatHour(start)}–${formatHour(end)}`;
  };

  const handleDoubleTap = (idx: number) => {
    const now = Date.now();
    if (lastTapRef.current && lastTapRef.current.index === idx && now - lastTapRef.current.time < 300) {
      const newActiveBoxes = [...activeBoxes];
      newActiveBoxes[idx] = !newActiveBoxes[idx];
      setActiveBoxes(newActiveBoxes);
      lastTapRef.current = null;
    } else {
      setSelectedPeriod(selectedPeriod === idx ? null : idx);
      lastTapRef.current = { index: idx, time: now };
    }
  };

  const canMoveBoundary = (row: "row1" | "row2", boundaryIndex: number, direction: "left" | "right" | null = null) => {
    if (row === "row1" && (boundaryIndex === 0 || boundaryIndex === 3)) return false;
    if (row === "row2" && (boundaryIndex === 0 || boundaryIndex === 3)) return false;
    const offset = row === "row1" ? 0 : 3;
    const leftBox = activeBoxes[offset + boundaryIndex - 1];
    const rightBox = activeBoxes[offset + boundaryIndex];
    if (direction === "left") return !leftBox;
    if (direction === "right") return !rightBox;
    return !leftBox && !rightBox;
  };

  const updateBoundary = (row: "row1" | "row2", i: number, val: number) => {
    setBoundaries((prev) => {
      const arr = [...prev[row]];
      const min = arr[i - 1];
      const max = arr[i + 1];
      const newVal = Math.max(min + step, Math.min(max - step, val));
      arr[i] = newVal;
      const newBoundaries = { ...prev, [row]: arr };
      
      return newBoundaries;
    });
  };

  const createPanGesture = (row: "row1" | "row2", index: number) => {
    const canMoveLeft = canMoveBoundary(row, index, "left");
    const canMoveRight = canMoveBoundary(row, index, "right");
    return Gesture.Pan()
      .enabled(canMoveLeft || canMoveRight)
      .onBegin(() => {
        if (!canMoveLeft && !canMoveRight) return;
        activeRow.value = row;
        activeIndex.value = index;
        initialPos.value = boundaries[row][index];
      })
      .onUpdate((e) => {
        if (!activeRow.value || activeIndex.value == null) return;
        const direction = e.translationX < 0 ? "left" : "right";
        if (direction === "left" && !canMoveLeft) return;
        if (direction === "right" && !canMoveRight) return;
        const translationHours = e.translationX * hourPerPixel;
        const newVal = initialPos.value + translationHours;
        const stepped = Math.round(newVal / step) * step;
        runOnJS(updateBoundary)(activeRow.value, activeIndex.value, stepped);
      })
      .onEnd(() => {
        activeRow.value = null;
        activeIndex.value = null;
      });
  };

  const getHandleColor = (row: "row1" | "row2", index: number) => {
    const canLeft = canMoveBoundary(row, index, "left");
    const canRight = canMoveBoundary(row, index, "right");
    const canMove = canLeft || canRight;
    return {
      bg: COLORS.accent,
      border: COLORS.accent,
      opacity: canMove ? 1 : 0.5,
    };
  };

  const renderSettingsPanel = (index: number) => {
    const s = periodSettings[index];
    const stat = periodStats[index];
    if (selectedPeriod !== index) return null;
    const range = getPeriodRange(index);
    return (
      <View
        style={{
          width: "100%",
          marginTop: 12,
          borderWidth: 1,
          borderColor: COLORS.border,
          borderRadius: 10,
          padding: 10,
        }}
      >
        <Text style={{ color: COLORS.accent, fontWeight: "600", marginBottom: 6, fontSize: 15 }}>
        {t('tou.periodSettings', { index: index + 1 })}
        </Text>
        <Text style={{ color: COLORS.textSoft, fontSize: 14, marginBottom: 8, opacity: 0.7 }}>
         {t('tou.periodRange', { index: index + 1, range })}
        </Text>
        <Text style={{ color: COLORS.textSoft, fontSize: 15, fontWeight: "400" }}>
          {t('tou.batteryDischarge')}: {stat.dischargePower} kW
        </Text>
        <Text style={{ color: COLORS.textSoft, fontSize: 15, marginBottom: 10, fontWeight: "400" }}>
         {t('tou.targetSoc')}: {stat.targetSOC}%
        </Text>
        <View style={styles.switchRow}>
          <Text style={styles.label}>{t('tou.gridCharging')}</Text>
          <Switch
            value={s.gridCharging}
            onValueChange={(v) => toggleGrid(index, v)}
            thumbColor={s.gridCharging ? COLORS.accent : isDark ? "#323030ff" : "#eae1e1ff"}
            trackColor={{ false: isDark ? "#555" : "#ccc", true: COLORS.accent + "66" }}
          />
        </View>
        <View style={styles.switchRow}>
          <Text style={styles.label}>{t('tou.generatorCharging')}</Text>
          <Switch
            value={s.generatorCharging}
            onValueChange={(v) => toggleGenerator(index, v)}
            thumbColor={s.generatorCharging ? COLORS.accent : isDark ? "#323030ff" : "#eae1e1ff"}
            trackColor={{ false: isDark ? "#555" : "#ccc", true: COLORS.accent + "66" }}
          />
        </View>
        <TouchableOpacity activeOpacity={0.8} onPress={() => toggleExportPanel(index)}>
          <View style={styles.dropdownRow}>
            <Text style={styles.subCardTitle}>{t('tou.exportMode')}</Text>
            <Text style={styles.valueAccent}>{s.exportMode}</Text>
            <FontAwesome
              name={s.showExportPanel ? "chevron-up" : "chevron-down"}
              size={16}
              color={COLORS.textSoft}
            />
          </View>
        </TouchableOpacity>
        {s.showExportPanel && (
          <View style={{ marginTop: 10 }}>
            <ModeSelectionPanel
              type="export"
              selectedExport={s.pendingMode}
              onSelectExport={(mode: string) => setPendingMode(index, mode)}
              visible={s.showExportPanel}
            />
          </View>
        )}
      </View>
    );
  };

  const renderRow = (row: "row1" | "row2", offset: number) => {
    const arr = boundaries[row];
    return (
      <View style={{ width: totalWidth, marginBottom: 22 }}>
        <View style={{ flexDirection: "row", justifyContent: "space-between", marginBottom: 6 }}>
          <Text style={styles.timeLabel}>{row === "row1" ? "00:00" : "12:00"}</Text>
          <Text style={styles.timeLabel}>{row === "row1" ? "12:00" : "24:00"}</Text>
        </View>
        <View style={{ flexDirection: "row", width: "100%", height: 60 }}>
          {[0, 1, 2].map((i) => {
            const start = arr[i];
            const end = arr[i + 1];
            const flex = end - start;
            const idx = offset + i;
            const isActive = activeBoxes[idx];
            return (
              <View
                key={i}
                style={[
                  styles.periodBox,
                  { flex, borderRightWidth: i < 2 ? 1 : 0 },
                  isActive && styles.periodBoxActive,
                ]}
              >
                <TouchableOpacity
                  activeOpacity={0.8}
                  onPress={() => handleDoubleTap(idx)}
                  style={{ width: "100%", height: "100%", justifyContent: "center", alignItems: "center" }}
                >
                  <Text style={{ fontWeight: "700", fontSize: 14 }}>{`P${idx + 1}`}</Text>
                </TouchableOpacity>
              </View>
            );
          })}
        </View>
        {[1, 2].map((boundaryIndex) => {
          const hour = arr[boundaryIndex];
          const leftPosition = ((hour - (row === "row2" ? 12 : 0)) / 12) * totalWidth - 14;
          const handleColor = getHandleColor(row, boundaryIndex);
          const gesture = createPanGesture(row, boundaryIndex);
          return (
            <GestureDetector key={boundaryIndex} gesture={gesture}>
              <Animated.View
                style={{
                  position: "absolute",
                  top: 0,
                  left: leftPosition,
                  alignItems: "center",
                  zIndex: 30,
                }}
              >
                <Text style={styles.timeLabel}>{formatHour(hour)}</Text>
                <Animated.View
                  style={{
                    top: 77,
                    width: 28,
                    height: 28,
                    backgroundColor: handleColor.bg,
                    borderRadius: 6,
                    justifyContent: "center",
                    alignItems: "center",
                    borderWidth: 1,
                    borderColor: handleColor.border,
                    opacity: handleColor.opacity,
                    shadowColor: "#000",
                    shadowOffset: { width: 0, height: 2 },
                    shadowOpacity: 0.25,
                    shadowRadius: 3.5,
                    elevation: 3,
                  }}
                >
                  <Text style={{ fontSize: 16, color: "#fff" }}>⟷</Text>
                </Animated.View>
              </Animated.View>
            </GestureDetector>
          );
        })}
      </View>
    );
  };

  if (!hasTouParam) {
    return null;
  }

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('tou.title')}</Text>

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          setCollapsed(!collapsed);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>{t('tou.schedule')}</Text>
          <FontAwesome
            name={collapsed ? "chevron-down" : "chevron-up"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {!collapsed && (
        <View style={[styles.subCard, { alignItems: "center", paddingTop: 20 }]}>
          {renderRow("row1", 0)}
          <View style={{ marginTop: 30 }}>{renderRow("row2", 3)}</View>
          <View style={{ width: "100%", marginTop: 20 }}>
            {Array.from({ length: 6 }).map((_, idx) => (
              <View key={`bottom-panel-${idx}`}>{renderSettingsPanel(idx)}</View>
            ))}
          </View>

          <TouchableOpacity
            activeOpacity={0.8}
            onPress={async () => {
              setIsLoading?.(true);
              await saveToBackend();
              setTimeout(() => {
                setIsLoading?.(false);
              }, 1500);
            }}
            disabled={isSaving}
            style={{
              width: "90%",
              marginTop: 24,
              marginBottom: 16,
              backgroundColor: COLORS.accent,
              paddingVertical: 14,
              borderRadius: 10,
              alignItems: "center",
              justifyContent: "center",
              opacity: isSaving ? 0.6 : 1,
            }}
          >
            {isSaving ? (
              <ActivityIndicator size="small" color="#fff" />
            ) : (
              <Text
                style={{
                  color: "#fff",
                  fontSize: 16,
                  fontWeight: "700",
                }}
              >
                {t('energyProfile.saveSettings')}
              </Text>
            )}
          </TouchableOpacity>
        </View>
      )}
    </View>
  );
}
