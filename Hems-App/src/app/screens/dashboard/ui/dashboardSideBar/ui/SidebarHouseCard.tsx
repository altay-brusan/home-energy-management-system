import React from "react";
import { TouchableOpacity, View, Text, Image } from "react-native";
import { useNavigation } from "@react-navigation/native";
import { NativeStackNavigationProp } from "@react-navigation/native-stack";
import Card from "../../../../../../components/ui/Card/Card";
import { useTranslation } from "react-i18next";

type RootStackParamList = {
  DeviceAdd: { siteId: string };
  MainTabs: undefined;
};

type Props = {
  house: { id: string; name: string; address: string; isEmpty?: boolean };
  isDark: boolean;
  selected: boolean;
  onSelect: () => void;
  styles: any;
};

export function SidebarHouseCard({ house, isDark, selected, onSelect, styles }: Props) {
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
    <TouchableOpacity activeOpacity={0.9} onPress={handlePress}>
      <Card
        style={[styles.houseCard, selected && styles.houseCardActive]}
        variant="elevated"
      >
        <Image
          source={
            isDark
              ? require("../../../../../../assets/images/energy_home.bg.png")
              : require("../../../../../../assets/images/energy_home.png")
          }
          style={styles.houseImageFull}
          resizeMode="contain"
        />

        <View style={styles.badgeContainer}>
          <View style={styles.radioOuter}>
            {selected && <View style={styles.radioInner} />}
          </View>
          <Text style={styles.badgeText}>{house.name}</Text>
        </View>

        {house.isEmpty && (
          <View style={styles.noDeviceBadge}>
               <Text style={styles.noDeviceBadgeText}>{t("dashboardSidebar.noDevice")}</Text>
          </View>
        )}
      </Card>
    </TouchableOpacity>
  );
}
