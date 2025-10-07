import React, { useMemo, useState, useCallback, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  StyleProp,
  ViewStyle,
  Pressable,
  useWindowDimensions,
  LayoutChangeEvent,
} from 'react-native';
import Svg, {
  Defs,
  LinearGradient,
  Stop,
  Rect,
  Path,
  Text as SvgText,
  G,
  Circle,
} from 'react-native-svg';
import {
  sankey,
  sankeyLinkHorizontal,
  SankeyNodeMinimal,
  SankeyLinkMinimal,
} from 'd3-sankey';

type Theme = Record<string, string>;
type Palette = { solar: string; battery: string; grid: string };

type SankeyNodeInput = {
  name: string;
  value?: number;
  unit?: string;
  icon?: string;
  subtitle?: string;
  color?: string;
};

type SankeyLinkInput = {
  source: number | string;
  target: number | string;
  value: number;
  label?: string;
  percentage?: number;
  color?: string;
};

type SankeyNodeExtended = SankeyNodeMinimal<SankeyNodeExtended, SankeyLinkExtended> & SankeyNodeInput;
type SankeyLinkExtended = SankeyLinkMinimal<SankeyNodeExtended, SankeyLinkExtended> & SankeyLinkInput;

type SankeyDataInput = {
  nodes: SankeyNodeInput[];
  links: SankeyLinkInput[];
};

type ElegantSankeyChartProps = {
  theme: Theme;
  palette: Palette;
  width?: number;
  height?: number;
  data?: SankeyDataInput;
  style?: StyleProp<ViewStyle>;
};

const MIN_CHART_WIDTH = 200;
const MAX_CHART_WIDTH = 680;
const MIN_CHART_HEIGHT = 220;
const MAX_CHART_HEIGHT = 420;
const SIDE_PADDING_GUESS = 64;

function clampWidth(value: number) {
  if (!Number.isFinite(value)) return MIN_CHART_WIDTH;
  return Math.min(Math.max(value, MIN_CHART_WIDTH), MAX_CHART_WIDTH);
}

function clampHeight(value: number) {
  if (!Number.isFinite(value)) return MIN_CHART_HEIGHT;
  return Math.min(Math.max(value, MIN_CHART_HEIGHT), MAX_CHART_HEIGHT);
}

function hexToRgb(hex: string) {
  if (!hex || typeof hex !== 'string') return null;
  const normalized = hex.replace('#', '');
  const full = normalized.length === 3
    ? normalized
        .split('')
        .map(char => char + char)
        .join('')
    : normalized;
  if (full.length !== 6) return null;
  const int = Number.parseInt(full, 16);
  if (Number.isNaN(int)) return null;
  return {
    r: (int >> 16) & 255,
    g: (int >> 8) & 255,
    b: int & 255,
  };
}

function mixColor(base: string, target: string, amount: number) {
  const baseRgb = hexToRgb(base);
  const targetRgb = hexToRgb(target);
  if (!baseRgb || !targetRgb) return base;
  const weight = Math.max(0, Math.min(1, amount));
  const r = Math.round(baseRgb.r * (1 - weight) + targetRgb.r * weight);
  const g = Math.round(baseRgb.g * (1 - weight) + targetRgb.g * weight);
  const b = Math.round(baseRgb.b * (1 - weight) + targetRgb.b * weight);
  return `rgb(${r}, ${g}, ${b})`;
}

function lightenColor(color: string, amount = 0.2) {
  return mixColor(color, '#ffffff', amount);
}

function darkenColor(color: string, amount = 0.2) {
  return mixColor(color, '#000000', amount);
}

