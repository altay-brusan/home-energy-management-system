import React, { useState, useRef, useImperativeHandle, forwardRef, useMemo } from 'react';
import { Pressable, Text, View, StyleProp, ViewStyle } from 'react-native';
import { Swipeable } from 'react-native-gesture-handler';
import type { DeviceNotification, NotificationSeverityLevel } from '../../../../hooks/useDeviceNotifications';
import { useI18n } from '../../../../hooks/useI18n';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import StatusPill from './StatusPill';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

type Props = {
  n: DeviceNotification;
  styles: Styles;
  isDark: boolean;
  accent?: string;
  onOpen: (n: DeviceNotification) => void;
  onDelete: (id: string) => void;
  onMarkRead: (id: string) => void;
  onSwipeableOpen?: (id: string, closePrevious: () => void) => void;
};

export type NotificationItemRef = {
  close: () => void;
};

const getSeverityStyle = (severityLevel: NotificationSeverityLevel, styles: Styles): StyleProp<ViewStyle> => {
  switch (severityLevel) {
    case 'ERROR':
      return styles.itemRowError;
    case 'WARNING':
      return styles.itemRowWarning;
    case 'ALARM':
      return styles.itemRowAlarm;
    case 'INFO':
    default:
      return styles.itemRowInfo;
  }
};

export const NotificationItem = forwardRef<NotificationItemRef, Props>(({ n, styles, isDark, accent, onOpen, onDelete, onMarkRead, onSwipeableOpen }, ref) => {
  const [showActions, setShowActions] = useState(false);
  const swipeableRef = useRef<Swipeable>(null);
  const { t } = useI18n();
  const metaText = new Date(n.createdAt).toLocaleString();

  const severityStyle = useMemo(() => getSeverityStyle(n.severityLevel, styles), [n.severityLevel, styles]);

  useImperativeHandle(ref, () => ({
    close: () => {
      swipeableRef.current?.close();
      setShowActions(false);
    },
  }));

  const handleMarkRead = async () => {
    try {
      await onMarkRead(n.id);
      swipeableRef.current?.close();
      setShowActions(false);
    } catch (error) {}
  };

  const handleSwipeableOpen = () => {
    if (onSwipeableOpen) {
      onSwipeableOpen(n.id, () => {
        swipeableRef.current?.close();
        setShowActions(false);
      });
    }
    setShowActions(true);
  };

  const handleSwipeableClose = () => {
    setShowActions(false);
  };

  const handleDelete = () => {
    onDelete(n.id);
    swipeableRef.current?.close();
    setShowActions(false);
  };

  return (
    <Swipeable
      ref={swipeableRef}
      renderLeftActions={!n.read ? () => (
        <Pressable onPress={handleMarkRead} style={styles.swipeLeft}>
          <Text style={styles.swipeLeftText}>{t('notifications.status.read')}</Text>
        </Pressable>
      ) : undefined}
      renderRightActions={() => (
        <Pressable onPress={handleDelete} style={styles.swipeRight}>
          <Text style={styles.swipeRightText}>{t('common.delete')}</Text>
        </Pressable>
      )}
      onSwipeableWillClose={handleSwipeableClose}
      onSwipeableWillOpen={handleSwipeableOpen}
      overshootLeft={false}
      overshootRight={false}
      friction={2}
    >
      <Pressable onPress={() => onOpen(n)} style={styles.itemPress}>
        <View style={[styles.itemRow, severityStyle]}>
          <View style={styles.itemContent}>
            <View style={styles.itemHeader}>
              <Text
                style={[styles.itemTitle, n.read && styles.itemTitleRead]}
                numberOfLines={2}
                ellipsizeMode="tail"
              >
                {n.title}
              </Text>
              {!n.read && <StatusPill read={n.read} styles={styles} isDark={isDark} accent={accent} />}
            </View>
            <Text
              style={[styles.itemMsg, n.read && styles.itemMsgRead]}
              numberOfLines={2}
              ellipsizeMode="tail"
            >
              {n.message}
            </Text>
            <View style={styles.itemMetaRow}>
              <View style={styles.metaLeft}>
                {n.severity === 'critical' && (
                  <Text style={styles.criticalText}>{t('notifications.critical')}</Text>
                )}
              </View>
              <Text style={styles.itemMeta}>{metaText}</Text>
            </View>
          </View>
        </View>
      </Pressable>
    </Swipeable>
  );
});

NotificationItem.displayName = 'NotificationItem';

export default NotificationItem;

