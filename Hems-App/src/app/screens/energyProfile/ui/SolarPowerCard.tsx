import React, { useState } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";

export default function SolarPowerCard({ styles, COLORS }: any) {
  const [showSolarExport, setShowSolarExport] = useState(true);
  const [showPvArray, setShowPvArray] = useState(false);
  const [solarSelling, setSolarSelling] = useState(false);
  const [enableMpptScan, setEnableMpptScan] = useState(false);

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}> Solar Power Management</Text>

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowSolarExport(!showSolarExport);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>Solar Export Control</Text>
          <FontAwesome
            name={showSolarExport ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showSolarExport && (
        <View style={styles.subCard}>
          <View style={styles.switchRow}>
            <Text style={styles.label}>Enable Solar Selling to Grid</Text>
            <Switch
              value={solarSelling}
              onValueChange={setSolarSelling}
              thumbColor={solarSelling ? COLORS.accent : COLORS.surface}
              trackColor={{
                false: COLORS.border,
                true: COLORS.accent + "66",
              }}
            />
          </View>
          <Text style={styles.valueText}>
            Max Solar Export Power: <Text style={styles.valueAccent}>5.2 kW</Text>
          </Text>
          <Text style={styles.valueText}>
            Total Export Limit: <Text style={styles.valueAccent}>12.0 kW</Text>
          </Text>
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
          <Text style={styles.subCardTitle}>PV Array Configuration</Text>
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
            MPPT Trackers: <Text style={styles.valueAccent}>2</Text>
          </Text>
          <View style={styles.switchRow}>
            <Text style={styles.label}>Enable MPPT Scanning</Text>
            <Switch
              value={enableMpptScan}
              onValueChange={setEnableMpptScan}
              thumbColor={enableMpptScan ? COLORS.accent : COLORS.surface}
              trackColor={{
                false: COLORS.border,
                true: COLORS.accent + "66",
              }}
            />
          </View>
          <Text style={styles.valueText}>
            Max PV Voltage: <Text style={styles.valueAccent}>480 V</Text>
          </Text>
          <Text style={styles.valueText}>
            Max PV Current: <Text style={styles.valueAccent}>12.5 A</Text>
          </Text>
        </View>
      )}
    </View>
  );
}
