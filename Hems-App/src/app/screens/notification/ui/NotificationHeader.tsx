import React from 'react';
import { TouchableOpacity, Text, View, ActivityIndicator, Alert } from 'react-native';
import { ArrowLeft, CheckCheck, Trash2 } from 'lucide-react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

type Props = {
  styles: Styles;
  textColor: string;
  accentColor: string;
  onMarkAllRead: () => void;
  onPressTrash: () => void;
  onPressBack: () => void;
  markingAllAsRead?: boolean;
};

export function NotificationHeader({
  styles,
  textColor,
  accentColor,
  onMarkAllRead,
  onPressTrash,
  onPressBack,
  markingAllAsRead
}: Props) {
  const { t } = useI18n();

  const handleMarkAllReadPress = () => {
    Alert.alert(
      t('notifications.markAllReadConfirm.title'),
      t('notifications.markAllReadConfirm.message'),
      [
        {
          text: t('notifications.markAllReadConfirm.cancel'),
          style: 'cancel',
        },
        {
          text: t('notifications.markAllReadConfirm.confirm'),
          onPress: onMarkAllRead,
          style: 'default',
        },
      ],
      { cancelable: true }
    );
  };

  return (
    <View style={styles.header}>
      <TouchableOpacity onPress={onPressBack} style={styles.headerBackButton}>
        <ArrowLeft size={24} color={textColor} />
      </TouchableOpacity>

      <Text style={styles.headerTitle}>{t('notifications.title')}</Text>

      <View style={styles.headerActions}>
        <TouchableOpacity
          onPress={handleMarkAllReadPress}
          style={[styles.headerActionBtn, { opacity: markingAllAsRead ? 0.5 : 1 }]}
          disabled={markingAllAsRead}
          activeOpacity={0.7}
        >
          {markingAllAsRead ? (
            <ActivityIndicator size="small" color={accentColor} />
          ) : (
            <CheckCheck size={22} color={accentColor} />
          )}
        </TouchableOpacity>
        <TouchableOpacity
          onPress={onPressTrash}
          style={styles.headerActionBtn}
          activeOpacity={0.7}
        >
          <Trash2 size={22} color={textColor} />
        </TouchableOpacity>
      </View>
    </View>
  );
}

export default NotificationHeader;
