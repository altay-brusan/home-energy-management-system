import React from "react";
import { View, StyleProp, ViewStyle } from "react-native";
import Svg, { Rect, Path, Text as SvgText } from "react-native-svg";
import {
  sankey,
  sankeyLinkHorizontal,
  SankeyNodeMinimal,
  SankeyLinkMinimal,
} from "d3-sankey";

type SankeyChartProps = {
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
}
interface Link extends SankeyLinkMinimal<Node, Link> {
  value: number;
}

const data = {
  nodes: [
    { name: "SOLAR" },  
    { name: "BATTERY" }, 
    { name: "GRID" },    
  ] as Node[],
  links: [
    { source: 0, target: 1, value: 17.64 }, 
    { source: 0, target: 2, value: 0.31 },  
    { source: 1, target: 2, value: 4.47 }, 
  ] as Link[],
};

export default function SankeyChart({
  COLORS,
  width = 380,
  height = 320,
  yOffset = 0,
  style,
}: SankeyChartProps) {
  const sankeyGenerator = sankey<Node, Link>()
    .nodeWidth(20)
    .nodePadding(20)
    .extent([
      [0, 0],
      [width - 30, height - 20],
    ]);

  const { nodes, links } = sankeyGenerator({
    nodes: data.nodes.map((d) => ({ ...d })),
    links: data.links.map((d) => ({ ...d })),
  });

  return (
    <View style={style}>
      <Svg width={width} height={height}>
        {links.map((link, i) => {
          const path: string = sankeyLinkHorizontal()(link) || "";
          return (
            <Path
              key={`link-${i}`}
              d={path}
              stroke={
                (link.source as Node).name === "SOLAR"
                  ? COLORS.solar
                  : (link.source as Node).name === "BATTERY"
                  ? COLORS.battery
                  : COLORS.grid
              }
              strokeWidth={Math.max(1, link.width ?? 1)}
              fill="none"
              opacity={0.35}
            />
          );
        })}

        {nodes.map((node, i) => (
          <React.Fragment key={`node-${i}`}>
            <Rect
              x={node.x0 ?? 0}
              y={(node.y0 ?? 0) + yOffset}
              width={(node.x1 ?? 0) - (node.x0 ?? 0)}
              height={(node.y1 ?? 0) - (node.y0 ?? 0)}
              fill={
                node.name === "SOLAR"
                  ? COLORS.solar
                  : node.name === "BATTERY"
                  ? COLORS.battery
                  : COLORS.grid
              }
              rx={6}
            />
            <SvgText
              x={(node.x0 ?? 0) - 5}
              y={((node.y0 ?? 0) + (node.y1 ?? 0)) / 2 + yOffset}
              fontSize="12"
              fill="white"
              textAnchor="end"
              alignmentBaseline="middle"
            >
              {node.name}
            </SvgText>
          </React.Fragment>
        ))}
      </Svg>
    </View>
  );
}
