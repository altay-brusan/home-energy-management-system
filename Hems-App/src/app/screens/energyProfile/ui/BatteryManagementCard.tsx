import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, LayoutAnimation, Switch } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation } from "@react-navigation/native";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function BatteryManagementCard({ styles, COLORS, deviceParams }: any) {
  const navigation = useNavigation<any>();
  const [showGridCharging, setShowGridCharging] = useState(false);
  const [showGeneratorCharging, setShowGeneratorCharging] = useState(false);
  const [showProtection, setShowProtection] = useState(false);
  const { t } = useI18n();

  // Grid Charging parameters
  const [gridChargingEnabled, setGridChargingEnabled] = useState(false);
  const [gridChargingStartSOC, setGridChargingStartSOC] = useState<string>("30");
  const [gridChargingCurrent, setGridChargingCurrent] = useState<string>("40");

  // Generator Charging parameters
  const [generatorChargingEnabled, setGeneratorChargingEnabled] = useState(false);
  const [generatorChargingStartSOC, setGeneratorChargingStartSOC] = useState<string>("30");
  const [generatorChargingCurrent, setGeneratorChargingCurrent] = useState<string>("40");
  const [generatorMaxRunTime, setGeneratorMaxRunTime] = useState<string>("24");
  const [generatorCoolingTime, setGeneratorCoolingTime] = useState<string>("0");

  // Protection Thresholds
  const [shutdownSOC, setShutdownSOC] = useState<string>("20");
  const [lowBatteryWarning, setLowBatteryWarning] = useState<string>("35");
  const [restartSOC, setRestartSOC] = useState<string>("50");

  // Param references for updates
  const [gridChargingEnabledParam, setGridChargingEnabledParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [gridChargingStartSOCParam, setGridChargingStartSOCParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [gridChargingCurrentParam, setGridChargingCurrentParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [generatorChargingEnabledParam, setGeneratorChargingEnabledParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [shutdownSOCParam, setShutdownSOCParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [lowBatteryWarningParam, setLowBatteryWarningParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [restartSOCParam, setRestartSOCParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);

  // Has flags
  const [hasGridChargingEnabled, setHasGridChargingEnabled] = useState(false);
  const [hasGridChargingStartSOC, setHasGridChargingStartSOC] = useState(false);
  const [hasGridChargingCurrent, setHasGridChargingCurrent] = useState(false);
  const [hasGeneratorChargingEnabled, setHasGeneratorChargingEnabled] = useState(false);
  const [hasGeneratorChargingStartSOC, setHasGeneratorChargingStartSOC] = useState(false);
  const [hasGeneratorChargingCurrent, setHasGeneratorChargingCurrent] = useState(false);
  const [hasGeneratorMaxRunTime, setHasGeneratorMaxRunTime] = useState(false);
  const [hasGeneratorCoolingTime, setHasGeneratorCoolingTime] = useState(false);
  const [hasShutdownSOC, setHasShutdownSOC] = useState(false);
  const [hasLowBatteryWarning, setHasLowBatteryWarning] = useState(false);
  const [hasRestartSOC, setHasRestartSOC] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    // Grid Charging
    const gridChargingEnabledFound = deviceParams.findParameterByName("GridChargingEnabled");
    if (gridChargingEnabledFound?.setupDeviceParameterValue) {
      setGridChargingEnabledParam({
        id: gridChargingEnabledFound.id,
        valueId: gridChargingEnabledFound.setupDeviceParameterValue.id,
        modelNumber: gridChargingEnabledFound.setupDeviceParameterValue.modelNumber,
      });
      setGridChargingEnabled(gridChargingEnabledFound.setupDeviceParameterValue.value === "true");
      setHasGridChargingEnabled(true);
    }

    const gridChargingStartSOCFound = deviceParams.findParameterByName("GridChargingStartSOC");
    if (gridChargingStartSOCFound?.setupDeviceParameterValue) {
      setGridChargingStartSOCParam({
        id: gridChargingStartSOCFound.id,
        valueId: gridChargingStartSOCFound.setupDeviceParameterValue.id,
        modelNumber: gridChargingStartSOCFound.setupDeviceParameterValue.modelNumber,
      });
      setGridChargingStartSOC(gridChargingStartSOCFound.setupDeviceParameterValue.value || "30");
      setHasGridChargingStartSOC(true);
    }

    const gridChargingCurrentFound = deviceParams.findParameterByName("GridChargingCurrent");
    if (gridChargingCurrentFound?.setupDeviceParameterValue) {
      setGridChargingCurrentParam({
        id: gridChargingCurrentFound.id,
        valueId: gridChargingCurrentFound.setupDeviceParameterValue.id,
        modelNumber: gridChargingCurrentFound.setupDeviceParameterValue.modelNumber,
      });
      setGridChargingCurrent(gridChargingCurrentFound.setupDeviceParameterValue.value || "40");
      setHasGridChargingCurrent(true);
    }

    // Generator Charging
    const generatorChargingEnabledFound = deviceParams.findParameterByName("GeneratorChargingEnabled");
    if (generatorChargingEnabledFound?.setupDeviceParameterValue) {
      setGeneratorChargingEnabledParam({
        id: generatorChargingEnabledFound.id,
        valueId: generatorChargingEnabledFound.setupDeviceParameterValue.id,
        modelNumber: generatorChargingEnabledFound.setupDeviceParameterValue.modelNumber,
      });
      setGeneratorChargingEnabled(generatorChargingEnabledFound.setupDeviceParameterValue.value === "true");
      setHasGeneratorChargingEnabled(true);
    }

    const generatorChargingStartSOCFound = deviceParams.findParameterByName("GeneratorChargingStartSOC");
    if (generatorChargingStartSOCFound?.setupDeviceParameterValue) {
      setGeneratorChargingStartSOC(generatorChargingStartSOCFound.setupDeviceParameterValue.value || "30");
      setHasGeneratorChargingStartSOC(true);
    }

    const generatorChargingCurrentFound = deviceParams.findParameterByName("GeneratorChargingCurrent");
    if (generatorChargingCurrentFound?.setupDeviceParameterValue) {
      setGeneratorChargingCurrent(generatorChargingCurrentFound.setupDeviceParameterValue.value || "40");
      setHasGeneratorChargingCurrent(true);
    }

    const generatorMaxRunTimeFound = deviceParams.findParameterByName("GeneratorMaxRunTime");
    if (generatorMaxRunTimeFound?.setupDeviceParameterValue) {
      setGeneratorMaxRunTime(generatorMaxRunTimeFound.setupDeviceParameterValue.value || "24");
      setHasGeneratorMaxRunTime(true);
    }

    const generatorCoolingTimeFound = deviceParams.findParameterByName("GeneratorCoolingTime");
    if (generatorCoolingTimeFound?.setupDeviceParameterValue) {
      setGeneratorCoolingTime(generatorCoolingTimeFound.setupDeviceParameterValue.value || "0");
      setHasGeneratorCoolingTime(true);
    }

    // Protection Thresholds
    const shutdownSOCFound = deviceParams.findParameterByName("ShutdownSOC");
    if (shutdownSOCFound?.setupDeviceParameterValue) {
      setShutdownSOCParam({
        id: shutdownSOCFound.id,
        valueId: shutdownSOCFound.setupDeviceParameterValue.id,
        modelNumber: shutdownSOCFound.setupDeviceParameterValue.modelNumber,
      });
      setShutdownSOC(shutdownSOCFound.setupDeviceParameterValue.value || "20");
      setHasShutdownSOC(true);
    }

    const lowBatteryWarningFound = deviceParams.findParameterByName("LowBatteryWarning");
    if (lowBatteryWarningFound?.setupDeviceParameterValue) {
      setLowBatteryWarningParam({
        id: lowBatteryWarningFound.id,
        valueId: lowBatteryWarningFound.setupDeviceParameterValue.id,
        modelNumber: lowBatteryWarningFound.setupDeviceParameterValue.modelNumber,
      });
      setLowBatteryWarning(lowBatteryWarningFound.setupDeviceParameterValue.value || "35");
      setHasLowBatteryWarning(true);
    }

    const restartSOCFound = deviceParams.findParameterByName("RestartSOC");
    if (restartSOCFound?.setupDeviceParameterValue) {
      setRestartSOCParam({
        id: restartSOCFound.id,
        valueId: restartSOCFound.setupDeviceParameterValue.id,
        modelNumber: restartSOCFound.setupDeviceParameterValue.modelNumber,
      });
      setRestartSOC(restartSOCFound.setupDeviceParameterValue.value || "50");
      setHasRestartSOC(true);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handleGridChargingEnabledChange = async (value: boolean) => {
    setGridChargingEnabled(value);
    if (gridChargingEnabledParam) {
      try {
        await setupDeviceParameterValuesService.update(gridChargingEnabledParam.valueId, {
          id: gridChargingEnabledParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: gridChargingEnabledParam.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update GridChargingEnabled:", error);
      }
    }
  };

  const handleGeneratorChargingEnabledChange = async (value: boolean) => {
    setGeneratorChargingEnabled(value);
    if (generatorChargingEnabledParam) {
      try {
        await setupDeviceParameterValuesService.update(generatorChargingEnabledParam.valueId, {
          id: generatorChargingEnabledParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: generatorChargingEnabledParam.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update GeneratorChargingEnabled:", error);
      }
    }
  };

  const hasAnyGridCharging = hasGridChargingEnabled || hasGridChargingStartSOC || hasGridChargingCurrent;
  const hasAnyGeneratorCharging = hasGeneratorChargingEnabled || hasGeneratorChargingStartSOC || hasGeneratorChargingCurrent || hasGeneratorMaxRunTime || hasGeneratorCoolingTime;
  const hasAnyProtection = hasShutdownSOC || hasLowBatteryWarning || hasRestartSOC;

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('batteryManagement.title')}</Text>

      {/* Grid Charging Section */}
      {hasAnyGridCharging && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowGridCharging(!showGridCharging);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Grid Charging</Text>
              <FontAwesome
                name={showGridCharging ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showGridCharging && (
            <View style={styles.subCard}>
              {hasGridChargingEnabled && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Grid Charging Enabled</Text>
                  <Switch
                    value={gridChargingEnabled}
                    onValueChange={handleGridChargingEnabledChange}
                    thumbColor={gridChargingEnabled ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                  />
                </View>
              )}
              {hasGridChargingStartSOC && (
                <Text style={styles.valueText}>
                  Start SOC: <Text style={styles.valueAccent}>{gridChargingStartSOC}%</Text>
                </Text>
              )}
              {hasGridChargingCurrent && (
                <Text style={styles.valueText}>
                  Charging Current: <Text style={styles.valueAccent}>{gridChargingCurrent} A</Text>
                </Text>
              )}
            </View>
          )}
        </>
      )}

      {/* Generator Charging Section */}
      {hasAnyGeneratorCharging && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowGeneratorCharging(!showGeneratorCharging);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Generator Charging</Text>
              <FontAwesome
                name={showGeneratorCharging ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showGeneratorCharging && (
            <View style={styles.subCard}>
              {hasGeneratorChargingEnabled && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Generator Charging Enabled</Text>
                  <Switch
                    value={generatorChargingEnabled}
                    onValueChange={handleGeneratorChargingEnabledChange}
                    thumbColor={generatorChargingEnabled ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                  />
                </View>
              )}
              {hasGeneratorChargingStartSOC && (
                <Text style={styles.valueText}>
                  Start SOC: <Text style={styles.valueAccent}>{generatorChargingStartSOC}%</Text>
                </Text>
              )}
              {hasGeneratorChargingCurrent && (
                <Text style={styles.valueText}>
                  Charging Current: <Text style={styles.valueAccent}>{generatorChargingCurrent} A</Text>
                </Text>
              )}
              {hasGeneratorMaxRunTime && (
                <Text style={styles.valueText}>
                  Max Run Time: <Text style={styles.valueAccent}>{generatorMaxRunTime} hrs</Text>
                </Text>
              )}
              {hasGeneratorCoolingTime && (
                <Text style={styles.valueText}>
                  Cooling Time: <Text style={styles.valueAccent}>{generatorCoolingTime} hrs</Text>
                </Text>
              )}
            </View>
          )}
        </>
      )}

      {/* Protection Thresholds Section */}
      {hasAnyProtection && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowProtection(!showProtection);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Protection Thresholds</Text>
              <FontAwesome
                name={showProtection ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showProtection && (
            <View style={styles.subCard}>
              {hasShutdownSOC && (
                <Text style={styles.valueText}>
                  Shutdown SOC: <Text style={styles.valueAccent}>{shutdownSOC}%</Text>
                </Text>
              )}
              {hasLowBatteryWarning && (
                <Text style={styles.valueText}>
                  Low Battery Warning: <Text style={styles.valueAccent}>{lowBatteryWarning}%</Text>
                </Text>
              )}
              {hasRestartSOC && (
                <Text style={styles.valueText}>
                  Restart SOC: <Text style={styles.valueAccent}>{restartSOC}%</Text>
                </Text>
              )}

              <TouchableOpacity
                activeOpacity={0.7}
                onPress={() => navigation.navigate("BatteryLevelSettings")}
                style={{
                  flexDirection: "row",
                  alignItems: "center",
                  alignSelf: "flex-start",
                  marginTop: 16,
                }}
              >
                <Text
                  style={{
                    color: COLORS.accent,
                    fontWeight: "700",
                    fontSize: 18,
                    marginRight: 6,
                  }}
                >
                  {t('batteryManagement.batterySettings')}
                </Text>
                <FontAwesome name="cog" size={16} color={COLORS.accent} />
              </TouchableOpacity>
            </View>
          )}
        </>
      )}
    </View>
  );
}
