import React from 'react';
import { View } from 'react-native';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { Badge } from '../../../../../components';
import { getDashboardStatusRowStyles } from './DashboardStatusRow.styles';
import type { DashboardStatusRowProps } from './DashboardStatusRow.types';
import { useTranslation } from 'react-i18next';

export default function DashboardStatusRow({ COLORS }: DashboardStatusRowProps) {
  const styles = getDashboardStatusRowStyles(COLORS);
  const { t } = useTranslation();

  return (
    <Animated.View entering={FadeInUp.delay(100).duration(600)} style={styles.row}>
      <View style={styles.badgeContainer}>
        <Badge text={t('dashboardStatus.online')} variant="success" />
      </View>
      <View style={styles.badgeContainer}>
        <Badge text={t('dashboardStatus.normal')} variant="info" />
      </View>
      <View style={styles.badgeContainer}>
        <Badge text={t('dashboardStatus.battery', { value: 0 })} variant="warning" />
      </View>
      <View style={styles.badgeContainer}>
        <Badge text={t('dashboardStatus.grid')} variant="success" />
      </View>
    </Animated.View>
  );
}
