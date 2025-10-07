import React, { useState } from "react";
import {
  View,
  Text,
  TouchableOpacity,
  LayoutAnimation,
  TextInput,
} from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import ModeSelectionPanel, { GeneratorMode } from "./ModeSelectionPanel"; 

export default function GeneratorSmartLoadsCard({ styles, COLORS, setIsLoading }: any) {
  const [showGeneratorPanel, setShowGeneratorPanel] = useState(false);
 const [selectedGeneratorMode, setSelectedGeneratorMode] = useState<GeneratorMode>("Smart Load Output");
  const [turnOnValue, setTurnOnValue] = useState("");
  const [turnOffValue, setTurnOffValue] = useState("");

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>Generator & Smart Loads</Text>

      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowGeneratorPanel(!showGeneratorPanel);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>Port Mode</Text>
          <Text style={styles.valueAccent}>{selectedGeneratorMode}</Text>
          <FontAwesome
            name={showGeneratorPanel ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </View>
      </TouchableOpacity>
       {showGeneratorPanel && (
        <ModeSelectionPanel
          type="generator"
          selectedGeneratorMode={selectedGeneratorMode}
          onSelectGenerator={setSelectedGeneratorMode}
          onSave={() => {
            LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
            setShowGeneratorPanel(false);
            setIsLoading(true);
            setTimeout(() => setIsLoading(false), 1500);
          }}
          visible={showGeneratorPanel}
        />
      )}

      <View style={styles.subCard}>
        <Text style={styles.subCardTitle}>Smart Load Control</Text>

        <Text style={styles.valueText}>Turn ON when battery &gt;</Text>
        <TextInput
          style={{
            borderWidth: 1,
            borderColor: COLORS.border,
            borderRadius: 8,
            paddingVertical: 6,
            paddingHorizontal: 10,
            marginTop: 4,
            marginBottom: 10,
            color: COLORS.textSoft,
          }}
          placeholder="Enter %"
          placeholderTextColor={COLORS.textSoft + "88"}
          keyboardType="numeric"
          value={turnOnValue}
          onChangeText={setTurnOnValue}
        />

        <Text style={styles.valueText}>Turn OFF when battery &lt;</Text>
        <TextInput
          style={{
            borderWidth: 1,
            borderColor: COLORS.border,
            borderRadius: 8,
            paddingVertical: 6,
            paddingHorizontal: 10,
            marginTop: 4,
            color: COLORS.textSoft,
          }}
          placeholder="Enter %"
          placeholderTextColor={COLORS.textSoft + "88"}
          keyboardType="numeric"
          value={turnOffValue}
          onChangeText={setTurnOffValue}
        />
      </View>

      
    </View>
  );
}
