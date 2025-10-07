import React, { useEffect, useRef } from "react";
import {
  View,
  Text,
  Modal,
  Pressable,
  Animated,
  Dimensions,
  ScrollView,
} from "react-native";
import { useThemeStore } from "../../../../store/themeStore";
import { getThemeColors, SPACING } from "../../../../theme/constants";
import { makeBatteryDetailStyles } from "./BatteryDetailBottomSheet.styles";
import Button from "../../../../components/ui/Button";
import FontAwesome from "@react-native-vector-icons/fontawesome";

const { height: SCREEN_HEIGHT } = Dimensions.get("window");
const SHEET_H = Math.round(SCREEN_HEIGHT * 0.69);

interface BatteryDetailBottomSheetProps {
  visible: boolean;
  onClose: () => void;
  onRestart: () => void;
  batteryData: {
    soc: number;
    power: number;
    voltage: number;
    preheating: boolean;
    discharge: number;
    avgCellVoltage: number;
    avgTemp: number;
    sn: string;
    version: string;
    model: string;
    capacity: number;
  };
}

export default function BatteryDetailBottomSheet({
  visible,
  onClose,
  onRestart,
  batteryData,
}: BatteryDetailBottomSheetProps) {
  const sheetY = useRef(new Animated.Value(SHEET_H)).current;
  const sheetOpacity = useRef(new Animated.Value(0)).current;

  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const styles = makeBatteryDetailStyles(COLORS, isDark);

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

  return (
    <Modal
      transparent
      visible={visible}
      onRequestClose={onClose}
      statusBarTranslucent
      animationType="none"
    >
      <Animated.View style={[styles.scrim, { opacity: sheetOpacity }]}>
        <Pressable style={{ flex: 1 }} onPress={onClose} />
      </Animated.View>

      <Animated.View
        style={[
          styles.modalContent,
          { height: SHEET_H, transform: [{ translateY: sheetY }] },
        ]}
      >
        <View style={styles.handleContainer}>
          <View style={styles.handle} />
        </View>

        <View style={styles.sheetHeader}>
          <View style={styles.sheetHeaderLeft}>
            <Text style={styles.title}>Battery Detail</Text>
            <Text style={styles.subtitle}>Real Time Info</Text>
          </View>
          <View style={styles.sheetIconContainer}>
            <FontAwesome name="battery-full" size={28} color="#fff" />
          </View>
        </View>

        <ScrollView showsVerticalScrollIndicator={false}>
          <View style={styles.section}>
            <View style={styles.row}>
              <Text style={styles.label}>Battery SOC</Text>
              <Text style={styles.value}>{batteryData.soc}%</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Charging & Discharging Power</Text>
              <Text style={styles.value}>{batteryData.power} kW</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Battery Pack Voltage</Text>
              <Text style={styles.value}>{batteryData.voltage} V</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Battery Preheating Status</Text>
              <Text style={styles.value}>
                {batteryData.preheating ? "On" : "Off"}
              </Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Total Discharge Amount</Text>
              <Text style={styles.value}>{batteryData.discharge} MWh</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Average Cell Voltage</Text>
              <Text style={styles.value}>{batteryData.avgCellVoltage} V</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Average Cell Temperature</Text>
              <Text style={styles.value}>{batteryData.avgTemp} °C</Text>
            </View>
          </View>

          <Text style={styles.subtitle}>Parameter</Text>
          <View style={styles.section}>
            <View style={styles.row}>
              <Text style={styles.label}>Device SN</Text>
              <Text style={styles.value}>{batteryData.sn}</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Software Version</Text>
              <Text style={styles.value}>{batteryData.version}</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Device Model</Text>
              <Text style={styles.value}>{batteryData.model}</Text>
            </View>
            <View style={styles.row}>
              <Text style={styles.label}>Rated Battery Capacity</Text>
              <Text style={styles.value}>{batteryData.capacity} kWh</Text>
            </View>
          </View>
        </ScrollView>

  <View style={styles.buttonRow}>
  <Button
    title="Kapat"
    onPress={onClose}
    variant="secondary"
    style={styles.actionBtn}
  />
  <Button
    title="Cihazı Tekrar Başlat"
    onPress={onRestart}
    variant="danger"
    style={styles.actionBtn}
  />
</View>
      </Animated.View>
    </Modal>
  );
}
