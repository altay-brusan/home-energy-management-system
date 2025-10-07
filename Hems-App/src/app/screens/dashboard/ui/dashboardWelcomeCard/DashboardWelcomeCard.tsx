import React from 'react';
import { Text, View, useWindowDimensions } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { Badge } from '../../../../../components';
import { getDashboardWelcomeCardStyles } from './DashboardWelcomeCard.styles';
import type { DashboardWelcomeCardProps } from './DashboardWelcomeCard.types';

export default function DashboardWelcomeCard({ isDark, COLORS }: DashboardWelcomeCardProps) {
  const { width } = useWindowDimensions();
  const styles = getDashboardWelcomeCardStyles(COLORS, isDark, width);
  return (
    <Animated.View entering={FadeInDown.duration(600)} style={styles.root}>
      <Animated.Image
        entering={FadeInDown.delay(200).duration(700)}
        source={{
          uri: 'https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=600&q=80',
        }}
        style={styles.image}
        resizeMode="cover"
      />
      <View style={styles.content}>
        <Text style={styles.title} numberOfLines={2} ellipsizeMode="tail">
          Welcome Home!
        </Text>
        <Text style={styles.subtitle} numberOfLines={2} ellipsizeMode="tail">
          Your smart energy dashboard
        </Text>
        <View style={styles.statusRow}>
          <Badge text="Active" variant="success" />
          <Text style={styles.lastSync}>Last sync: 2 min ago</Text>
        </View>
        <View style={styles.metricsRow}>
          <View style={styles.metricPill}>
            <Text style={styles.metricLabel}>Today</Text>
            <Text style={styles.metricValue}>24.1 kWh</Text>
          </View>
          <View style={styles.metricPill}>
            <Text style={styles.metricLabel}>Savings</Text>
            <Text style={styles.metricValue}>12%</Text>
          </View>
        </View>
      </View>
    </Animated.View>
  );
}
