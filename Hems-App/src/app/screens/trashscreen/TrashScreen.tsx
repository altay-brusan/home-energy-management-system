import React, { JSX, useEffect, useMemo, useRef, useState, useCallback } from 'react';
import { View, RefreshControl, Animated, FlatList } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { makeTrashScreenStyles } from './TrashScreen.styles';
import { usePlatform } from '../../../hooks/usePlatform';
import { useNetInfo } from '../../../hooks/useNetInfo';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useI18n } from '../../../hooks/useI18n';
import { useDeviceNotifications } from '../../../hooks/useDeviceNotifications';
import { toast } from '../../../utils/toast';

import {
  TrashHeader,
  TrashItem,
  SectionHeader,
  EmptyState,
  FilterChips,
} from './ui';
import type { TrashItemRef } from './ui/TrashItem';

type Row =
  | { type: 'header'; id: string; title: string }
  | { type: 'item'; id: string; n: any };

type Filter = 'all' | 'today' | 'critical';

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
  const { t } = useI18n();

  // Use real API for deleted notifications
  const { 
    deletedNotifications,
    loading,
    refreshing,
    loadingMore,
    error,
    hasNextPage,
    refreshNotifications,
    loadMoreNotifications,
    restoreNotification,
    permanentDeleteNotification
  } = useDeviceNotifications({ includeDeleted: true });

  const [filter, setFilter] = useState<Filter>('all');
  const openSwipeableRef = useRef<Map<string, TrashItemRef>>(new Map());

  const opacity = useRef(new Animated.Value(0)).current;
  const translateY = useRef(new Animated.Value(12)).current;
  useEffect(() => {
    Animated.parallel([
      Animated.timing(opacity, { toValue: 1, duration: 220, useNativeDriver: true }),
      Animated.timing(translateY, { toValue: 0, duration: 220, useNativeDriver: true }),
    ]).start();
  }, []);

  useFocusEffect(
    useCallback(() => {
      if (isConnected) {
        refreshNotifications();
      }
    }, [isConnected, refreshNotifications])
  );

  const filtered = useMemo(() => {
    switch (filter) {
      case 'today':
        return deletedNotifications.filter((n) => isToday(n.createdAt));
      case 'critical':
        return deletedNotifications.filter((n) => n.severity === 'critical');
      default:
        return deletedNotifications;
    }
  }, [deletedNotifications, filter]);

  const splitGroups = (list: any[]) => {
    const today: any[] = [];
    const earlier: any[] = [];
    list.forEach((n) => (isToday(n.createdAt) ? today : earlier).push(n));
    return { today, earlier };
  };

  const flatData: Row[] = useMemo(() => {
    const { today, earlier } = splitGroups(filtered);
    const rows: Row[] = [];
    if (today.length) {
      rows.push({ type: 'header', id: 'h-today', title: t('trash.today') });
      today.forEach((n) => rows.push({ type: 'item', id: n.id, n }));
    }
    if (earlier.length) {
      rows.push({ type: 'header', id: 'h-earlier', title: t('trash.earlier') });
      earlier.forEach((n) => rows.push({ type: 'item', id: n.id, n }));
    }
    return rows;
  }, [filtered, t]);

  const onRefresh = useCallback(async () => {
    if (!isConnected) return;
    await refreshNotifications();
  }, [isConnected, refreshNotifications]);

  const handleRestore = async (id: string) => {
    try {
      await restoreNotification(id);
      openSwipeableRef.current.delete(id);
      toast.success(t('trash.restoreSuccess'), t('common.success'));
    } catch (error) {toast.error(t('trash.restoreFailed'), t('common.error'));
    }
  };
  
  const handleDelete = async (id: string) => {
    try {
      await permanentDeleteNotification(id);
      openSwipeableRef.current.delete(id);
      toast.success(t('trash.deleteSuccess'), t('common.success'));
    } catch (error) {toast.error(t('trash.deleteFailed'), t('common.error'));
    }
  };
  
  const handleBack = () => {
    navigation.goBack();
  };

  const openDetail = (n: any) => {
    navigation.navigate('NotificationDetail', { item: n });
  };

  const ListHeader = (
    <>
      <TrashHeader
        styles={styles}
        onBack={handleBack}
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
        ref={(ref) => {
          if (ref) {
            openSwipeableRef.current.set(item.n.id, ref);
          } else {
            openSwipeableRef.current.delete(item.n.id);
          }
        }}
        n={n}
        styles={styles}
        color={color}
        onRestore={handleRestore}
        onDelete={handleDelete}
        onOpen={openDetail}
        onSwipeableOpen={(id, closePrevious) => {
          openSwipeableRef.current.forEach((ref, itemId) => {
            if (itemId !== id) {
              ref.close();
            }
          });
        }}
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
