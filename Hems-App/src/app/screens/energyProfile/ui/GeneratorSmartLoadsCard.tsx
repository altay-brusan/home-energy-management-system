import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function GeneratorSmartLoadsCard({ styles, COLORS, setIsLoading, isDark, deviceParams }: any) {
  const { t } = useI18n();
  const [showGeneratorSettings, setShowGeneratorSettings] = useState(false);
  const [showGeneratorPeakShaving, setShowGeneratorPeakShaving] = useState(false);
  const [showSmartLoad, setShowSmartLoad] = useState(false);

  // Generator Settings
  const [generatorCharge, setGeneratorCharge] = useState(false);
  const [generatorGridCharge, setGeneratorGridCharge] = useState(false);
  const [generatorStartSOC, setGeneratorStartSOC] = useState<string>("30");
  const [generatorChargeCurrent, setGeneratorChargeCurrent] = useState<string>("40");

  // Generator Peak Shaving
  const [generatorPeakShavingEnabled, setGeneratorPeakShavingEnabled] = useState(false);
  const [generatorPeakShavingLimitPower, setGeneratorPeakShavingLimitPower] = useState<string>("8000");

  // Smart Load
  const [smartLoadEnabled, setSmartLoadEnabled] = useState(false);
  const [smartLoadOffSOC, setSmartLoadOffSOC] = useState<string>("95");
  const [smartLoadOnSOC, setSmartLoadOnSOC] = useState<string>("100");
  const [smartLoadPVPowerThreshold, setSmartLoadPVPowerThreshold] = useState<string>("0");
  const [smartLoadOpenDelay, setSmartLoadOpenDelay] = useState<string>("100");
  const [smartLoadOnGridAlwaysOn, setSmartLoadOnGridAlwaysOn] = useState(false);

  // Param references
  const [generatorChargeParam, setGeneratorChargeParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [generatorGridChargeParam, setGeneratorGridChargeParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [generatorPeakShavingEnabledParam, setGeneratorPeakShavingEnabledParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [smartLoadEnabledParam, setSmartLoadEnabledParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);
  const [smartLoadOnGridAlwaysOnParam, setSmartLoadOnGridAlwaysOnParam] = useState<{id: string, valueId: string, modelNumber: string} | null>(null);

  // Has flags
  const [hasGeneratorCharge, setHasGeneratorCharge] = useState(false);
  const [hasGeneratorGridCharge, setHasGeneratorGridCharge] = useState(false);
  const [hasGeneratorStartSOC, setHasGeneratorStartSOC] = useState(false);
  const [hasGeneratorChargeCurrent, setHasGeneratorChargeCurrent] = useState(false);
  const [hasGeneratorPeakShavingEnabled, setHasGeneratorPeakShavingEnabled] = useState(false);
  const [hasGeneratorPeakShavingLimitPower, setHasGeneratorPeakShavingLimitPower] = useState(false);
  const [hasSmartLoadEnabled, setHasSmartLoadEnabled] = useState(false);
  const [hasSmartLoadOffSOC, setHasSmartLoadOffSOC] = useState(false);
  const [hasSmartLoadOnSOC, setHasSmartLoadOnSOC] = useState(false);
  const [hasSmartLoadPVPowerThreshold, setHasSmartLoadPVPowerThreshold] = useState(false);
  const [hasSmartLoadOpenDelay, setHasSmartLoadOpenDelay] = useState(false);
  const [hasSmartLoadOnGridAlwaysOn, setHasSmartLoadOnGridAlwaysOn] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    // Generator Settings
    const generatorChargeFound = deviceParams.findParameterByName("GeneratorCharge");
    if (generatorChargeFound?.setupDeviceParameterValue) {
      setGeneratorChargeParam({
        id: generatorChargeFound.id,
        valueId: generatorChargeFound.setupDeviceParameterValue.id,
        modelNumber: generatorChargeFound.setupDeviceParameterValue.modelNumber,
      });
      setGeneratorCharge(generatorChargeFound.setupDeviceParameterValue.value === "true");
      setHasGeneratorCharge(true);
    }

    const generatorGridChargeFound = deviceParams.findParameterByName("GeneratorGridCharge");
    if (generatorGridChargeFound?.setupDeviceParameterValue) {
      setGeneratorGridChargeParam({
        id: generatorGridChargeFound.id,
        valueId: generatorGridChargeFound.setupDeviceParameterValue.id,
        modelNumber: generatorGridChargeFound.setupDeviceParameterValue.modelNumber,
      });
      setGeneratorGridCharge(generatorGridChargeFound.setupDeviceParameterValue.value === "true");
      setHasGeneratorGridCharge(true);
    }

    const generatorStartSOCFound = deviceParams.findParameterByName("GeneratorStartSOC");
    if (generatorStartSOCFound?.setupDeviceParameterValue) {
      setGeneratorStartSOC(generatorStartSOCFound.setupDeviceParameterValue.value || "30");
      setHasGeneratorStartSOC(true);
    }

    const generatorChargeCurrentFound = deviceParams.findParameterByName("GeneratorChargeCurrent");
    if (generatorChargeCurrentFound?.setupDeviceParameterValue) {
      setGeneratorChargeCurrent(generatorChargeCurrentFound.setupDeviceParameterValue.value || "40");
      setHasGeneratorChargeCurrent(true);
    }

    // Generator Peak Shaving
    const generatorPeakShavingEnabledFound = deviceParams.findParameterByName("GeneratorPeakShavingEnabled");
    if (generatorPeakShavingEnabledFound?.setupDeviceParameterValue) {
      setGeneratorPeakShavingEnabledParam({
        id: generatorPeakShavingEnabledFound.id,
        valueId: generatorPeakShavingEnabledFound.setupDeviceParameterValue.id,
        modelNumber: generatorPeakShavingEnabledFound.setupDeviceParameterValue.modelNumber,
      });
      setGeneratorPeakShavingEnabled(generatorPeakShavingEnabledFound.setupDeviceParameterValue.value === "true");
      setHasGeneratorPeakShavingEnabled(true);
    }

    const generatorPeakShavingLimitPowerFound = deviceParams.findParameterByName("GeneratorPeakShavingLimitPower");
    if (generatorPeakShavingLimitPowerFound?.setupDeviceParameterValue) {
      setGeneratorPeakShavingLimitPower(generatorPeakShavingLimitPowerFound.setupDeviceParameterValue.value || "8000");
      setHasGeneratorPeakShavingLimitPower(true);
    }

    // Smart Load
    const smartLoadEnabledFound = deviceParams.findParameterByName("SmartLoadEnabled");
    if (smartLoadEnabledFound?.setupDeviceParameterValue) {
      setSmartLoadEnabledParam({
        id: smartLoadEnabledFound.id,
        valueId: smartLoadEnabledFound.setupDeviceParameterValue.id,
        modelNumber: smartLoadEnabledFound.setupDeviceParameterValue.modelNumber,
      });
      setSmartLoadEnabled(smartLoadEnabledFound.setupDeviceParameterValue.value === "true");
      setHasSmartLoadEnabled(true);
    }

    const smartLoadOffSOCFound = deviceParams.findParameterByName("SmartLoadOffSOC");
    if (smartLoadOffSOCFound?.setupDeviceParameterValue) {
      setSmartLoadOffSOC(smartLoadOffSOCFound.setupDeviceParameterValue.value || "95");
      setHasSmartLoadOffSOC(true);
    }

    const smartLoadOnSOCFound = deviceParams.findParameterByName("SmartLoadOnSOC");
    if (smartLoadOnSOCFound?.setupDeviceParameterValue) {
      setSmartLoadOnSOC(smartLoadOnSOCFound.setupDeviceParameterValue.value || "100");
      setHasSmartLoadOnSOC(true);
    }

    const smartLoadPVPowerThresholdFound = deviceParams.findParameterByName("SmartLoadPVPowerThreshold");
    if (smartLoadPVPowerThresholdFound?.setupDeviceParameterValue) {
      setSmartLoadPVPowerThreshold(smartLoadPVPowerThresholdFound.setupDeviceParameterValue.value || "0");
      setHasSmartLoadPVPowerThreshold(true);
    }

    const smartLoadOpenDelayFound = deviceParams.findParameterByName("SmartLoadOpenDelay");
    if (smartLoadOpenDelayFound?.setupDeviceParameterValue) {
      setSmartLoadOpenDelay(smartLoadOpenDelayFound.setupDeviceParameterValue.value || "100");
      setHasSmartLoadOpenDelay(true);
    }

    const smartLoadOnGridAlwaysOnFound = deviceParams.findParameterByName("SmartLoadOnGridAlwaysOn");
    if (smartLoadOnGridAlwaysOnFound?.setupDeviceParameterValue) {
      setSmartLoadOnGridAlwaysOnParam({
        id: smartLoadOnGridAlwaysOnFound.id,
        valueId: smartLoadOnGridAlwaysOnFound.setupDeviceParameterValue.id,
        modelNumber: smartLoadOnGridAlwaysOnFound.setupDeviceParameterValue.modelNumber,
      });
      setSmartLoadOnGridAlwaysOn(smartLoadOnGridAlwaysOnFound.setupDeviceParameterValue.value === "true");
      setHasSmartLoadOnGridAlwaysOn(true);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handleSwitchChange = async (
    param: {id: string, valueId: string, modelNumber: string} | null,
    value: boolean,
    setter: (v: boolean) => void
  ) => {
    setter(value);
    if (param) {
      try {
        await setupDeviceParameterValuesService.update(param.valueId, {
          id: param.valueId,
          value: value ? "true" : "false",
          sortOrder: 0,
          modelNumber: param.modelNumber,
        });
      } catch (error) {
        console.error("Failed to update parameter:", error);
      }
    }
  };

  const hasAnyGeneratorSettings = hasGeneratorCharge || hasGeneratorGridCharge || hasGeneratorStartSOC || hasGeneratorChargeCurrent;
  const hasAnyGeneratorPeakShaving = hasGeneratorPeakShavingEnabled || hasGeneratorPeakShavingLimitPower;
  const hasAnySmartLoad = hasSmartLoadEnabled || hasSmartLoadOffSOC || hasSmartLoadOnSOC || hasSmartLoadPVPowerThreshold || hasSmartLoadOpenDelay || hasSmartLoadOnGridAlwaysOn;

  if (!hasAnyGeneratorSettings && !hasAnyGeneratorPeakShaving && !hasAnySmartLoad) {
    return null;
  }

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('generatorSmart.title')}</Text>

      {/* Generator Settings */}
      {hasAnyGeneratorSettings && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowGeneratorSettings(!showGeneratorSettings);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Generator Settings</Text>
              <FontAwesome
                name={showGeneratorSettings ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showGeneratorSettings && (
            <View style={styles.subCard}>
              {hasGeneratorCharge && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Generator Charge</Text>
                  <Switch
                    value={generatorCharge}
                    onValueChange={(v) => handleSwitchChange(generatorChargeParam, v, setGeneratorCharge)}
                    thumbColor={generatorCharge ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                  />
                </View>
              )}
              {hasGeneratorGridCharge && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Generator Grid Charge</Text>
                  <Switch
                    value={generatorGridCharge}
                    onValueChange={(v) => handleSwitchChange(generatorGridChargeParam, v, setGeneratorGridCharge)}
                    thumbColor={generatorGridCharge ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                  />
                </View>
              )}
              {hasGeneratorStartSOC && (
                <Text style={styles.valueText}>
                  Start SOC: <Text style={styles.valueAccent}>{generatorStartSOC}%</Text>
                </Text>
              )}
              {hasGeneratorChargeCurrent && (
                <Text style={styles.valueText}>
                  Charge Current: <Text style={styles.valueAccent}>{generatorChargeCurrent} A</Text>
                </Text>
              )}
            </View>
          )}
        </>
      )}

      {/* Generator Peak Shaving */}
      {hasAnyGeneratorPeakShaving && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowGeneratorPeakShaving(!showGeneratorPeakShaving);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Generator Peak Shaving</Text>
              <FontAwesome
                name={showGeneratorPeakShaving ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showGeneratorPeakShaving && (
            <View style={styles.subCard}>
              {hasGeneratorPeakShavingEnabled && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Peak Shaving Enabled</Text>
                  <Switch
                    value={generatorPeakShavingEnabled}
                    onValueChange={(v) => handleSwitchChange(generatorPeakShavingEnabledParam, v, setGeneratorPeakShavingEnabled)}
                    thumbColor={generatorPeakShavingEnabled ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                  />
                </View>
              )}
              {hasGeneratorPeakShavingLimitPower && (
                <Text style={styles.valueText}>
                  Limit Power: <Text style={styles.valueAccent}>{generatorPeakShavingLimitPower} W</Text>
                </Text>
              )}
            </View>
          )}
        </>
      )}

      {/* Smart Load */}
      {hasAnySmartLoad && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowSmartLoad(!showSmartLoad);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>Smart Load</Text>
              <FontAwesome
                name={showSmartLoad ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showSmartLoad && (
            <View style={styles.subCard}>
              {hasSmartLoadEnabled && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>Smart Load Enabled</Text>
                  <Switch
                    value={smartLoadEnabled}
                    onValueChange={(v) => handleSwitchChange(smartLoadEnabledParam, v, setSmartLoadEnabled)}
                    thumbColor={smartLoadEnabled ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
                  />
                </View>
              )}
              {hasSmartLoadOffSOC && (
                <Text style={styles.valueText}>
                  Turn OFF SOC: <Text style={styles.valueAccent}>{smartLoadOffSOC}%</Text>
                </Text>
              )}
              {hasSmartLoadOnSOC && (
                <Text style={styles.valueText}>
                  Turn ON SOC: <Text style={styles.valueAccent}>{smartLoadOnSOC}%</Text>
                </Text>
              )}
              {hasSmartLoadPVPowerThreshold && (
                <Text style={styles.valueText}>
                  PV Power Threshold: <Text style={styles.valueAccent}>{smartLoadPVPowerThreshold} W</Text>
                </Text>
              )}
              {hasSmartLoadOpenDelay && (
                <Text style={styles.valueText}>
                  Open Delay: <Text style={styles.valueAccent}>{smartLoadOpenDelay} min</Text>
                </Text>
              )}
              {hasSmartLoadOnGridAlwaysOn && (
                <View style={styles.switchRow}>
                  <Text style={styles.label}>On Grid Always On</Text>
                  <Switch
                    value={smartLoadOnGridAlwaysOn}
                    onValueChange={(v) => handleSwitchChange(smartLoadOnGridAlwaysOnParam, v, setSmartLoadOnGridAlwaysOn)}
                    thumbColor={smartLoadOnGridAlwaysOn ? COLORS.accent : "#ccc"}
                    trackColor={{ false: "#ccc", true: COLORS.accent + "66" }}
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
