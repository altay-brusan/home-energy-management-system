import React from 'react';
import { Text, View } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

export function SectionHeader({ title, styles }: { title: string; styles: Styles }) {
  return (
    <Text style={styles.groupHeader}>{title}</Text>
  );
}

export default SectionHeader;

