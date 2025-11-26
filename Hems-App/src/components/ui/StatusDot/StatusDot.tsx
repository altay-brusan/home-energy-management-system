import React from 'react';
import { View, StyleSheet } from 'react-native';

export type StatusDotStatus = 'online' | 'offline' | 'warning' | 'error';

interface StatusDotProps {
  status: StatusDotStatus;
  size?: number;
}

export const StatusDot: React.FC<StatusDotProps> = ({ status, size = 10 }) => {
  const getStatusColor = () => {
    switch (status) {
      case 'online':
        return '#10B981'; // Green
      case 'offline':
        return '#6B7280'; // Gray
      case 'warning':
        return '#F59E0B'; // Yellow/Orange
      case 'error':
        return '#EF4444'; // Red
      default:
        return '#6B7280';
    }
  };

  return (
    <View
      style={[
        styles.dot,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
          backgroundColor: getStatusColor(),
        },
      ]}
    />
  );
};

const styles = StyleSheet.create({
  dot: {
  },
});
