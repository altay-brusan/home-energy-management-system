import React from 'react';
import { Text } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

export function EmptyState({ styles }: { styles: Styles }) {
  return (
    <Animated.View entering={FadeInDown.duration(600)} style={styles.emptyWrap}>
      <Text style={styles.emptyIcon}>📭</Text>
      <Text style={styles.emptyText}>No deleted notifications</Text>
    </Animated.View>
  );
}

