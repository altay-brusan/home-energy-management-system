import React, { useState, useCallback, useEffect } from "react";
import {
  ScrollView,
  View,
  Text,
  RefreshControl,
  Pressable,
  LayoutAnimation,
  Platform,
  UIManager,
  ActivityIndicator,
} from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useNavigation } from "@react-navigation/native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import MainLayout from "../../layouts/MainLayout";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors } from "../../../theme/constants";
import { getEnergyProfileStyles } from "./EnergyProfile.styles";

import {
  EnergyPriorityCard,
  SolarPowerCard,
  BatteryManagementCard,
  TimeOfUseCard,
  ProtectionSafetyCard,
   GeneratorSmartLoadsCard,
} from "./ui";

export default function EnergyProfileScreen() {
  const insets = useSafeAreaInsets();
  const navigation = useNavigation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const styles = getEnergyProfileStyles(COLORS, isDark);

  const [refreshing, setRefreshing] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const [selectedMode, setSelectedMode] = useState("Load Priority");
  const [selectedGrid, setSelectedGrid] = useState("IEEE-1547");

  useEffect(() => {
    if (
      Platform.OS === "android" &&
      UIManager.setLayoutAnimationEnabledExperimental
    ) {
      UIManager.setLayoutAnimationEnabledExperimental(true);
    }
  }, []);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 1000);
  }, []);

  const handleSave = () => {
    setIsLoading(true);
    setTimeout(() => setIsLoading(false), 1500);
  };

  return (
    <MainLayout>
      {isLoading && (
        <View style={styles.loadingOverlay}>
          <View style={styles.loadingBox}>
            <ActivityIndicator size="large" color={COLORS.accent} />
            <Text style={styles.loadingText}>Applying settings...</Text>
          </View>
        </View>
      )}

      <ScrollView
        style={styles.container}
        contentContainerStyle={{ paddingBottom: insets.bottom + 32 }}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={[COLORS.accent]}
            tintColor={COLORS.accent}
          />
        }
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.headerRow}>
          <Pressable
            style={styles.backButton}
            onPress={() => navigation.goBack()}
          >
            <FontAwesome name="chevron-left" size={22} color={COLORS.textSoft} />
          </Pressable>
          <Text style={styles.screenTitle}>My Energy Profile</Text>
          <View style={{ width: 32 }} />
        </View>

        <EnergyPriorityCard
          styles={styles}
          COLORS={COLORS}
          selectedMode={selectedMode}
          setSelectedMode={setSelectedMode}
          selectedGrid={selectedGrid}
          setSelectedGrid={setSelectedGrid}
          handleSave={handleSave}
        />

        <SolarPowerCard styles={styles} COLORS={COLORS} />

        <BatteryManagementCard styles={styles} COLORS={COLORS} />

        <TimeOfUseCard
          styles={styles}
          COLORS={COLORS}
          setIsLoading={setIsLoading}
        />
        <ProtectionSafetyCard styles={styles} COLORS={COLORS} />
        <GeneratorSmartLoadsCard 
  styles={styles} 
  COLORS={COLORS} 
  setIsLoading={setIsLoading} 
/> 
      </ScrollView>
    </MainLayout>
  );
}
