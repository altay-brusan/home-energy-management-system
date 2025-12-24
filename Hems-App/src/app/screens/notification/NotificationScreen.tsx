import React, { JSX, useEffect, useMemo, useRef, useState, useCallback } from 'react';
import { View, Text, RefreshControl, Animated, FlatList, ActivityIndicator } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeNotificationScreenStyles } from './NotificationScreen.styles';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useNetInfo } from '../../../hooks/useNetInfo';
import { useErrorHandler } from '../../../hooks/useErrorHandler';
import { usePlatform } from '../../../hooks/usePlatform';
import { useDeviceNotifications, DeviceNotification } from '../../../hooks/useDeviceNotifications';
import { EmptyState, Filter, FilterChips, NotificationHeader, NotificationItem, OfflineBanner, SectionHeader } from './ui';
import type { NotificationItemRef } from './ui/NotificationItem';
import { useI18n } from '../../../hooks/useI18n';
import { toast } from '../../../utils/toast';
import { useRoles } from '../../../hooks/useRoles';
import { useSelectedSiteUtils } from '../../../hooks/useSelectedSite';


function isToday(iso: string) {
  const d = new Date(iso), n = new Date();
  return d.getFullYear() === n.getFullYear() && d.getMonth() === n.getMonth() && d.getDate() === n.getDate();
}

type Row =
  | { type: 'header'; id: string; title: string }
  | { type: 'item'; id: string; n: DeviceNotification };

