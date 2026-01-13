import React, { useState } from 'react';
import { RefreshControl, View, Text, FlatList } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { makeUserDetailStyles } from './DeviceSharingDetail.styles';
import Card from '../../../../../components/ui/Card/Card';
import Button from '../../../../../components/ui/Button/Button';
import { useNetInfo } from '../../../../../hooks/useNetInfo';
import { useI18n } from '../../../../../hooks/useI18n';
import { useInvitation } from '../../../../../hooks/useInvitation';

type Props = {
  route: any;
  navigation: any;
};

export default function DeviceSharingDetailScreen({ route, navigation }: Props) {
  const { userId, email, status, since, active, invitationId, hasInvitedUserId, onRemove } = route.params;
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS: Record<string, string> = getThemeColors(isDark);
  const styles = makeUserDetailStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { isConnected } = useNetInfo();
  const { t, language } = useI18n();
  const { removeInvitation, removeUser } = useInvitation();

  const [refreshing, setRefreshing] = useState(false);
  const [removing, setRemoving] = useState(false);

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      const localeString = language === 'tr' ? 'tr-TR' : 'en-US';
      return date.toLocaleDateString(localeString, {
        day: '2-digit',
        month: 'long',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateString;
    }
  };

  const formattedSince = formatDate(since);

  const logs = [
    { id: '1', text: t('deviceSharing.detail.invitationSent', { date: formattedSince }) },
    { id: '2', text: t('deviceSharing.detail.statusChanged', { status, date: formattedSince }) },
    { id: '4', text: active ? t('deviceSharing.detail.accessGranted', { date: formattedSince }) : t('deviceSharing.detail.accessRevoked', { date: formattedSince }) },
    { id: '5', text: active ? t('deviceSharing.detail.accessGranted', { date: formattedSince }) : t('deviceSharing.detail.accessRevoked', { date: formattedSince }) },
    { id: '6', text: active ? t('deviceSharing.detail.accessGranted', { date: formattedSince }) : t('deviceSharing.detail.accessRevoked', { date: formattedSince }) },
    { id: '7', text: active ? t('deviceSharing.detail.accessGranted', { date: formattedSince }) : t('deviceSharing.detail.accessRevoked', { date: formattedSince }) },
  ];

  const handleRemove = async () => {
    try {
      setRemoving(true);
      
      if (invitationId) {
        await removeUser(invitationId);
      } else if (userId) {
        await removeInvitation(userId);
      }
      
      navigation.goBack();
    } catch (error) {
    } finally {
      setRemoving(false);
    }
  };

  const onRefresh = async () => {
    if (!isConnected) return;
    setRefreshing(true);
    try {
    } finally {
      setRefreshing(false);
    }
  };

return (
  <FlatList
    style={styles.container}
    data={logs}
    keyExtractor={(item) => item.id}
    renderItem={({ item }) => (
      <Text style={styles.logItem}>{item.text}</Text>
    )}
    contentContainerStyle={[
      styles.pageScroll,
      { paddingBottom: insets.bottom + 40 }, 
    ]}
    refreshControl={
      <RefreshControl
        refreshing={refreshing}
        onRefresh={onRefresh}
        enabled={!!isConnected}
        colors={[COLORS.accent]}
        tintColor={COLORS.accent}
      />
    }
    ListHeaderComponent={
      <View style={[styles.headerContainer, { paddingTop: insets.top + 16 }]}>
        <Text style={styles.title}>{t('deviceSharing.detail.title')}</Text>

        <Card>
          <Text style={styles.label}>{t('deviceSharing.detail.email')}</Text>
          <Text style={styles.value}>{email}</Text>

          <Text style={styles.label}>{t('deviceSharing.detail.accessSinceLabel')}</Text>
          <Text style={styles.value}>{formattedSince}</Text>

          <Text style={styles.label}>{t('deviceSharing.detail.active')}</Text>
          <Text style={styles.value}>{active ? t('deviceSharing.detail.yes') : t('deviceSharing.detail.no')}</Text>

          <Text style={styles.label}>{t('deviceSharing.detail.statusLabel')}</Text>
          <Text style={styles.value}>{t(`deviceSharing.status.${status}`)}</Text>
        </Card>

        <View style={styles.buttonRow}>
          <Button
            title={removing ? t('deviceSharing.removing') : t('deviceSharing.detail.removeUser')}
            style={styles.removeBtn}
            textStyle={styles.removeBtnText}
            disabled={removing}
            onPress={handleRemove}
          />
          <Button
            title={t('deviceSharing.cancel')}
            style={styles.cancelBtn}
            textStyle={styles.cancelBtnText}
            onPress={() => navigation.goBack()}
          />
        </View>

        <Text style={styles.subtitle}>{t('deviceSharing.detail.activityLog')}</Text>
      </View>
    }
    showsVerticalScrollIndicator
  />
);

}
