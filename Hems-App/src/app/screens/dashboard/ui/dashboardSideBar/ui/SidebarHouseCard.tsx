import React from "react";
import { TouchableOpacity, View, Text, Image } from "react-native";
import Card from "../../../../../../components/ui/Card/Card";

type Props = {
  house: { id: string; name: string; address: string; isEmpty?: boolean };
  isDark: boolean;
  selected: boolean;
  onSelect: () => void;
  styles: any;
};

export function SidebarHouseCard({ house, isDark, selected, onSelect, styles }: Props) {
  return (
    <TouchableOpacity activeOpacity={0.9} onPress={onSelect}>
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
            <Text style={styles.noDeviceBadgeText}>no device</Text>
          </View>
        )}
      </Card>
    </TouchableOpacity>
  );
}
