import React, { useEffect, useState } from 'react';
import { View, Text, TouchableOpacity, ActivityIndicator } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { getDashboardModeCardStyles } from './DashboardModeCard.styles';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

type DashboardMode = {
  mode: string;
  description: string;
};

type RootStackParamList = {
  EnergyProfile: undefined;
};

type DashboardModeCardProps = {
  COLORS: Record<string, string>;
};

export default function DashboardModeCard({ COLORS }: DashboardModeCardProps) {
  const styles = getDashboardModeCardStyles(COLORS);
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();

  const [data, setData] = useState<DashboardMode | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const timer = setTimeout(() => {
      setData({
        mode: "Self-Consumption",
        description: "Store solar energy during the day and use it at night to reduce grid reliance."
      });
      setLoading(false);
    }, 800);

    return () => clearTimeout(timer);
  }, []);

  if (loading) {
    return (
      <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.widget}>
        <ActivityIndicator color={COLORS.accent} />
      </Animated.View>
    );
  }

  if (!data) return null;

  return (
    <Animated.View
      entering={FadeInDown.delay(200).duration(600)}
      style={styles.widget}
    >
      <TouchableOpacity
        style={{ flex: 1 }}
        activeOpacity={0.8}
        onPress={() => navigation.navigate("EnergyProfile")}
      >
        <View style={styles.headerRow}>
          <Text style={styles.title}>Current Mode</Text>
          <MaterialIcons name="settings" size={20} color={COLORS.textSoft} />
        </View>

        <View style={styles.modeRow}>
          <Text style={styles.mode}>{data.mode}</Text>
          <MaterialIcons name="check" size={20} color={COLORS.accent} style={styles.check} />
        </View>
        <Text style={styles.desc}>{data.description}</Text>
      </TouchableOpacity>
    </Animated.View>
  );
}
