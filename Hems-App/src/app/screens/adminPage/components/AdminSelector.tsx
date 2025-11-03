import React from "react";
import { View, TouchableOpacity, Text } from "react-native";
import MaterialIcons from "@react-native-vector-icons/material-icons";
import { CountrySelector, CitySelector } from "../../../../components";

export default function AdminSelector({
  type,
  label,
  selected,
  onSelect,
  COLORS,
  styles,
}: any) {
  if (type === "country") {
    return (
      <View style={styles.selectorBox}>
        <CountrySelector
          selectedCountry={selected}
          onCountrySelect={onSelect}
          label=""
          placeholder={label}
        />
      </View>
    );
  }

  if (type === "city") {
    return (
      <View style={styles.selectorBox}>
        <CitySelector
          selectedCity={selected}
          onCitySelect={onSelect}
          label=""
          placeholder={label}
        />
      </View>
    );
  }

  return (
    <TouchableOpacity
      activeOpacity={0.8}
      onPress={() => {}}
      style={styles.selectorBox}
    >
      <Text style={styles.selectorText}>{selected || label}</Text>
      <MaterialIcons
        name="keyboard-arrow-down"
        size={22}
        color={COLORS.textSoft}
      />
    </TouchableOpacity>
  );
}
