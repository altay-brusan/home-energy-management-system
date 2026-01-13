import React from 'react';
import { Text, View } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

export function OfflineBanner({ styles }: { styles: Styles }) {
  const { t } = useI18n();
  
  return (
    <View style={styles.offlineBox}>
      <Text style={styles.offlineText}>{t('notifications.offline')}</Text>
    </View>
  );
}

export default OfflineBanner;

