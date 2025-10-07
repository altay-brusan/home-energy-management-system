import React, { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Swipeable } from 'react-native-gesture-handler';
import type { Notification } from '../../../../hooks/useNotifications';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import StatusPill from './StatusPill';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

type Props = {
  n: Notification;
  styles: Styles;
  isDark: boolean;
  accent?: string;
  onOpen: (n: Notification) => void;
  onDelete: (id: string) => void;
  onMarkRead: (id: string) => void;
};

export function NotificationItem({ n, styles, isDark, accent, onOpen, onDelete, onMarkRead }: Props) {
  const [showActions, setShowActions] = useState(true);
  const color = n.severity === 'critical' ? '#EB5757' : (accent ?? '#1DD1A1');
  const metaText = new Date(n.createdAt).toLocaleString();

  return (
    <Swipeable
      renderLeftActions={showActions ? () => (
        <Pressable onPress={() => onMarkRead(n.id)} style={styles.swipeLeft}>
          <Text style={styles.swipeLeftText}>Read</Text>
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
              <StatusPill read={n.read} styles={styles} isDark={isDark} accent={accent} />
            </View>
            <Text style={styles.itemMsg} numberOfLines={2}>{n.message}</Text>
            <View style={styles.itemMetaRow}>
              <View style={styles.metaLeft}>
                {n.severity === 'critical' && <Text style={styles.criticalText}>Critical</Text>}
              </View>
              <Text style={styles.itemMeta}>{metaText}</Text>
            </View>
          </View>
        </View>
      </Pressable>
    </Swipeable>
  );
}

export default NotificationItem;

