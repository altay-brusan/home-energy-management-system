import React from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeNotificationScreenStyles>;

export type Filter = 'all' | 'today' | 'unread' | 'critical';

type Props = {
  value: Filter;
  onChange: (f: Filter) => void;
  styles: Styles;
};

function Chip({ active, label, onPress, styles }: { active: boolean; label: string; onPress: () => void; styles: Styles }) {
  return (
    <Pressable onPress={onPress} style={[styles.chip, active && styles.chipActive]}>
      <Text 
        style={[styles.chipText, active && styles.chipTextActive]}
        allowFontScaling={false}
      >
        {label}
      </Text>
    </Pressable>
  );
}

export function FilterChips({ value, onChange, styles }: Props) {
  const { t } = useI18n();
  
  return (
    <ScrollView 
      horizontal 
      showsHorizontalScrollIndicator={false}
      contentContainerStyle={styles.chipsWrap}
      style={styles.chipsScrollView}
    >
      <Chip active={value === 'all'} label={t('notifications.filters.all')} onPress={() => onChange('all')} styles={styles} />
      <Chip active={value === 'today'} label={t('notifications.filters.today')} onPress={() => onChange('today')} styles={styles} />
      <Chip active={value === 'unread'} label={t('notifications.filters.unread')} onPress={() => onChange('unread')} styles={styles} />
      <Chip active={value === 'critical'} label={t('notifications.filters.critical')} onPress={() => onChange('critical')} styles={styles} />
    </ScrollView>
  );
}

export default FilterChips;

