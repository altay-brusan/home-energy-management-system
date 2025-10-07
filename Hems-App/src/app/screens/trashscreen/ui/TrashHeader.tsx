import React from 'react';
import { Pressable, Text, View } from 'react-native';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

type Props = {
  styles: Styles;
  onRestoreAll: () => void;
  onDeleteAll: () => void;
};

export function TrashHeader({ styles, onRestoreAll, onDeleteAll }: Props) {
  return (
    <View style={styles.headerRow}>
      <Text style={styles.title}>Trash Bin</Text>
      <View style={styles.headerActions}>
        <Pressable onPress={onRestoreAll} style={styles.headerBtn}>
          <Text style={styles.headerBtnText}>Restore All</Text>
        </Pressable>
        <Pressable onPress={onDeleteAll} style={styles.headerBtnDanger}>
          <Text style={styles.headerBtnDangerText}>Delete All</Text>
        </Pressable>
      </View>
    </View>
  );
}

export default TrashHeader;
