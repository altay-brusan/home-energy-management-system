import React from 'react';
import { Pressable, Text, View } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

type Props = {
  styles: Styles;
  onMarkAllRead: () => void;
  onPressTrash: () => void;
};

export function NotificationHeader({ styles, onMarkAllRead, onPressTrash }: Props) {
  return (
    <View style={styles.headerRow}>
      <Text style={styles.title}>Notifications</Text>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
        <Pressable onPress={onMarkAllRead} style={styles.headerBtn}>
          <Text style={styles.headerBtnText}>Mark all read</Text>
        </Pressable>
        <Pressable onPress={onPressTrash} style={styles.headerBtn}>
          <FontAwesome name="trash" size={18} color={styles.headerBtnText.color} />
        </Pressable>
      </View>
    </View>
  );
}

export default NotificationHeader;
