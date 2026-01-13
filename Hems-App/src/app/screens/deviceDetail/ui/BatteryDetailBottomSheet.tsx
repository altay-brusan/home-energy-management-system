import React, { useEffect, useRef } from "react";
import {
  View,
  Text,
  Modal,
  Pressable,
  Animated,
  Dimensions,
  ScrollView,
  ActivityIndicator,
} from "react-native";
import { useThemeStore } from "../../../../store/themeStore";
import { getThemeColors } from "../../../../theme/constants";
import { makeBatteryDetailStyles } from "./BatteryDetailBottomSheet.styles";
import Button from "../../../../components/ui/Button";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useI18n } from "../../../../hooks/useI18n";
import { useSafeAreaInsets } from "react-native-safe-area-context";

const { height: SCREEN_HEIGHT } = Dimensions.get("window");
const SHEET_H = Math.round(SCREEN_HEIGHT * 0.69);

const formatDateTime = (isoString: string | null | undefined, locale: string = 'tr-TR'): string => {
  if (!isoString || isoString === "-") return "-";
  
  try {
    const date = new Date(isoString);
    if (isNaN(date.getTime())) return isoString;
    
    return date.toLocaleString(locale, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false
    });
  } catch (error) {
    return isoString;
  }
};

interface BatteryDetailBottomSheetProps {
  visible: boolean;
  onClose: () => void;
  onRestart: () => void;
  deviceName: string;
  loading?: boolean;
  batteryData: any;
}

