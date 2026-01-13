import React from 'react';
import { Text } from 'react-native';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

type Props = {
  title: string;
  styles: Styles;
};

export function SectionHeader({ title, styles }: Props) {
  return <Text style={styles.groupHeader}>{title}</Text>;
}

export default SectionHeader;
