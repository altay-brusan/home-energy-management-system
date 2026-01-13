import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation } from "@react-navigation/native";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function SolarPowerCard({ styles, COLORS, isDark, deviceParams }: any) {
  const navigation = useNavigation<any>();
  const [showInverterMode, setShowInverterMode] = useState(false);
  const [showExportStrategy, setShowExportStrategy] = useState(false);
  const { t } = useI18n();

  // InverterMode parameters from API
  const [solarSell, setSolarSell] = useState(false);
  const [maxSellPower, setMaxSellPower] = useState<string>("12000");
  const [zeroExportLimit, setZeroExportLimit] = useState<string>("2");
  const [forceOffGrid, setForceOffGrid] = useState(false);

  // ExportStrategy parameter
  const [exportStrategy, setExportStrategy] = useState<string>("ZeroExportToCT");

  // Param references for updates
  const [solarSellParam, setSolarSellParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [maxSellPowerParam, setMaxSellPowerParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [zeroExportLimitParam, setZeroExportLimitParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [forceOffGridParam, setForceOffGridParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [exportStrategyParam, setExportStrategyParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);

  // Has flags
  const [hasSolarSell, setHasSolarSell] = useState(false);
  const [hasMaxSellPower, setHasMaxSellPower] = useState(false);
  const [hasZeroExportLimit, setHasZeroExportLimit] = useState(false);
  const [hasForceOffGrid, setHasForceOffGrid] = useState(false);
  const [hasExportStrategy, setHasExportStrategy] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    // SolarSell
    const solarSellFound = deviceParams.findParameterByName("SolarSell");
    if (solarSellFound?.setupDeviceParameterValue) {
      setSolarSellParam({
        id: solarSellFound.id,
        valueId: solarSellFound.setupDeviceParameterValue.id,
        modelNumber: solarSellFound.setupDeviceParameterValue.modelNumber,
      });
      setSolarSell(solarSellFound.setupDeviceParameterValue.value === "true");
      setHasSolarSell(true);
    }

    // MaxSellPower
    const maxSellPowerFound = deviceParams.findParameterByName("MaxSellPower");
    if (maxSellPowerFound?.setupDeviceParameterValue) {
      setMaxSellPowerParam({
        id: maxSellPowerFound.id,
        valueId: maxSellPowerFound.setupDeviceParameterValue.id,
        modelNumber: maxSellPowerFound.setupDeviceParameterValue.modelNumber,
      });
      setMaxSellPower(maxSellPowerFound.setupDeviceParameterValue.value || "12000");
      setHasMaxSellPower(true);
    }

    // ZeroExportLimit
    const zeroExportLimitFound = deviceParams.findParameterByName("ZeroExportLimit");
    if (zeroExportLimitFound?.setupDeviceParameterValue) {
      setZeroExportLimitParam({
        id: zeroExportLimitFound.id,
        valueId: zeroExportLimitFound.setupDeviceParameterValue.id,
        modelNumber: zeroExportLimitFound.setupDeviceParameterValue.modelNumber,
      });
      setZeroExportLimit(zeroExportLimitFound.setupDeviceParameterValue.value || "2");
      setHasZeroExportLimit(true);
    }

    // ForceOffGrid
    const forceOffGridFound = deviceParams.findParameterByName("ForceOffGrid");
    if (forceOffGridFound?.setupDeviceParameterValue) {
      setForceOffGridParam({
        id: forceOffGridFound.id,
        valueId: forceOffGridFound.setupDeviceParameterValue.id,
        modelNumber: forceOffGridFound.setupDeviceParameterValue.modelNumber,
      });
      setForceOffGrid(forceOffGridFound.setupDeviceParameterValue.value === "true");
      setHasForceOffGrid(true);
    }

    // ExportStrategySelection
    const exportStrategyFound = deviceParams.findParameterByName("ExportStrategySelection");
    if (exportStrategyFound?.setupDeviceParameterValue) {
      setExportStrategyParam({
        id: exportStrategyFound.id,
        valueId: exportStrategyFound.setupDeviceParameterValue.id,
        modelNumber: exportStrategyFound.setupDeviceParameterValue.modelNumber,
      });
      setExportStrategy(exportStrategyFound.setupDeviceParameterValue.value || "ZeroExportToCT");
      setHasExportStrategy(true);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handleSolarSellChange = async (value: boolean) => {
    setSolarSell(value);
    if (solarSellParam) {
      try {
        await setupDeviceParameterValuesService.update(solarSellParam.valueId, {
          id: solarSellParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: solarSellParam.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update SolarSell:", error);
      }
    }
  };

  const handleForceOffGridChange = async (value: boolean) => {
    setForceOffGrid(value);
    if (forceOffGridParam) {
      try {
        await setupDeviceParameterValuesService.update(forceOffGridParam.valueId, {
          id: forceOffGridParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: forceOffGridParam.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update ForceOffGrid:", error);
      }
    }
  };

  const hasAnyInverterMode = hasSolarSell || hasMaxSellPower || hasZeroExportLimit || hasForceOffGrid;
  const hasAnyExportStrategy = hasExportStrategy;

  const getExportStrategyLabel = (value: string) => {
    switch (value) {
      case "SellingFirst": return "Selling First";
      case "ZeroExportToLoad": return "Zero Export to Load";
      case "ZeroExportToCT": return "Zero Export to CT";
      default: return value;
    }
  };

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('solarPower.title')}</Text>

      {/* Export Strategy Section */}
      {hasAnyExportStrategy && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowExportStrategy(!showExportStrategy);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Export Strategy</Text>
              <FontAwesome
                name={showExportStrategy ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showExportStrategy && (
            <View style={styles.subCard}>
              <Text style={styles.valueText}>
                Current Strategy: <Text style={styles.valueAccent}>{getExportStrategyLabel(exportStrategy)}</Text>
              </Text>
            </View>
          )}
        </>
      )}

      {/* Inverter Mode Section */}
      {hasAnyInverterMode && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowInverterMode(!showInverterMode);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Inverter Mode</Text>
              <FontAwesome
                name={showInverterMode ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showInverterMode && (
            <View style={styles.subCard}>
              {hasSolarSell && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Solar Sell</Text>
                  <Switch
                    value={solarSell}
                    onValueChange={handleSolarSellChange}
                    thumbColor={solarSell ? COLORS.accent : isDark ? "#323030ff" : "#eae1e1ff"}
                    trackColor={{ false: isDark ? "#555" : "#ccc", true: COLORS.accent + "66" }}
                    ios_backgroundColor={isDark ? "#555" : "#ccc"}
                  />
                </View>
              )}
              {hasMaxSellPower && (
                <Text style={styles.valueText}>
                  Max Sell Power: <Text style={styles.valueAccent}>{maxSellPower} W</Text>
                </Text>
              )}
              {hasZeroExportLimit && (
                <Text style={styles.valueText}>
                  Zero Export Limit: <Text style={styles.valueAccent}>{zeroExportLimit} W</Text>
                </Text>
              )}
              {hasForceOffGrid && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Force Off-Grid</Text>
                  <Switch
                    value={forceOffGrid}
                    onValueChange={handleForceOffGridChange}
                    thumbColor={forceOffGrid ? COLORS.accent : isDark ? "#323030ff" : "#eae1e1ff"}
                    trackColor={{ false: isDark ? "#555" : "#ccc", true: COLORS.accent + "66" }}
                    ios_backgroundColor={isDark ? "#555" : "#ccc"}
                  />
                </View>
              )}
            </View>
          )}
        </>
      )}
    </View>
  );
}
