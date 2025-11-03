import React, { useState } from "react";
import {
  View,
  Text,
  TouchableOpacity,
  FlatList,
  Pressable,
  Animated,
  Dimensions,
} from "react-native";
import MaterialIcons from "@react-native-vector-icons/material-icons";

const SCREEN_HEIGHT = Dimensions.get("window").height;

export default function BottomSheetSelect({
  label,
  options,
  selected,
  onSelect,
  COLORS,
  styles,
}: any) {
  const [visible, setVisible] = useState(false);
  const [anim] = useState(new Animated.Value(0));

  const openSheet = () => {
    setVisible(true);
    Animated.timing(anim, {
      toValue: 1,
      duration: 250,
      useNativeDriver: true,
    }).start();
  };

  const closeSheet = () => {
    Animated.timing(anim, {
      toValue: 0,
      duration: 200,
      useNativeDriver: true,
    }).start(() => setVisible(false));
  };

  const translateY = anim.interpolate({
    inputRange: [0, 1],
    outputRange: [SCREEN_HEIGHT, 0],
  });

  return (
    <>
      {/* 🔹 Görsel olarak kutu */}
      <TouchableOpacity
        activeOpacity={0.8}
        onPress={openSheet}
        style={styles.selectorBox}
      >
        <Text style={styles.selectorText}>{selected || label}</Text>
        <MaterialIcons
          name="keyboard-arrow-down"
          size={22}
          color={COLORS.textSoft}
        />
      </TouchableOpacity>

      {/* 🔹 Bottom Sheet */}
      {visible && (
        <Pressable style={styles.sheetOverlay} onPress={closeSheet}>
          <Animated.View
            style={[styles.sheetContainer, { transform: [{ translateY }] }]}
          >
            <View style={styles.sheetHeader}>
              <Text style={styles.sheetTitle}>{label}</Text>
              <MaterialIcons
                name="close"
                size={22}
                color={COLORS.textSoft}
                onPress={closeSheet}
              />
            </View>

            <FlatList
              data={options}
              keyExtractor={(item) => item}
              renderItem={({ item }) => (
                <TouchableOpacity
                  onPress={() => {
                    onSelect(item);
                    closeSheet();
                  }}
                  style={styles.sheetItem}
                >
                  <Text style={styles.sheetItemText}>{item}</Text>
                </TouchableOpacity>
              )}
            />
          </Animated.View>
        </Pressable>
      )}
    </>
  );
}
