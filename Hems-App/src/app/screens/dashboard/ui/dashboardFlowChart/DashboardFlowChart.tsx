import React from 'react';
import { View, Text, Dimensions } from 'react-native';
import Animated, { FadeInUp } from 'react-native-reanimated';
import { SPACING, TYPOGRAPHY } from '../../../../../theme/constants';
import FlowSankey, { SankeyNode, SankeyLink } from '../../../../../components/FlowSankey';
import { getDashboardFlowChartStyles } from './DashboardFlowChart.styles';

type Props = {
  COLORS: Record<string, string>;
  isDark: boolean;
};

const { width: screenWidth } = Dimensions.get('window');
const chartWidth = screenWidth - (SPACING.lg * 2) - (SPACING.md * 2);

export default function DashboardFlowChart({ COLORS, isDark }: Props) {
  const styles = getDashboardFlowChartStyles(COLORS);

  const nodes: SankeyNode[] = [
    { id: 'SOLAR', label: 'SOLAR', side: 'left' },
    { id: 'BATTERY', label: 'BATTERY', side: 'left' },
    { id: 'GRID', label: 'GRID', side: 'left' },
    { id: 'BATTERY_R', label: 'BATTERY', side: 'right' },
    { id: 'LOAD', label: 'LOAD', side: 'right' },
    { id: 'GRID_R', label: 'GRID', side: 'right' },
  ];

  const links: SankeyLink[] = [
    { source: 'GRID', target: 'BATTERY_R', value: 0.41 },
    { source: 'BATTERY', target: 'BATTERY_R', value: 0.41 },
    { source: 'GRID', target: 'LOAD', value: 0.01 },
    { source: 'BATTERY', target: 'LOAD', value: 0.0 },
  ];

  const percentByNodeId: Record<string, number> = {
    GRID: 0.9762,
    BATTERY_R: 0.9762,
    GRID_R: 0.0,
    LOAD: 0.0,
  };

  return (
    <Animated.View
      entering={FadeInUp.delay(300).duration(600)}
      style={styles.card}
    >
      <Text style={styles.title}>Energy Flow</Text>
      
      <View style={styles.chartContainer}>
        <FlowSankey
          width={chartWidth}
          height={380}
          nodes={nodes}
          links={links}
          percentByNodeId={percentByNodeId}
          theme={isDark ? 'dark' : 'light'}
          layoutOpts={{
            nodeWidth: 100,
            minHeight: 70,
            gapY: 10,
            padding: 8,
            curviness: 0.18,
            fontScale: 0.85,
          }}
        />
      </View>

      <View style={styles.legend}>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#FDB52E' }]} />
          <Text style={styles.legendText}>Solar</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#12C6A9' }]} />
          <Text style={styles.legendText}>Battery</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#9D76F2' }]} />
          <Text style={styles.legendText}>Grid</Text>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendColor, { backgroundColor: '#FF6B6B' }]} />
          <Text style={styles.legendText}>Load</Text>
        </View>
      </View>
    </Animated.View>
  );
}