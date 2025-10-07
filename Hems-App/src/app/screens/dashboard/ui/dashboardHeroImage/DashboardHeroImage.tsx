import React from 'react';
import { Image, View, Text } from 'react-native';
import { getDashboardHeroImageStyles } from './DashboardHeroImage.styles';

type Props = {
  COLORS: Record<string, string>;
};

export default function DashboardHeroImage({ COLORS }: Props) {
  const styles = getDashboardHeroImageStyles(COLORS);
  return (
    <View style={styles.root}>
      <Image
        source={require('../../../../../assets/images/energy_home.bg.png')}
        style={styles.image}
        resizeMode="contain"
      />
      <View style={styles.content}>
        <Text style={styles.title}>Home Energy Status</Text>
        <Text style={styles.subtitle}>Today's consumption and real-time power</Text>
        <View style={styles.metaRow}>
          <Text style={styles.metaItem}>Today: 24.1 kWh</Text>
          <Text style={styles.metaItem}>Current: 320 W</Text>
          <Text style={styles.metaItem}>Last sync: 2 min ago</Text>
        </View>
      </View>
    </View>
  );
}