function toRgba(color: string, alpha: number) {
  if (!color) return color;
  const clamp = Math.max(0, Math.min(1, alpha));
  const hex = hexToRgb(color);
  if (hex) {
    return `rgba(${hex.r}, ${hex.g}, ${hex.b}, ${clamp})`;
  }

  if (color.startsWith('rgba(') || color.startsWith('rgb(')) {
    const numeric = color.replace(/rgba?\(/, '').replace(')', '').split(',');
    if (numeric.length >= 3) {
      const r = Number(numeric[0].trim());
      const g = Number(numeric[1].trim());
      const b = Number(numeric[2].trim());
      if ([r, g, b].every(val => Number.isFinite(val))) {
        return `rgba(${r}, ${g}, ${b}, ${clamp})`;
      }
    }
  }

  return color;
}

function resolveColorByName(name: string, palette: Palette) {
  const normalized = name?.toLowerCase() ?? '';
  if (
    normalized.includes('güne') ||
    normalized.includes('solar') ||
    normalized.includes('sun') ||
    normalized.includes('pv')
  ) {
    return palette.solar;
  }
  if (
    normalized.includes('bat') ||
    normalized.includes('battery') ||
    normalized.includes('storage')
  ) {
    return palette.battery;
  }
  if (
    normalized.includes('grid') ||
    normalized.includes('şeb') ||
    normalized.includes('seb') ||
    normalized.includes('network')
  ) {
    return palette.grid;
  }
  return palette.grid;
}

function formatEnergy(value: number) {
  if (!Number.isFinite(value)) return '-';
  if (value >= 100) return `${value.toFixed(0)} kWh`;
  if (value >= 10) return `${value.toFixed(1)} kWh`;
  return `${value.toFixed(2)} kWh`;
}

const defaultData = (palette: Palette): SankeyDataInput => ({
  nodes: [
    {
      name: 'Güneş Paneli',
      icon: '☀️',
      value: 18.2,
      unit: 'kWh',
      subtitle: 'Üretim',
      color: palette.solar,
    },
    {
      name: 'Batarya',
      icon: '🔋',
      value: 12.8,
      unit: 'kWh',
      subtitle: 'Depolama',
      color: palette.battery,
    },
    {
      name: 'Şebeke',
      icon: '⚡',
      value: 8.5,
      unit: 'kWh',
      subtitle: 'Tüketim',
      color: palette.grid,
    },
  ],
  links: [
    {
      source: 0,
      target: 1,
      value: 12.8,
      label: 'Şarj',
      percentage: 70,
      color: palette.solar,
    },
    {
      source: 0,
      target: 2,
      value: 5.4,
      label: 'Satış',
      percentage: 30,
      color: palette.solar,
    },
    {
      source: 1,
      target: 2,
      value: 3.1,
      label: 'Deşarj',
      percentage: 24,
      color: palette.battery,
    },
  ],
});

const createStyles = () =>
  StyleSheet.create({
    container: {
      width: '100%',
      alignItems: 'center',
    },
    svgWrapper: {
      width: '100%',
      alignItems: 'center',
      maxWidth: '100%',
    },
    statsRow: {
      width: '100%',
      flexDirection: 'row',
      alignItems: 'stretch',
      borderRadius: 18,
      borderWidth: 1,
      marginTop: 20,
      overflow: 'hidden',
    },
    statItem: {
      flex: 1,
      paddingVertical: 12,
      paddingHorizontal: 12,
      justifyContent: 'center',
      alignItems: 'flex-start',
    },
    statHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 6,
    },
    statBullet: {
      width: 10,
      height: 10,
      borderRadius: 5,
      marginRight: 8,
    },
    statLabel: {
      fontSize: 11,
      fontWeight: '600',
    },
    statValue: {
      fontSize: 14,
      fontWeight: '700',
    },
    statPercentage: {
      fontSize: 11,
      fontWeight: '500',
      marginTop: 2,
    },
  });

const linkPathGenerator = sankeyLinkHorizontal<SankeyNodeExtended, SankeyLinkExtended>();

