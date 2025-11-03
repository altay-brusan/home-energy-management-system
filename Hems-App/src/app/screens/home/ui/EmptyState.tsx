import React from 'react';
import { View, Text } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useTranslation } from 'react-i18next';

type Props = {
  styles: any;
  COLORS: any;
};

export function EmptyState({ styles, COLORS }: Props) {
  const { t } = useTranslation();

  return (
    <View style={styles.emptyState}>
      <MaterialIcons name="home" size={64} color={COLORS.slate400} />
      <Text style={styles.emptyTitle}>{t('homeEmptyState.title')}</Text>
      <Text style={styles.emptySubtitle}>{t('homeEmptyState.subtitle')}</Text>
    </View>
  );
}
