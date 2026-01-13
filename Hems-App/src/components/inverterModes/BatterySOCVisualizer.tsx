import React, { useState, useRef, useCallback } from "react";
import { View, Text, Dimensions, StyleSheet } from "react-native";
import { Gesture, GestureDetector } from "react-native-gesture-handler";
import Animated, {
  useSharedValue,
  useAnimatedStyle,
  useAnimatedReaction,
  runOnJS,
} from "react-native-reanimated";
import { useTranslation } from "react-i18next";
import BatterySOCBar from "./BatterySOCBar";

const { width: SCREEN_WIDTH } = Dimensions.get("window");
const isSmall = SCREEN_WIDTH <= 380;
const isVerySmall = SCREEN_WIDTH <= 340;

const HANDLE_HEIGHT = 44;
const DEFAULT_BAR_HEIGHT = 280;
const COLLISION_THRESHOLD = 50;

interface SOCValues {
  chargingSOC: number;
  backupSOC: number;
  warningSOC: number;
  dischargeSOC: number;
}

interface BatterySOCVisualizerProps {
  values: SOCValues;
  onValuesChange: (values: SOCValues) => void;
  barHeight?: number;
  onGestureStart?: () => void;
  onGestureEnd?: () => void;
  COLORS: Record<string, string>;
  isDark: boolean;
}

const levelColors = {
  charge: "#4CAF50",
  backup: "#2196F3",
  warning: "#FF9800",
  discharge: "#F44336",
};