const ElegantSankeyChart = ({
  theme,
  palette,
  width,
  height,
  data,
  style,
}: ElegantSankeyChartProps) => {
  const styles = useMemo(() => createStyles(), []);
  const windowWidth = useWindowDimensions().width;
  const [containerWidth, setContainerWidth] = useState<number>(
    clampWidth(width ?? windowWidth - SIDE_PADDING_GUESS),
  );

  useEffect(() => {
    if (width) {
      setContainerWidth(clampWidth(width));
      return;
    }
    const next = clampWidth(windowWidth - SIDE_PADDING_GUESS);
    setContainerWidth(prev => (Math.abs(prev - next) > 2 ? next : prev));
  }, [width, windowWidth]);

  const handleLayout = useCallback(
    (event: LayoutChangeEvent) => {
      if (width) return;
      const nextMeasured = Math.round(event.nativeEvent.layout.width);
      if (!nextMeasured) return;
      const clamped = clampWidth(nextMeasured);
      setContainerWidth(prev => (Math.abs(prev - clamped) > 2 ? clamped : prev));
    },
    [width],
  );

  const chartWidth = clampWidth(width ?? containerWidth);
  const chartHeight = useMemo(() => {
    if (height) {
      return clampHeight(height);
    }
    const responsive = chartWidth * 0.68;
    return clampHeight(responsive);
  }, [chartWidth, height]);

  const horizontalMargin = Math.min(
    chartWidth / 3,
    Math.max(24, chartWidth * 0.08),
  );
  const marginTop = Math.max(28, chartHeight * 0.14);
  const marginBottom = Math.max(48, chartHeight * 0.22);
  const marginLeft = horizontalMargin;
  const marginRight = horizontalMargin;

  const textColor = theme.textSoft ?? '#ffffff';
  const secondaryText = theme.slate400 ?? toRgba(textColor, 0.68);

  const dataset = useMemo(() => {
    const base = data ?? defaultData(palette);
    const nodes = base.nodes.map(node => {
      const color = node.color ?? resolveColorByName(node.name, palette);
      return { ...node, color };
    });
    const nameToIndex = new Map<string, number>();
    nodes.forEach((node, index) => nameToIndex.set(node.name, index));

    const links = base.links.map(link => {
      const next: SankeyLinkInput = { ...link };
      if (typeof next.source === 'string' && nameToIndex.has(next.source)) {
        next.source = nameToIndex.get(next.source)!;
      }
      if (typeof next.target === 'string' && nameToIndex.has(next.target)) {
        next.target = nameToIndex.get(next.target)!;
      }
      if (!next.color && typeof next.source === 'number') {
        const sourceNode = nodes[next.source];
        if (sourceNode?.color) {
          next.color = sourceNode.color;
        }
      }
      return next;
    });

    return { nodes, links };
  }, [data, palette]);

  const nodeWidthValue = Math.round(Math.min(60, Math.max(34, chartWidth * 0.11)));
  const nodePaddingValue = Math.round(
    Math.min(48, Math.max(20, chartHeight * 0.12)),
  );

  const sankeyGenerator = useMemo(
    () =>
      sankey<SankeyNodeExtended, SankeyLinkExtended>()
        .nodeWidth(nodeWidthValue)
        .nodePadding(nodePaddingValue)
        .extent([
          [marginLeft, marginTop],
          [
            Math.max(chartWidth - marginRight, marginLeft + nodeWidthValue * 2),
            Math.max(
              chartHeight - marginBottom,
              marginTop + nodePaddingValue * 2,
            ),
          ],
        ]),
    [
      chartWidth,
      chartHeight,
      marginLeft,
      marginRight,
      marginTop,
      marginBottom,
      nodeWidthValue,
      nodePaddingValue,
    ],
  );

  const { nodes, links } = useMemo(() => {
    const nodeCopy = dataset.nodes.map(node => ({ ...node })) as unknown as SankeyNodeExtended[];
    const linkCopy = dataset.links.map(link => ({ ...link })) as unknown as SankeyLinkExtended[];
    const graph = sankeyGenerator({ nodes: nodeCopy, links: linkCopy });
    return {
      nodes: graph.nodes,
      links: graph.links,
    };
  }, [sankeyGenerator, dataset]);

  const [activeLinkIndex, setActiveLinkIndex] = useState<number | null>(null);

  const activeNodeIndices = useMemo(() => {
    if (activeLinkIndex === null) return null;
    const link = links[activeLinkIndex];
    if (!link) return null;
    const set = new Set<number>();
    const sourceIndex = (link.source as SankeyNodeExtended)?.index;
    const targetIndex = (link.target as SankeyNodeExtended)?.index;
    if (typeof sourceIndex === 'number') set.add(sourceIndex);
    if (typeof targetIndex === 'number') set.add(targetIndex);
    return set;
  }, [activeLinkIndex, links]);

  const linkGradientMap = useMemo(() => {
    const map = new Map<string, { id: string; start: string; end: string }>();
    let counter = 0;
    links.forEach(link => {
      const baseColor =
        link.color ?? resolveColorByName((link.source as SankeyNodeExtended)?.name ?? '', palette);
      if (!map.has(baseColor)) {
        map.set(baseColor, {
          id: `link-gradient-${counter}`,
          start: lightenColor(baseColor, 0.25),
          end: darkenColor(baseColor, 0.1),
        });
        counter += 1;
      }
    });
    return map;
  }, [links, palette]);

  const totalFlow = useMemo(
    () => links.reduce((sum, link) => sum + (link.value ?? 0), 0),
    [links],
  );

  const stats = useMemo(
    () =>
      links.map((link, idx) => {
        const source = link.source as SankeyNodeExtended;
        const target = link.target as SankeyNodeExtended;
        const baseColor = link.color ?? resolveColorByName(source?.name ?? '', palette);
        const label = link.label ?? `${source?.name ?? ''} → ${target?.name ?? ''}`;
        const percentage =
          typeof link.percentage === 'number'
            ? link.percentage
            : totalFlow > 0
            ? Math.round((link.value / totalFlow) * 100)
            : undefined;
        return {
          id: idx,
          label,
          color: baseColor,
          value: link.value,
          displayValue: formatEnergy(link.value),
          percentage,
        };
      }),
    [links, totalFlow, palette],
  );

  const backgroundPadding = 24;
  const backgroundWidth = Math.max(
    0,
    chartWidth - (marginLeft + marginRight) + backgroundPadding * 2,
  );
  const backgroundHeight = Math.max(
    0,
    chartHeight - (marginTop + marginBottom) + backgroundPadding * 2,
  );
  const backgroundX = Math.max(0, marginLeft - backgroundPadding);
  const backgroundY = Math.max(0, marginTop - backgroundPadding);

  const cardBackground = theme.cardBg ?? theme.surface ?? '#232b36';
  const cardBorder = theme.border ?? '#2d3748';

  const stackStats = chartWidth < 340;

  return (
    <View style={[styles.container, style]} onLayout={handleLayout}>
      <View style={[styles.svgWrapper, { width: chartWidth }]}>
        <Svg width={chartWidth} height={chartHeight}>
          <Defs>
            <LinearGradient id="chartBackground" x1="0%" y1="0%" x2="100%" y2="0%">
              <Stop offset="0%" stopColor={palette.solar} stopOpacity="0.1" />
              <Stop offset="50%" stopColor={palette.battery} stopOpacity="0.08" />
              <Stop offset="100%" stopColor={palette.grid} stopOpacity="0.08" />
            </LinearGradient>
            {nodes.map((node, idx) => {
              const baseColor = node.color ?? resolveColorByName(node.name, palette);
              return (
                <LinearGradient
                  key={`node-gradient-${idx}`}
                  id={`node-gradient-${idx}`}
                  x1="0%"
                  y1="0%"
                  x2="0%"
                  y2="100%"
                >
                  <Stop offset="0%" stopColor={lightenColor(baseColor, 0.28)} stopOpacity="1" />
                  <Stop offset="100%" stopColor={darkenColor(baseColor, 0.15)} stopOpacity="1" />
                </LinearGradient>
              );
            })}
            {Array.from(linkGradientMap.entries()).map(([, info]) => (
              <LinearGradient key={info.id} id={info.id} x1="0%" y1="0%" x2="100%" y2="0%">
                <Stop offset="0%" stopColor={info.start} stopOpacity="0.9" />
                <Stop offset="100%" stopColor={info.end} stopOpacity="0.55" />
              </LinearGradient>
            ))}
          </Defs>

          {backgroundWidth > 0 && backgroundHeight > 0 && (
            <Rect
              x={backgroundX}
              y={backgroundY}
              width={backgroundWidth}
              height={backgroundHeight}
              rx={28}
              fill="url(#chartBackground)"
              stroke={toRgba(cardBorder, 0.6)}
              strokeWidth={1}
            />
          )}

          {links.map((link, idx) => {
            const path = linkPathGenerator(link) || '';
            const source = link.source as SankeyNodeExtended;
            const target = link.target as SankeyNodeExtended;
            const baseColor =
              link.color ?? resolveColorByName(source?.name ?? '', palette);
            const gradientInfo = linkGradientMap.get(baseColor);
            const isActive = activeLinkIndex === idx;
            const dimmed = activeLinkIndex !== null && !isActive;
            const mainOpacity = dimmed ? 0.2 : isActive ? 0.98 : 0.7;
            const overlayOpacity = dimmed ? 0.05 : isActive ? 0.6 : 0.25;
            const thickness = Math.max(6, (link.width ?? 6) * 0.9);
            const overlayThickness = Math.max(2, thickness * 0.45);
            const startX = source?.x1 ?? 0;
            const startY = (link.y0 ?? 0) + thickness / 2;
            const endX = target?.x0 ?? 0;
            const endY = (link.y1 ?? 0) + thickness / 2;
            const midX = (startX + endX) / 2;
            const midY = ((link.y0 ?? 0) + (link.y1 ?? 0) + thickness) / 2;

            return (
              <G key={`link-${idx}`}>
                <Path
                  d={path}
                  stroke={
                    gradientInfo ? `url(#${gradientInfo.id})` : toRgba(baseColor, mainOpacity)
                  }
                  strokeWidth={thickness}
                  strokeLinecap="round"
                  opacity={mainOpacity}
                  onPress={() =>
                    setActiveLinkIndex(current => (current === idx ? null : idx))
                  }
                />
                <Path
                  d={path}
                  stroke={toRgba(lightenColor(baseColor, 0.1), 0.6)}
                  strokeWidth={overlayThickness}
                  strokeLinecap="round"
                  opacity={overlayOpacity}
                  pointerEvents="none"
                />
                <Circle
                  cx={startX}
                  cy={startY}
                  r={3}
                  fill={toRgba(baseColor, isActive ? 0.9 : 0.4)}
                  pointerEvents="none"
                />
                <Circle
                  cx={endX}
                  cy={endY}
                  r={3}
                  fill={toRgba(baseColor, isActive ? 0.9 : 0.4)}
                  pointerEvents="none"
                />
                {isActive && (
                  <>
                    <SvgText
                      x={midX}
                      y={midY - 12}
                      fontSize="12"
                      fontWeight="600"
                      fill={toRgba(textColor, 0.92)}
                      textAnchor="middle"
                      alignmentBaseline="middle"
                    >
                      {link.label ?? `${source?.name ?? ''} → ${target?.name ?? ''}`}
                    </SvgText>
                    <SvgText
                      x={midX}
                      y={midY + 6}
                      fontSize="11"
                      fill={toRgba(secondaryText, 0.85)}
                      textAnchor="middle"
                      alignmentBaseline="middle"
                    >
                      {`${link.value.toFixed(1)} kWh${
                        typeof link.percentage === 'number' ? ` • %${link.percentage}` : ''
                      }`}
                    </SvgText>
                  </>
                )}
              </G>
            );
          })}

          {nodes.map((node, idx) => {
            const baseColor = node.color ?? resolveColorByName(node.name, palette);
            const nodeIndex = typeof node.index === 'number' ? node.index : idx;
            const isDimmed = activeNodeIndices && !activeNodeIndices.has(nodeIndex);
            const opacity = isDimmed ? 0.45 : 1;
            const x0 = node.x0 ?? 0;
            const x1 = node.x1 ?? 0;
            const y0 = node.y0 ?? 0;
            const y1 = node.y1 ?? 0;
            const nodeWidth = Math.max(0, x1 - x0);
            const nodeHeight = Math.max(0, y1 - y0);
            const centerX = x0 + nodeWidth / 2;
            const iconY = y0 + Math.min(28, nodeHeight / 3);
            const valueY = y0 + nodeHeight / 2 + 6;

            return (
              <G key={`node-${idx}`} opacity={opacity}>
                <Rect
                  x={x0 - 10}
                  y={y0 - 8}
                  width={nodeWidth + 20}
                  height={nodeHeight + 16}
                  rx={18}
                  fill={toRgba(baseColor, 0.12)}
                  pointerEvents="none"
                />
                <Rect
                  x={x0}
                  y={y0}
                  width={nodeWidth}
                  height={nodeHeight}
                  rx={16}
                  fill={`url(#${`node-gradient-${idx}`})`}
                  stroke={toRgba(baseColor, 0.55)}
                  strokeWidth={1.2}
                />
                <SvgText
                  x={centerX}
                  y={y0 - 14}
                  fontSize="12"
                  fontWeight="700"
                  fill={textColor}
                  textAnchor="middle"
                  alignmentBaseline="middle"
                >
                  {node.name}
                </SvgText>
                {node.subtitle && (
                  <SvgText
                    x={centerX}
                    y={y0 - 2}
                    fontSize="10"
                    fill={secondaryText}
                    textAnchor="middle"
                    alignmentBaseline="middle"
                  >
                    {node.subtitle}
                  </SvgText>
                )}
                {node.icon && (
                  <SvgText
                    x={centerX}
                    y={iconY}
                    fontSize="20"
                    textAnchor="middle"
                    alignmentBaseline="middle"
                  >
                    {node.icon}
                  </SvgText>
                )}
                {typeof node.value === 'number' && (
                  <SvgText
                    x={centerX}
                    y={valueY}
                    fontSize="12"
                    fontWeight="700"
                    fill={textColor}
                    textAnchor="middle"
                    alignmentBaseline="middle"
                  >
                    {`${node.value.toFixed(1)} ${node.unit ?? ''}`.trim()}
                  </SvgText>
                )}
                {node.unit && (
                  <SvgText
                    x={centerX}
                    y={valueY + 18}
                    fontSize="10"
                    fill={secondaryText}
                    textAnchor="middle"
                    alignmentBaseline="middle"
                  >
                    {node.unit}
                  </SvgText>
                )}
              </G>
            );
          })}
        </Svg>
      </View>

      {stats.length > 0 && (
        <View
          style={[
            styles.statsRow,
            {
              backgroundColor: cardBackground,
              borderColor: toRgba(cardBorder, 0.5),
              flexDirection: stackStats ? 'column' : 'row',
            },
          ]}
        >
          {stats.map((stat, idx) => (
            <Pressable
              key={stat.id}
              onPress={() =>
                setActiveLinkIndex(current => (current === stat.id ? null : stat.id))
              }
              style={({ pressed }) => {
                const baseStyles: Array<StyleProp<ViewStyle>> = [styles.statItem];

                if (!stackStats && idx < stats.length - 1) {
                  baseStyles.push({
                    borderRightWidth: 1,
                    borderRightColor: toRgba(cardBorder, 0.35),
                  });
                }

                if (stackStats) {
                  baseStyles.push({
                    width: '100%',
                    borderBottomWidth: idx < stats.length - 1 ? 1 : 0,
                    borderBottomColor: toRgba(cardBorder, 0.35),
                    alignItems: 'center',
                    paddingVertical: 16,
                  });
                }
                if (!stackStats) {
                  baseStyles.push({ alignItems: 'flex-start' });
                }

                if (activeLinkIndex === stat.id || pressed) {
                  baseStyles.push({
                    backgroundColor: toRgba(stat.color, stackStats ? 0.12 : 0.16),
                  });
                }

                return baseStyles;
              }}
            >
              <View
                style={[
                  styles.statHeader,
                  stackStats && { justifyContent: 'center' },
                ]}
              >
                <View
                  style={[
                    styles.statBullet,
                    { backgroundColor: toRgba(stat.color, 0.9) },
                  ]}
                />
                <Text
                  style={[
                    styles.statLabel,
                    { color: textColor, textAlign: stackStats ? 'center' : 'left' },
                  ]}
                >
                  {stat.label}
                </Text>
              </View>
              <Text
                style={[
                  styles.statValue,
                  { color: textColor, textAlign: stackStats ? 'center' : 'left' },
                ]}
              >
                {stat.displayValue}
              </Text>
              {stat.percentage !== undefined && (
                <Text
                  style={[
                    styles.statPercentage,
                    {
                      color: secondaryText,
                      textAlign: stackStats ? 'center' : 'left',
                    },
                  ]}
                >
                  %{stat.percentage}
                </Text>
              )}
            </Pressable>
          ))}
        </View>
      )}
    </View>
  );
};

export default ElegantSankeyChart;
