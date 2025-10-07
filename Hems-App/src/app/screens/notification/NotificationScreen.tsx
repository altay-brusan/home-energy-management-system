import React, { JSX, useEffect, useMemo, useRef, useState } from 'react';
import { View, Text, RefreshControl, Animated, FlatList } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeNotificationScreenStyles } from './NotificationScreen.styles';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useNetInfo } from '../../../hooks/useNetInfo';
import { useErrorHandler } from '../../../hooks/useErrorHandler';
import { usePlatform } from '../../../hooks/usePlatform';
import { useNotifications, Notification } from '../../../hooks/useNotifications';
import { EmptyState, Filter, FilterChips, NotificationHeader, NotificationItem, OfflineBanner, SectionHeader } from './ui';

const seed: Notification[] = [
  { id: 'n1', title: 'Welcome Home', message: 'System is running normally.', description: 'The system started successfully and is running in optimal condition.', createdAt: new Date().toISOString(), read: false, severity: 'normal' },
  { id: 'n2', title: 'Battery Low', message: 'Battery level is 18%. Charging is recommended.', description: 'The battery level has dropped below 20%. Please connect to a charger to avoid interruption.', createdAt: new Date(Date.now() - 1000 * 60 * 40).toISOString(), read: false, severity: 'normal' },
  { id: 'n3', title: 'Grid Disconnected', message: 'Grid connection lost. Please check the system.', description: 'The power grid connection was interrupted. This may cause instability. Please verify your grid connection immediately.', createdAt: new Date(Date.now() - 1000 * 60 * 60 * 26).toISOString(), read: true, severity: 'critical' },
  { id: 'n4', title: 'Firmware Info', message: 'New firmware notes are available.', description: 'A new firmware update has been released. Review the release notes for bug fixes and improvements.', createdAt: new Date(Date.now() - 1000 * 60 * 60 * 48).toISOString(), read: true, severity: 'normal' },
];

function isToday(iso: string) {
  const d = new Date(iso), n = new Date();
  return d.getFullYear() === n.getFullYear() && d.getMonth() === n.getMonth() && d.getDate() === n.getDate();
}

type Row =
  | { type: 'header'; id: string; title: string }
  | { type: 'item'; id: string; n: Notification };

export default function NotificationScreen(): JSX.Element {
  const insets = useSafeAreaInsets();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark) as any;
  const styles = useMemo(() => makeNotificationScreenStyles(isDark), [isDark]);

  const { items, setItems, deleteNotification, markRead, markAllRead } = useNotifications(seed);

  const [refreshing, setRefreshing] = useState(false);
  const [filter, setFilter] = useState<Filter>('all');

  const navigation = useNavigation<NativeStackNavigationProp<any>>();
  const { isAndroid } = usePlatform();
  const { isConnected } = useNetInfo();
  const { handleError } = useErrorHandler();

  const filtered = useMemo(() => {
    switch (filter) {
      case 'today': return items.filter(n => isToday(n.createdAt));
      case 'unread': return items.filter(n => !n.read);
      case 'critical': return items.filter(n => n.severity === 'critical');
      default: return items;
    }
  }, [items, filter]);

  const openDetail = (n: Notification) => {
    if (!n.read) markRead(n.id);
    navigation.navigate('NotificationDetail', { item: n });
  };

  const splitGroups = (list: Notification[]) => {
    const today: Notification[] = [];
    const earlier: Notification[] = [];
    list.forEach(n => (isToday(n.createdAt) ? today : earlier).push(n));
    return { today, earlier };
  };

  const flatData: Row[] = useMemo(() => {
    const { today, earlier } = splitGroups(filtered);
    const rows: Row[] = [];
    if (today.length) {
      rows.push({ type: 'header', id: 'h-today', title: 'Today' });
      today.forEach(n => rows.push({ type: 'item', id: n.id, n }));
    }
    if (earlier.length) {
      rows.push({ type: 'header', id: 'h-earlier', title: 'Earlier' });
      earlier.forEach(n => rows.push({ type: 'item', id: n.id, n }));
    }
    return rows;
  }, [filtered]);

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
    setRefreshing(true);
    try { setItems(prev => [...prev]); } catch (e) { handleError(e); } finally { setRefreshing(false); }
  };

  const ListHeader = (
    <>
   <NotificationHeader
  styles={styles}
  onMarkAllRead={markAllRead}
  onPressTrash={() => navigation.navigate('Trash')}
/>
      <View style={styles.filterSticky}>
        <View style={styles.filterRow}>
          <View style={styles.flex1} />
          <FilterChips value={filter} onChange={setFilter} styles={styles} />
        </View>
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
                n={item.n}
                styles={styles}
                isDark={isDark}
                accent={COLORS.accent}
                onOpen={openDetail}
               onDelete={(id) => deleteNotification(id)} 
                onMarkRead={markRead}
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
