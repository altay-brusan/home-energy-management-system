import React from "react";
import { View, Text } from "react-native";
import MaterialIcons from "@react-native-vector-icons/material-icons";
import { useI18n } from "../../../../hooks/useI18n";

interface AdminStatsProps {
  homes: any[];
  totalCount: number;
  totalDeviceCount: number;
  COLORS: any;
  styles: any;
}

export default function AdminStats({ homes, totalCount, totalDeviceCount, COLORS, styles }: AdminStatsProps) {
  const { t } = useI18n();

  return (
    <View style={styles.bottomRow}>
      <View style={styles.statCard}>
        <MaterialIcons name="home" size={22} color={COLORS.accent} />
        <Text style={styles.statLabel}>{t("adminStats.homes")}</Text>
        <Text style={styles.statValue}>{totalCount}</Text>
      </View>

      <View style={styles.statCard}>
        <MaterialIcons name="device-hub" size={22} color={COLORS.success} />
        <Text style={styles.statLabel}>{t("adminStats.devices")}</Text>
        <Text style={styles.statValue}>{totalDeviceCount}</Text>
      </View>
    </View>
  );
}
