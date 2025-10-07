import React, { JSX, useEffect, useMemo, useRef, useState } from 'react';
import { View, RefreshControl, Animated, FlatList } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeTrashScreenStyles } from './TrashScreen.styles';
import { usePlatform } from '../../../hooks/usePlatform';
import { useNetInfo } from '../../../hooks/useNetInfo';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

import {
  TrashHeader,
  TrashItem,
  SectionHeader,
  EmptyState,
  FilterChips,
} from './ui';

export type TrashNotification = {
  id: string;
  title: string;
  message: string;
  description: string;
  createdAt: string;
  read: boolean;
  severity: 'normal' | 'critical';
  deletedAt: string;
};

type Row =
  | { type: 'header'; id: string; title: string }
  | { type: 'item'; id: string; n: TrashNotification };

type Filter = 'all' | 'today' | 'critical';

const now = Date.now();
const seed: TrashNotification[] = [
  {
    id: 't1',
    title: 'Welcome Home',
    message: 'System is running normally.',
    description: 'The system started successfully and is running in optimal condition.',
    createdAt: new Date().toISOString(),
    read: false,
    severity: 'normal',
    deletedAt: new Date().toISOString(),
  },
  {
    id: 't2',
    title: 'Grid Disconnected',
    message: 'Grid connection lost. Please check the system.',
    description: 'The power grid connection was interrupted. This may cause instability.',
    createdAt: new Date(now - 1000 * 60 * 60 * 26).toISOString(),
    read: true,
    severity: 'critical',
    deletedAt: new Date(now - 1000 * 60 * 60 * 26).toISOString(),
  },
];

function isToday(iso: string) {
  const d = new Date(iso),
    n = new Date();
  return (
    d.getFullYear() === n.getFullYear() &&
    d.getMonth() === n.getMonth() &&
    d.getDate() === n.getDate()
  );
}

export default function TrashScreen(): JSX.Element {
  const insets = useSafeAreaInsets();
  const isDark = useThemeStore((s) => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark) as any;
  const styles = useMemo(() => makeTrashScreenStyles(isDark, COLORS), [isDark, COLORS]);
  const { isConnected } = useNetInfo();
  const { isAndroid } = usePlatform();
  const navigation = useNavigation<NativeStackNavigationProp<any>>();

  const [refreshing, setRefreshing] = useState(false);
  const [items, setItems] = useState<TrashNotification[]>(seed);
  const [filter, setFilter] = useState<Filter>('all');

  const opacity = useRef(new Animated.Value(0)).current;
  const translateY = useRef(new Animated.Value(12)).current;
  useEffect(() => {
    Animated.parallel([
      Animated.timing(opacity, { toValue: 1, duration: 220, useNativeDriver: true }),
      Animated.timing(translateY, { toValue: 0, duration: 220, useNativeDriver: true }),
    ]).start();
  }, []);

  const filtered = useMemo(() => {
    switch (filter) {
      case 'today':
        return items.filter((n) => isToday(n.deletedAt));
      case 'critical':
        return items.filter((n) => n.severity === 'critical');
      default:
        return items;
    }
  }, [items, filter]);

  const splitGroups = (list: TrashNotification[]) => {
    const today: TrashNotification[] = [];
    const earlier: TrashNotification[] = [];
    list.forEach((n) => (isToday(n.deletedAt) ? today : earlier).push(n));
    return { today, earlier };
  };

  const flatData: Row[] = useMemo(() => {
    const { today, earlier } = splitGroups(filtered);
    const rows: Row[] = [];
    if (today.length) {
      rows.push({ type: 'header', id: 'h-today', title: 'Today' });
      today.forEach((n) => rows.push({ type: 'item', id: n.id, n }));
    }
    if (earlier.length) {
      rows.push({ type: 'header', id: 'h-earlier', title: 'Earlier Deleted' });
      earlier.forEach((n) => rows.push({ type: 'item', id: n.id, n }));
    }
    return rows;
  }, [filtered]);

  const onRefresh = async () => {
    if (!isConnected) return;
    setRefreshing(true);
    try {
      setItems((prev) => [...prev]);
    } finally {
      setRefreshing(false);
    }
  };

  const handleRestore = (id: string) =>
    setItems((prev) => prev.filter((n) => n.id !== id));
  const handleDelete = (id: string) =>
    setItems((prev) => prev.filter((n) => n.id !== id));
  const handleRestoreAll = () => setItems([]);
  const handleDeleteAll = () => setItems([]);

  const openDetail = (n: TrashNotification) => {
    navigation.navigate('NotificationDetail', { item: n });
  };

  const ListHeader = (
    <>
      <TrashHeader
        styles={styles}
        onRestoreAll={handleRestoreAll}
        onDeleteAll={handleDeleteAll}
      />
      <FilterChips value={filter} onChange={setFilter} styles={styles} />
    </>
  );

  const renderItem = ({ item }: { item: Row }) => {
    if (item.type === 'header') {
      return <SectionHeader title={item.title} styles={styles} />;
    }

    const n = item.n;
    const color = n.severity === 'critical' ? COLORS.error : COLORS.accent;

    return (
      <TrashItem
        n={n}
        styles={styles}
        color={color}
        onRestore={handleRestore}
        onDelete={handleDelete}
        onOpen={openDetail}
      />
    );
  };

  return (
    <Animated.View
      style={[
        styles.screen,
        { paddingTop: insets.top + 8, opacity, transform: [{ translateY }] },
      ]}
    >
      <FlatList<Row>
        data={flatData}
        keyExtractor={(it) => it.id}
        renderItem={renderItem}
        contentContainerStyle={styles.pageScroll}
        ListHeaderComponent={ListHeader}
        ListEmptyComponent={<EmptyState styles={styles} />}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            enabled={!!isConnected}
            colors={[COLORS.accent]}
            tintColor={COLORS.accent}
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
