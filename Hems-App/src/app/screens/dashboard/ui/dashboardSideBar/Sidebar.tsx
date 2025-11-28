import React, { useState, forwardRef, useImperativeHandle, useEffect } from "react";
import {
  TouchableOpacity,
  Animated,
  Dimensions,
  ScrollView,
} from "react-native";
import { useNavigation } from "@react-navigation/native";
import { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useThemeStore } from "../../../../../store/themeStore";
import { getThemeColors } from "../../../../../theme/constants";
import { makeSidebarStyles } from "./Sidebar.styles";
import { SidebarHeader, SidebarHouseCard, SidebarAddButton } from "./ui";
import { useTranslation } from "react-i18next";
import { useSitesStore } from "../../../../../store/sitesStore";

const { width } = Dimensions.get("window");

type House = { id: string; name: string; address: string; isEmpty?: boolean };

type RootStackParamList = {
  DeviceSetup: undefined;
  MainTabs: undefined;
};

const Sidebar = forwardRef((props, ref) => {
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { t } = useTranslation();

  const { sites, selectedSite, getUserSites, selectSiteById } = useSitesStore();

  const [isOpen, setIsOpen] = useState(false);
  const [selectedHouse, setSelectedHouse] = useState("");
  const [houses, setHouses] = useState<House[]>([]);
  const translateX = useState(new Animated.Value(-width))[0];

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
    const loadSites = async () => {
      try {
        await getUserSites();
      } catch (error) {}
    };
    loadSites();
  }, [getUserSites]);

  useEffect(() => {
    if (selectedSite) {
      setSelectedHouse(selectedSite.id);
    }
  }, [selectedSite]);

  useEffect(() => {
    const housesFromSites = sites.map(site => ({
      id: site.id,
      name: site.name,
      address: `${site.city || ''}, ${site.country || ''}`.replace(/^,\s*|,\s*$/g, ''),
      isEmpty: !site.hasDevice
    }));

    setHouses(housesFromSites);
  }, [sites]);
    
  return (
    <>
      {isOpen && (
        <>
          <TouchableOpacity
            style={styles.overlay}
            onPress={toggleSidebar}
            activeOpacity={1}
            hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
          />
          <Animated.View
            style={[styles.sidebar, { transform: [{ translateX }] }]}
          >
            <ScrollView
              contentContainerStyle={styles.scrollContent}
              showsVerticalScrollIndicator={false}
              bounces={false}
            >
              <SidebarHeader styles={styles} />

              {houses.map((house) => (
                <SidebarHouseCard
                  key={house.id}
                  house={house}
                  isDark={isDark}
                  selected={selectedHouse === house.id}
                  onSelect={async () => {
                    await selectSiteById(house.id);
                    setSelectedHouse(house.id);
                  }}
                  styles={styles}
                />
              ))}

              <SidebarAddButton styles={styles} navigation={navigation} />
            </ScrollView>
          </Animated.View>
        </>
      )}
    </>
  );
});

export default Sidebar;
