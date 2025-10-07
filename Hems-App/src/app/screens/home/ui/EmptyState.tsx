import React from 'react';
import { View, Text } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';

type Props = {
  styles: any;
  COLORS: any;
};

export function EmptyState({ styles, COLORS }: Props) {
  return (
    <View style={styles.emptyState}>
      <MaterialIcons name="home" size={64} color={COLORS.slate400} />
      <Text style={styles.emptyTitle}>No homes yet</Text>
      <Text style={styles.emptySubtitle}>
        Add your first home and device to start managing energy
      </Text>
    </View>
  );
}
