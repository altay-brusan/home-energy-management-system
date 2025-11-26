import React from 'react';
import { Pressable, Text, View } from 'react-native';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { ArrowLeft } from 'lucide-react-native';
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
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
        {onBack && (
          <Pressable onPress={onBack} style={{ paddingVertical: 8, paddingRight: 8 }}>
            <ArrowLeft size={24} color={styles.title.color} strokeWidth={2} />
          </Pressable>
        )}
        <Text style={styles.title}>{t('trash.title')}</Text>
      </View>
      <FontAwesome name="trash-o" size={20} color={styles.title.color} />
    </View>
  );
}

export default TrashHeader;
