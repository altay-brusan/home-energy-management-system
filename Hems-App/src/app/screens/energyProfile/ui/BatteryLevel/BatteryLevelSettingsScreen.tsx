import React, { useState, useEffect, useMemo } from "react";
import { View, Text, TouchableOpacity, Dimensions, ActivityIndicator, ScrollView } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation } from "@react-navigation/native";
import { GestureHandlerRootView, Gesture, GestureDetector } from "react-native-gesture-handler";
import Animated, { useSharedValue, useAnimatedStyle, runOnJS, cancelAnimation, useAnimatedReaction } from "react-native-reanimated";
import BatteryBar from "./BatteryBar";
import { getBatteryLevelStyles } from "./BatteryLevel.styles";
import { getThemeColors } from "../../../../../theme/constants";
import { useThemeStore } from "../../../../../store/themeStore";
import MainLayout from "../../../../layouts/MainLayout";
import { useI18n } from "../../../../../hooks/useI18n";
import { useDeviceParameters } from "../../../../../hooks/useDeviceParameters";
import { setupDeviceParameterValuesService } from "../../../../../api/services/setupDeviceParameterValuesService";

const { height: SCREEN_HEIGHT, width: SCREEN_WIDTH } = Dimensions.get("window");
const isSmall = SCREEN_WIDTH <= 380;
const isVerySmall = SCREEN_WIDTH <= 340;
const BAR_HEIGHT = SCREEN_HEIGHT * 0.6;
const HANDLE_HEIGHT = 44;

