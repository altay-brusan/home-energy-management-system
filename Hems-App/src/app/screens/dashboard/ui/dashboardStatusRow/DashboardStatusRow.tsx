import React from 'react';
import { View } from 'react-native';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { Badge } from '../../../../../components';
import { getDashboardStatusRowStyles } from './DashboardStatusRow.styles';
import type { DashboardStatusRowProps } from './DashboardStatusRow.types';

export default function DashboardStatusRow({
  COLORS,
}: DashboardStatusRowProps) {
  const styles = getDashboardStatusRowStyles(COLORS);
  return (
    <Animated.View
      entering={FadeInUp.delay(100).duration(600)}
      style={styles.row}
    >
      <View style={styles.badgeContainer}>
        <Badge text="Online" variant="success" />
      </View>
      <View style={styles.badgeContainer}>
        <Badge text="Normal" variant="info" />
      </View>
      <View style={styles.badgeContainer}>
        <Badge text="Battery: 82%" variant="warning" />
      </View>
      <View style={styles.badgeContainer}>
        <Badge text="Grid: Connected" variant="success" />
      </View>
    </Animated.View>
  );
}
