import React, { useState, useEffect } from "react";
import { View, StyleProp, ViewStyle, Text, Animated } from "react-native";
import Svg, { Rect, Path, Text as SvgText, Defs, LinearGradient, Stop, Circle, G } from "react-native-svg";
import {
  sankey,
  sankeyLinkHorizontal,
  SankeyNodeMinimal,
  SankeyLinkMinimal,
} from "d3-sankey";

type DeviceSankeyChartProps = {
  COLORS: {
    solar: string;
    battery: string;
    grid: string;
  };
  width?: number;   
  height?: number;  
  yOffset?: number; 
  style?: StyleProp<ViewStyle>; 
};

interface Node extends SankeyNodeMinimal<Node, Link> {
  name: string;
  value?: number;
  unit?: string;
  icon?: string;
}
interface Link extends SankeyLinkMinimal<Node, Link> {
  value: number;
  label?: string;
  percentage?: number;
}

const data = {
  nodes: [
    { name: "Güneş Paneli", value: 18.2, unit: "kWh", icon: "☀️" },  
    { name: "Batarya", value: 12.8, unit: "kWh", icon: "🔋" }, 
    { name: "Şebeke", value: 8.5, unit: "kWh", icon: "⚡" },    
  ] as Node[],
  links: [
    { source: 0, target: 1, value: 12.8, label: "Şarj", percentage: 70 }, 
    { source: 0, target: 2, value: 5.4, label: "Direkt Satış", percentage: 30 },  
    { source: 1, target: 2, value: 3.1, label: "Deşarj", percentage: 24 }, 
  ] as Link[],
};

export default function DeviceSankeyChart({
  COLORS,
  width = 380,
  height = 320,
  yOffset = 0,
  style,
}: DeviceSankeyChartProps) {
  const [hoveredLink, setHoveredLink] = useState<number | null>(null);
  const [animationValue] = useState(new Animated.Value(0));
  
  useEffect(() => {
    Animated.timing(animationValue, {
      toValue: 1,
      duration: 800,
      useNativeDriver: false,
    }).start();
  }, []);

  const sankeyGenerator = sankey<Node, Link>()
    .nodeWidth(32)
    .nodePadding(24)
    .extent([
      [40, 40],
      [width - 40, height - 80],
    ]);

  const { nodes, links } = sankeyGenerator({
    nodes: data.nodes.map((d) => ({ ...d })),
    links: data.links.map((d) => ({ ...d })),
  });

  const getNodeColor = (nodeName: string) => {
    if (nodeName.includes("Güneş")) return COLORS.solar;
    if (nodeName.includes("Batarya")) return COLORS.battery;
    return COLORS.grid;
  };

  const getLinkColor = (sourceName: string) => {
    if (sourceName.includes("Güneş")) return COLORS.solar;
    if (sourceName.includes("Batarya")) return COLORS.battery;
    return COLORS.grid;
  };

  return (
    <View style={style}>
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
          
          <LinearGradient id="linkGradient1" x1="0%" y1="0%" x2="100%" y2="0%">
            <Stop offset="0%" stopColor={COLORS.solar} stopOpacity="0.4" />
            <Stop offset="100%" stopColor={COLORS.solar} stopOpacity="0.2" />
          </LinearGradient>
          <LinearGradient id="linkGradient2" x1="0%" y1="0%" x2="100%" y2="0%">
            <Stop offset="0%" stopColor={COLORS.battery} stopOpacity="0.4" />
            <Stop offset="100%" stopColor={COLORS.battery} stopOpacity="0.2" />
          </LinearGradient>
        </Defs>

        {links.map((link, i) => {
          const path: string = sankeyLinkHorizontal()(link) || "";
          const sourceName = (link.source as Node).name;
          const isHovered = hoveredLink === i;
          
          return (
            <G key={`link-${i}`}>
              <Path
                d={path}
                stroke={
                  sourceName.includes("Güneş") 
                    ? "url(#linkGradient1)"
                    : sourceName.includes("Batarya")
                    ? "url(#linkGradient2)"
                    : COLORS.grid
                }
                strokeWidth={Math.max(3, (link.width ?? 3) * 0.8)}
                fill="none"
                opacity={isHovered ? 0.6 : 0.3}
                onPress={() => setHoveredLink(hoveredLink === i ? null : i)}
              />
              
              {isHovered && (
                <SvgText
                  x={width / 2}
                  y={((link.y0 ?? 0) + (link.y1 ?? 0)) / 2 + yOffset - 8}
                  fontSize="12"
                  fill="white"
                  textAnchor="middle"
                  alignmentBaseline="middle"
                  fontWeight="600"
                >
                  %{link.percentage}
                </SvgText>
              )}
            </G>
          );
        })}

        {nodes.map((node, i) => {
          const gradientId = node.name.includes("Güneş") ? "solarGradient" : 
                           node.name.includes("Batarya") ? "batteryGradient" : "gridGradient";
          
          return (
            <G key={`node-${i}`}>
              <Rect
                x={node.x0 ?? 0}
                y={(node.y0 ?? 0) + yOffset}
                width={(node.x1 ?? 0) - (node.x0 ?? 0)}
                height={(node.y1 ?? 0) - (node.y0 ?? 0)}
                fill={`url(#${gradientId})`}
                rx={12}
                stroke="rgba(255,255,255,0.1)"
                strokeWidth={1}
              />
              
              <SvgText
                x={(node.x0 ?? 0) + ((node.x1 ?? 0) - (node.x0 ?? 0)) / 2}
                y={(node.y0 ?? 0) + yOffset + 20}
                fontSize="16"
                textAnchor="middle"
                alignmentBaseline="middle"
              >
                {node.icon}
              </SvgText>
              
              <SvgText
                x={(node.x0 ?? 0) + ((node.x1 ?? 0) - (node.x0 ?? 0)) / 2}
                y={(node.y0 ?? 0) + yOffset + 40}
                fontSize="11"
                fill="rgba(255,255,255,0.9)"
                textAnchor="middle"
                alignmentBaseline="middle"
                fontWeight="600"
              >
                {node.name}
              </SvgText>
              
              <SvgText
                x={(node.x0 ?? 0) + ((node.x1 ?? 0) - (node.x0 ?? 0)) / 2}
                y={(node.y0 ?? 0) + yOffset + 55}
                fontSize="10"
                fill="rgba(255,255,255,0.7)"
                textAnchor="middle"
                alignmentBaseline="middle"
                fontWeight="500"
              >
                {node.value?.toFixed(1)} {node.unit}
              </SvgText>
            </G>
          );
        })}
      </Svg>
      
      <View style={{ 
        flexDirection: 'row', 
        justifyContent: 'space-between', 
        marginTop: 16,
        paddingHorizontal: 20,
        backgroundColor: 'rgba(255,255,255,0.05)',
        borderRadius: 12,
        paddingVertical: 12
      }}>
        {links.map((link, i) => (
          <View key={i} style={{ alignItems: 'center', flex: 1 }}>
            <Text style={{ 
              fontSize: 12, 
              color: 'rgba(255,255,255,0.8)',
              fontWeight: '600',
              marginBottom: 4
            }}>
              {link.label}
            </Text>
            <Text style={{ 
              fontSize: 14, 
              color: 'white',
              fontWeight: '700'
            }}>
              {link.value.toFixed(1)} kWh
            </Text>
            <Text style={{ 
              fontSize: 10, 
              color: 'rgba(255,255,255,0.6)',
              marginTop: 2
            }}>
              %{link.percentage}
            </Text>
          </View>
        ))}
      </View>
    </View>
  );
}