export default function BatteryLevelSettingsScreen() {
  const navigation = useNavigation<any>();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = getBatteryLevelStyles(COLORS, isDark);
  const deviceParams = useDeviceParameters();

  const [chargingSOC, setChargingSOC] = useState(100);
  const [backupSOC, setBackupSOC] = useState(25);
  const [dischargeSOC, setDischargeSOC] = useState(0);
  const [warningSOC, setWarningSOC] = useState(15);
  const [isApplying, setIsApplying] = useState(false);

  const [chargingSOCParam, setChargingSOCParam] = useState<{id: string, valueId: string} | null>(null);
  const [backupSOCParam, setBackupSOCParam] = useState<{id: string, valueId: string} | null>(null);
  const [warningSOCParam, setWarningSOCParam] = useState<{id: string, valueId: string} | null>(null);
  const [dischargeSOCParam, setDischargeSOCParam] = useState<{id: string, valueId: string} | null>(null);

  const levelColors = {
    charge: "#4CAF50",      // Green
    backup: "#2196F3",      // Blue
    warning: "#FF9800",     // Orange
    discharge: "#F44336",   // Red
  };

  const chargeY = useSharedValue((1 - chargingSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2);
  const backupY = useSharedValue((1 - backupSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2);
  const dischargeY = useSharedValue((1 - dischargeSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2);
  const warningY = useSharedValue((1 - warningSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2);

  const updateSOC = (y: number, setter: (v: number) => void, type: "charge" | "backup" | "discharge" | "warning") => {
    let value = 100 - ((y + HANDLE_HEIGHT / 2) / BAR_HEIGHT) * 100;
    value = Math.max(0, Math.min(100, Math.round(value)));

    if (type === "discharge") value = Math.min(value, warningSOC - 1, backupSOC - 1, chargingSOC - 1);
    if (type === "warning") {
      value = Math.min(value, backupSOC - 1);
      value = Math.max(value, dischargeSOC + 1);
    }
    if (type === "backup") {
      value = Math.min(value, chargingSOC - 1);
      value = Math.max(value, warningSOC + 1);
    }
    if (type === "charge") value = Math.max(value, backupSOC + 1);

    runOnJS(setter)(value);
  };

  const chargeGesture = useMemo(() => {
    return Gesture.Pan()
      .onBegin(() => {
        cancelAnimation(chargeY);
        (chargeY as any).startY = chargeY.value;
      })
      .onUpdate((e) => {
        let newY = (chargeY as any).startY + e.translationY;
        newY = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, newY));

        const toY = (v: number) => (1 - v / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
        const lowerLimitY = toY(backupSOC + 1) + 10;
        if (newY > lowerLimitY) newY = lowerLimitY;

        chargeY.value = newY;
        runOnJS(updateSOC)(newY, setChargingSOC, "charge");
      })
      .onEnd(() => {
        chargeY.value = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, chargeY.value));
      })
      .runOnJS(true);
  }, [backupSOC]);

  const backupGesture = useMemo(() => {
    return Gesture.Pan()
      .onBegin(() => {
        cancelAnimation(backupY);
        (backupY as any).startY = backupY.value;
      })
      .onUpdate((e) => {
        let newY = (backupY as any).startY + e.translationY;
        newY = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, newY));

        const toY = (v: number) => (1 - v / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
        const upperLimitY = toY(chargingSOC - 1) - 10;
        const lowerLimitY = toY(warningSOC + 1) + 10;
        if (newY < upperLimitY) newY = upperLimitY;
        if (newY > lowerLimitY) newY = lowerLimitY;

        backupY.value = newY;
        runOnJS(updateSOC)(newY, setBackupSOC, "backup");
      })
      .onEnd(() => {
        backupY.value = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, backupY.value));
      })
      .runOnJS(true);
  }, [chargingSOC, warningSOC]);

  const warningGesture = useMemo(() => {
    return Gesture.Pan()
      .onBegin(() => {
        cancelAnimation(warningY);
        (warningY as any).startY = warningY.value;
      })
      .onUpdate((e) => {
        let newY = (warningY as any).startY + e.translationY;
        newY = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, newY));

        const toY = (v: number) => (1 - v / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
        const upperLimitY = toY(backupSOC - 1) - 10;
        const lowerLimitY = toY(dischargeSOC + 1) + 10;
        if (newY < upperLimitY) newY = upperLimitY;
        if (newY > lowerLimitY) newY = lowerLimitY;

        warningY.value = newY;
        runOnJS(updateSOC)(newY, setWarningSOC, "warning");
      })
      .onEnd(() => {
        warningY.value = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, warningY.value));
      })
      .runOnJS(true);
  }, [backupSOC, dischargeSOC]);

  const dischargeGesture = useMemo(() => {
    return Gesture.Pan()
      .onBegin(() => {
        cancelAnimation(dischargeY);
        (dischargeY as any).startY = dischargeY.value;
      })
      .onUpdate((e) => {
        let newY = (dischargeY as any).startY + e.translationY;
        newY = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, newY));

        const toY = (v: number) => (1 - v / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
        const warningLimitY = toY(warningSOC - 1);
        if (newY < warningLimitY) newY = warningLimitY;

        dischargeY.value = newY;
        runOnJS(updateSOC)(newY, setDischargeSOC, "discharge");
      })
      .onEnd(() => {
        dischargeY.value = Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, Math.max(-HANDLE_HEIGHT / 2, dischargeY.value));
      })
      .runOnJS(true);
  }, [warningSOC]);

  const chargeStyle = useAnimatedStyle(() => {
    const isAllClose = 
      Math.abs(chargeY.value - backupY.value) < 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) < 50;
    const isThreeTop = 
      Math.abs(chargeY.value - backupY.value) < 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) >= 50;
    
    if (isAllClose) {
      const avgY = (chargeY.value + backupY.value + warningY.value + dischargeY.value) / 4;
      return { transform: [{ translateY: avgY }] };
    }
    if (isThreeTop) {
      const avgY = (chargeY.value + backupY.value + warningY.value) / 3;
      return { transform: [{ translateY: avgY }] };
    }
    return { transform: [{ translateY: chargeY.value }] };
  });
  
  const backupStyle = useAnimatedStyle(() => {
    const isAllClose = 
      Math.abs(chargeY.value - backupY.value) < 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) < 50;
    const isThreeTop = 
      Math.abs(chargeY.value - backupY.value) < 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) >= 50;
    const isThreeBottom = 
      Math.abs(chargeY.value - backupY.value) >= 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) < 50;
    
    if (isAllClose) {
      const avgY = (chargeY.value + backupY.value + warningY.value + dischargeY.value) / 4;
      return { transform: [{ translateY: avgY }] };
    }
    if (isThreeTop) {
      const avgY = (chargeY.value + backupY.value + warningY.value) / 3;
      return { transform: [{ translateY: avgY }] };
    }
    if (isThreeBottom) {
      const avgY = (backupY.value + warningY.value + dischargeY.value) / 3;
      return { transform: [{ translateY: avgY }] };
    }
    return { transform: [{ translateY: backupY.value }] };
  });
  
  const warningStyle = useAnimatedStyle(() => {
    const isAllClose = 
      Math.abs(chargeY.value - backupY.value) < 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) < 50;
    const isThreeTop = 
      Math.abs(chargeY.value - backupY.value) < 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) >= 50;
    const isThreeBottom = 
      Math.abs(chargeY.value - backupY.value) >= 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) < 50;
    
    if (isAllClose) {
      const avgY = (chargeY.value + backupY.value + warningY.value + dischargeY.value) / 4;
      return { transform: [{ translateY: avgY + 30 }] };
    }
    if (isThreeTop) {
      const avgY = (chargeY.value + backupY.value + warningY.value) / 3;
      return { transform: [{ translateY: avgY + 25 }] };
    }
    if (isThreeBottom) {
      const avgY = (backupY.value + warningY.value + dischargeY.value) / 3;
      return { transform: [{ translateY: avgY }] };
    }
    return { transform: [{ translateY: warningY.value }] };
  });
  
  const dischargeStyle = useAnimatedStyle(() => {
    const isAllClose = 
      Math.abs(chargeY.value - backupY.value) < 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) < 50;
    const isThreeBottom = 
      Math.abs(chargeY.value - backupY.value) >= 50 &&
      Math.abs(backupY.value - warningY.value) < 50 &&
      Math.abs(warningY.value - dischargeY.value) < 50;
    
    if (isAllClose) {
      const avgY = (chargeY.value + backupY.value + warningY.value + dischargeY.value) / 4;
      return { transform: [{ translateY: avgY + 30 }] };
    }
    if (isThreeBottom) {
      const avgY = (backupY.value + warningY.value + dischargeY.value) / 3;
      return { transform: [{ translateY: avgY + 25 }] };
    }
    return { transform: [{ translateY: dischargeY.value }] };
  });
  
  // Convert to regular values for use in render
  const [chargeBackupClose, setChargeBackupClose] = useState(false);
  const [backupWarningClose, setBackupWarningClose] = useState(false);
  const [warningDischargeClose, setWarningDischargeClose] = useState(false);
  
  // Check if all 4 are close together
  const allFourClose = chargeBackupClose && backupWarningClose && warningDischargeClose;
  
  // Check for 3-element close groups
  const threeTopClose = chargeBackupClose && backupWarningClose && !warningDischargeClose; // charge, backup, warning
  const threeBottomClose = backupWarningClose && warningDischargeClose && !chargeBackupClose; // backup, warning, discharge

  // Use animated reactions to update state when values change
  useAnimatedReaction(
    () => Math.abs(chargeY.value - backupY.value) < 50,
    (isClose) => {
      runOnJS(setChargeBackupClose)(isClose);
    }
  );

  useAnimatedReaction(
    () => Math.abs(backupY.value - warningY.value) < 50,
    (isClose) => {
      runOnJS(setBackupWarningClose)(isClose);
    }
  );

  useAnimatedReaction(
    () => Math.abs(warningY.value - dischargeY.value) < 50,
    (isClose) => {
      runOnJS(setWarningDischargeClose)(isClose);
    }
  );

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    const chargingSOCFound = deviceParams.findParameterByName("Charging Cutoff SOC");
    if (chargingSOCFound?.setupDeviceParameterValue) {
      setChargingSOCParam({
        id: chargingSOCFound.id,
        valueId: chargingSOCFound.setupDeviceParameterValue.id,
      });
      const value = parseInt(chargingSOCFound.setupDeviceParameterValue.value || "100", 10);
      setChargingSOC(value);
    }

    const backupSOCFound = deviceParams.findParameterByName("Backup SOC");
    if (backupSOCFound?.setupDeviceParameterValue) {
      setBackupSOCParam({
        id: backupSOCFound.id,
        valueId: backupSOCFound.setupDeviceParameterValue.id,
      });
      const value = parseInt(backupSOCFound.setupDeviceParameterValue.value || "25", 10);
      setBackupSOC(value);
    }

    const warningSOCFound = deviceParams.findParameterByName("Warning SOC");
    if (warningSOCFound?.setupDeviceParameterValue) {
      setWarningSOCParam({
        id: warningSOCFound.id,
        valueId: warningSOCFound.setupDeviceParameterValue.id,
      });
      const value = parseInt(warningSOCFound.setupDeviceParameterValue.value || "15", 10);
      setWarningSOC(value);
    }

    const dischargeSOCFound = deviceParams.findParameterByName("Discharging Cut-off SOC");
    if (dischargeSOCFound?.setupDeviceParameterValue) {
      setDischargeSOCParam({
        id: dischargeSOCFound.id,
        valueId: dischargeSOCFound.setupDeviceParameterValue.id,
      });
      const value = parseInt(dischargeSOCFound.setupDeviceParameterValue.value || "0", 10);
      setDischargeSOC(value);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handleSave = async () => {
    setIsApplying(true);

    if (deviceParams?.setupDeviceId) {
      try {
        if (chargingSOCParam) {
          await setupDeviceParameterValuesService.update(chargingSOCParam.valueId, {
            id: chargingSOCParam.valueId,
            value: chargingSOC.toString(),
            sortOrder: 1,
            modelNumber: "NX-200",
          });
        }

        if (backupSOCParam) {
          await setupDeviceParameterValuesService.update(backupSOCParam.valueId, {
            id: backupSOCParam.valueId,
            value: backupSOC.toString(),
            sortOrder: 1,
            modelNumber: "NX-200",
          });
        }

        if (warningSOCParam) {
          await setupDeviceParameterValuesService.update(warningSOCParam.valueId, {
            id: warningSOCParam.valueId,
            value: warningSOC.toString(),
            sortOrder: 1,
            modelNumber: "NX-200",
          });
        }

        if (dischargeSOCParam) {
          await setupDeviceParameterValuesService.update(dischargeSOCParam.valueId, {
            id: dischargeSOCParam.valueId,
            value: dischargeSOC.toString(),
            sortOrder: 1,
            modelNumber: "NX-200",
          });
        }
      } catch (error) {
      }
    }

    setTimeout(() => setIsApplying(false), 1000);
  };

  useEffect(() => {
    chargeY.value = (1 - chargingSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
    backupY.value = (1 - backupSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
    dischargeY.value = (1 - dischargeSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
    warningY.value = (1 - warningSOC / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;
  }, [chargingSOC, backupSOC, dischargeSOC, warningSOC]);

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <MainLayout>
        <View style={styles.headerRow}>
          <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
            <FontAwesome name="chevron-left" size={22} color={COLORS.textSoft} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>{t('batteryLevel.title')}</Text>
          <TouchableOpacity onPress={handleSave} style={styles.saveButton}>
            <Text style={[styles.saveText, { color: COLORS.accent }]}>{t('batteryLevel.save')}</Text>
          </TouchableOpacity>
        </View>
        <View style={styles.infoBanner}>
          <Text style={styles.infoText}>{t('batteryLevel.info')}</Text>
        </View>
        {isApplying && (
          <View style={{ alignSelf: "center", flexDirection: "row", alignItems: "center", marginBottom: 10 }}>
            <ActivityIndicator color={COLORS.accent} size="small" />
            <Text style={{ color: COLORS.textSoft, fontSize: 15, marginLeft: 8, fontWeight: "600" }}>{t('batteryLevel.applying')}</Text>
          </View>
        )}
        <ScrollView 
          style={{ flex: 1 }}
          contentContainerStyle={{ paddingBottom: 0 }}
          showsVerticalScrollIndicator={false}
        >
        <View style={[
          styles.cardContainer, 
          { 
            marginTop: 15, 
            flexDirection: 'column',
            paddingBottom: 0,
          }
        ]}>
          <View style={{ flexDirection: "row", alignItems: "flex-start", minHeight: BAR_HEIGHT }}>
            <BatteryBar COLORS={COLORS} isDark={isDark} minSOC={dischargeSOC} />
            <View style={{ height: BAR_HEIGHT, marginLeft: isVerySmall ? 12 : isSmall ? 18 : 24, flex: 1 }}>
              {/* Charge */}
              <GestureDetector gesture={chargeGesture}>
                <Animated.View 
                  style={[
                    styles.handleCard, 
                    chargeStyle,
                    allFourClose && {
                      width: '48%',
                      left: 0,
                    },
                    threeTopClose && {
                      width: '48%',
                      left: 0,
                    },
                    chargeBackupClose && !allFourClose && !threeTopClose && {
                      width: '48%',
                      left: 0,
                      right: undefined,
                    }
                  ]} 
                  key="charge"
                >
                  {(chargeBackupClose || allFourClose || threeTopClose) ? (
                    <View style={{ 
                      flexDirection: 'row', 
                      alignItems: 'center', 
                      justifyContent: 'center',
                      width: '100%',
                      backgroundColor: `${levelColors.charge}20`,
                      borderRadius: 8,
                      paddingVertical: (allFourClose || threeTopClose) ? 3 : 4,
                      paddingHorizontal: (allFourClose || threeTopClose) ? 6 : 8,
                    }}>
                      <Text 
                        style={[
                          styles.handleText, 
                          { 
                            color: levelColors.charge,
                            fontSize: (allFourClose || threeTopClose)
                              ? (isVerySmall ? 14 : isSmall ? 16 : 18)
                              : (isVerySmall ? 16 : isSmall ? 18 : 20),
                            fontWeight: '700',
                          }
                        ]}
                      >
                        {chargingSOC}%
                      </Text>
                    </View>
                  ) : (
                    <Text 
                      style={[styles.handleText]}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      <Text style={{ color: levelColors.charge, fontWeight: '700' }}>{chargingSOC}%</Text>{' '}
                      <Text style={[styles.handleSub, { color: levelColors.charge, opacity: 0.9 }]}>{t('batteryLevel.chargeCutoff')}</Text>
                    </Text>
                  )}
                </Animated.View>
              </GestureDetector>
              
              {/* Backup */}
              <GestureDetector gesture={backupGesture}>
                <Animated.View 
                  style={[
                    styles.handleCard, 
                    backupStyle,
                    allFourClose && {
                      width: '48%',
                      right: 0,
                    },
                    threeTopClose && {
                      width: '48%',
                      right: 0,
                    },
                    threeBottomClose && {
                      width: '48%',
                      right: 0,
                    },
                    chargeBackupClose && !allFourClose && !threeTopClose && {
                      width: '48%',
                      right: 0,
                      left: undefined,
                    },
                    backupWarningClose && !chargeBackupClose && !allFourClose && !threeTopClose && !threeBottomClose && {
                      width: '48%',
                      left: 0,
                      right: undefined,
                    }
                  ]} 
                  key="backup"
                >
                  {(chargeBackupClose || backupWarningClose || allFourClose || threeTopClose || threeBottomClose) ? (
                    <View style={{ 
                      flexDirection: 'row', 
                      alignItems: 'center', 
                      justifyContent: 'center',
                      width: '100%',
                      backgroundColor: `${levelColors.backup}20`,
                      borderRadius: 8,
                      paddingVertical: (allFourClose || threeTopClose || threeBottomClose) ? 3 : 4,
                      paddingHorizontal: (allFourClose || threeTopClose || threeBottomClose) ? 6 : 8,
                    }}>
                      <Text 
                        style={[
                          styles.handleText, 
                          { 
                            color: levelColors.backup,
                            fontSize: (allFourClose || threeTopClose || threeBottomClose)
                              ? (isVerySmall ? 14 : isSmall ? 16 : 18)
                              : (isVerySmall ? 16 : isSmall ? 18 : 20),
                            fontWeight: '700',
                          }
                        ]}
                      >
                        {backupSOC}%
                      </Text>
                    </View>
                  ) : (
                    <Text 
                      style={[styles.handleText]}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      <Text style={{ color: levelColors.backup, fontWeight: '700' }}>{backupSOC}%</Text>{' '}
                      <Text style={[styles.handleSub, { color: levelColors.backup, opacity: 0.9 }]}>{t('batteryLevel.backupSOC')}</Text>
                    </Text>
                  )}
                </Animated.View>
              </GestureDetector>
              
              {/* Warning */}
              <GestureDetector gesture={warningGesture}>
                <Animated.View 
                  style={[
                    styles.handleCard, 
                    warningStyle,
                    allFourClose && {
                      width: '48%',
                      left: 0,
                    },
                    threeTopClose && {
                      width: '48%',
                      left: 0,
                    },
                    threeBottomClose && {
                      width: '48%',
                      left: 0,
                    },
                    backupWarningClose && !warningDischargeClose && !allFourClose && !threeTopClose && {
                      width: '48%',
                      right: 0,
                      left: undefined,
                    },
                    warningDischargeClose && !backupWarningClose && !allFourClose && !threeBottomClose && {
                      width: '48%',
                      left: 0,
                      right: undefined,
                    }
                  ]} 
                  key="warning"
                >
                  {(backupWarningClose || warningDischargeClose || allFourClose || threeTopClose || threeBottomClose) ? (
                    <View style={{ 
                      flexDirection: 'row', 
                      alignItems: 'center', 
                      justifyContent: 'center',
                      width: '100%',
                      backgroundColor: `${levelColors.warning}20`,
                      borderRadius: 8,
                      paddingVertical: (allFourClose || threeTopClose || threeBottomClose) ? 3 : 4,
                      paddingHorizontal: (allFourClose || threeTopClose || threeBottomClose) ? 6 : 8,
                    }}>
                      <Text 
                        style={[
                          styles.handleText, 
                          { 
                            color: levelColors.warning,
                            fontSize: (allFourClose || threeTopClose || threeBottomClose)
                              ? (isVerySmall ? 14 : isSmall ? 16 : 18)
                              : (isVerySmall ? 16 : isSmall ? 18 : 20),
                            fontWeight: '700',
                          }
                        ]}
                      >
                        {warningSOC}%
                      </Text>
                    </View>
                  ) : (
                    <Text 
                      style={[styles.handleText]}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      <Text style={{ color: levelColors.warning, fontWeight: '700' }}>{warningSOC}%</Text>{' '}
                      <Text style={[styles.handleSub, { color: levelColors.warning, opacity: 0.9 }]}>{t('batteryLevel.warningSOC')}</Text>
                    </Text>
                  )}
                </Animated.View>
              </GestureDetector>
              
              {/* Discharge */}
              <GestureDetector gesture={dischargeGesture}>
                <Animated.View 
                  style={[
                    styles.handleCard, 
                    dischargeStyle,
                    allFourClose && {
                      width: '48%',
                      right: 0,
                    },
                    threeBottomClose && {
                      width: '48%',
                      right: 0,
                    },
                    warningDischargeClose && !allFourClose && !threeBottomClose && {
                      width: '48%',
                      right: 0,
                      left: undefined,
                    }
                  ]} 
                  key="discharge"
                >
                  {(warningDischargeClose || allFourClose || threeBottomClose) ? (
                    <View style={{ 
                      flexDirection: 'row', 
                      alignItems: 'center', 
                      justifyContent: 'center',
                      width: '100%',
                      backgroundColor: `${levelColors.discharge}20`,
                      borderRadius: 8,
                      paddingVertical: (allFourClose || threeBottomClose) ? 3 : 4,
                      paddingHorizontal: (allFourClose || threeBottomClose) ? 6 : 8,
                    }}>
                      <Text 
                        style={[
                          styles.handleText, 
                          { 
                            color: levelColors.discharge,
                            fontSize: (allFourClose || threeBottomClose)
                              ? (isVerySmall ? 14 : isSmall ? 16 : 18)
                              : (isVerySmall ? 16 : isSmall ? 18 : 20),
                            fontWeight: '700',
                          }
                        ]}
                      >
                        {dischargeSOC}%
                      </Text>
                    </View>
                  ) : (
                    <Text 
                      style={[styles.handleText]}
                      numberOfLines={1}
                      ellipsizeMode="tail"
                    >
                      <Text style={{ color: levelColors.discharge, fontWeight: '700' }}>{dischargeSOC}%</Text>{' '}
                      <Text style={[styles.handleSub, { color: levelColors.discharge, opacity: 0.9 }]}>{t('batteryLevel.dischargeCutoff')}</Text>
                    </Text>
                  )}
                </Animated.View>
              </GestureDetector>
            </View>
          </View>
          
          {/* Legend - Always visible */}
          <View style={{
              marginTop: 20,
              paddingTop: 16,
              paddingHorizontal: 4,
              paddingBottom: 20,
              borderTopWidth: 1,
              borderTopColor: isDark ? COLORS.border : COLORS.gray200,
              width: '100%',
              alignSelf: 'stretch',
            }}>
              <Text style={{
                fontSize: 11,
                fontWeight: '600',
                color: COLORS.textSoft,
                opacity: 0.7,
                marginBottom: 10,
                textAlign: 'center',
              }}>
                {t('batteryLevel.legendTitle')}
              </Text>
              <View style={{
                flexDirection: 'row',
                flexWrap: 'wrap',
                justifyContent: 'center',
              }}>
                {/* Charge */}
                <View style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  marginRight: 12,
                  marginBottom: 8,
                  flex: isVerySmall ? 1 : 0,
                  minWidth: isVerySmall ? '45%' : undefined,
                }}>
                  <View style={{
                    width: 16,
                    height: 16,
                    borderRadius: 8,
                    backgroundColor: levelColors.charge,
                    marginRight: 8,
                    flexShrink: 0,
                  }} />
                  <Text 
                    style={{
                      fontSize: 12,
                      color: levelColors.charge,
                      fontWeight: '700',
                      flexShrink: 1,
                    }}
                    numberOfLines={2}
                  >
                    {t('batteryLevel.chargeCutoff')}
                  </Text>
                </View>
                
                {/* Backup */}
                <View style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  marginRight: 12,
                  marginBottom: 10,
                  flex: isVerySmall ? 1 : 0,
                  minWidth: isVerySmall ? '45%' : undefined,
                }}>
                  <View style={{
                    width: 16,
                    height: 16,
                    borderRadius: 8,
                    backgroundColor: levelColors.backup,
                    marginRight: 8,
                    flexShrink: 0,
                  }} />
                  <Text 
                    style={{
                      fontSize: 12,
                      color: levelColors.backup,
                      fontWeight: '700',
                      flexShrink: 1,
                    }}
                    numberOfLines={2}
                  >
                    {t('batteryLevel.backupSOC')}
                  </Text>
                </View>
                
                {/* Warning */}
                <View style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  marginRight: 12,
                  marginBottom: 10,
                  flex: isVerySmall ? 1 : 0,
                  minWidth: isVerySmall ? '45%' : undefined,
                }}>
                  <View style={{
                    width: 16,
                    height: 16,
                    borderRadius: 8,
                    backgroundColor: levelColors.warning,
                    marginRight: 8,
                    flexShrink: 0,
                  }} />
                  <Text 
                    style={{
                      fontSize: 12,
                      color: levelColors.warning,
                      fontWeight: '700',
                      flexShrink: 1,
                    }}
                    numberOfLines={2}
                  >
                    {t('batteryLevel.warningSOC')}
                  </Text>
                </View>
                
                {/* Discharge */}
                <View style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  marginRight: 12,
                  marginBottom: 10,
                  flex: isVerySmall ? 1 : 0,
                  minWidth: isVerySmall ? '45%' : undefined,
                }}>
                  <View style={{
                    width: 16,
                    height: 16,
                    borderRadius: 8,
                    backgroundColor: levelColors.discharge,
                    marginRight: 8,
                    flexShrink: 0,
                  }} />
                  <Text 
                    style={{
                      fontSize: 12,
                      color: levelColors.discharge,
                      fontWeight: '700',
                      flexShrink: 1,
                    }}
                    numberOfLines={2}
                  >
                    {t('batteryLevel.dischargeCutoff')}
                  </Text>
                </View>
              </View>
            </View>
        </View>
        </ScrollView>
      </MainLayout>
    </GestureHandlerRootView>
  );
}
