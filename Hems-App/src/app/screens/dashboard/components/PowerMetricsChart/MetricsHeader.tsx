import React from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import { MetricsHeaderProps } from './PowerMetricsChart.types';
import { metricsHeaderStyles as styles } from './PowerMetricsChart.styles';

const metricConfigs = [
  { key: 'solar', label: 'Solar Produced', color: '#FFD700' },
  { key: 'battery', label: 'Battery Discharge', color: '#00CED1' },
  { key: 'grid', label: 'Grid Imported', color: '#9370DB' },
  { key: 'load', label: 'Load Consumed', color: '#BA55D3' },
];

export const MetricsHeader: React.FC<MetricsHeaderProps> = ({
  currentValues,
  isLoading,
}) => {
  const getCurrentTime = () => {
    const now = new Date();
    return now.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  };

  const formatValue = (value: number): string => {
    return `${Math.abs(value).toFixed(2)} kW`;
  };

  const getMetricLabel = (key: string, value: number): string => {
    if (key === 'battery') {
      return value >= 0 ? 'Battery Discharge' : 'Battery Charge';
    }
    if (key === 'grid') {
      return value >= 0 ? 'Grid Imported' : 'Grid Exported';
    }
    return metricConfigs.find(m => m.key === key)?.label || key;
  };

  if (isLoading) {
    return (
      <View style={styles.container}>
        <ActivityIndicator size="small" color="#ffffff" />
        <Text style={styles.loadingText}>Loading metrics...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.timeLabel}>{getCurrentTime()}</Text>
      <View style={styles.metricsGrid}>
        {metricConfigs.map((metric) => {
          const value = currentValues?.[metric.key as keyof typeof currentValues] || 0;
          const label = getMetricLabel(metric.key, value as number);

          return (
            <View key={metric.key} style={styles.metricRow}>
              <View style={[styles.metricDot, { backgroundColor: metric.color }]} />
              <Text style={styles.metricLabel}>
                {label}:
              </Text>
              <Text style={styles.metricValue}>
                {formatValue(value as number)}
              </Text>
            </View>
          );
        })}
      </View>
    </View>
  );
};