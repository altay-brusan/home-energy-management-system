import React, { useState } from 'react';
import { RefreshControl, View, Text, FlatList } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { makeUserDetailStyles } from './DeviceSharingDetail.styles';
import Card from '../../../../../components/ui/Card/Card';
import Button from '../../../../../components/ui/Button/Button';
import { useNetInfo } from '../../../../../hooks/useNetInfo';

type Props = {
  route: any;
  navigation: any;
};

export default function DeviceSharingDetailScreen({ route, navigation }: Props) {
  const { userId, email, status, since, active, onRemove } = route.params;
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS: Record<string, string> = getThemeColors(isDark);
  const styles = makeUserDetailStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { isConnected } = useNetInfo();

  const [refreshing, setRefreshing] = useState(false);

  const logs = [
    { id: '1', text: `Invitation email sent on ${since}` },
    { id: '2', text: `Status changed to ${status} on ${since}` },
    { id: '4', text: `Access ${active ? 'granted' : 'revoked'} on ${since}` },
     { id: '5', text: `Access ${active ? 'granted' : 'revoked'} on ${since}` },
      { id: '6', text: `Access ${active ? 'granted' : 'revoked'} on ${since}` },
       { id: '7', text: `Access ${active ? 'granted' : 'revoked'} on ${since}` },
     

  
  ];

  const handleRemove = () => {
    if (onRemove) {
      onRemove(userId);
    }
    navigation.goBack();
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
        <Text style={styles.title}>User Detail</Text>

        <Card>
          <Text style={styles.label}>Email:</Text>
          <Text style={styles.value}>{email}</Text>

          <Text style={styles.label}>Access Since:</Text>
          <Text style={styles.value}>{since}</Text>

          <Text style={styles.label}>Active:</Text>
          <Text style={styles.value}>{active ? 'Yes' : 'No'}</Text>

          <Text style={styles.label}>Status:</Text>
          <Text style={styles.value}>{status}</Text>
        </Card>

        <View style={styles.buttonRow}>
          <Button
            title="Remove User"
            style={styles.removeBtn}
            textStyle={styles.removeBtnText}
            onPress={handleRemove}
          />
          <Button
            title="Cancel"
            style={styles.cancelBtn}
            textStyle={styles.cancelBtnText}
            onPress={() => navigation.goBack()}
          />
        </View>

        <Text style={styles.subtitle}>Activity Log</Text>
      </View>
    }
    showsVerticalScrollIndicator
  />
);

}
