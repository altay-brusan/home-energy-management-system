import React from 'react';
import { View, Text } from 'react-native';
import { getBadgeStyles } from './Badge.styles';

interface BadgeProps {
  text: string;
  variant?: 'success' | 'warning' | 'error' | 'info';
}

export default function Badge({ text, variant = 'info' }: BadgeProps) {
  const styles = getBadgeStyles();
  return (
    <View style={[styles.badge, styles[variant]]}>
      <Text style={styles.text}>{text}</Text>
    </View>
  );
}
