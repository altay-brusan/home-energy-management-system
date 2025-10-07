import React from 'react';
import { Text, View } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

export function OfflineBanner({ styles }: { styles: Styles }) {
  return (
    <View style={styles.offlineBox}>
      <Text style={styles.offlineText}>Offline — showing cached notifications</Text>
    </View>
  );
}

export default OfflineBanner;

