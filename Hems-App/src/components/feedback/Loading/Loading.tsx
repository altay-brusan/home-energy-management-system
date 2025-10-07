import React from 'react';
import { View, ActivityIndicator, Text } from 'react-native';
import { getLoadingStyles } from './Loading.styles';

interface LoadingProps {
  message?: string;
  size?: 'small' | 'large';
  color?: string;
}

export default function Loading({
  message,
  size = 'large',
  color,
}: LoadingProps) {
  const styles = getLoadingStyles();
  return (
    <View style={styles.container}>
      <ActivityIndicator size={size} color={color ?? styles.indicatorColor} />
      {message && <Text style={styles.message}>{message}</Text>}
    </View>
  );
}
