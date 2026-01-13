import React from 'react';
import { View, Text } from 'react-native';
import type { makeDeviceSharingStyles } from '../DeviceSharingScreen.styles';
import { useI18n } from '../../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeDeviceSharingStyles>;

export function DeviceSharingHeader({ styles }: { styles: Styles }) {
  const { t } = useI18n();
  
  return (
    <View style={styles.pageScroll}>
      <Text style={styles.title}>{t('deviceSharing.title')}</Text>
    </View>
  );
}

export default DeviceSharingHeader;
