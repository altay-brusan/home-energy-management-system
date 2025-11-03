import React from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';
import { useTranslation } from 'react-i18next';

type Styles = ReturnType<typeof makeTrashScreenStyles>;
type Filter = 'all' | 'today' | 'critical';

type Props = {
  value: Filter;
  onChange: (f: Filter) => void;
  styles: Styles;
};

export function FilterChips({ value, onChange, styles }: Props) {
  const { t } = useTranslation();
  return (
    <ScrollView 
      horizontal 
      showsHorizontalScrollIndicator={false}
      contentContainerStyle={styles.filterRow}
      style={styles.chipsScrollView}
    >
      <Pressable
        onPress={() => onChange('all')}
        style={[styles.chip, value === 'all' && styles.chipActive]}
      >
        <Text style={[styles.chipText, value === 'all' && styles.chipTextActive]}>
          {t('trash.filters.all')}
        </Text>
      </Pressable>
      <Pressable
        onPress={() => onChange('today')}
        style={[styles.chip, value === 'today' && styles.chipActive]}
      >
        <Text style={[styles.chipText, value === 'today' && styles.chipTextActive]}>
          {t('trash.filters.today')}
        </Text>
      </Pressable>
      <Pressable
        onPress={() => onChange('critical')}
        style={[styles.chip, value === 'critical' && styles.chipActive]}
      >
        <Text style={[styles.chipText, value === 'critical' && styles.chipTextActive]}>
          {t('trash.filters.critical')}
        </Text>
      </Pressable>
    </ScrollView>
  );
}

