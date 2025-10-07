import React from 'react';
import { View, Text } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';

type Props = {
  homes: { id: string; name: string; address: string; hasDevice: boolean }[];
  styles: any;
  COLORS: any;
};

export function HomeStats({ homes, styles, COLORS }: Props) {
  return (
    <View style={styles.statsSection}>
      <View style={styles.statCard}>
        <MaterialIcons name="home" size={24} color={COLORS.accent} />
        <Text style={styles.statNumber}>{homes.length}</Text>
        <Text style={styles.statLabel}>Homes</Text>
      </View>
      <View style={styles.statCard}>
        <MaterialIcons name="device-hub" size={24} color={COLORS.success} />
        <Text style={styles.statNumber}>2</Text>
        <Text style={styles.statLabel}>Devices</Text>
      </View>
      <View style={styles.statCard}>
        <MaterialIcons name="flash-on" size={24} color={COLORS.warning} />
        <Text style={styles.statNumber}>2.4kW</Text>
        <Text style={styles.statLabel}>Total Power</Text>
      </View>
    </View>
  );
}
