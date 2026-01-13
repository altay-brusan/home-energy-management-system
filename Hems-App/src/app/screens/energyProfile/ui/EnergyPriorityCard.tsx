import React, { useEffect, useState } from "react";
import { LayoutAnimation, Switch, Text, TouchableOpacity, View } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";
import { useI18n } from "../../../../hooks/useI18n";

export default function EnergyPriorityCard({
  styles,
  COLORS,
  selectedMode,
  setSelectedMode,
  selectedGrid,
  setSelectedGrid,
  handleSave,
  deviceParams,
}: any) {
  const [showEnergyPriority, setShowEnergyPriority] = useState(false);
  const [showGridPeakShaving, setShowGridPeakShaving] = useState(false);
  const { t } = useI18n();

  // API Parameters
  const [energyPriority, setEnergyPriority] = useState<string>("BatteryFirst");
  const [gridPeakShavingEnabled, setGridPeakShavingEnabled] = useState(false);
  const [gridPeakShavingLimitPower, setGridPeakShavingLimitPower] = useState<string>("8000");

  // Param references
  const [energyPriorityParam, setEnergyPriorityParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [gridPeakShavingEnabledParam, setGridPeakShavingEnabledParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [gridPeakShavingLimitPowerParam, setGridPeakShavingLimitPowerParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);

  // Has flags
  const [hasEnergyPriority, setHasEnergyPriority] = useState(false);
  const [hasGridPeakShavingEnabled, setHasGridPeakShavingEnabled] = useState(false);
  const [hasGridPeakShavingLimitPower, setHasGridPeakShavingLimitPower] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    // EnergyPrioritySelection
    const energyPriorityFound = deviceParams.findParameterByName("EnergyPrioritySelection");
    if (energyPriorityFound?.setupDeviceParameterValue) {
      setEnergyPriorityParam({
        id: energyPriorityFound.id,
        valueId: energyPriorityFound.setupDeviceParameterValue.id,
        modelNumber: energyPriorityFound.setupDeviceParameterValue.modelNumber,
      });
      setEnergyPriority(energyPriorityFound.setupDeviceParameterValue.value || "BatteryFirst");
      setSelectedMode(energyPriorityFound.setupDeviceParameterValue.value === "BatteryFirst" ? "Battery Priority" : "Load Priority");
      setHasEnergyPriority(true);
    }

    // GridPeakShavingEnabled
    const gridPeakShavingEnabledFound = deviceParams.findParameterByName("GridPeakShavingEnabled");
    if (gridPeakShavingEnabledFound?.setupDeviceParameterValue) {
      setGridPeakShavingEnabledParam({
        id: gridPeakShavingEnabledFound.id,
        valueId: gridPeakShavingEnabledFound.setupDeviceParameterValue.id,
        modelNumber: gridPeakShavingEnabledFound.setupDeviceParameterValue.modelNumber,
      });
      setGridPeakShavingEnabled(gridPeakShavingEnabledFound.setupDeviceParameterValue.value === "true");
      setHasGridPeakShavingEnabled(true);
    }

    // GridPeakShavingLimitPower
    const gridPeakShavingLimitPowerFound = deviceParams.findParameterByName("GridPeakShavingLimitPower");
    if (gridPeakShavingLimitPowerFound?.setupDeviceParameterValue) {
      setGridPeakShavingLimitPowerParam({
        id: gridPeakShavingLimitPowerFound.id,
        valueId: gridPeakShavingLimitPowerFound.setupDeviceParameterValue.id,
        modelNumber: gridPeakShavingLimitPowerFound.setupDeviceParameterValue.modelNumber,
      });
      setGridPeakShavingLimitPower(gridPeakShavingLimitPowerFound.setupDeviceParameterValue.value || "8000");
      setHasGridPeakShavingLimitPower(true);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName, setSelectedMode]);

  const handleEnergyPriorityChange = async (value: string) => {
    setEnergyPriority(value);
    setSelectedMode(value === "BatteryFirst" ? "Battery Priority" : "Load Priority");

    if (energyPriorityParam) {
      try {
        await setupDeviceParameterValuesService.update(energyPriorityParam.valueId, {
          id: energyPriorityParam.valueId,
          value: value,
          sortOrder: 0,
          modelNumber: energyPriorityParam.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update EnergyPriority:", error);
      }
    }
  };

  const handleGridPeakShavingEnabledChange = async (value: boolean) => {
    setGridPeakShavingEnabled(value);
    if (gridPeakShavingEnabledParam) {
      try {
        await setupDeviceParameterValuesService.update(gridPeakShavingEnabledParam.valueId, {
          id: gridPeakShavingEnabledParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: gridPeakShavingEnabledParam.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update GridPeakShavingEnabled:", error);
      }
    }
  };

  const getEnergyPriorityLabel = (value: string) => {
    switch (value) {
      case "BatteryFirst": return "Battery First";
      case "LoadFirst": return "Load First";
      default: return value;
    }
  };

  const hasAnyEnergyPriority = hasEnergyPriority;
  const hasAnyGridPeakShaving = hasGridPeakShavingEnabled || hasGridPeakShavingLimitPower;

  if (!hasAnyEnergyPriority && !hasAnyGridPeakShaving) {
    return null;
  }

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('energyPriority.title')}</Text>

      {/* Energy Priority Selection */}
      {hasAnyEnergyPriority && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowEnergyPriority(!showEnergyPriority);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Energy Priority</Text>
              <FontAwesome
                name={showEnergyPriority ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showEnergyPriority && (
            <View style={styles.subCard}>
              <Text style={styles.valueText}>
                Current: <Text style={styles.valueAccent}>{getEnergyPriorityLabel(energyPriority)}</Text>
              </Text>

              <View style={{ flexDirection: 'row', marginTop: 12, gap: 8 }}>
                <TouchableOpacity
                  onPress={() => handleEnergyPriorityChange("BatteryFirst")}
                  style={{
                    flex: 1,
                    padding: 12,
                    borderRadius: 8,
                    backgroundColor: energyPriority === "BatteryFirst" ? COLORS.accent : COLORS.surface,
                    borderWidth: 1,
                    borderColor: energyPriority === "BatteryFirst" ? COLORS.accent : COLORS.border,
                  }}
                >
                  <Text style={{
                    textAlign: 'center',
                    color: energyPriority === "BatteryFirst" ? '#fff' : COLORS.text,
                    fontWeight: '600',
                  }}>
                    Battery First
                  </Text>
                </TouchableOpacity>

                <TouchableOpacity
                  onPress={() => handleEnergyPriorityChange("LoadFirst")}
                  style={{
                    flex: 1,
                    padding: 12,
                    borderRadius: 8,
                    backgroundColor: energyPriority === "LoadFirst" ? COLORS.accent : COLORS.surface,
                    borderWidth: 1,
                    borderColor: energyPriority === "LoadFirst" ? COLORS.accent : COLORS.border,
                  }}
                >
                  <Text style={{
                    textAlign: 'center',
                    color: energyPriority === "LoadFirst" ? '#fff' : COLORS.text,
                    fontWeight: '600',
                  }}>
                    Load First
                  </Text>
                </TouchableOpacity>
              </View>
            </View>
          )}
        </>
      )}

      {/* Grid Peak Shaving */}
      {hasAnyGridPeakShaving && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowGridPeakShaving(!showGridPeakShaving);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Grid Peak Shaving</Text>
              <FontAwesome
                name={showGridPeakShaving ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showGridPeakShaving && (
            <View style={styles.subCard}>
              {hasGridPeakShavingEnabled && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Peak Shaving Enabled</Text>
                  <Switch
                    value={gridPeakShavingEnabled}
                    onValueChange={handleGridPeakShavingEnabledChange}
                    thumbColor={gridPeakShavingEnabled ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                  />
                </View>
              )}
              {hasGridPeakShavingLimitPower && (
                <Text style={styles.valueText}>
                  Limit Power: <Text style={styles.valueAccent}>{gridPeakShavingLimitPower} W</Text>
                </Text>
              )}
            </View>
          )}
        </>
      )}
    </View>
  );
}
