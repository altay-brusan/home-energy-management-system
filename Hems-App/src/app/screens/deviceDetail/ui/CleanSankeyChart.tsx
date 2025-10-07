import React, { useState } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import Svg, { Rect, Path, Text as SvgText, Defs, LinearGradient, Stop, Circle } from 'react-native-svg';
import {
  sankey,
  sankeyLinkHorizontal,
  SankeyNodeMinimal,
  SankeyLinkMinimal,
} from 'd3-sankey';

type CleanSankeyChartProps = {
  COLORS: {
    solar: string;
    battery: string;
    grid: string;
  };
  width?: number;
  height?: number;
  style?: any;
};

interface Node extends SankeyNodeMinimal<Node, Link> {
  name: string;
  value?: number;
  unit?: string;
}

interface Link extends SankeyLinkMinimal<Node, Link> {
  value: number;
  label?: string;
  percentage?: number;
}

const data = {
  nodes: [
    { name: "Güneş", value: 18.2, unit: "kWh" },
    { name: "Batarya", value: 12.8, unit: "kWh" },
    { name: "Şebeke", value: 8.5, unit: "kWh" },
  ] as Node[],
  links: [
    { source: 0, target: 1, value: 12.8, label: "Şarj", percentage: 70 },
    { source: 0, target: 2, value: 5.4, label: "Satış", percentage: 30 },
    { source: 1, target: 2, value: 3.1, label: "Deşarj", percentage: 24 },
  ] as Link[],
};

export default function CleanSankeyChart({
  COLORS,
  width = 380,
  height = 280,
  style,
}: CleanSankeyChartProps) {
  const [hoveredLink, setHoveredLink] = useState<number | null>(null);

  const sankeyGenerator = sankey<Node, Link>()
    .nodeWidth(30)
    .nodePadding(20)
    .extent([
      [40, 40],
      [width - 40, height - 40],
    ]);

  const { nodes, links } = sankeyGenerator({
    nodes: data.nodes.map((d) => ({ ...d })),
    links: data.links.map((d) => ({ ...d })),
  });

  return (
    <View style={[styles.container, style]}>
      <Svg width={width} height={height}>
        <Defs>
          <LinearGradient id="solarGradient" x1="0%" y1="0%" x2="100%" y2="100%">
            <Stop offset="0%" stopColor={COLORS.solar} stopOpacity="0.9" />
            <Stop offset="100%" stopColor={COLORS.solar} stopOpacity="0.7" />
          </LinearGradient>
          <LinearGradient id="batteryGradient" x1="0%" y1="0%" x2="100%" y2="100%">
            <Stop offset="0%" stopColor={COLORS.battery} stopOpacity="0.9" />
            <Stop offset="100%" stopColor={COLORS.battery} stopOpacity="0.7" />
          </LinearGradient>
          <LinearGradient id="gridGradient" x1="0%" y1="0%" x2="100%" y2="100%">
            <Stop offset="0%" stopColor={COLORS.grid} stopOpacity="0.9" />
            <Stop offset="100%" stopColor={COLORS.grid} stopOpacity="0.7" />
          </LinearGradient>
        </Defs>

        {links.map((link, i) => {
          const path: string = sankeyLinkHorizontal()(link) || "";
          const sourceName = (link.source as Node).name;
          const isHovered = hoveredLink === i;
          
          return (
            <Path
              key={`link-${i}`}
              d={path}
              stroke={
                sourceName.includes("Güneş") 
                  ? COLORS.solar
                  : sourceName.includes("Batarya")
                  ? COLORS.battery
                  : COLORS.grid
              }
              strokeWidth={Math.max(2, (link.width ?? 2) * 0.8)}
              fill="none"
              opacity={isHovered ? 0.8 : 0.4}
              onPress={() => setHoveredLink(hoveredLink === i ? null : i)}
            />
          );
        })}

        {nodes.map((node, i) => {
          const gradientId = node.name.includes("Güneş") ? "solarGradient" : 
                           node.name.includes("Batarya") ? "batteryGradient" : "gridGradient";
          
          return (
            <React.Fragment key={`node-${i}`}>
              <Rect
                x={node.x0 ?? 0}
                y={(node.y0 ?? 0)}
                width={(node.x1 ?? 0) - (node.x0 ?? 0)}
                height={(node.y1 ?? 0) - (node.y0 ?? 0)}
                fill={`url(#${gradientId})`}
                rx={8}
                stroke="rgba(255,255,255,0.2)"
                strokeWidth={1}
              />
              
              <SvgText
                x={(node.x0 ?? 0) + ((node.x1 ?? 0) - (node.x0 ?? 0)) / 2}
                y={(node.y0 ?? 0) - 10}
                fontSize="12"
                fill="rgba(255,255,255,0.9)"
                textAnchor="middle"
                alignmentBaseline="middle"
                fontWeight="600"
              >
                {node.name}
              </SvgText>
              
              <SvgText
                x={(node.x0 ?? 0) + ((node.x1 ?? 0) - (node.x0 ?? 0)) / 2}
                y={(node.y0 ?? 0) + ((node.y1 ?? 0) - (node.y0 ?? 0)) / 2 + 4}
                fontSize="10"
                fill="rgba(255,255,255,0.8)"
                textAnchor="middle"
                alignmentBaseline="middle"
                fontWeight="500"
              >
                {node.value?.toFixed(1)} {node.unit}
              </SvgText>
            </React.Fragment>
          );
        })}
      </Svg>
      
      <View style={styles.statsContainer}>
        {links.map((link, i) => (
          <View key={i} style={styles.statItem}>
            <View style={[styles.statDot, { 
              backgroundColor: i === 0 ? COLORS.solar : i === 1 ? COLORS.solar : COLORS.battery 
            }]} />
            <Text style={styles.statText}>
              {link.label}: {link.value.toFixed(1)} kWh (%{link.percentage})
            </Text>
          </View>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  statsContainer: {
    marginTop: 16,
    paddingHorizontal: 20,
  },
  statItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  statDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    marginRight: 12,
  },
  statText: {
    fontSize: 12,
    color: 'rgba(255,255,255,0.8)',
    fontWeight: '500',
  },
});

