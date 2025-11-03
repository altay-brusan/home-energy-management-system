import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation } from "@react-navigation/native";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function SolarPowerCard({ styles, COLORS, isDark, deviceParams }: any) {
  const navigation = useNavigation<any>();
  const [showSolarExport, setShowSolarExport] = useState(true);
  const [showPvArray, setShowPvArray] = useState(false);
  const [solarSelling, setSolarSelling] = useState(false);
  const { t } = useI18n();
  const [enableMpptScan, setEnableMpptScan] = useState(false);
  
  const [solarSellingParam, setSolarSellingParam] = useState<{id: string, valueId: string} | null>(null);
  const [enableMpptScanParam, setEnableMpptScanParam] = useState<{id: string, valueId: string} | null>(null);
  
  const [hasSolarSelling, setHasSolarSelling] = useState(false);
  const [hasMpptScan, setHasMpptScan] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    const solarSellingParamFound = deviceParams.findParameterByName("Enable Solar Selling to Grid");
    if (solarSellingParamFound?.setupDeviceParameterValue) {
      setSolarSellingParam({
        id: solarSellingParamFound.id,
        valueId: solarSellingParamFound.setupDeviceParameterValue.id,
      });
      setHasSolarSelling(true);
      if (solarSellingParamFound.setupDeviceParameterValue.value === "true") {
        setSolarSelling(true);
      } else {
        setSolarSelling(false);
      }
    } else {
      setHasSolarSelling(false);
    }

    const enableMpptScanParamFound = deviceParams.findParameterByName("Enable MPPT Scanning");
    if (enableMpptScanParamFound?.setupDeviceParameterValue) {
      setEnableMpptScanParam({
        id: enableMpptScanParamFound.id,
        valueId: enableMpptScanParamFound.setupDeviceParameterValue.id,
      });
      setHasMpptScan(true);
      if (enableMpptScanParamFound.setupDeviceParameterValue.value === "true") {
        setEnableMpptScan(true);
      } else {
        setEnableMpptScan(false);
      }
    } else {
      setHasMpptScan(false);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handleSolarSellingChange = async (value: boolean) => {
    setSolarSelling(value);

    if (deviceParams?.setupDeviceId && solarSellingParam) {
      try {
        await setupDeviceParameterValuesService.update(solarSellingParam.valueId, {
          id: solarSellingParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      }
    }
  };

  const handleMpptScanChange = async (value: boolean) => {
    setEnableMpptScan(value);

    if (deviceParams?.setupDeviceId && enableMpptScanParam) {
      try {
        await setupDeviceParameterValuesService.update(enableMpptScanParam.valueId, {
          id: enableMpptScanParam.valueId,
          value: value ? "true" : "false",
          sortOrder: 1,
          modelNumber: "NX-200",
        });
      } catch (error) {
      }
    }
  };

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('solarPower.title')}</Text>

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowSolarExport(!showSolarExport);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>{t('solarPower.exportControl')}</Text>
          <FontAwesome
            name={showSolarExport ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showSolarExport && (
        <View style={styles.subCard}>
          {hasSolarSelling && (
            <View style={styles.switchRow}>
              <Text style={styles.label}>{t('solarPower.enableSelling')}</Text>
              <Switch
                value={solarSelling}
                onValueChange={handleSolarSellingChange}
                thumbColor={
                  solarSelling
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
          )}
        </View>
      )}

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowPvArray(!showPvArray);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>{t('solarPower.pvArrayConfig')}</Text>
          <FontAwesome
            name={showPvArray ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showPvArray && (
        <View style={styles.subCard}>
          <Text style={styles.valueText}>
           {t('solarPower.mpptTrackers')}: <Text style={styles.valueAccent}>2</Text>
          </Text>

          {hasMpptScan && (
            <View style={styles.switchRow}>
              <Text
                style={{
                  color: COLORS.textSoft,
                  fontWeight: "600",
                  fontSize: 16,
                }}
              >
                {t('solarPower.enableMpptScanning')}
              </Text>
              <Switch
                value={enableMpptScan}
                onValueChange={handleMpptScanChange}
                thumbColor={
                  enableMpptScan
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
          )}

          <Text style={styles.valueText}>
           {t('solarPower.maxPvVoltage')}: <Text style={styles.valueAccent}>480 V</Text>
          </Text>
          <Text style={styles.valueText}>
           {t('solarPower.maxPvCurrent')}: <Text style={styles.valueAccent}>12.5 A</Text>
          </Text>

          <TouchableOpacity
            activeOpacity={0.7}
            onPress={() => navigation.navigate("SolarSourcePriority")}
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
             {t('solarPower.prioritySettings')}
            </Text>
            <FontAwesome name="cog" size={16} color={COLORS.accent} />
          </TouchableOpacity>
        </View>
      )}
    </View>
  );
}
