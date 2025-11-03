import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation } from "@react-navigation/native";
import { useI18n } from "../../../../hooks/useI18n";

export default function BatteryManagementCard({ styles, COLORS, deviceParams }: any) {
  const navigation = useNavigation<any>();
  const [showBatteryCharge, setShowBatteryCharge] = useState(false);
  const [showBatteryProtection, setShowBatteryProtection] = useState(false);
  const { t } = useI18n();

  const [maxChargeCurrent, setMaxChargeCurrent] = useState<string>("40");
  const [maxChargePower, setMaxChargePower] = useState<string>("18.5");
  const [maxDischargePower, setMaxDischargePower] = useState<string>("15");
  const [backupReserve, setBackupReserve] = useState<string>("25");
  const [dischargeCutoff, setDischargeCutoff] = useState<string>("10");
  const [lowBatteryWarning, setLowBatteryWarning] = useState<string>("15");
  const [maxDischargeCurrent, setMaxDischargeCurrent] = useState<string>("35");

  const [maxChargeCurrentParam, setMaxChargeCurrentParam] = useState<{id: string, valueId: string} | null>(null);
  const [maxChargePowerParam, setMaxChargePowerParam] = useState<{id: string, valueId: string} | null>(null);
  const [maxDischargePowerParam, setMaxDischargePowerParam] = useState<{id: string, valueId: string} | null>(null);
  const [backupReserveParam, setBackupReserveParam] = useState<{id: string, valueId: string} | null>(null);
  const [dischargeCutoffParam, setDischargeCutoffParam] = useState<{id: string, valueId: string} | null>(null);
  const [lowBatteryWarningParam, setLowBatteryWarningParam] = useState<{id: string, valueId: string} | null>(null);
  
  const [hasMaxChargeCurrent, setHasMaxChargeCurrent] = useState(false);
  const [hasMaxChargePower, setHasMaxChargePower] = useState(false);
  const [hasMaxDischargePower, setHasMaxDischargePower] = useState(false);
  const [hasBackupReserve, setHasBackupReserve] = useState(false);
  const [hasDischargeCutoff, setHasDischargeCutoff] = useState(false);
  const [hasLowBatteryWarning, setHasLowBatteryWarning] = useState(false);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    const maxChargeCurrentFound = deviceParams.findParameterByName("Max Charge Current");
    if (maxChargeCurrentFound?.setupDeviceParameterValue) {
      setMaxChargeCurrentParam({
        id: maxChargeCurrentFound.id,
        valueId: maxChargeCurrentFound.setupDeviceParameterValue.id,
      });
      setMaxChargeCurrent(maxChargeCurrentFound.setupDeviceParameterValue.value || "40");
      setHasMaxChargeCurrent(true);
    } else {
      setHasMaxChargeCurrent(false);
    }

    const maxChargePowerFound = deviceParams.findParameterByName("Max Charge Power");
    if (maxChargePowerFound?.setupDeviceParameterValue) {
      setMaxChargePowerParam({
        id: maxChargePowerFound.id,
        valueId: maxChargePowerFound.setupDeviceParameterValue.id,
      });
      setMaxChargePower(maxChargePowerFound.setupDeviceParameterValue.value || "18.5");
      setHasMaxChargePower(true);
    } else {
      setHasMaxChargePower(false);
    }

    const maxDischargePowerFound = deviceParams.findParameterByName("Max DisCharge Power");
    if (maxDischargePowerFound?.setupDeviceParameterValue) {
      setMaxDischargePowerParam({
        id: maxDischargePowerFound.id,
        valueId: maxDischargePowerFound.setupDeviceParameterValue.id,
      });
      setMaxDischargePower(maxDischargePowerFound.setupDeviceParameterValue.value || "15");
      setHasMaxDischargePower(true);
    } else {
      setHasMaxDischargePower(false);
    }

    const backupReserveFound = deviceParams.findParameterByName("Backup Reserve");
    if (backupReserveFound?.setupDeviceParameterValue) {
      setBackupReserveParam({
        id: backupReserveFound.id,
        valueId: backupReserveFound.setupDeviceParameterValue.id,
      });
      setBackupReserve(backupReserveFound.setupDeviceParameterValue.value || "25");
      setHasBackupReserve(true);
    } else {
      setHasBackupReserve(false);
    }

    const dischargeCutoffFound = deviceParams.findParameterByName("Discharge Cutoff");
    if (dischargeCutoffFound?.setupDeviceParameterValue) {
      setDischargeCutoffParam({
        id: dischargeCutoffFound.id,
        valueId: dischargeCutoffFound.setupDeviceParameterValue.id,
      });
      setDischargeCutoff(dischargeCutoffFound.setupDeviceParameterValue.value || "10");
      setHasDischargeCutoff(true);
    } else {
      setHasDischargeCutoff(false);
    }

    const lowBatteryWarningFound = deviceParams.findParameterByName("Low Battery Warning");
    if (lowBatteryWarningFound?.setupDeviceParameterValue) {
      setLowBatteryWarningParam({
        id: lowBatteryWarningFound.id,
        valueId: lowBatteryWarningFound.setupDeviceParameterValue.id,
      });
      setLowBatteryWarning(lowBatteryWarningFound.setupDeviceParameterValue.value || "15");
      setHasLowBatteryWarning(true);
    } else {
      setHasLowBatteryWarning(false);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('batteryManagement.title')}</Text>

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowBatteryCharge(!showBatteryCharge);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>{t('batteryManagement.chargeDischargeLimits')}</Text>
          <FontAwesome
            name={showBatteryCharge ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showBatteryCharge && (
        <View style={styles.subCard}>
          {(hasMaxChargeCurrent || hasMaxChargePower) && (
            <>
              <Text style={styles.subHeader}>{t('batteryManagement.charging')}</Text>
              {hasMaxChargeCurrent && (
                <Text style={styles.valueText}>
                  {t('batteryManagement.maxChargeCurrent')}: <Text style={styles.valueAccent}>{maxChargeCurrent} A</Text>
                </Text>
              )}
              {hasMaxChargePower && (
                <Text style={styles.valueText}>
                  {t('batteryManagement.maxChargePower')}: <Text style={styles.valueAccent}>{maxChargePower} kW</Text>
                </Text>
              )}
            </>
          )}

          {(hasMaxDischargePower || maxDischargeCurrent) && (
            <>
              <Text style={styles.subHeader}>{t('batteryManagement.discharging')}</Text>
              <Text style={styles.valueText}>
                {t('batteryManagement.maxDischargeCurrent')}: <Text style={styles.valueAccent}>{maxDischargeCurrent} A</Text>
              </Text>
              {hasMaxDischargePower && (
                <Text style={styles.valueText}>
                  {t('batteryManagement.maxDischargePower')}: <Text style={styles.valueAccent}>{maxDischargePower} kW</Text>
                </Text>
              )}
            </>
          )}
        </View>
      )}

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowBatteryProtection(!showBatteryProtection);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>{t('batteryManagement.protectionReserve')}</Text>
          <FontAwesome
            name={showBatteryProtection ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showBatteryProtection && (
        <View style={styles.subCard}>
          {hasBackupReserve && (
            <Text style={styles.valueText}>
              {t('batteryManagement.backupReserve')}: <Text style={styles.valueAccent}>{backupReserve}%</Text>
            </Text>
          )}
          {hasDischargeCutoff && (
            <Text style={styles.valueText}>
              {t('batteryManagement.dischargeCutoff')}: <Text style={styles.valueAccent}>{dischargeCutoff}%</Text>
            </Text>
          )}
          {hasLowBatteryWarning && (
            <Text style={styles.valueText}>
              {t('batteryManagement.lowBatteryWarning')}: <Text style={styles.valueAccent}>{lowBatteryWarning}%</Text>
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
    </View>
  );
}
