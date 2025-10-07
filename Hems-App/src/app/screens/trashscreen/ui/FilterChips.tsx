import React from 'react';
import { Pressable, Text, View } from 'react-native';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';

type Styles = ReturnType<typeof makeTrashScreenStyles>;
type Filter = 'all' | 'today' | 'critical';

type Props = {
  value: Filter;
  onChange: (f: Filter) => void;
  styles: Styles;
};

export function FilterChips({ value, onChange, styles }: Props) {
  return (
    <View style={styles.filterRow}>
      <Pressable
        onPress={() => onChange('all')}
        style={[styles.chip, value === 'all' && styles.chipActive]}
      >
        <Text style={[styles.chipText, value === 'all' && styles.chipTextActive]}>All</Text>
      </Pressable>
      <Pressable
        onPress={() => onChange('today')}
        style={[styles.chip, value === 'today' && styles.chipActive]}
      >
        <Text style={[styles.chipText, value === 'today' && styles.chipTextActive]}>Today</Text>
      </Pressable>
      <Pressable
        onPress={() => onChange('critical')}
        style={[styles.chip, value === 'critical' && styles.chipActive]}
      >
        <Text style={[styles.chipText, value === 'critical' && styles.chipTextActive]}>
          Critical
        </Text>
      </Pressable>
    </View>
  );
}

