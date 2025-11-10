import React from 'react';
import { Text, View } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

export function SectionHeader({ title, styles }: { title: string; styles: Styles }) {
  return (
    <View style={{ paddingHorizontal: 16 }}>
      <Text style={styles.groupHeader}>{title}</Text>
    </View>
  );
}

export default SectionHeader;

