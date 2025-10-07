import React, { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Swipeable } from 'react-native-gesture-handler';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';
import { TrashNotification } from '../TrashScreen';
import StatusPill from './StatusPill';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

type Props = {
  n: TrashNotification;
  styles: Styles;
  color: string;
  onRestore: (id: string) => void;
  onDelete: (id: string) => void;
  onOpen: (n: TrashNotification) => void;
};

export function TrashItem({ n, styles, color, onRestore, onDelete, onOpen }: Props) {
  const [showActions, setShowActions] = useState(true);

  return (
    <Swipeable
      renderLeftActions={showActions ? () => (
        <Pressable onPress={() => onRestore(n.id)} style={styles.swipeLeft}>
          <Text style={styles.swipeLeftText}>Restore</Text>
        </Pressable>
      ) : undefined}
      renderRightActions={showActions ? () => (
        <Pressable onPress={() => onDelete(n.id)} style={styles.swipeRight}>
          <Text style={styles.swipeRightText}>Delete</Text>
        </Pressable>
      ) : undefined}
      onSwipeableWillClose={() => setShowActions(false)}
      onSwipeableWillOpen={() => setShowActions(true)}
      overshootLeft={false}
      overshootRight={false}
    >
      <Pressable onPress={() => onOpen(n)} style={styles.itemPress}>
        <View style={[styles.itemRow, { borderLeftColor: color }]}>
          <View style={styles.itemContent}>
            <View style={styles.itemHeader}>
              <Text style={styles.itemTitle}>{n.title}</Text>
              <StatusPill styles={styles} />
            </View>
            <Text style={styles.itemMsg} numberOfLines={2}>{n.message}</Text>
            <View style={styles.itemMetaRow}>
              <View style={styles.metaLeft}>
                {n.severity === 'critical' && <Text style={styles.criticalText}>Critical</Text>}
              </View>
              <Text style={styles.itemMeta}>Deleted at {n.deletedAt}</Text>
            </View>
          </View>
        </View>
      </Pressable>
    </Swipeable>
  );
}

export default TrashItem;
