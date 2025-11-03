import React, { useState, useEffect } from "react";
import { View, Text, TouchableOpacity, Dimensions } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import ModeSelectionPanel, { GeneratorMode } from "./ModeSelectionPanel";
import Animated, {
  useSharedValue,
  useAnimatedGestureHandler,
  useAnimatedStyle,
  runOnJS,
  interpolateColor,
  withTiming,
} from "react-native-reanimated";
import { PanGestureHandler } from "react-native-gesture-handler";
import { useI18n } from "../../../../hooks/useI18n";
import { setupDeviceParameterValuesService } from "../../../../api/services/setupDeviceParameterValuesService";

export default function GeneratorSmartLoadsCard({ styles, COLORS, setIsLoading, isDark, deviceParams }: any) {
  const { width } = Dimensions.get("window");
  const isSmall = width <= 380;
  const isVerySmall = width <= 340;
  
  const [showGeneratorPanel, setShowGeneratorPanel] = useState(false);
  const [showSmartLoad, setShowSmartLoad] = useState(false);
  const [selectedGeneratorMode, setSelectedGeneratorMode] =
    useState<GeneratorMode>("Smart Load Output");

  const [turnOnValue, setTurnOnValue] = useState<number>(75);
  const [turnOffValue, setTurnOffValue] = useState<number>(25);
  const { t } = useI18n();

  const [portModeParams, setPortModeParams] = useState<{
    "Generator Input Only": {id: string, valueId: string} | null;
    "Smart Load Output": {id: string, valueId: string} | null;
    "Microinverter Input": {id: string, valueId: string} | null;
  }>({
    "Generator Input Only": null,
    "Smart Load Output": null,
    "Microinverter Input": null,
  });

  const [turnOnParam, setTurnOnParam] = useState<{id: string, valueId: string} | null>(null);
  const [turnOffParam, setTurnOffParam] = useState<{id: string, valueId: string} | null>(null);
  
  const [hasPortMode, setHasPortMode] = useState(false);
  const [hasSmartLoadControl, setHasSmartLoadControl] = useState(false);

  const sliderWidth = isVerySmall ? width * 0.8 : isSmall ? width * 0.85 : 300;
  const handleSize = 26;
  const onX = useSharedValue(((turnOnValue - 75) / 25) * sliderWidth); 
  const offX = useSharedValue((turnOffValue / 25) * sliderWidth);

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) return;

    const generatorInputFound = deviceParams.findParameterByName("Generator Input Only");
    const smartLoadOutputFound = deviceParams.findParameterByName("Smart Load Output");
    const microinverterInputFound = deviceParams.findParameterByName("Microinverter Input");

    const portParams: typeof portModeParams = {
      "Generator Input Only": null,
      "Smart Load Output": null,
      "Microinverter Input": null,
    };

    if (generatorInputFound?.setupDeviceParameterValue) {
      portParams["Generator Input Only"] = {
        id: generatorInputFound.id,
        valueId: generatorInputFound.setupDeviceParameterValue.id,
      };
      if (generatorInputFound.setupDeviceParameterValue.value === "true") {
        setSelectedGeneratorMode("Generator Input Only");
      }
    }

    if (smartLoadOutputFound?.setupDeviceParameterValue) {
      portParams["Smart Load Output"] = {
        id: smartLoadOutputFound.id,
        valueId: smartLoadOutputFound.setupDeviceParameterValue.id,
      };
      if (smartLoadOutputFound.setupDeviceParameterValue.value === "true") {
        setSelectedGeneratorMode("Smart Load Output");
      }
    }

    if (microinverterInputFound?.setupDeviceParameterValue) {
      portParams["Microinverter Input"] = {
        id: microinverterInputFound.id,
        valueId: microinverterInputFound.setupDeviceParameterValue.id,
      };
      if (microinverterInputFound.setupDeviceParameterValue.value === "true") {
        setSelectedGeneratorMode("Microinverter Input");
      }
    }

    setPortModeParams(portParams);
    setHasPortMode(Object.values(portParams).some(param => param !== null));

    const turnOnFound = deviceParams.findParameterByName("Turn On When Battery >");
    if (turnOnFound?.setupDeviceParameterValue) {
      setTurnOnParam({
        id: turnOnFound.id,
        valueId: turnOnFound.setupDeviceParameterValue.id,
      });
      let value = parseInt(turnOnFound.setupDeviceParameterValue.value || "75", 10);
      value = Math.min(100, Math.max(75, value));
      setTurnOnValue(value);
      onX.value = ((value - 75) / 25) * sliderWidth;
    }

    const turnOffFound = deviceParams.findParameterByName("Turn OFF When Battery <");
    if (turnOffFound?.setupDeviceParameterValue) {
      setTurnOffParam({
        id: turnOffFound.id,
        valueId: turnOffFound.setupDeviceParameterValue.id,
      });
      let value = parseInt(turnOffFound.setupDeviceParameterValue.value || "25", 10);
      value = Math.min(25, Math.max(1, value));
      setTurnOffValue(value);
      offX.value = (value / 25) * sliderWidth;
    }
    
    setHasSmartLoadControl(!!turnOnFound?.setupDeviceParameterValue || !!turnOffFound?.setupDeviceParameterValue);
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  const handlePortModeSelect = (mode: GeneratorMode) => {
    setSelectedGeneratorMode(mode);
    if (mode !== "Smart Load Output") setShowSmartLoad(false);
  };

  const handleTurnOnChange = (value: number) => {
    const clampedValue = Math.min(100, Math.max(75, value));
    setTurnOnValue(clampedValue);
    onX.value = ((clampedValue - 75) / 25) * sliderWidth;
  };

  const handleTurnOffChange = (value: number) => {
    const clampedValue = Math.min(25, Math.max(1, value));
    setTurnOffValue(clampedValue);
    offX.value = (clampedValue / 25) * sliderWidth;
  }; 

  const handleSaveAndClose = async () => {
    setIsLoading(true);
    
    if (deviceParams?.setupDeviceId) {
      try {
        const selectedParam = portModeParams[selectedGeneratorMode];
        if (selectedParam) {
          await setupDeviceParameterValuesService.update(selectedParam.valueId, {
            id: selectedParam.valueId,
            value: "true",
            sortOrder: 1,
            modelNumber: "NX-200",
          });

          for (const [key, param] of Object.entries(portModeParams)) {
            if (key !== selectedGeneratorMode && param) {
              await setupDeviceParameterValuesService.update(param.valueId, {
                id: param.valueId,
                value: "false",
                sortOrder: 1,
                modelNumber: "NX-200",
              });
            }
          }
        }

        if (turnOnParam) {
          await setupDeviceParameterValuesService.update(turnOnParam.valueId, {
            id: turnOnParam.valueId,
            value: turnOnValue.toString(),
            sortOrder: 1,
            modelNumber: "NX-200",
          });
        }

        if (turnOffParam) {
          await setupDeviceParameterValuesService.update(turnOffParam.valueId, {
            id: turnOffParam.valueId,
            value: turnOffValue.toString(),
            sortOrder: 1,
            modelNumber: "NX-200",
          });
        }
      } catch (error) {
      }
    }

    await new Promise<void>((resolve) => setTimeout(resolve, 1500));
    setIsLoading(false);
    setShowGeneratorPanel(false);
  };

  const gestureHandler = (
    sharedValue: Animated.SharedValue<number>,
    setter: (v: number) => void,
    isOn: boolean
  ) =>
    useAnimatedGestureHandler({
      onStart: (_, ctx: any) => {
        ctx.startX = sharedValue.value;
      },
      onActive: (event, ctx: any) => {
        const newPos = Math.min(Math.max(0, ctx.startX + event.translationX), sliderWidth);
        sharedValue.value = newPos;

        if (isOn) {
          const newVal = 75 + Math.round((newPos / sliderWidth) * 25);
          runOnJS(setter)(newVal);
        } else {
          const newVal = Math.max(1, Math.round((newPos / sliderWidth) * 25));
          runOnJS(setter)(newVal);
        }
      },
    });

  const gestureOn = gestureHandler(onX, handleTurnOnChange, true);
  const gestureOff = gestureHandler(offX, handleTurnOffChange, false);

  const makeAnimatedBar = (
    sharedValue: Animated.SharedValue<number>,
    color1: string,
    color2: string
  ) =>
    useAnimatedStyle(() => {
      const color = interpolateColor(sharedValue.value, [0, sliderWidth], [color1, color2]);
      return {
        backgroundColor: color,
        width: withTiming(sharedValue.value, { duration: 60 }),
      };
    });

  const makeHandleStyle = (sharedValue: Animated.SharedValue<number>) =>
    useAnimatedStyle(() => ({
      transform: [
        { translateX: withTiming(sharedValue.value - handleSize / 2, { duration: 60 }) },
      ],
    }));

  const barOn = makeAnimatedBar(onX, COLORS.accent, "#ccc");
  const barOff = makeAnimatedBar(offX, "#ff9f43", "#ccc");
  const handleOn = makeHandleStyle(onX);
  const handleOff = makeHandleStyle(offX);

  if (!hasPortMode && !hasSmartLoadControl) {
    return null;
  }
