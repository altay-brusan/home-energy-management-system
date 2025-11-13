import React from 'react';
import { Pressable, Text, View, ActivityIndicator } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { ArrowLeft } from 'lucide-react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

type Props = {
  styles: Styles;
  onMarkAllRead: () => void;
  onPressTrash: () => void;
  onPressBack: () => void;
  markingAllAsRead?: boolean;
};

export function NotificationHeader({ styles, onMarkAllRead, onPressTrash, onPressBack, markingAllAsRead }: Props) {
  const { t } = useI18n();

  return (
    <View style={styles.headerRow}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 16 }}>
        <Pressable
          onPress={onPressBack}
          style={{
            paddingVertical: 8,
            paddingRight: 8,
            marginLeft: -4
          }}
        >
          <ArrowLeft size={26} color={styles.title.color} strokeWidth={2.5} />
        </Pressable>
        <Text style={styles.title}>{t('notifications.title')}</Text>
      </View>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10 }}>
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
