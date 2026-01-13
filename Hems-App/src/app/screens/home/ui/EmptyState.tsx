import React from 'react';
import { View, Text } from 'react-native';
import { Home } from 'lucide-react-native';
import { useTranslation } from 'react-i18next';

type Props = {
  styles: any;
  COLORS: any;
};

export function EmptyState({ styles, COLORS }: Props) {
  const { t } = useTranslation();

  return (
    <View style={styles.emptyState}>
      <Home size={48} color={COLORS.textSoft} strokeWidth={1.5} />
      <Text style={styles.emptyTitle}>{t('homeEmptyState.title')}</Text>
      <Text style={styles.emptySubtitle}>{t('homeEmptyState.subtitle')}</Text>
    </View>
  );
}
