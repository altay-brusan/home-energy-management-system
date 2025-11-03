import React from 'react';
import { Pressable, Text, View, ActivityIndicator } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

type Props = {
  styles: Styles;
  onMarkAllRead: () => void;
  onPressTrash: () => void;
  markingAllAsRead?: boolean;
};

export function NotificationHeader({ styles, onMarkAllRead, onPressTrash, markingAllAsRead }: Props) {
  const { t } = useI18n();
  
  return (
    <View style={styles.headerRow}>
      <Text style={styles.title}>{t('notifications.title')}</Text>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
        <Pressable 
          onPress={onMarkAllRead} 
          style={[styles.headerBtn, { opacity: markingAllAsRead ? 0.6 : 1 }]}
          disabled={markingAllAsRead}
        >
          {markingAllAsRead ? (
            <ActivityIndicator size="small" color={styles.headerBtnText.color} />
          ) : (
            <Text style={styles.headerBtnText}>{t('notifications.markAllRead')}</Text>
          )}
        </Pressable>
        <Pressable onPress={onPressTrash} style={styles.headerBtn}>
          <FontAwesome name="trash" size={18} color={styles.headerBtnText.color} />
        </Pressable>
      </View>
    </View>
  );
}

export default NotificationHeader;