export default function BatteryDetailBottomSheet({
  visible,
  onClose,
  onRestart,
  loading,
  deviceName,
  batteryData,
}: BatteryDetailBottomSheetProps) {
  const sheetY = useRef(new Animated.Value(SHEET_H)).current;
  const sheetOpacity = useRef(new Animated.Value(0)).current;

  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const styles = makeBatteryDetailStyles(COLORS, isDark);
  const { t, language } = useI18n();
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const isInverter = deviceName?.toLowerCase().includes("inverter");
  const isBms = deviceName?.toLowerCase().includes("bms");
  const insets = useSafeAreaInsets();

  useEffect(() => {
    if (visible) {
      sheetY.setValue(SHEET_H);
      sheetOpacity.setValue(0);
      Animated.parallel([
        Animated.spring(sheetY, {
          toValue: 0,
          useNativeDriver: true,
          bounciness: 7,
          speed: 18,
        }),
        Animated.timing(sheetOpacity, {
          toValue: 1,
          duration: 180,
          useNativeDriver: true,
        }),
      ]).start();
    } else {
      Animated.parallel([
        Animated.timing(sheetY, {
          toValue: SHEET_H,
          duration: 180,
          useNativeDriver: true,
        }),
        Animated.timing(sheetOpacity, {
          toValue: 0,
          duration: 140,
          useNativeDriver: true,
        }),
      ]).start();
    }
  }, [visible]);

  const renderIcon = () => {
    if (isInverter) return "bolt";
    if (isBms) return "microchip";
    return "battery-full";
  };

  return (
    <Modal transparent visible={visible} onRequestClose={onClose} statusBarTranslucent animationType="none">
      <Animated.View style={[styles.scrim, { opacity: sheetOpacity }]}>
        <Pressable style={{ flex: 1 }} onPress={onClose} />
      </Animated.View>

  <Animated.View
  style={[
    styles.modalContent,
    {
      transform: [{ translateY: sheetY }],
      paddingBottom: insets.bottom + 6
    },
  ]}
>
        <View style={styles.handleContainer}>
          <View style={styles.handle} />
        </View>

        <View style={styles.sheetHeader}>
          <View style={styles.sheetHeaderLeft}>
           <Text style={styles.title}>
   {t(`deviceNames.${deviceName?.toLowerCase()}`, { defaultValue: deviceName })}
 </Text>
 <Text style={styles.subtitle}>
   {t("deviceDetailBottomsheet.common.title")}
 </Text>
          </View>
          <View style={styles.sheetIconContainer}>
            <FontAwesome name={renderIcon()} size={28} color="#fff" />
          </View>
        </View>

        {loading ? (
          <View style={{ flex: 1, justifyContent: "center", alignItems: "center", paddingVertical: 32 }}>
            <ActivityIndicator size="large" color={COLORS.accent} />
            <Text style={{ color: COLORS.textSoft, marginTop: 10 }}>
  {t("deviceDetailBottomsheet.common.loading")}
</Text>
          </View>
        ) : (
          <ScrollView showsVerticalScrollIndicator={false}>
            {/* 🟦 INVERTER */}
           {isInverter && (
  <>
    <View style={styles.section}>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.activePower")}</Text>
        <Text style={styles.value}>{batteryData["Active Power"]} W</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.reactivePower")}</Text>
        <Text style={styles.value}>{batteryData["Reactive Power"]} VAR</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.powerFactor")}</Text>
        <Text style={styles.value}>{batteryData["Power Factor"]}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.insulationResistance")}</Text>
        <Text style={styles.value}>{batteryData["Insulation Resistance"]} MΩ</Text>
      </View>
    </View>

    <Text style={styles.subtitle}>{t("deviceDetailBottomsheet.inverter.phaseData")}</Text>
    {batteryData.Phases?.map((p: any, i: number) => (
      <View key={i} style={styles.row}>
        <Text style={styles.label}>
          {t("deviceDetailBottomsheet.inverter.phaseLabel", { id: p["Phase Id"] })}
        </Text>
        <Text style={styles.value}>
          {p.Voltage.toFixed(1)} V / {p.Current.toFixed(2)} A
        </Text>
      </View>
    ))}

    <Text style={styles.subtitle}>{t("deviceDetailBottomsheet.inverter.info")}</Text>
    <View style={styles.section}>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.startupTime")}</Text>
        <Text style={styles.value}>{formatDateTime(batteryData["Last Inverter Startup Time"], locale)}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.shutdownTime")}</Text>
        <Text style={styles.value}>{formatDateTime(batteryData["Last Inverter Shutdown Time"], locale)}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.deviceSN")}</Text>
        <Text style={styles.value}>{batteryData["Device Serial Number"]}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.inverter.firmwareVersion")}</Text>
        <Text style={styles.value}>{batteryData["Firmware Version"]}</Text>
      </View>
    </View>
  </>
)}

            {/* 🟧 BMS */}
           {isBms && (
  <>
    <View style={styles.section}>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.soc")}</Text>
        <Text style={styles.value}>{batteryData.SOC}%</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.packVoltage")}</Text>
        <Text style={styles.value}>{batteryData.VoltagePack} V</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.packCurrent")}</Text>
        <Text style={styles.value}>{batteryData.CurrentPack} A</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.maxClusterVoltage")}</Text>
        <Text style={styles.value}>{batteryData.MaxVoltageOfBatteryCluster} V</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.minClusterVoltage")}</Text>
        <Text style={styles.value}>{batteryData.MinVoltageOfBatteryCluster} V</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.totalChargeCapacity")}</Text>
        <Text style={styles.value}>{batteryData.TotalChargeCapacity} Ah</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.totalDischargeCapacity")}</Text>
        <Text style={styles.value}>{batteryData.TotalDischargeCapacity} Ah</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.dateTime")}</Text>
        <Text style={styles.value}>{batteryData.DateTime}</Text>
      </View>
    </View>

    <Text style={styles.subtitle}>{t("deviceDetailBottomsheet.bms.info")}</Text>
    <View style={styles.section}>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.deviceSN")}</Text>
        <Text style={styles.value}>{batteryData["Device Serial Number"]}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.bms.firmwareVersion")}</Text>
        <Text style={styles.value}>{batteryData["Firmware Version"]}</Text>
      </View>
    </View>
  </>
)}

            {/* 🟩 BATTERY */}
         {!isInverter && !isBms && (
  <>
    <View style={styles.section}>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.soc")}</Text>
        <Text style={styles.value}>{batteryData.soc}%</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.soh")}</Text>
        <Text style={styles.value}>{batteryData.soh}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.voltage")}</Text>
        <Text style={styles.value}>{batteryData.voltage?.toFixed(2)} V</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.current")}</Text>
        <Text style={styles.value}>{batteryData.current?.toFixed(2)} A</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.avgCellVoltage")}</Text>
        <Text style={styles.value}>{batteryData.avgCellVoltage?.toFixed(3)} V</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.avgTemp")}</Text>
        <Text style={styles.value}>{batteryData.avgTemp?.toFixed(2)} °C</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.maxCellVoltage")}</Text>
        <Text style={styles.value}>{batteryData.maxCellVoltage?.toFixed(3)} V</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.minCellVoltage")}</Text>
        <Text style={styles.value}>{batteryData.minCellVoltage?.toFixed(3)} V</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.maxCellTemp")}</Text>
        <Text style={styles.value}>{batteryData.maxCellTemp?.toFixed(2)} °C</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.minCellTemp")}</Text>
        <Text style={styles.value}>{batteryData.minCellTemp?.toFixed(2)} °C</Text>
      </View>
    </View>

    <Text style={styles.subtitle}>{t("deviceDetailBottomsheet.battery.parameter")}</Text>
    <View style={styles.section}>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.totalCharge")}</Text>
        <Text style={styles.value}>{batteryData.totalCharge}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.totalDischarge")}</Text>
        <Text style={styles.value}>{batteryData.totalDischarge} MWh</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.deviceModel")}</Text>
        <Text style={styles.value}>{batteryData.model}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.deviceSN")}</Text>
        <Text style={styles.value}>{batteryData.sn}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.firmwareVersion")}</Text>
        <Text style={styles.value}>{batteryData.version}</Text>
      </View>
      <View style={styles.row}>
        <Text style={styles.label}>{t("deviceDetailBottomsheet.battery.dateTime")}</Text>
        <Text style={styles.value}>{batteryData.dateTime}</Text>
      </View>
    </View>
  </>
)}
          </ScrollView>
        )}

      <View style={styles.buttonRow}>
  <Button
    title={t("deviceDetailBottomsheet.common.close")}
    onPress={onClose}
    variant="secondary"
    style={styles.actionBtn}
  />
  <Button
    title={t("deviceDetailBottomsheet.common.restart")}
    onPress={onRestart}
    variant="danger"
    style={styles.actionBtn}
    disabled={true}
  />
</View>
      </Animated.View>
    </Modal>
  );
}
