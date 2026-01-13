import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, LayoutAnimation, Switch } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function AdvancedPowerControlCard({ styles, COLORS, isDark, deviceParams }: any) {
  const [showVoltageWatt, setShowVoltageWatt] = useState(false);
  const [showFrequencyWatt, setShowFrequencyWatt] = useState(false);
  const [enableVoltage, setEnableVoltage] = useState(false);
  const [enableFrequency, setEnableFrequency] = useState(false);
  const { t } = useI18n();

  const [voltageWattParam, setVoltageWattParam] = useState<{id: string, valueId: string} | null>(null);
  const [frequencyWattParam, setFrequencyWattParam] = useState<{id: string, valueId: string} | null>(null);
  
  const [hasVoltageWatt, setHasVoltageWatt] = useState(false);
  const [hasFrequencyWatt, setHasFrequencyWatt] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterById) return;

    const voltageWattParamFound = deviceParams.findParameterById("7e457c21-468e-4708-b5ee-780c9a410893");
    if (voltageWattParamFound?.setupDeviceParameterValue) {
      setVoltageWattParam({
        id: voltageWattParamFound.id,
        valueId: voltageWattParamFound.setupDeviceParameterValue.id,
      });
      setHasVoltageWatt(true);
      if (voltageWattParamFound.setupDeviceParameterValue.value === "true") {
        setEnableVoltage(true);
      } else {
        setEnableVoltage(false);
      }
    } else {
      setHasVoltageWatt(false);
    }

    const frequencyWattParamFound = deviceParams.findParameterById("2d940388-bd9b-4872-9115-3c691586a2d7");
    if (frequencyWattParamFound?.setupDeviceParameterValue) {
      setFrequencyWattParam({
        id: frequencyWattParamFound.id,
        valueId: frequencyWattParamFound.setupDeviceParameterValue.id,
      });
      setHasFrequencyWatt(true);
      if (frequencyWattParamFound.setupDeviceParameterValue.value === "true") {
        setEnableFrequency(true);
      } else {
        setEnableFrequency(false);
      }
    } else {
      setHasFrequencyWatt(false);
    }
  }, [deviceParams?.data, deviceParams?.findParameterById]);

  const handleVoltageWattChange = async (value: boolean) => {
    setEnableVoltage(value);

    if (deviceParams?.setupDeviceId && voltageWattParam) {
      try {
        await setupDeviceParameterValuesService.update(voltageWattParam.valueId, {
          id: voltageWattParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      }
    }
  };

  const handleFrequencyWattChange = async (value: boolean) => {
    setEnableFrequency(value);

    if (deviceParams?.setupDeviceId && frequencyWattParam) {
      try {
        await setupDeviceParameterValuesService.update(frequencyWattParam.valueId, {
          id: frequencyWattParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      }
    }
  };

  if (!hasVoltageWatt && !hasFrequencyWatt) {
    return null;
  }

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('advancedPower.title')}</Text>

      {hasVoltageWatt && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowVoltageWatt(!showVoltageWatt);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>{t('advancedPower.voltageWatt')}</Text>
              <FontAwesome
                name={showVoltageWatt ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showVoltageWatt && (
            <View style={styles.subCard}>
              <View style={styles.switchRow}>
                <Text style={styles.label}>{t('advancedPower.enable')}</Text>
                <Switch
                  value={enableVoltage}
                  onValueChange={handleVoltageWattChange}
                  thumbColor={
                    enableVoltage
                      ? COLORS.accent
                      : isDark
                      ? "#323030ff"
                      : "#eae1e1ff"
                  }
                  trackColor={{
                    false: isDark ? "#555" : "#ccc",
                    true: COLORS.accent + "66",
                  }}
                  ios_backgroundColor={isDark ? "#555" : "#ccc"}
                  style={{ transform: [{ scale: 1.05 }] }}
                />
              </View>
            </View>
          )}
        </>
      )}

      {hasFrequencyWatt && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowFrequencyWatt(!showFrequencyWatt);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text style={styles.subCardTitle}>{t('advancedPower.frequencyWatt')}</Text>
              <FontAwesome
                name={showFrequencyWatt ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showFrequencyWatt && (
            <View style={styles.subCard}>
              <View style={styles.switchRow}>
                <Text style={styles.label}>{t('advancedPower.enable')}</Text>
                <Switch
                  value={enableFrequency}
                  onValueChange={handleFrequencyWattChange}
                  thumbColor={
                    enableFrequency
                      ? COLORS.accent
                      : isDark
                      ? "#323030ff"
                      : "#eae1e1ff"
                  }
                  trackColor={{
                    false: isDark ? "#555" : "#ccc",
                    true: COLORS.accent + "66",
                  }}
                  ios_backgroundColor={isDark ? "#555" : "#ccc"}
                  style={{ transform: [{ scale: 1.05 }] }}
                />
              </View>
            </View>
          )}
        </>
      )}
    </View>
  );
}
