import React from 'react';
import { Pressable, Text, View } from 'react-native';
import type { makeNotificationScreenStyles } from '../NotificationScreen.styles';

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
      <Text style={[styles.chipText, active && styles.chipTextActive]}>{label}</Text>
    </Pressable>
  );
}

export function FilterChips({ value, onChange, styles }: Props) {
  return (
    <View style={styles.chipsWrap}>
      <Chip active={value === 'all'} label="All" onPress={() => onChange('all')} styles={styles} />
      <Chip active={value === 'today'} label="Today" onPress={() => onChange('today')} styles={styles} />
      <Chip active={value === 'unread'} label="Unread" onPress={() => onChange('unread')} styles={styles} />
      <Chip active={value === 'critical'} label="Critical" onPress={() => onChange('critical')} styles={styles} />
    </View>
  );
}

export default FilterChips;

