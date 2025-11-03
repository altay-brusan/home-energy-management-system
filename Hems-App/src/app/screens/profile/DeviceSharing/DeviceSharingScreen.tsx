import React, { useState, useMemo, useCallback, useRef } from 'react';
import { RefreshControl, View, FlatList, ActivityIndicator, Text } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useFocusEffect } from '@react-navigation/native';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeDeviceSharingStyles } from './DeviceSharingScreen.styles';
import { useNetInfo } from '../../../../hooks/useNetInfo';
import { useI18n } from '../../../../hooks/useI18n';
import { useInvitation } from '../../../../hooks/useInvitation';

import {
  DeviceSharingHeader,
  DeviceSharingForm,
  DeviceSharingUserItem,
} from './ui';

export default function DeviceSharingScreen({ navigation }: any) {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeDeviceSharingStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { isConnected } = useNetInfo();
  const { t } = useI18n();

  const { email, setEmail, error, message, loading, invitations, sendInvitation, removeInvitation, removeUser, reload } = useInvitation();
  const [refreshing, setRefreshing] = useState(false);
  const hasLoadedOnceRef = useRef(false);
  const reloadRef = useRef(reload);

  reloadRef.current = reload;

  useFocusEffect(
    useCallback(() => {
      if (hasLoadedOnceRef.current) {
        reloadRef.current();
      } else {
        hasLoadedOnceRef.current = true;
      }
    }, [])
  );

  const handleAdd = useCallback(async () => { 
    await sendInvitation(); 
  }, [sendInvitation]);

  const handleCancel = useCallback(() => {
    navigation?.goBack?.();
  }, [navigation]);

  const onRefresh = useCallback(async () => {
    if (!isConnected) return;
    setRefreshing(true);
    try { 
      await reload(); 
    } finally { 
      setRefreshing(false); 
    }
  }, [isConnected, reload]);

  const allUsers = useMemo(() => {
    if (!invitations) return [];
    const withUser = Array.isArray(invitations.withUser) ? invitations.withUser : [];
    const withoutUser = Array.isArray(invitations.withoutUser) ? invitations.withoutUser : [];
    return [...withUser, ...withoutUser];
  }, [invitations]);

  const listKey = useMemo(() => {
    if (!invitations) return 'empty';
    const withUser = Array.isArray(invitations.withUser) ? invitations.withUser : [];
    const withoutUser = Array.isArray(invitations.withoutUser) ? invitations.withoutUser : [];
    const allIds = [...withUser, ...withoutUser].map(item => item.id).sort().join(',');
    return `${withUser.length}-${withoutUser.length}-${allIds}`;
  }, [invitations]);

  const handleRemoveUser = useCallback(async (invitedUserId: string | undefined, invitationId?: string, hasInvitedUserId?: boolean) => {
    try {
      if (invitationId) {
        await removeUser(invitationId);
      } else {
        await removeInvitation(invitedUserId || '');
      }
    } catch (error) {
    }
  }, [removeUser, removeInvitation]);

  const renderUser = useCallback(({ item }: { item: any }) => (
    <DeviceSharingUserItem
      item={{
        id: item.invitedUserId || item.id,
        email: item.email,
        role: 'member',
        status: item.invitedUserId ? 'active' : 'pending',
        since: item.created,
        invitationId: item.id,
        hasInvitedUserId: !!item.invitedUserId,
      }}
      styles={styles}
      COLORS={COLORS}
      navigation={navigation}
      onRemove={(userId: string, invitationId?: string, hasInvitedUserId?: boolean) => {
        handleRemoveUser(undefined, invitationId || item.id, hasInvitedUserId);
      }}
    />
  ), [styles, COLORS, navigation, handleRemoveUser]);

  return (
    <FlatList
      style={[styles.container, { paddingTop: insets.top + 16 }]}
      data={allUsers}
      extraData={invitations} // Force re-render when invitations object changes (new reference)
      keyExtractor={(item) => item.id || `invitation-${item.email}`} // Use ID or email as fallback
      renderItem={renderUser}
      ItemSeparatorComponent={() => <View style={styles.separator} />}
      ListEmptyComponent={
        loading ? (
          <ActivityIndicator color={COLORS.accent} style={{ marginTop: 40 }} />
        ) : (
          <Text style={styles.emptyText}>{t('deviceSharing.noInvitations')}</Text>
        )
      }
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
            COLORS={COLORS}
            styles={styles}
            navigation={navigation}
          />
        </View>
      }
      ListFooterComponent={<View style={{ height: insets.bottom + 40 }} />}
      showsVerticalScrollIndicator
    />
  );
}
