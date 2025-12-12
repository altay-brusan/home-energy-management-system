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
import { ArrowLeft } from 'lucide-react-native';
import MainLayout from "../../layouts/MainLayout";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors } from "../../../theme/constants";
import { getEnergyProfileStyles } from "./EnergyProfile.styles";
import { useI18n } from "../../../hooks/useI18n";
import { useDeviceParameters } from "../../../hooks/useDeviceParameters";
import { useSitesStore } from "../../../store/sitesStore";

import {
  EnergyPriorityCard,
  SolarPowerCard,
  BatteryManagementCard,
  TimeOfUseCard,
  ProtectionSafetyCard,
   GeneratorSmartLoadsCard,
   AdvancedPowerControlCard
} from "./ui";

export default function EnergyProfileScreen() {
  const insets = useSafeAreaInsets();
  const navigation = useNavigation();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const styles = getEnergyProfileStyles(COLORS, isDark);
  const { t } = useI18n();
  const { loadSelectedSiteDevice } = useSitesStore();
  const deviceParams = useDeviceParameters();

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

  // Load device parameters when screen mounts
  useEffect(() => {
    loadSelectedSiteDevice().catch(() => {
      // Silently handle errors - device parameters will stay null
    });
  }, [loadSelectedSiteDevice]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await deviceParams.refetch();
    setTimeout(() => setRefreshing(false), 1000);
  }, [deviceParams]);

const handleSave = async (onClose?: () => void) => {
  setIsLoading(true);
  await new Promise<void>((resolve) => setTimeout(resolve, 1500)); 
  setIsLoading(false);

  if (onClose) {
    setTimeout(() => {
      LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
      onClose();
    }, 300); 
  }
};
  return (
    <MainLayout>
      {isLoading && (
        <View style={styles.loadingOverlay}>
          <View style={styles.loadingBox}>
            <ActivityIndicator size="large" color={COLORS.accent} />
            <Text style={styles.loadingText}>{t('energyProfile.applyingSettings')}</Text>
          </View>
        </View>
      )}

      <ScrollView
        style={styles.container}
        contentContainerStyle={{ paddingBottom: insets.bottom + 100 }}
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
            style={{ paddingVertical: 8, paddingRight: 8 }}
            onPress={() => navigation.goBack()}
          >
            <ArrowLeft size={24} color={COLORS.textSoft} strokeWidth={2} />
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
          deviceParams={deviceParams}
        />

        <SolarPowerCard styles={styles} COLORS={COLORS} isDark={isDark} deviceParams={deviceParams} />

        <BatteryManagementCard styles={styles} COLORS={COLORS} deviceParams={deviceParams} />

        <TimeOfUseCard
          styles={styles}
          COLORS={COLORS}
          deviceParams={deviceParams}
        />
        <ProtectionSafetyCard styles={styles} COLORS={COLORS} isDark={isDark} deviceParams={deviceParams} />
        <GeneratorSmartLoadsCard 
          styles={styles} 
          COLORS={COLORS} 
          setIsLoading={setIsLoading}
          deviceParams={deviceParams}
        /> 
        <AdvancedPowerControlCard styles={styles} COLORS={COLORS} isDark={isDark} deviceParams={deviceParams} />
      </ScrollView>
    </MainLayout>
  );
}