export default function NotificationScreen(): JSX.Element {
  const insets = useSafeAreaInsets();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark) as any;
  const styles = useMemo(() => makeNotificationScreenStyles(isDark), [isDark]);
  const { t } = useI18n();
  const { isAdmin, hasRole } = useRoles();
  const isAdminUser = isAdmin || hasRole("Administrator");
  const { selectedSite } = useSelectedSiteUtils();
  const navigation = useNavigation<NativeStackNavigationProp<any>>();

  // Admin kullanıcılar için ev seçmeden Notifications'a geçmeyi engelle
  useFocusEffect(
    useCallback(() => {
      if (isAdminUser && !selectedSite) {
        // Admin kullanıcı ev seçmemişse Admin tab'ına yönlendir
        // Tab navigator içinde jumpTo kullanmalıyız
        (navigation as any).jumpTo?.('Admin');
      }
    }, [isAdminUser, selectedSite, navigation])
  );

  const { 
    notifications, 
    loading, 
    refreshing, 
    loadingMore, 
    error, 
    hasNextPage,
    refreshNotifications,
    loadMoreNotifications,
    markAsRead, 
    markAllAsRead, 
    deleteNotification 
  } = useDeviceNotifications();

  const [filter, setFilter] = useState<Filter>('all');
  const [markingAllAsRead, setMarkingAllAsRead] = useState(false);
  const { isAndroid } = usePlatform();
  const { isConnected } = useNetInfo();
  const { handleError } = useErrorHandler();
  const openSwipeableRef = useRef<Map<string, NotificationItemRef>>(new Map());

  const filtered = useMemo(() => {
    switch (filter) {
      case 'today': return notifications.filter(n => isToday(n.createdAt));
      case 'unread': return notifications.filter(n => !n.read);
      case 'critical': return notifications.filter(n => n.severity === 'critical');
      default: return notifications;
    }
  }, [notifications, filter]);

  const openDetail = async (n: DeviceNotification) => {
    if (!n.read) {
      try {
        await markAsRead(n.id);
        // Toast gösterilmiyor - sadece detaya gidiyor
      } catch (error) {// Hata durumunda da toast gösterilmiyor
      }
    }
    navigation.navigate('NotificationDetail', { item: n });
  };

  const splitGroups = (list: DeviceNotification[]) => {
    const today: DeviceNotification[] = [];
    const earlier: DeviceNotification[] = [];
    list.forEach(n => (isToday(n.createdAt) ? today : earlier).push(n));
    return { today, earlier };
  };

  const flatData: Row[] = useMemo(() => {
    const { today, earlier } = splitGroups(filtered);
    const rows: Row[] = [];
    if (today.length) {
      rows.push({ type: 'header', id: 'h-today', title: t('notifications.today') });
      today.forEach((n, index) => rows.push({ type: 'item', id: `today-${n.id}-${index}`, n }));
    }
    if (earlier.length) {
      rows.push({ type: 'header', id: 'h-earlier', title: t('notifications.earlier') });
      earlier.forEach((n, index) => rows.push({ type: 'item', id: `earlier-${n.id}-${index}`, n }));
    }
    return rows;
  }, [filtered, t]);

  const opacity = useRef(new Animated.Value(0)).current;
  const translateY = useRef(new Animated.Value(12)).current;
  useEffect(() => {
    Animated.parallel([
      Animated.timing(opacity, { toValue: 1, duration: 220, useNativeDriver: true }),
      Animated.timing(translateY, { toValue: 0, duration: 220, useNativeDriver: true }),
    ]).start();
  }, []);

  const onRefresh = async () => {
    if (!isConnected) return;
    refreshNotifications();
  };

  const handleMarkAllAsRead = async () => {
    try {
      setMarkingAllAsRead(true);
      await markAllAsRead();
      toast.success(t('notifications.markAllReadSuccess'), t('common.success'));
    } catch (error) {toast.error(t('notifications.markAllReadError'), t('common.error'));
    } finally {
      setMarkingAllAsRead(false);
    }
  };

  const ListHeader = (
    <>
      <NotificationHeader
        styles={styles}
        textColor={COLORS.text}
        accentColor={COLORS.accent}
        onMarkAllRead={handleMarkAllAsRead}
        onPressTrash={() => navigation.navigate('Trash')}
        onPressBack={() => navigation.goBack()}
        markingAllAsRead={markingAllAsRead}
      />
      <View style={styles.filterSticky}>
        <FilterChips value={filter} onChange={setFilter} styles={styles} />
        {!isConnected && <OfflineBanner styles={styles} />}
      </View>
    </>
  );

  return (
    <Animated.View style={[styles.screen, { paddingTop: insets.top + 8, opacity, transform: [{ translateY }] }]}>
      <FlatList<Row>
        data={flatData}
        keyExtractor={(it) => it.id}
        renderItem={({ item }) =>
          item.type === 'header'
            ? <SectionHeader title={item.title} styles={styles} />
            : (
              <NotificationItem
                ref={(ref) => {
                  if (ref) {
                    openSwipeableRef.current.set(item.n.id, ref);
                  } else {
                    openSwipeableRef.current.delete(item.n.id);
                  }
                }}
                n={item.n}
                styles={styles}
                isDark={isDark}
                accent={COLORS.accent}
                onOpen={openDetail}
                onDelete={async (id) => {
                  try {
                    await deleteNotification(id);
                    openSwipeableRef.current.delete(id);
                    toast.success(t('notifications.deleteToast.success'), t('common.success'));
                  } catch (error) {toast.error(t('notifications.deleteToast.error'), t('common.error'));
                  }
                }}
                onMarkRead={async (id) => {
                  const notification = notifications.find(n => n.id === id);
                  if (notification?.read) {return;
                  }
                  
                  try {
                    await markAsRead(id);
                    openSwipeableRef.current.delete(id);
                    toast.success(t('notifications.markAsRead'), t('common.success'));
                  } catch (error) {toast.error(t('notifications.markAsReadFailed'), t('common.error'));
                  }
                }}
                onSwipeableOpen={(id, closePrevious) => {
                  openSwipeableRef.current.forEach((ref, itemId) => {
                    if (itemId !== id) {
                      ref.close();
                    }
                  });
                }}
              />
            )
        }
        contentContainerStyle={styles.pageScroll}
        ListHeaderComponent={ListHeader}
        ListEmptyComponent={<EmptyState styles={styles} />}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            enabled={!!isConnected}
            colors={[COLORS.accent ?? '#00D1A1']}
            tintColor={COLORS.accent ?? '#00D1A1'}
          />
        }
        onEndReached={loadMoreNotifications}
        onEndReachedThreshold={0.5}
        ListFooterComponent={() => 
          loadingMore ? (
            <View style={{ padding: 20, alignItems: 'center' }}>
              <ActivityIndicator size="small" color={COLORS.accent} />
            </View>
          ) : null
        }
        ItemSeparatorComponent={() => <View style={styles.separator} />}
        showsVerticalScrollIndicator={false}
        bounces
        alwaysBounceVertical
        overScrollMode={isAndroid ? 'always' : 'auto'}
        decelerationRate="normal"
        initialNumToRender={8}
        windowSize={10}
        removeClippedSubviews
      />
    </Animated.View>
  );
}
