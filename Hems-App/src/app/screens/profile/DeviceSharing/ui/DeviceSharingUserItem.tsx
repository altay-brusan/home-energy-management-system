import React from 'react';
import { TouchableOpacity, View, Text } from 'react-native';
import Card from '../../../../../components/ui/Card/Card';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import type { makeDeviceSharingStyles } from '../DeviceSharingScreen.styles';
import { useI18n } from '../../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeDeviceSharingStyles>;

type SharedUser = {
  id: string;
  email: string;
  role: 'owner' | 'member';
  status: 'pending' | 'accepted' | 'rejected' | 'active' | 'inactive';
  since?: string;
  invitationId?: string;
  hasInvitedUserId?: boolean;
};

type Props = {
  item: SharedUser;
  styles: Styles;
  COLORS: any;
  navigation: any;
  onRemove: (userId: string, invitationId?: string, hasInvitedUserId?: boolean) => void;
};

export function DeviceSharingUserItem({
  item,
  styles,
  COLORS,
  navigation,
  onRemove,
}: Props) {
  const { t } = useI18n();

  const formattedDate = item.since
    ? new Date(item.since).toLocaleString('en-GB', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    : null;

  return (
    <TouchableOpacity
      activeOpacity={0.9}
      disabled={item.role === 'owner'}
      onPress={() => {
        if (item.role !== 'owner') {
          navigation.navigate('DeviceSharingDetail', {
            userId: item.id,
            email: item.email,
            status: item.status,
            since: item.since,
            role: item.role,
            invitationId: item.invitationId,
            hasInvitedUserId: item.hasInvitedUserId,
            onRemove,
          });
        }
      }}
    >
      <Card style={styles.row}>
        <View style={styles.leftCol}>
          {/* 🔹 Email tek satır ve taşarsa '...' ile kısalır */}
          <Text style={styles.email} numberOfLines={1} ellipsizeMode="tail">
            {item.email}
          </Text>

          {item.role !== 'owner' && (
            <Text
              style={[
                styles.status,
                item.status === 'pending'
                  ? styles.pending
                  : item.status === 'accepted' || item.status === 'active'
                  ? styles.accepted
                  : styles.rejected,
              ]}
            >
              {t(`deviceSharing.status.${item.status}`)}
            </Text>
          )}
        </View>

        <View style={styles.rightCol}>
          {item.role === 'owner' ? (
            <Text style={styles.ownerBadge}>{t('deviceSharing.owner')}</Text>
          ) : (
            <FontAwesome name="chevron-right" size={18} color={COLORS.textSoft} />
          )}
          {formattedDate && (
            <Text style={styles.since}>
              {t('deviceSharing.accessSince', { date: formattedDate })}
            </Text>
          )}
        </View>
      </Card>
    </TouchableOpacity>
  );
}

export default DeviceSharingUserItem;
