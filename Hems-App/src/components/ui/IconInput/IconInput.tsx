import React, { ReactNode } from 'react';
import { View, TextInput, TextInputProps } from 'react-native';
import { getIconInputStyles } from './IconInput.styles';

type Props = TextInputProps & {
  containerStyle?: any;
  left?: ReactNode;
  right?: ReactNode;
};

export default function IconInput({ containerStyle, left, right, style, ...rest }: Props) {
  const styles = getIconInputStyles();
  return (
    <View style={[styles.wrap, containerStyle]}>
      {left && <View style={styles.side}>{left}</View>}
      <TextInput
        {...rest}
        style={[styles.input, style]}
        placeholderTextColor="#7a8a9a"
      />
      {right && <View style={styles.side}>{right}</View>}
    </View>
  );
}
