import React from 'react';
import { Text } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

export function EmptyState({ styles }: { styles: Styles }) {
  const { t } = useI18n();
  
  return (
    <Animated.View entering={FadeInDown.duration(600)} style={styles.emptyWrap}>
      <Text style={styles.emptyIcon}>📭</Text>
      <Text style={styles.emptyText}>{t('trash.empty')}</Text>
    </Animated.View>
  );
}

