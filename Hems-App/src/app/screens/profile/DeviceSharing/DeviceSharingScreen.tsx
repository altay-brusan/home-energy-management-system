import React, { useState } from 'react';
import { RefreshControl, View, FlatList } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeDeviceSharingStyles } from './DeviceSharingScreen.styles';
import { useNetInfo } from '../../../../hooks/useNetInfo';

import {
  DeviceSharingHeader,
  DeviceSharingForm,
  DeviceSharingUserItem,
} from './ui';

type SharedUser = {
  id: string;
  email: string;
  role: 'owner' | 'member';
  status: 'pending' | 'accepted' | 'rejected' | 'active' | 'inactive';
  since?: string;
};

export default function DeviceSharingScreen({ navigation }: any) {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeDeviceSharingStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { isConnected } = useNetInfo();

  const [email, setEmail] = useState('');
  const [users, setUsers] = useState<SharedUser[]>([
    { id: '1', email: 'owner@example.com', role: 'owner', status: 'active', since: '2024-01-01' },
    { id: '2', email: 'ex2@example.com', role: 'member', status: 'pending', since: '2024-02-01' },
    { id: '3', email: 'ex3@example.com', role: 'member', status: 'accepted', since: '2024-02-01' },
    { id: '4', email: 'ex4@example.com', role: 'member', status: 'accepted', since: '2024-02-01' },
  ]);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [refreshing, setRefreshing] = useState(false);

  const handleAdd = () => {
    setError('');
    setMessage('');
    if (!email.includes('@')) {
      setError('Please enter a valid email address.');
      return;
    }
    setUsers((prev) => [
      ...prev,
      {
        id: Date.now().toString(),
        email,
        role: 'member',
        status: 'pending',
        since: new Date().toISOString().split('T')[0],
      },
    ]);
    setMessage('Invitation sent successfully.');
    setEmail('');
  };

  const handleCancel = () => {
    setError('');
    setMessage('');
    navigation?.goBack?.();
  };

  const onRefresh = async () => {
    if (!isConnected) return;
    setRefreshing(true);
    try {
      setUsers((prev) => [...prev]);
    } finally {
      setRefreshing(false);
    }
  };

  const renderUser = ({ item }: { item: SharedUser }) => (
    <DeviceSharingUserItem
      item={item}
      styles={styles}
      COLORS={COLORS}
      navigation={navigation}
      onRemove={(id: string) =>
        setUsers((prev) => prev.filter((u) => u.id !== id))
      }
    />
  );

  return (
    <FlatList
      style={[styles.container, { paddingTop: insets.top + 16 }]}
      data={users}
      keyExtractor={(item) => item.id}
      renderItem={renderUser}
      ItemSeparatorComponent={() => <View style={styles.separator} />}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={onRefresh}
          enabled={!!isConnected}
          colors={[COLORS.accent ?? '#00D1A1']}
          tintColor={COLORS.accent ?? '#00D1A1'}
        />
      }
      contentContainerStyle={[
        styles.pageScroll,
        { paddingBottom: insets.bottom + 80 },
      ]}
      ListHeaderComponent={
        <View style={styles.pageScroll}>
          <DeviceSharingHeader styles={styles} />
          <DeviceSharingForm
            email={email}
            setEmail={setEmail}
            error={error}
            message={message}
            COLORS={COLORS}
            styles={styles}
            onInvite={handleAdd}
            onCancel={handleCancel}
          />
        </View>
      }
      ListFooterComponent={<View style={{ height: insets.bottom + 40 }} />}
      showsVerticalScrollIndicator
    />
  );
}
