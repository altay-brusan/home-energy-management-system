import React from 'react';
import { Pressable, Text, View } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

type Props = {
  styles: Styles;
  onBack?: () => void;
};

export function TrashHeader({ styles, onBack }: Props) {
  const { t } = useI18n();
  
  return (
    <View style={styles.headerRow}>
      <View style={styles.headerTop}>
        {onBack && (
          <Pressable onPress={onBack} style={styles.backButton}>
            <FontAwesome name="arrow-left" size={20} color={styles.backButtonText.color} />
          </Pressable>
        )}
        <Text style={styles.title}>{t('trash.title')}</Text>
      </View>
    </View>
  );
}

export default TrashHeader;
