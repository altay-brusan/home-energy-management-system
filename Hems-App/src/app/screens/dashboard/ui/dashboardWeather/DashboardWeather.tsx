import React from 'react';
import { View, Text } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { getDashboardWeatherStyles } from './DashboardWeather.styles';
import type { DashboardWeatherProps } from './DashboardWeather.types';

export default function DashboardWeather({ COLORS, isDark }: DashboardWeatherProps) {
  const styles = getDashboardWeatherStyles(COLORS);
  return (
    <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.widget}>
      <Text style={styles.title}>Weather</Text>
      <View style={styles.row}>
        <Text style={styles.icon}>☀️</Text>
        <View>
          <Text style={styles.temp}>27°C</Text>
          <Text style={styles.desc}>Sunny</Text>
          <Text style={styles.meta}>Sunrise: 06:12 • Sunset: 20:45</Text>
        </View>
      </View>
    </Animated.View>
  );
}
