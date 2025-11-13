import React from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import { ChartLegendProps } from './PowerMetricsChart.types';
import { chartLegendStyles as styles, legendDynamicStyles } from './PowerMetricsChart.styles';

export const ChartLegend: React.FC<ChartLegendProps> = ({
  metrics,
  onToggleMetric,
}) => {
  return (
    <View style={styles.container}>
      <View style={styles.legendGrid}>
        {metrics.map((metric) => (
          <TouchableOpacity
            key={metric.key}
            style={styles.legendItem}
            onPress={() => onToggleMetric(metric.key)}
            activeOpacity={0.7}
          >
            <View
              style={[
                styles.legendCheckbox,
                { borderColor: metric.color },
              ]}
            >
              {metric.visible && (
                <View
                  style={[
                    styles.legendCheckmark,
                    { backgroundColor: metric.color },
                  ]}
                />
              )}
            </View>
            <View
              style={[
                styles.legendDot,
                metric.visible
                  ? { backgroundColor: metric.color }
                  : legendDynamicStyles.legendDotInactive,
              ]}
            />
            <Text
              style={[
                styles.legendLabel,
                metric.visible
                  ? legendDynamicStyles.legendLabelActive
                  : legendDynamicStyles.legendLabelInactive,
              ]}
            >
              {metric.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
};