import React from 'react';
import { TouchableOpacity, View, Text } from 'react-native';
import Card from '../../../../../components/ui/Card/Card';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import type { makeDeviceSharingStyles } from '../DeviceSharingScreen.styles';

type Styles = ReturnType<typeof makeDeviceSharingStyles>;

type SharedUser = {
  id: string;
  email: string;
  role: 'owner' | 'member';
  status: 'pending' | 'accepted' | 'rejected' | 'active' | 'inactive';
  since?: string;
};

type Props = {
  item: SharedUser;
  styles: Styles;
  COLORS: any;
  navigation: any;
  onRemove: (id: string) => void;
};

export function DeviceSharingUserItem({
  item,
  styles,
  COLORS,
  navigation,
  onRemove,
}: Props) {
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
            onRemove,
          });
        }
      }}
    >
      <Card style={styles.row}>
        <View style={styles.leftCol}>
          <Text style={styles.email}>{item.email}</Text>
          {item.role !== 'owner' && (
            <Text
              style={[
                styles.status,
                item.status === 'pending'
                  ? styles.pending
                  : item.status === 'accepted'
                  ? styles.accepted
                  : styles.rejected,
              ]}
            >
              {item.status}
            </Text>
          )}
        </View>
        <View style={styles.rightCol}>
          {item.role === 'owner' ? (
            <Text style={styles.ownerBadge}>Owner</Text>
          ) : (
            <FontAwesome name="chevron-right" size={18} color={COLORS.textSoft} />
          )}
          {item.since && (
            <Text style={styles.since}>Access since: {item.since}</Text>
          )}
        </View>
      </Card>
    </TouchableOpacity>
  );
}

export default DeviceSharingUserItem;