const getGeneratorModeKey = (mode: GeneratorMode) => {
  switch (mode) {
    case "Generator Input Only":
      return "inputOnly";
    case "Smart Load Output":
      return "smartLoad";
    case "Microinverter Input":
      return "microInput";
    default:
      return "";
  }
};
  return (
    
    <View style={styles.mainCard}>
      <Text style={styles.cardTitle}>{t('generatorSmart.title')}</Text>

      {hasPortMode && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => setShowGeneratorPanel(!showGeneratorPanel)}
          >
            <View style={styles.dropdownRow}>
              <Text 
                style={styles.subCardTitle}
                numberOfLines={1}
                ellipsizeMode="tail"
              >
                {t('generatorSmart.portMode')}
              </Text>
             <Text style={styles.valueAccent}>
  {t(`modeSelection.generator.${getGeneratorModeKey(selectedGeneratorMode)}.title`)}
</Text>
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
              onSelectGenerator={handlePortModeSelect}
              onSave={handleSaveAndClose}
              visible={showGeneratorPanel}
            />
          )}
        </>
      )}

      {selectedGeneratorMode === "Smart Load Output" && hasSmartLoadControl && (
        <>
          <TouchableOpacity
            activeOpacity={0.8}
            onPress={() => setShowSmartLoad(!showSmartLoad)}
          >
            <View style={styles.dropdownRow}>
              <Text 
                style={styles.subCardTitle}
                numberOfLines={1}
                ellipsizeMode="tail"
              >
                {t('generatorSmart.smartLoadControl')}
              </Text>
              <FontAwesome
                name={showSmartLoad ? "chevron-up" : "chevron-down"}
                size={16}
                color={COLORS.textSoft}
              />
            </View>
          </TouchableOpacity>

          {showSmartLoad && (
            <View style={[styles.subCard, { alignItems: "center", marginTop: 8 }]}>
              <Text
                style={{
                  marginBottom: 10,
                  fontSize: 17,
                  fontWeight: "700",
                  color: COLORS.accent,
                }}
              >
                {t('generatorSmart.turnOn', { value: turnOnValue })}{turnOnValue < 100 ? ' > ' : ' '}{turnOnValue}%
              </Text>

              <View
                style={{
                  width: sliderWidth,
                  height: 20,
                  borderRadius: 8,
                  backgroundColor: isDark ? "#fff" : "#1a1a1a",
                  overflow: "hidden",
                  marginBottom: 18,
                }}
              >
                <Animated.View
                  style={[{ position: "absolute", left: 0, height: "100%", borderRadius: 8 }, barOn]}
                />
                <PanGestureHandler onGestureEvent={gestureOn}>
                  <Animated.View
                    style={[
                      {
                        position: "absolute",
                        width: handleSize,
                        height: handleSize,
                        borderRadius: handleSize / 2,
                        backgroundColor: COLORS.accent,
                        top: -4,
                      },
                      handleOn,
                    ]}
                  />
                </PanGestureHandler>
              </View>

              <Text
                style={{
                  marginBottom: 10,
                  fontSize: 17,
                  fontWeight: "700",
                  color: COLORS.accent,
                }}
              >
                {t('generatorSmart.turnOff')}{turnOffValue > 0 ? ' < ' : ' '}{turnOffValue}%
              </Text>

              <View
                style={{
                  width: sliderWidth,
                  height: 20,
                  borderRadius: 8,
                  backgroundColor: isDark ? "#fff" : "#1a1a1a",
                  overflow: "hidden",
                }}
              >
                <Animated.View
                  style={[{ position: "absolute", left: 0, height: "100%", borderRadius: 8 }, barOff]}
                />
                <PanGestureHandler onGestureEvent={gestureOff}>
                  <Animated.View
                    style={[
                      {
                        position: "absolute",
                        width: handleSize,
                        height: handleSize,
                        borderRadius: handleSize / 2,
                        backgroundColor: COLORS.accent,
                        top: -4,
                      },
                      handleOff,
                    ]}
                  />
                </PanGestureHandler>
              </View>
            </View>
          )}
        </>
      )}
    </View>
  );
}
