import React from 'react';
import { View, Text } from 'react-native';
import { SunInfoProps } from './PowerMetricsChart.types';
import { sunInfoStyles as styles } from './PowerMetricsChart.styles';

export const SunInfo: React.FC<SunInfoProps> = ({ sunrise, sunset }) => {
  return (
    <View style={styles.container}>
      <View style={styles.sunItem}>
        <Text style={styles.sunText}>
          Sunrise <Text style={styles.sunTime}>{sunrise}</Text>
        </Text>
      </View>
      <Text style={[styles.sunText, styles.sunDivider]}>|</Text>
      <View style={styles.sunItem}>
        <Text style={styles.sunText}>
          Sunset <Text style={styles.sunTime}>{sunset}</Text>
        </Text>
      </View>
    </View>
  );
};