export default function BatterySOCVisualizer({
  values,
  onValuesChange,
  barHeight = DEFAULT_BAR_HEIGHT,
  onGestureStart,
  onGestureEnd,
  COLORS,
  isDark,
}: BatterySOCVisualizerProps) {
  const { t } = useTranslation();
  const BAR_HEIGHT = barHeight;

  // Use ref to track current values during gesture
  const currentValues = useRef<SOCValues>({ ...values });
  const isDragging = useRef(false);

  // Update ref when values change from outside (only if not dragging)
  React.useEffect(() => {
    if (!isDragging.current) {
      currentValues.current = { ...values };
    }
  }, [values.chargingSOC, values.backupSOC, values.warningSOC, values.dischargeSOC]);

  // Calculate Y position from SOC value
  const socToY = (soc: number) => (1 - soc / 100) * BAR_HEIGHT - HANDLE_HEIGHT / 2;

  // Calculate SOC from Y position
  const yToSoc = (y: number) => {
    let value = 100 - ((y + HANDLE_HEIGHT / 2) / BAR_HEIGHT) * 100;
    return Math.max(0, Math.min(100, Math.round(value)));
  };

  // Shared values for animation
  const chargeY = useSharedValue(socToY(values.chargingSOC));
  const backupY = useSharedValue(socToY(values.backupSOC));
  const warningY = useSharedValue(socToY(values.warningSOC));
  const dischargeY = useSharedValue(socToY(values.dischargeSOC));

  // Sync Y positions when external values change (only if not dragging)
  React.useEffect(() => {
    if (!isDragging.current) {
      chargeY.value = socToY(values.chargingSOC);
      backupY.value = socToY(values.backupSOC);
      warningY.value = socToY(values.warningSOC);
      dischargeY.value = socToY(values.dischargeSOC);
    }
  }, [values.chargingSOC, values.backupSOC, values.warningSOC, values.dischargeSOC]);

  // For display
  const [displayValues, setDisplayValues] = useState<SOCValues>({ ...values });

  React.useEffect(() => {
    if (!isDragging.current) {
      setDisplayValues({ ...values });
    }
  }, [values.chargingSOC, values.backupSOC, values.warningSOC, values.dischargeSOC]);

  // Collision detection state
  const [chargeBackupClose, setChargeBackupClose] = useState(false);
  const [backupWarningClose, setBackupWarningClose] = useState(false);
  const [warningDischargeClose, setWarningDischargeClose] = useState(false);

  useAnimatedReaction(
    () => Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD,
    (isClose) => runOnJS(setChargeBackupClose)(isClose)
  );

  useAnimatedReaction(
    () => Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD,
    (isClose) => runOnJS(setBackupWarningClose)(isClose)
  );

  useAnimatedReaction(
    () => Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD,
    (isClose) => runOnJS(setWarningDischargeClose)(isClose)
  );

  const allFourClose = chargeBackupClose && backupWarningClose && warningDischargeClose;
  const threeTopClose = chargeBackupClose && backupWarningClose && !warningDischargeClose;
  const threeBottomClose = backupWarningClose && warningDischargeClose && !chargeBackupClose;

  const updateDisplay = useCallback((key: keyof SOCValues, value: number) => {
    setDisplayValues(prev => ({ ...prev, [key]: value }));
    currentValues.current = { ...currentValues.current, [key]: value };
  }, []);

  const handleGestureStart = useCallback(() => {
    isDragging.current = true;
    onGestureStart?.();
  }, [onGestureStart]);

  const commitChanges = useCallback(() => {
    onValuesChange({ ...currentValues.current });
    isDragging.current = false;
    onGestureEnd?.();
  }, [onValuesChange, onGestureEnd]);

  // Store start positions for gestures
  const gestureStartY = useRef({ charge: 0, backup: 0, warning: 0, discharge: 0 });

  // Gesture handlers
  const chargeGesture = Gesture.Pan()
    .runOnJS(true)
    .onStart(() => {
      gestureStartY.current.charge = chargeY.value;
      handleGestureStart();
    })
    .onUpdate((e) => {
      let newY = gestureStartY.current.charge + e.translationY;

      newY = Math.max(-HANDLE_HEIGHT / 2, Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, newY));

      // Must be above backup (lower Y = higher SOC)
      const minY = socToY(currentValues.current.backupSOC + 1);
      if (newY > minY) newY = minY;

      chargeY.value = newY;
      const newSoc = yToSoc(newY);
      updateDisplay('chargingSOC', newSoc);
    })
    .onEnd(() => {
      commitChanges();
    });

  const backupGesture = Gesture.Pan()
    .runOnJS(true)
    .onStart(() => {
      gestureStartY.current.backup = backupY.value;
      handleGestureStart();
    })
    .onUpdate((e) => {
      let newY = gestureStartY.current.backup + e.translationY;

      newY = Math.max(-HANDLE_HEIGHT / 2, Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, newY));

      // Must be below charge
      const maxY = socToY(displayValues.chargingSOC - 1);
      if (newY < maxY) newY = maxY;

      // Must be above warning
      const minY = socToY(currentValues.current.warningSOC + 1);
      if (newY > minY) newY = minY;

      backupY.value = newY;
      const newSoc = yToSoc(newY);
      updateDisplay('backupSOC', newSoc);
    })
    .onEnd(() => {
      commitChanges();
    });

  const warningGesture = Gesture.Pan()
    .runOnJS(true)
    .onStart(() => {
      gestureStartY.current.warning = warningY.value;
      handleGestureStart();
    })
    .onUpdate((e) => {
      let newY = gestureStartY.current.warning + e.translationY;

      newY = Math.max(-HANDLE_HEIGHT / 2, Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, newY));

      // Must be below backup
      const maxY = socToY(displayValues.backupSOC - 1);
      if (newY < maxY) newY = maxY;

      // Must be above discharge
      const minY = socToY(currentValues.current.dischargeSOC + 1);
      if (newY > minY) newY = minY;

      warningY.value = newY;
      const newSoc = yToSoc(newY);
      updateDisplay('warningSOC', newSoc);
    })
    .onEnd(() => {
      commitChanges();
    });

  const dischargeGesture = Gesture.Pan()
    .runOnJS(true)
    .onStart(() => {
      gestureStartY.current.discharge = dischargeY.value;
      handleGestureStart();
    })
    .onUpdate((e) => {
      let newY = gestureStartY.current.discharge + e.translationY;

      newY = Math.max(-HANDLE_HEIGHT / 2, Math.min(BAR_HEIGHT - HANDLE_HEIGHT / 2, newY));

      // Must be below warning
      const maxY = socToY(displayValues.warningSOC - 1);
      if (newY < maxY) newY = maxY;

      dischargeY.value = newY;
      const newSoc = yToSoc(newY);
      updateDisplay('dischargeSOC', newSoc);
    })
    .onEnd(() => {
      commitChanges();
    });

  // Animated styles with clustering
  const chargeStyle = useAnimatedStyle(() => {
    const isAllClose =
      Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD;
    const isThreeTop =
      Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) >= COLLISION_THRESHOLD;

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
      Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD;
    const isThreeTop =
      Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) >= COLLISION_THRESHOLD;
    const isThreeBottom =
      Math.abs(chargeY.value - backupY.value) >= COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD;

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
      Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD;
    const isThreeTop =
      Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) >= COLLISION_THRESHOLD;
    const isThreeBottom =
      Math.abs(chargeY.value - backupY.value) >= COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD;

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
      Math.abs(chargeY.value - backupY.value) < COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD;
    const isThreeBottom =
      Math.abs(chargeY.value - backupY.value) >= COLLISION_THRESHOLD &&
      Math.abs(backupY.value - warningY.value) < COLLISION_THRESHOLD &&
      Math.abs(warningY.value - dischargeY.value) < COLLISION_THRESHOLD;

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

  const handleWidth = isVerySmall ? 160 : isSmall ? 175 : 190;

  // Render compact badge when handles are close
  const renderCompactBadge = (color: string, value: number) => (
    <View style={[styles.compactBadge, { backgroundColor: `${color}20` }]}>
      <Text style={[styles.compactText, { color, fontSize: isVerySmall ? 14 : isSmall ? 16 : 18 }]}>
        {value}%
      </Text>
    </View>
  );

  // Render full label
  const renderFullLabel = (color: string, value: number, label: string) => (
    <Text style={styles.handleText} numberOfLines={1} ellipsizeMode="tail">
      <Text style={{ color, fontWeight: '700' }}>{value}%</Text>{' '}
      <Text style={[styles.handleSub, { color, opacity: 0.9 }]}>{label}</Text>
    </Text>
  );

  return (
    <View style={styles.container}>
      <View style={[styles.cardContainer, { backgroundColor: isDark ? COLORS.cardBg : COLORS.white }]}>
        <View style={[styles.visualizerRow, { minHeight: BAR_HEIGHT }]}>
          <BatterySOCBar barHeight={BAR_HEIGHT} COLORS={COLORS} isDark={isDark} />

          <View style={[styles.handlesContainer, { height: BAR_HEIGHT, marginLeft: isVerySmall ? 12 : isSmall ? 18 : 24 }]}>
            {/* Charge Handle */}
            <GestureDetector gesture={chargeGesture}>
              <Animated.View
                style={[
                  styles.handleCard,
                  { width: handleWidth },
                  chargeStyle,
                  allFourClose && { width: '48%', left: 0 },
                  threeTopClose && { width: '48%', left: 0 },
                  chargeBackupClose && !allFourClose && !threeTopClose && { width: '48%', left: 0 },
                ]}
              >
                {(chargeBackupClose || allFourClose || threeTopClose)
                  ? renderCompactBadge(levelColors.charge, displayValues.chargingSOC)
                  : renderFullLabel(levelColors.charge, displayValues.chargingSOC, t('batteryLevel.chargeCutoff'))
                }
              </Animated.View>
            </GestureDetector>

            {/* Backup Handle */}
            <GestureDetector gesture={backupGesture}>
              <Animated.View
                style={[
                  styles.handleCard,
                  { width: handleWidth },
                  backupStyle,
                  allFourClose && { width: '48%', right: 0, left: undefined },
                  threeTopClose && { width: '48%', right: 0, left: undefined },
                  threeBottomClose && { width: '48%', right: 0, left: undefined },
                  chargeBackupClose && !allFourClose && !threeTopClose && { width: '48%', right: 0, left: undefined },
                  backupWarningClose && !chargeBackupClose && !allFourClose && !threeTopClose && !threeBottomClose && { width: '48%', left: 0 },
                ]}
              >
                {(chargeBackupClose || backupWarningClose || allFourClose || threeTopClose || threeBottomClose)
                  ? renderCompactBadge(levelColors.backup, displayValues.backupSOC)
                  : renderFullLabel(levelColors.backup, displayValues.backupSOC, t('batteryLevel.backupSOC'))
                }
              </Animated.View>
            </GestureDetector>

            {/* Warning Handle */}
            <GestureDetector gesture={warningGesture}>
              <Animated.View
                style={[
                  styles.handleCard,
                  { width: handleWidth },
                  warningStyle,
                  allFourClose && { width: '48%', left: 0 },
                  threeTopClose && { width: '48%', left: 0 },
                  threeBottomClose && { width: '48%', left: 0 },
                  backupWarningClose && !warningDischargeClose && !allFourClose && !threeTopClose && { width: '48%', right: 0, left: undefined },
                  warningDischargeClose && !backupWarningClose && !allFourClose && !threeBottomClose && { width: '48%', left: 0 },
                ]}
              >
                {(backupWarningClose || warningDischargeClose || allFourClose || threeTopClose || threeBottomClose)
                  ? renderCompactBadge(levelColors.warning, displayValues.warningSOC)
                  : renderFullLabel(levelColors.warning, displayValues.warningSOC, t('batteryLevel.warningSOC'))
                }
              </Animated.View>
            </GestureDetector>

            {/* Discharge Handle */}
            <GestureDetector gesture={dischargeGesture}>
              <Animated.View
                style={[
                  styles.handleCard,
                  { width: handleWidth },
                  dischargeStyle,
                  allFourClose && { width: '48%', right: 0, left: undefined },
                  threeBottomClose && { width: '48%', right: 0, left: undefined },
                  warningDischargeClose && !allFourClose && !threeBottomClose && { width: '48%', right: 0, left: undefined },
                ]}
              >
                {(warningDischargeClose || allFourClose || threeBottomClose)
                  ? renderCompactBadge(levelColors.discharge, displayValues.dischargeSOC)
                  : renderFullLabel(levelColors.discharge, displayValues.dischargeSOC, t('batteryLevel.dischargeCutoff'))
                }
              </Animated.View>
            </GestureDetector>
          </View>
        </View>

        {/* Legend */}
        <View style={[styles.legendContainer, { borderTopColor: isDark ? COLORS.border : COLORS.gray200 }]}>
          <Text style={[styles.legendTitle, { color: COLORS.textSoft }]}>{t('batteryLevel.legendTitle')}</Text>
          <View style={styles.legendRow}>
            {[
              { key: 'charge', label: t('batteryLevel.chargeCutoff'), color: levelColors.charge },
              { key: 'backup', label: t('batteryLevel.backupSOC'), color: levelColors.backup },
              { key: 'warning', label: t('batteryLevel.warningSOC'), color: levelColors.warning },
              { key: 'discharge', label: t('batteryLevel.dischargeCutoff'), color: levelColors.discharge },
            ].map((item) => (
              <View key={item.key} style={[styles.legendItem, isVerySmall && { flex: 1, minWidth: '45%' }]}>
                <View style={[styles.legendDot, { backgroundColor: item.color }]} />
                <Text style={[styles.legendText, { color: item.color }]} numberOfLines={2}>
                  {item.label}
                </Text>
              </View>
            ))}
          </View>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    width: '100%',
    paddingVertical: 8,
  },
  cardContainer: {
    borderRadius: 24,
    padding: isVerySmall ? 12 : isSmall ? 16 : 20,
    paddingBottom: 0,
    shadowColor: '#000',
    shadowOpacity: 0.15,
    shadowRadius: 10,
    elevation: 5,
  },
  visualizerRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  handlesContainer: {
    flex: 1,
    position: 'relative',
  },
  handleCard: {
    position: 'absolute',
    backgroundColor: 'transparent',
    paddingVertical: isVerySmall ? 8 : 10,
    paddingHorizontal: isVerySmall ? 10 : 12,
  },
  handleText: {
    fontWeight: '700',
    fontSize: isVerySmall ? 13 : isSmall ? 14 : 15,
    flexShrink: 1,
  },
  handleSub: {
    fontSize: isVerySmall ? 11 : isSmall ? 12 : 13,
    fontWeight: '500',
  },
  compactBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    width: '100%',
    borderRadius: 8,
    paddingVertical: 3,
    paddingHorizontal: 6,
  },
  compactText: {
    fontWeight: '700',
  },
  legendContainer: {
    marginTop: 20,
    paddingTop: 16,
    paddingBottom: 20,
    borderTopWidth: 1,
  },
  legendTitle: {
    fontSize: 11,
    fontWeight: '600',
    opacity: 0.7,
    marginBottom: 10,
    textAlign: 'center',
  },
  legendRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: 12,
    marginBottom: 8,
  },
  legendDot: {
    width: 16,
    height: 16,
    borderRadius: 8,
    marginRight: 8,
  },
  legendText: {
    fontSize: 12,
    fontWeight: '700',
  },
});
