import React from 'react';
import { View, Text } from 'react-native';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

export function StatusPill({ styles }: { styles: Styles }) {
  const { t } = useI18n();
  
  return (
    <View style={styles.statusPill}>
      <Text style={styles.statusPillText}>{t('trash.deleted')}</Text>
    </View>
  );
}

export default StatusPill;
