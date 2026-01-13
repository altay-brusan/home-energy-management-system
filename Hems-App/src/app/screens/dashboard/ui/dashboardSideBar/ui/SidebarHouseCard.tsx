import React from "react";
import { TouchableOpacity, View, Text, Image, StyleSheet } from "react-native";
import { useNavigation } from "@react-navigation/native";
import { NativeStackNavigationProp } from "@react-navigation/native-stack";
import { useTranslation } from "react-i18next";

const houseImage = require("../../../../../../assets/images/energy_home.png");

type RootStackParamList = {
  DeviceAdd: { siteId: string };
  MainTabs: undefined;
};

type Props = {
  house: { id: string; name: string; address: string; isEmpty?: boolean };
  isDark?: boolean;
  selected: boolean;
  onSelect: () => void;
  styles: any;
};

export function SidebarHouseCard({ house, selected, onSelect, styles }: Props) {
  const { t } = useTranslation();
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();

  const handlePress = () => {
    if (house.isEmpty) {
      // Navigate to device add screen for sites without devices
      navigation.navigate('DeviceAdd', { siteId: house.id });
    } else {
      // Normal site selection for sites with devices
      onSelect();
    }
  };

  return (
    <TouchableOpacity activeOpacity={0.85} onPress={handlePress}>
      <View style={[styles.houseCard, selected && styles.houseCardActive]}>
        <View style={styles.houseCardContent}>
          <View style={styles.radioOuter}>
            {selected && <View style={styles.radioInner} />}
          </View>
          <View style={{ flex: 1, marginLeft: 12 }}>
            <Text style={styles.badgeText}>{house.name}</Text>
            {house.address && (
              <Text style={styles.addressText}>{house.address}</Text>
            )}
          </View>
          {house.isEmpty && (
            <View style={styles.noDeviceBadge}>
              <Text style={styles.noDeviceBadgeText}>{t("dashboardSidebar.noDevice")}</Text>
            </View>
          )}
          <Image
            source={houseImage}
            style={localStyles.houseImage}
            resizeMode="contain"
          />
        </View>
      </View>
    </TouchableOpacity>
  );
}

const localStyles = StyleSheet.create({
  houseImage: {
    width: 40,
    height: 40,
    marginLeft: 8,
    opacity: 0.85,
  },
});
