import React from 'react';
import { Text } from 'react-native';
import Animated, { FadeIn, FadeInUp } from 'react-native-reanimated';
import { getDashboardNotificationsStyles } from './DashboardNotifications.styles';
import { useTranslation } from 'react-i18next';
import type {
  DashboardNotificationsProps,
  Notification,
} from './DashboardNotifications.types';

export default function DashboardNotifications({
  COLORS,
}: DashboardNotificationsProps) {
  const styles = getDashboardNotificationsStyles(COLORS);
   const { t } = useTranslation();

  const notifications: Notification[] = [];

  return (
    <Animated.View
      entering={FadeIn.delay(600).duration(600)}
      style={styles.notifications}
    >
   <Text style={styles.sectionTitle}>{t('dashboardNotifications.title')}</Text>
      {notifications.length === 0 ? (
        <Text style={styles.noNotification}>{t('dashboardNotifications.empty')}</Text>
      ) : (
        notifications.map(n => (
          <Animated.View
            key={n.id}
            entering={FadeInUp.delay(650 + n.id * 80).duration(400)}
            style={styles.notificationItem}
          >
            <Text style={styles.notificationText}>{n.text}</Text>
          </Animated.View>
        ))
      )}
    </Animated.View>
  );
}
