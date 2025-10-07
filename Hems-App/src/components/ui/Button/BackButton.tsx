import React from 'react';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import MaterialIcons from '@react-native-vector-icons/material-icons';

import Button from './Button';

type Props = {
  title?: string;
  onPress?: () => void;
  variant?: 'primary' | 'secondary' | 'danger';
};

export default function BackButton({
  title = 'Back',
  onPress,
  variant = 'secondary',
}: Props) {
  const navigation = useNavigation<NativeStackNavigationProp<any>>();

  return (
    <Button
      title={title}
      variant={variant}
      onPress={onPress ? onPress : () => navigation.goBack()}
      leftIcon={<MaterialIcons name="arrow-back" size={20} color="inherit" />}
    />
  );
}
