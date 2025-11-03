import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function ProtectionSafetyCard({ styles, COLORS, isDark, deviceParams }: any) {
  const [showGridProtection, setShowGridProtection] = useState(false);
  const [showSystemProtection, setShowSystemProtection] = useState(false);
  const [lvrtHvrt, setLvrtHvrt] = useState(false);
  const [afci, setAfci] = useState(false);
  const [islanding, setIslanding] = useState(false);
  const { t } = useI18n();

  const [overVoltage, setOverVoltage] = useState<string>("250");
  const [underVoltage, setUnderVoltage] = useState<string>("190");
  const [overFrequency, setOverFrequency] = useState<string>("52.0");
  const [underFrequency, setUnderFrequency] = useState<string>("47.5");

  const [lvrtHvrtParam, setLvrtHvrtParam] = useState<{id: string, valueId: string} | null>(null);
  const [afciParam, setAfciParam] = useState<{id: string, valueId: string} | null>(null);
  const [islandingParam, setIslandingParam] = useState<{id: string, valueId: string} | null>(null);
  
  const [hasOverVoltage, setHasOverVoltage] = useState(false);
  const [hasUnderVoltage, setHasUnderVoltage] = useState(false);
  const [hasOverFrequency, setHasOverFrequency] = useState(false);
  const [hasUnderFrequency, setHasUnderFrequency] = useState(false);
  const [hasLvrtHvrt, setHasLvrtHvrt] = useState(false);
  const [hasAfci, setHasAfci] = useState(false);
  const [hasIslanding, setHasIslanding] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    const overVoltageFound = deviceParams.findParameterByName("Over Voltage");
    if (overVoltageFound?.setupDeviceParameterValue) {
      setOverVoltage(overVoltageFound.setupDeviceParameterValue.value || "250");
      setHasOverVoltage(true);
    } else {
      setHasOverVoltage(false);
    }

    const underVoltageFound = deviceParams.findParameterByName("Under Voltage");
    if (underVoltageFound?.setupDeviceParameterValue) {
      setUnderVoltage(underVoltageFound.setupDeviceParameterValue.value || "190");
      setHasUnderVoltage(true);
    } else {
      setHasUnderVoltage(false);
    }

    const overFrequencyFound = deviceParams.findParameterByName("Over Frequency");
    if (overFrequencyFound?.setupDeviceParameterValue) {
      setOverFrequency(overFrequencyFound.setupDeviceParameterValue.value || "52.0");
      setHasOverFrequency(true);
    } else {
      setHasOverFrequency(false);
    }

    const underFrequencyFound = deviceParams.findParameterByName("Under Frequency");
    if (underFrequencyFound?.setupDeviceParameterValue) {
      setUnderFrequency(underFrequencyFound.setupDeviceParameterValue.value || "47.5");
      setHasUnderFrequency(true);
    } else {
      setHasUnderFrequency(false);
    }

    let lvrtHvrtFound = deviceParams.findParameterByName("Enable LVRT/HVRT");
    if (!lvrtHvrtFound && deviceParams.findParameterById) {
      lvrtHvrtFound = deviceParams.findParameterById("ca099487-edfa-4d33-83ba-3f994a089ed4");
    }
    if (lvrtHvrtFound?.setupDeviceParameterValue) {
      setLvrtHvrtParam({
        id: lvrtHvrtFound.id,
        valueId: lvrtHvrtFound.setupDeviceParameterValue.id,
      });
      setHasLvrtHvrt(true);
      if (lvrtHvrtFound.setupDeviceParameterValue.value === "true") {
        setLvrtHvrt(true);
      } else {
        setLvrtHvrt(false);
      }
    } else {
      setHasLvrtHvrt(false);
    }

    let afciFound = deviceParams.findParameterByName("Enable AFCI (Arc Fault Protection)");
    if (!afciFound && deviceParams.findParameterById) {
      afciFound = deviceParams.findParameterById("dbb740dd-ca40-49d4-ae9f-620471d2e45d");
    }
    if (afciFound?.setupDeviceParameterValue) {
      setAfciParam({
        id: afciFound.id,
        valueId: afciFound.setupDeviceParameterValue.id,
      });
      setHasAfci(true);
      if (afciFound.setupDeviceParameterValue.value === "true") {
        setAfci(true);
      } else {
        setAfci(false);
      }
    } else {
      setHasAfci(false);
    }

    let islandingFound = deviceParams.findParameterByName("Enable Islanding Protection");
    if (!islandingFound && deviceParams.findParameterById) {
      islandingFound = deviceParams.findParameterById("b133d4c4-de1c-41e4-b47b-7d838abc7b44");
    }
    if (islandingFound?.setupDeviceParameterValue) {
      setIslandingParam({
        id: islandingFound.id,
        valueId: islandingFound.setupDeviceParameterValue.id,
      });
      setHasIslanding(true);
      if (islandingFound.setupDeviceParameterValue.value === "true") {
        setIslanding(true);
      } else {
        setIslanding(false);
      }
    } else {
      setHasIslanding(false);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handleLvrtHvrtChange = async (value: boolean) => {
    setLvrtHvrt(value);

    if (deviceParams?.setupDeviceId && lvrtHvrtParam) {
      try {
        await setupDeviceParameterValuesService.update(lvrtHvrtParam.valueId, {
          id: lvrtHvrtParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      }
    }
  };

  const handleAfciChange = async (value: boolean) => {
    setAfci(value);

    if (deviceParams?.setupDeviceId && afciParam) {
      try {
        await setupDeviceParameterValuesService.update(afciParam.valueId, {
          id: afciParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      }
    }
  };

  const handleIslandingChange = async (value: boolean) => {
    setIslanding(value);

    if (deviceParams?.setupDeviceId && islandingParam) {
      try {
        await setupDeviceParameterValuesService.update(islandingParam.valueId, {
          id: islandingParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      }
    }
  };

  const hasAnyGridProtection = hasOverVoltage || hasUnderVoltage || hasOverFrequency || hasUnderFrequency || hasLvrtHvrt;
  const hasAnySystemProtection = hasAfci || hasIslanding;
  if (!hasAnyGridProtection && !hasAnySystemProtection) {
    return null;
  }

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('protectionSafety.title')}</Text>

      {hasAnyGridProtection && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowGridProtection(!showGridProtection);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text 
                style={styles.subCardTitle}
                numberOfLines={1}
                ellipsizeMode="tail"
              >
                {t('protectionSafety.gridProtection.title')}
              </Text>
              <FontAwesome
                name={showGridProtection ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showGridProtection && (
            <View style={styles.subCard}>
              {(hasOverVoltage || hasUnderVoltage) && (
                <>
                  <Text style={styles.subHeader}>{t('protectionSafety.gridProtection.voltageLimits')}</Text>
                  {hasOverVoltage && (
                    <Text style={styles.valueText}>
                      {t('protectionSafety.gridProtection.overVoltage')}: <Text style={styles.valueAccent}>{overVoltage} V</Text>
                    </Text>
                  )}
                  {hasUnderVoltage && (
                    <Text style={styles.valueText}>
                      {t('protectionSafety.gridProtection.underVoltage')}: <Text style={styles.valueAccent}>{underVoltage} V</Text>
                    </Text>
                  )}
                </>
              )}

              {(hasOverFrequency || hasUnderFrequency) && (
                <>
                  <Text style={styles.subHeader}>{t('protectionSafety.gridProtection.frequencyLimits')}</Text>
                  {hasOverFrequency && (
                    <Text style={styles.valueText}>
                      {t('protectionSafety.gridProtection.overFrequency')}: <Text style={styles.valueAccent}>{overFrequency} Hz</Text>
                    </Text>
                  )}
                  {hasUnderFrequency && (
                    <Text style={styles.valueText}>
                      {t('protectionSafety.gridProtection.underFrequency')}: <Text style={styles.valueAccent}>{underFrequency} Hz</Text>
                    </Text>
                  )}
                </>
              )}

              {hasLvrtHvrt && (
                <>
                  <View style={[styles.switchRow, { marginTop: 10 }]}>
                    <Text 
                      style={[styles.label, { color: COLORS.error || "red", fontWeight: "700" }]}
                      numberOfLines={2}
                      ellipsizeMode="tail"
                    >
                      {t('protectionSafety.gridProtection.lvrtHvrt')}
                    </Text>
                    <Switch
                      value={lvrtHvrt}
                      onValueChange={handleLvrtHvrtChange}
                      thumbColor={
                        lvrtHvrt
                          ? COLORS.error || "red"
                          : isDark
                          ? "#323030ff"
                          : "#eae1e1ff"
                      }
                      trackColor={{
                        false: isDark ? "#555" : "#ccc",
                        true: (COLORS.error || "red") + "66",
                      }}
                      ios_backgroundColor={isDark ? "#555" : "#ccc"}
                      style={{ transform: [{ scale: 1.05 }] }}
                    />
                  </View>

                  <Text style={[styles.warningText, { color: COLORS.error || "red" }]}>
                    {t('protectionSafety.gridProtection.warning')}
                  </Text>
                </>
              )}
            </View>
          )}
        </>
      )}

      {hasAnySystemProtection && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => {
              LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
              setShowSystemProtection(!showSystemProtection);
            }}
          >
            <View style={styles.dropdownRow}>
              <Text 
                style={styles.subCardTitle}
                numberOfLines={1}
                ellipsizeMode="tail"
              >
                {t('protectionSafety.systemProtection.title')}
              </Text>
              <FontAwesome
                name={showSystemProtection ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showSystemProtection && (
            <View style={styles.subCard}>
              {hasAfci && (
                <>
                  <View style={styles.switchRow}>
                    <Text 
                      style={[styles.label, { color: COLORS.error || "red", fontWeight: "700" }]}
                      numberOfLines={2}
                      ellipsizeMode="tail"
                    >
                      {t('protectionSafety.systemProtection.afci')}
                    </Text>
                    <Switch
                      value={afci}
                      onValueChange={handleAfciChange}
                      thumbColor={
                        afci
                          ? COLORS.error || "red"
                          : isDark
                          ? "#323030ff"
                          : "#eae1e1ff"
                      }
                      trackColor={{
                        false: isDark ? "#555" : "#ccc",
                        true: (COLORS.error || "red") + "66",
                      }}
                      ios_backgroundColor={isDark ? "#555" : "#ccc"}
                      style={{ transform: [{ scale: 1.05 }] }}
                    />
                  </View>
                  <Text style={[styles.warningText, { color: COLORS.error || "red" }]}>
                    {t('protectionSafety.systemProtection.afciWarning')}
                  </Text>
                </>
              )}

              {hasIslanding && (
                <>
                  <View style={styles.switchRow}>
                    <Text 
                      style={[styles.label, { color: COLORS.error || "red", fontWeight: "700" }]}
                      numberOfLines={2}
                      ellipsizeMode="tail"
                    >
                      {t('protectionSafety.systemProtection.islanding')}
                    </Text>
                    <Switch
                      value={islanding}
                      onValueChange={handleIslandingChange}
                      thumbColor={
                        islanding
                          ? COLORS.error || "red"
                          : isDark
                          ? "#323030ff"
                          : "#eae1e1ff"
                      }
                      trackColor={{
                        false: isDark ? "#555" : "#ccc",
                        true: (COLORS.error || "red") + "66",
                      }}
                      ios_backgroundColor={isDark ? "#555" : "#ccc"}
                      style={{ transform: [{ scale: 1.05 }] }}
                    />
                  </View>
                  <Text style={[styles.warningText, { color: COLORS.error || "red" }]}>
                    {t('protectionSafety.systemProtection.islandingWarning')}
                  </Text>
                </>
              )}
            </View>
          )}
        </>
      )}
    </View>
  );
}
