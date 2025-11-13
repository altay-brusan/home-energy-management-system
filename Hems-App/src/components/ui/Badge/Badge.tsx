import React from 'react';
import { View, Text } from 'react-native';
import { getBadgeStyles } from './Badge.styles';

interface BadgeProps {
  text: string;
  variant?: 'default' | 'primary' | 'success' | 'warning' | 'error' | 'info';
}

export default function Badge({ text, variant = 'default' }: BadgeProps) {
  const styles = getBadgeStyles();
  const textStyleKey = `${variant}Text` as keyof ReturnType<typeof getBadgeStyles>;

  return (
    <View style={[styles.badge, styles[variant]]}>
      <Text style={[styles.text, styles[textStyleKey]]}>{text}</Text>
    </View>
  );
}
