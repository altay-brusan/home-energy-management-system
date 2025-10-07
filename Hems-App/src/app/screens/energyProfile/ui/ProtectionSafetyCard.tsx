import React, { useState } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";

export default function ProtectionSafetyCard({ styles, COLORS }: any) {
  const [showGridProtection, setShowGridProtection] = useState(false);
  const [showSystemProtection, setShowSystemProtection] = useState(false);

  const [lvrtHvrt, setLvrtHvrt] = useState(false);
  const [afci, setAfci] = useState(false);
  const [islanding, setIslanding] = useState(false);

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}> Protection & Safety</Text>

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowGridProtection(!showGridProtection);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>Grid Protection Settings</Text>
          <FontAwesome
            name={showGridProtection ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showGridProtection && (
        <View style={styles.subCard}>
          <Text style={styles.subHeader}> Voltage Limits</Text>
          <Text style={styles.valueText}>
            Over Voltage: <Text style={styles.valueAccent}>250 V</Text>
          </Text>
          <Text style={styles.valueText}>
            Under Voltage: <Text style={styles.valueAccent}>190 V</Text>
          </Text>

          <Text style={styles.subHeader}> Frequency Limits</Text>
          <Text style={styles.valueText}>
            Over Frequency: <Text style={styles.valueAccent}>52.0 Hz</Text>
          </Text>
          <Text style={styles.valueText}>
            Under Frequency: <Text style={styles.valueAccent}>47.5 Hz</Text>
          </Text>

          <View style={[styles.switchRow, { marginTop: 10 }]}>
            <Text style={styles.label}>Enable LVRT / HVRT</Text>
            <Switch
              value={lvrtHvrt}
              onValueChange={setLvrtHvrt}
              thumbColor={lvrtHvrt ? COLORS.accent : COLORS.surface}
              trackColor={{ false: COLORS.border, true: COLORS.accent + "66" }}
            />
          </View>
        </View>
      )}

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowSystemProtection(!showSystemProtection);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>System Protection</Text>
          <FontAwesome
            name={showSystemProtection ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>

      {showSystemProtection && (
        <View style={styles.subCard}>
          <View style={styles.switchRow}>
            <Text style={styles.label}> Enable AFCI (Arc Fault Protection)</Text>
            <Switch
              value={afci}
              onValueChange={setAfci}
              thumbColor={afci ? COLORS.accent : COLORS.surface}
              trackColor={{ false: COLORS.border, true: COLORS.accent + "66" }}
            />
          </View>

          <View style={styles.switchRow}>
            <Text style={styles.label}> Enable Islanding Protection</Text>
            <Switch
              value={islanding}
              onValueChange={setIslanding}
              thumbColor={islanding ? COLORS.accent : COLORS.surface}
              trackColor={{ false: COLORS.border, true: COLORS.accent + "66" }}
            />
          </View>
        </View>
      )}
    </View>
  );
}
