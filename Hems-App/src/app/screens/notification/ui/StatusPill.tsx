import React from 'react';
import { View, Text } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';

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
  const bg = read
    ? isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)'
    : accentSoft ?? (isDark ? 'rgba(29,209,161,0.12)' : 'rgba(29,209,161,0.10)');
  const fg = read
    ? textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280')
    : accent ?? '#1DD1A1';

  return (
    <View style={[styles.statusPill, { backgroundColor: bg }]}>
      <Text style={[styles.statusPillText, { color: fg }]}>{read ? 'Read' : 'Unread'}</Text>
    </View>
  );
}

export default StatusPill;

