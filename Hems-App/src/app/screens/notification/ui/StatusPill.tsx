import React from 'react';
import { View, Text } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

type Props = {
  read: boolean;
  styles: Styles;
  isDark: boolean;
  accent?: string;
  accentSoft?: string;
  textMuted?: string;
};

export function StatusPill({ read, styles, isDark, accent, accentSoft, textMuted }: Props) {
  const { t } = useI18n();

  return (
    <View style={styles.statusPill}>
      <Text style={styles.statusPillText}>{t('notifications.status.unread')}</Text>
    </View>
  );
}

export default StatusPill;

