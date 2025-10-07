import React from 'react';
import { View, Text } from 'react-native';
import type { makeDeviceSharingStyles } from '../DeviceSharingScreen.styles';

type Styles = ReturnType<typeof makeDeviceSharingStyles>;

export function DeviceSharingHeader({ styles }: { styles: Styles }) {
  return (
    <View style={styles.pageScroll}>
      <Text style={styles.title}>Device Sharing & Authorization</Text>
    </View>
  );
}

export default DeviceSharingHeader;
