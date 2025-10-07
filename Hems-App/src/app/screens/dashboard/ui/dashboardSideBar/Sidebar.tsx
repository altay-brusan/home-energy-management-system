import React, { useState, forwardRef, useImperativeHandle, useEffect, useRef } from "react";
import {
  Text,
  TouchableOpacity,
  Animated,
  Dimensions,
  ScrollView,
  ActivityIndicator,
  View,
} from "react-native";
import MaterialIcons from '@react-native-vector-icons/material-icons';

import { useNavigation } from "@react-navigation/native";
import { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useThemeStore } from "../../../../../store/themeStore";
import { getThemeColors } from "../../../../../theme/constants";
import { makeSidebarStyles } from "./Sidebar.styles";
import { SidebarHeader, SidebarHouseCard, SidebarAddButton } from "./ui";

const { width } = Dimensions.get("window");

type House = { id: string; name: string; address: string; isEmpty?: boolean };

type RootStackParamList = {
  DeviceSetup: undefined;
  MainTabs: undefined;
};

const Sidebar = forwardRef((props, ref) => {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();

  const houses: House[] = [
    { id: "1", name: "Istanbul Apartment", address: "Ankara / Turkey" },
    { id: "2", name: "Summer House", address: "Istanbul / Turkey" },
    { id: "empty", name: "Empty House", address: "Tap to add devices", isEmpty: true },
  ];
  const [isOpen, setIsOpen] = useState(false);
  const [selectedHouse, setSelectedHouse] = useState("1");
  const [isLoading, setIsLoading] = useState(false);
  const translateX = useState(new Animated.Value(-width))[0];
  const buttonScale = useState(new Animated.Value(1))[0];
  const prevSelectedHouse = useRef(selectedHouse);

  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === "dark";
  const COLORS = getThemeColors(isDark);
  const styles = makeSidebarStyles(isDark, COLORS);

  const toggleSidebar = () => {
    if (isOpen) {
      Animated.timing(translateX, {
        toValue: -width,
        duration: 300,
        useNativeDriver: true,
      }).start(() => setIsOpen(false));
    } else {
      setIsOpen(true);
      Animated.timing(translateX, {
        toValue: 0,
        duration: 300,
        useNativeDriver: true,
      }).start();
    }
  };

  useImperativeHandle(ref, () => ({ toggleSidebar }));


  useEffect(() => {
    if (prevSelectedHouse.current !== selectedHouse) {
      setIsLoading(true);
      toggleSidebar();

      setTimeout(() => {
        setIsLoading(false);
      }, 2000);

      prevSelectedHouse.current = selectedHouse;
    }
  }, [selectedHouse]);

  return (
    <>


      {isLoading && (
        <View style={styles.loadingOverlay}>
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color={COLORS.accent} />
            <Text style={styles.loadingText}>Switching home...</Text>
          </View>
        </View>
      )}
      <TouchableOpacity
        onPress={toggleSidebar}
        style={styles.centerToggleButton}
        activeOpacity={0.8}
      >
        <MaterialIcons
          name={isOpen ? "close" : "home"}
          size={24}
          color={isDark ? COLORS.accent : '#3B82F6'}
        />
      </TouchableOpacity>
      {isOpen && (
        <TouchableOpacity
          style={styles.overlay}
          onPress={toggleSidebar}
          activeOpacity={1}
        >
          <Animated.View
            style={[styles.sidebar, { transform: [{ translateX }] }]}
          >
            <ScrollView
              contentContainerStyle={styles.scrollContent}
              showsVerticalScrollIndicator={false}
              bounces={false}
            >
              <SidebarHeader styles={styles} />

              <SidebarAddButton styles={styles} />

              {houses.map((house) => (
                <SidebarHouseCard
                  key={house.id}
                  house={house}
                  isDark={isDark}
                  selected={selectedHouse === house.id}
                  onSelect={() => {
                    if (house.isEmpty) {
                      navigation.navigate('DeviceSetup');
                      toggleSidebar();
                    } else {
                      setSelectedHouse(house.id);
                    }
                  }}
                  styles={styles}
                />
              ))}
            </ScrollView>
          </Animated.View>
        </TouchableOpacity>
      )}
    </>
  );
});

export default Sidebar;
