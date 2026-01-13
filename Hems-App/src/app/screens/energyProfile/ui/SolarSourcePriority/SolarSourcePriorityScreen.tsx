import React, { useState, useEffect } from "react";
import {
  View,
  Text,
  TouchableOpacity,
  ScrollView,
  LayoutAnimation,
  ActivityIndicator,
} from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation } from "@react-navigation/native";
import Animated, {
  withSpring,
  useAnimatedStyle,
  useSharedValue,
} from "react-native-reanimated";
import { GestureHandlerRootView } from "react-native-gesture-handler";
import { getSolarPriorityStyles } from "./SolarSourcePriority.styles";
import { useThemeStore } from "../../../../../store/themeStore";
import { getThemeColors } from "../../../../../theme/constants";
import MainLayout from "../../../../layouts/MainLayout";
import { useI18n } from "../../../../../hooks/useI18n";
import { useDeviceParameters } from "../../../../../hooks/useDeviceParameters";
import { setupDeviceParameterValuesService } from "../../../../../api/services/setupDeviceParameterValuesService";

export default function SolarSourcePriorityScreen() {
  const navigation = useNavigation<any>();
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = getSolarPriorityStyles(COLORS, isDark);
  const deviceParams = useDeviceParameters();
  const { findParameterByName, setupDeviceId } = deviceParams;


  const defaultItems = [
    { id: 1, title: "Grid", icon: "industry", locked: true },
    { id: 2, title: "Smart Load", icon: "bolt", locked: true },
    { id: 3, title: "Load", icon: "plug", locked: true },
    { id: 4, title: "Battery", icon: "battery-half", locked: true },
  ];

  const [items, setItems] = useState(defaultItems);
  const [expanded, setExpanded] = useState(false);
  const [selected, setSelected] = useState<number | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [parameterId, setParameterId] = useState<string | null>(null);
  const [setupDeviceParameterValueId, setSetupDeviceParameterValueId] = useState<string | null>(null);

  const positions = items.map((_, i) => useSharedValue(i));

  const swapItems = (i1: number, i2: number) => {
    const newItems = [...items];
    const temp = newItems[i1];
    newItems[i1] = newItems[i2];
    newItems[i2] = temp;
    setItems(newItems);
    const p1 = positions[i1].value;
    positions[i1].value = withSpring(positions[i2].value);
    positions[i2].value = withSpring(p1);
  };

  const handleSelect = (index: number) => {
    const item = items[index];
    if (item.locked) return;

    if (selected === null) {
      setSelected(index);
    } else if (selected === index) {
      setSelected(null);
    } else {
      if (!items[selected].locked && !items[index].locked)
        swapItems(selected, index);
      setSelected(null);
    }
  };

  const toggleLock = (index: number) => {
    const updated = [...items];
    updated[index].locked = !updated[index].locked;
    setItems(updated);
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      if (setupDeviceId) {
        let valueIdToUpdate = setupDeviceParameterValueId;
        
        if (!valueIdToUpdate || !parameterId) {
          const solarPriorityParam = findParameterByName("Solar Producing Source Priority");
          if (solarPriorityParam?.setupDeviceParameterValue) {
            setParameterId(solarPriorityParam.id);
            setSetupDeviceParameterValueId(solarPriorityParam.setupDeviceParameterValue.id);
            valueIdToUpdate = solarPriorityParam.setupDeviceParameterValue.id;
          }
        }

        if (valueIdToUpdate) {
          const priorityString = items.map((item) => item.title).join(",");
          await setupDeviceParameterValuesService.update(valueIdToUpdate, {
            id: valueIdToUpdate,
            value: priorityString,
            sortOrder: 1,
            modelNumber: "NX-200",
          });
        }
      }
    } catch (error) {
    } finally {
      setTimeout(() => setIsSaving(false), 1000);
    }
  };

  useEffect(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) {
      setItems(defaultItems);
      return;
    }

    const solarPriorityParam = findParameterByName("Solar Producing Source Priority");
    
    if (solarPriorityParam?.setupDeviceParameterValue) {
      setParameterId(solarPriorityParam.id);
      setSetupDeviceParameterValueId(solarPriorityParam.setupDeviceParameterValue.id);
      
      const priorityString = solarPriorityParam.setupDeviceParameterValue.value;
      const priorityArray = priorityString.split(",").map((p) => p.trim());
      
      const mappedItems = priorityArray.map((title, index) => {
        const defaultItem = defaultItems.find((item) => item.title === title);
        return defaultItem 
          ? { ...defaultItem, id: index + 1, locked: true }
          : { id: index + 1, title, icon: "circle", locked: true };
      });
      
      const finalItems = mappedItems.map((item) => ({
        ...item,
        locked: true,
      }));
      
      setItems(finalItems.length > 0 ? finalItems : defaultItems);
    } else {
      setItems(defaultItems);
    }
  }, [deviceParams?.data, deviceParams?.findParameterByName]);

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <MainLayout>
        <View style={styles.headerRow}>
          <TouchableOpacity
            onPress={() => navigation.goBack()}
            style={styles.backButton}
          >
            <FontAwesome name="chevron-left" size={22} color={COLORS.textSoft} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>{t('solarPriority.title')}</Text>

          <TouchableOpacity
            onPress={handleSave}
            disabled={isSaving}
            style={styles.saveButton}
          >
            {isSaving ? (
              <ActivityIndicator size="small" color={COLORS.accent} />
            ) : (
              <Text style={[styles.saveText, { color: COLORS.accent }]}>
                {t('solarPriority.save')}
              </Text>
            )}
          </TouchableOpacity>
        </View>

        <TouchableOpacity
          activeOpacity={0.8}
          onPress={() => {
            LayoutAnimation.configureNext(
              LayoutAnimation.Presets.easeInEaseOut
            );
            setExpanded(!expanded);
          }}
          style={styles.explanationCard}
        >
          <Text style={styles.explanationTitle}>{t('solarPriority.explanationTitle')}</Text>
          <FontAwesome
            name={expanded ? "chevron-up" : "chevron-down"}
            size={16}
            color={COLORS.textSoft}
          />
        </TouchableOpacity>

        {expanded && (
          <View style={styles.explanationContent}>
            <Text style={styles.explanationText}>
              {t('solarPriority.explanationText')}
            </Text>
            <Text style={styles.learnMore}>{t('solarPriority.learnMore')}</Text>
          </View>
        )}

        <Text style={styles.instructionText}>
          {t('solarPriority.instructions')}
        </Text>

        <ScrollView
          showsVerticalScrollIndicator={false}
          style={{ flex: 1, paddingHorizontal: 16, paddingTop: 4 }}
          contentContainerStyle={{ paddingBottom: 100 }}
        >
          {items.map((item, index) => {
            const animatedStyle = useAnimatedStyle(() => ({
              transform: [{ translateY: withSpring(positions[index].value * 0) }],
            }));

            const isSelected = selected === index;

            return (
              <Animated.View
                key={item.id}
                style={[styles.priorityCard, animatedStyle]}
              >
                <TouchableOpacity
                  activeOpacity={item.locked ? 1 : 0.8}
                  onPress={() => handleSelect(index)}
                  style={[
                    styles.innerCardLarge,
                    {
                      borderWidth: isSelected ? 2 : 1,
                      borderColor: isSelected
                        ? COLORS.accent
                        : isDark
                        ? COLORS.border
                        : COLORS.gray200,
                      backgroundColor: isDark
                        ? COLORS.cardBg
                        : COLORS.white,
                      opacity: item.locked ? 0.85 : 1,
                    },
                  ]}
                >
                  <View style={styles.numberCircleLarge}>
                    <Text style={styles.numberText}>{index + 1}</Text>
                  </View>

                  <View style={styles.iconTitleRow}>
                    <FontAwesome
                      name={item.icon as any}
                      size={22}
                      color={COLORS.textSoft}
                      style={{ marginRight: 10 }}
                    />
                    <Text style={styles.cardLabelLarge}>{item.title}</Text>
                  </View>

                  <TouchableOpacity
                    onPress={() => toggleLock(index)}
                    activeOpacity={0.7}
                    style={styles.iconRightLarge}
                  >
                    <FontAwesome
                      name={item.locked ? "lock" : "unlock"}
                      size={18}
                      color={
                        item.locked ? COLORS.textSoft : COLORS.accent
                      }
                    />
                  </TouchableOpacity>
                </TouchableOpacity>
              </Animated.View>
            );
          })}
        </ScrollView>
      </MainLayout>
    </GestureHandlerRootView>
  );
}
