import React from 'react';
import { View, Text } from 'react-native';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

export function StatusPill({ styles }: { styles: Styles }) {
  return (
    <View style={styles.statusPill}>
      <Text style={styles.statusPillText}>Deleted</Text>
    </View>
  );
}

export default StatusPill;
