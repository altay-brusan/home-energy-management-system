import React, { useState } from "react";
import { View, Text, TouchableOpacity, LayoutAnimation } from "react-native";
import ModeSelectionPanel, { OperationMode } from "./ModeSelectionPanel";

export default function EnergyPriorityCard({
  styles,
  COLORS,
  selectedMode,
  setSelectedMode,
  selectedGrid,
  setSelectedGrid,
  handleSave,
}: any) {
  const [showOperationPanel, setShowOperationPanel] = useState(false);
  const [showGridPanel, setShowGridPanel] = useState(false);

  return (
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}> Energy Priority & Mode Selection</Text>

      {/* Operation Mode */}
      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowOperationPanel(!showOperationPanel);
          if (showGridPanel) setShowGridPanel(false);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>Operation Mode</Text>
          <Text style={styles.valueAccent}>{selectedMode}</Text>
        </View>
      </TouchableOpacity>

      {showOperationPanel && (
        <ModeSelectionPanel
          type="operation"
          selectedMode={selectedMode}
          onSelectMode={setSelectedMode}
          onSave={handleSave}
          visible={showOperationPanel}
        />
      )}

      {/* Grid Interaction */}
      <TouchableOpacity
        activeOpacity={0.8}
        onPress={() => {
          LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
          setShowGridPanel(!showGridPanel);
          if (showOperationPanel) setShowOperationPanel(false);
        }}
      >
        <View style={styles.dropdownRow}>
          <Text style={styles.subCardTitle}>Grid Interaction</Text>
          <Text style={styles.valueAccent}>{selectedGrid}</Text>
        </View>
      </TouchableOpacity>

      {showGridPanel && (
        <ModeSelectionPanel
          type="grid"
          selectedGrid={selectedGrid}
          onSelectGrid={setSelectedGrid}
          onSave={handleSave}
          visible={showGridPanel}
        />
      )}
    </View>
  );
}
