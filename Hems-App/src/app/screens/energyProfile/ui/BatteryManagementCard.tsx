import React, { useState } from "react";
import { View, Text, TouchableOpacity, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";

export default function BatteryManagementCard({ styles, COLORS }: any) {
  const [showBatteryCharge, setShowBatteryCharge] = useState(false);
  const [showBatteryProtection, setShowBatteryProtection] = useState(false);

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>Battery Management</Text>

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowBatteryCharge(!showBatteryCharge);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>Battery Charge / Discharge Limits</Text>
          <FontAwesome
            name={showBatteryCharge ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showBatteryCharge && (
        <View style={styles.subCard}>
          <Text style={styles.subHeader}>Charging</Text>
          <Text style={styles.valueText}>
            Max Charge Current: <Text style={styles.valueAccent}>40 A</Text>
          </Text>
          <Text style={styles.valueText}>
            Max Charge Power: <Text style={styles.valueAccent}>18.5 kW</Text>
          </Text>

          <Text style={styles.subHeader}>Discharging</Text>
          <Text style={styles.valueText}>
            Max Discharge Current: <Text style={styles.valueAccent}>35 A</Text>
          </Text>
          <Text style={styles.valueText}>
            Max Discharge Power: <Text style={styles.valueAccent}>15.0 kW</Text>
          </Text>
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
          <Text style={styles.subCardTitle}>Battery Protection & Reserve</Text>
          <FontAwesome
            name={showBatteryProtection ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showBatteryProtection && (
        <View style={styles.subCard}>
          <Text style={styles.valueText}>
            Backup Reserve: <Text style={styles.valueAccent}>25%</Text>
          </Text>
          <Text style={styles.valueText}>
            Discharge Cutoff: <Text style={styles.valueAccent}>10%</Text>
          </Text>
          <Text style={styles.valueText}>
            Low Battery Warning: <Text style={styles.valueAccent}>15%</Text>
          </Text>
        </View>
      )}
    </View>
  );
}
