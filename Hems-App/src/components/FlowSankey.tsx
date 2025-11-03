import React, { memo, useMemo, useState, useEffect, useCallback } from "react";
import { View, StyleSheet, Text as RNText, Pressable } from "react-native";
import Svg, { Defs, LinearGradient, RadialGradient, Stop, Path, Rect, G, Text, Circle, Line, Image } from "react-native-svg";
import Animated, { useSharedValue, withTiming, useAnimatedStyle, interpolate, withRepeat, Easing, useAnimatedProps } from "react-native-reanimated";
import { useI18n } from "../hooks/useI18n";

const AnimatedPath = Animated.createAnimatedComponent(Path);
const AnimatedCircle = Animated.createAnimatedComponent(Circle);
const AnimatedLine = Animated.createAnimatedComponent(Line);
const AnimatedLinearGradient = Animated.createAnimatedComponent(LinearGradient);

export const fmtKWh = (value: number): string => {
    if (value >= 1000) return `${(value / 1000).toFixed(1)}M`;
    if (value >= 100) return value.toFixed(0);
    if (value >= 10) return value.toFixed(1);
    return value.toFixed(2);
};

const limitText = (text: string, maxLength: number = 8): string => {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength - 3) + '...';
};

type CatMeta = { chip: string; chip2?: string; text: string; stroke?: string };

const CAT: Record<string, CatMeta> = {
    SOLAR: { chip: "#FFE7B0", chip2: "#FFC56B", text: "#5C3B00", stroke: "#FFB23B" },
    PV: { chip: "#FFE7B0", chip2: "#FFC56B", text: "#5C3B00", stroke: "#FFB23B" },
    BATTERY: { chip: "#CFFAF3", chip2: "#A6F0E3", text: "#0A5A55", stroke: "#68E5D4" },
    GENERATOR: { chip: "#FFE0CC", chip2: "#FFB380", text: "#663300", stroke: "#FF9500" },
    GRID: { chip: "#E5DAFF", chip2: "#D3C4FF", text: "#3E2A80", stroke: "#B79AFF" },
    LOAD: { chip: "#FFD8D8", chip2: "#FFC2C2", text: "#7D1421", stroke: "#FF9AA5" },
    SYSTEM: { chip: "#D6EBFF", chip2: "#A8D5FF", text: "#003D7A", stroke: "#4A90E2" },
    CENTER: { chip: "#D6EBFF", chip2: "#A8D5FF", text: "#003D7A", stroke: "#4A90E2" },
};

const getCat = (label: string): CatMeta =>
    CAT[label.toUpperCase()] ?? { chip: "#EDEFF5", chip2: "#EDEFF5", text: "#1B2333", stroke: "#D8DEE9" };

export type SankeyNode = {
    id: string;
    label?: string;
    side: "left" | "center" | "right";
    color?: string;
};

export type SankeyLink = {
    source: string;
    target: string;
    value: number;
};

type ColorStop = {
    stop: number;
    color: string;
};

type Props = {
    width: number;
    height?: number;
    nodes: SankeyNode[];
    links: SankeyLink[];
    valueSuffix?: string;
    percentByNodeId?: Record<string, number>;
    theme?: "dark" | "light";
    layoutOpts?: {
        nodeWidth?: number;
        minHeight?: number;
        gapY?: number;
        padding?: number;
        curviness?: number;
        fontScale?: number;
    };
    enableFocusMode?: boolean;
    linkColorMode?: "absolute" | "shareOfSource" | "shareOfTarget";
    linkColorStops?: ColorStop[];
};

type LayoutNode = {
    x: number;
    y: number;
    w: number;
    h: number;
    label: string;
    id: string;
    side: "left" | "center" | "right";
};

const FlowSankey: React.FC<Props> = ({
    width,
    height = 560,
    nodes,
    links,
    valueSuffix = "kWh",
    percentByNodeId,
    theme = "dark",
    layoutOpts,
    enableFocusMode = true,
    linkColorMode = "absolute",
    linkColorStops = [
        { stop: 0.00, color: "#FF4757" },
        { stop: 0.25, color: "#FF7A00" },
        { stop: 0.50, color: "#FDB52E" },
        { stop: 0.75, color: "#10D3CF" },
        { stop: 1.00, color: "#5B6EF5" },
    ],
}) => {
    const {
        nodeWidth = 15,
        minHeight = 16,
        gapY = 3,
        padding = 8,
        curviness = 0.22,
        fontScale = 1,
    } = layoutOpts || {};

    const pad = padding;
    const nodeW = nodeWidth;
    const bg = theme === "dark" ? "#2B2F36" : "#F6F7FA";
    const text = theme === "dark" ? "#EAF0FF" : "#1B2333";
    const subtext = theme === "dark" ? "rgba(234,240,255,0.7)" : "rgba(27,35,51,0.6)";

    const valueFont = Math.max(6, Math.round(nodeW * 0.24 * fontScale));
    const unitFont = Math.max(4, Math.round(nodeW * 0.12 * fontScale));
    const labelFont = Math.max(5, Math.round(nodeW * 0.13 * fontScale));

    const flowProgress = useSharedValue(0);

    useEffect(() => {
        flowProgress.value = withRepeat(
            withTiming(1, {
                duration: 2000,
                easing: Easing.linear,
            }),
            -1,
            false
        );
    }, []);

    const { leftNodes, centerNodes, rightNodes } = useMemo(() => {
        const left = nodes.filter(n => n.side === "left");
        const center = nodes.filter(n => n.side === "center");
        const right = nodes.filter(n => n.side === "right");
        return { leftNodes: left, centerNodes: center, rightNodes: right };
    }, [nodes]);

    const layout = useMemo(() => {
        const placeColumn = (arr: SankeyNode[], x: number, isCenter = false): LayoutNode[] => {
            const count = arr.length || 1;
            const usable = height - pad * 2 - gapY * (count - 1);
            const eachH = Math.max(minHeight, Math.floor(usable / count));

            const centerW = 50;
            const centerH = 87;
            const finalW = isCenter ? centerW : nodeW;
            const finalH = isCenter ? centerH : eachH;

            return arr.map((n, i) => {
                const baseY = pad + i * (eachH + gapY);
                const yOffset = isCenter ? (eachH - finalH) / 2 : 0;

                return {
                    id: n.id,
                    label: n.label ?? n.id,
                    side: n.side || "left",
                    x: isCenter ? x - centerW / 2 : x,
                    y: baseY + yOffset,
                    w: finalW,
                    h: finalH,
                };
            });
        };
        const centerX = width / 2;
        return {
            left: placeColumn(leftNodes, pad, false),
            center: placeColumn(centerNodes, centerX, true),
            right: placeColumn(rightNodes, width - pad - nodeW, false),
        };
    }, [leftNodes, centerNodes, rightNodes, height, width, pad, gapY, minHeight, nodeW]);

    const byId: Record<string, LayoutNode> = useMemo(() => {
        const m: Record<string, LayoutNode> = {};
        [...layout.left, ...layout.center, ...layout.right].forEach(n => (m[n.id] = n));
        return m;
    }, [layout]);

    const { t } = useI18n();

    const [focusedNodeId, setFocusedNodeId] = useState<string | null>(null);
    const [focusedLinkId, setFocusedLinkId] = useState<string | null>(null);
    const [tip, setTip] = useState<{ x: number; y: number; text: string; type: 'node' | 'link' } | null>(null);
    const focusT = useSharedValue(0);
    const linkFocusT = useSharedValue(0);
    const animProgress = useSharedValue(0);

    useEffect(() => {
        animProgress.value = withRepeat(
            withTiming(1, {
                duration: 2000,
                easing: Easing.linear
            }),
            -1,
            false
        );
    }, []);

    const setFocusNode = (id: string | null) => {
        if (!enableFocusMode) return;
        setFocusedNodeId(id);
        focusT.value = withTiming(id ? 1 : 0, { duration: 220 });
    };

    const setFocusLink = (linkId: string | null) => {
        if (!enableFocusMode) return;
        setFocusedLinkId(linkId);
        linkFocusT.value = withTiming(linkId ? 1 : 0, { duration: 220 });
    };

    const handleLinkPress = (link: SankeyLink, event: any) => {
        const linkId = `${link.source}-${link.target}`;
        setFocusLink(focusedLinkId === linkId ? null : linkId);

        const s = byId[link.source];
        const t = byId[link.target];
        if (s && t) {
            const sourceLabel = s.label.length > 8 ? s.label.substring(0, 8) + '...' : s.label;
            const targetLabel = t.label.length > 8 ? t.label.substring(0, 8) + '...' : t.label;
            const value = fmtKWh(link.value || 0);
            const percentage = ((link.value || 0) / vMax * 100).toFixed(1);

            setTip({
                x: event.nativeEvent?.pageX || 0,
                y: event.nativeEvent?.pageY || 0,
                text: `${sourceLabel} → ${targetLabel}\n${value} kWh (${percentage}%)`,
                type: 'link'
            });
        }
    };

    const veilStyle = useAnimatedStyle(() => ({
        opacity: interpolate(focusT.value, [0, 1], [0, 0.25]),
    }));

    const clamp = (v: number, min: number, max: number) => Math.max(min, Math.min(max, v));

    const lerpColor = (c1: string, c2: string, t: number): string => {
        const a = parseInt(c1.slice(1), 16);
        const b = parseInt(c2.slice(1), 16);
        const r1 = (a >> 16) & 255, g1 = (a >> 8) & 255, b1 = a & 255;
        const r2 = (b >> 16) & 255, g2 = (b >> 8) & 255, b2 = b & 255;
        const r = Math.round(r1 + (r2 - r1) * t);
        const g = Math.round(g1 + (g2 - g1) * t);
        const bl = Math.round(b1 + (b2 - b1) * t);
        return `#${((1 << 24) + (r << 16) + (g << 8) + bl).toString(16).slice(1)}`;
    };

    const sampleColor = (stops: ColorStop[], t: number): string => {
        if (t <= stops[0].stop) return stops[0].color;
        if (t >= stops[stops.length - 1].stop) return stops[stops.length - 1].color;
        for (let i = 0; i < stops.length - 1; i++) {
            const a = stops[i];
            const b = stops[i + 1];
            if (t >= a.stop && t <= b.stop) {
                const local = (t - a.stop) / (b.stop - a.stop);
                return lerpColor(a.color, b.color, local);
            }
        }
        return stops[0].color;
    };

    const outTotals: Record<string, number> = {};
    const inTotals: Record<string, number> = {};
    links.forEach(l => {
        outTotals[l.source] = (outTotals[l.source] || 0) + (l.value || 0);
        inTotals[l.target] = (inTotals[l.target] || 0) + (l.value || 0);
    });

    const vals = links.map(l => l.value || 0);
    const vMin = Math.min(...vals);
    const vMax = Math.max(...vals);
    const norm = (v: number) => (vMax === vMin ? 1 : (v - vMin) / (vMax - vMin));

    const ratioFor = (l: SankeyLink): number => {
        const v = l.value || 0;
        if (linkColorMode === "shareOfSource") {
            return v / (outTotals[l.source] || v || 1);
        }
        if (linkColorMode === "shareOfTarget") {
            return v / (inTotals[l.target] || v || 1);
        }
        return norm(v);
    };

    const getIntelligentColor = (baseColor: string, intensity: number): string => {
        const hex = baseColor.replace('#', '');
        const r = parseInt(hex.substr(0, 2), 16);
        const g = parseInt(hex.substr(2, 2), 16);
        const b = parseInt(hex.substr(4, 2), 16);

        const minIntensity = 0.3;
        const adjustedIntensity = minIntensity + (intensity * (1 - minIntensity));

        const newR = Math.round(r * adjustedIntensity);
        const newG = Math.round(g * adjustedIntensity);
        const newB = Math.round(b * adjustedIntensity);

        return `rgb(${newR}, ${newG}, ${newB})`;
    };

    const createPerfectTriangularPath = (s: LayoutNode, t: LayoutNode) => {
        const x0 = s.x + s.w - 8;
        const y0 = s.y + s.h / 2;
        const x1 = t.x + 8;
        const y1 = t.y + t.h / 2;

        const isToCenter = t.side === "center";
        const isFromCenter = s.side === "center";

        if (isToCenter) {
            const targetHeight = t.h;
            const padding = 2;
            const rightTopY = t.y + padding;
            const rightBottomY = t.y + targetHeight - padding;

            const path = [
                `M ${x0},${y0}`,
                `L ${x1},${rightTopY}`,
                `L ${x1},${rightBottomY}`,
                `Z`
            ].join(' ');

            return path;
        } else if (isFromCenter) {
            const sourceHeight = s.h;
            const padding = 2;
            const leftTopY = s.y + padding;
            const leftBottomY = s.y + sourceHeight - padding;

            const path = [
                `M ${x0},${leftTopY}`,
                `L ${x1},${y1}`,
                `L ${x0},${leftBottomY}`,
                `Z`
            ].join(' ');

            return path;
        } else {
            const sourceHeight = s.h;
            const targetHeight = t.h;
            const padding = 2;

            const leftTopY = s.y + padding;
            const leftBottomY = s.y + sourceHeight - padding;
            const rightTopY = t.y + padding;
            const rightBottomY = t.y + targetHeight - padding;

            const path = [
                `M ${x0},${leftTopY}`,
                `L ${x1},${rightTopY}`,
                `L ${x1},${rightBottomY}`,
                `L ${x0},${leftBottomY}`,
                `Z`
            ].join(' ');

            return path;
        }
    };

    const createElectricFlow = (s: LayoutNode, t: LayoutNode, color: string, linkIndex: number, isFocused: boolean = true) => {
        const triangularPath = createPerfectTriangularPath(s, t);

        const offset = linkIndex * 0.15;
        const waveGradId = `wave-${linkIndex}`;

        const brightnessMultiplier = isFocused ? 1.0 : 0.5;

        const animProps = useAnimatedProps(() => {
            'worklet';
            const p = (animProgress.value + offset) % 1;

            const wavePos = -80 + (p * 260);
            const waveWidth = 35;

            return {
                x1: `${wavePos - waveWidth}%`,
                x2: `${wavePos + waveWidth}%`,
            };
        });

        return (
            <G key={`wave-${linkIndex}`}>
                <Defs>
                    <AnimatedLinearGradient
                        id={waveGradId}
                        animatedProps={animProps}
                        y1="0"
                        y2="0"
                    >
                        <Stop offset="0%" stopColor={color} stopOpacity={0 * brightnessMultiplier} />
                        <Stop offset="20%" stopColor={color} stopOpacity={0.4 * brightnessMultiplier} />
                        <Stop offset="40%" stopColor={color} stopOpacity={0.7 * brightnessMultiplier} />
                        <Stop offset="50%" stopColor={color} stopOpacity={1.0 * brightnessMultiplier} />
                        <Stop offset="60%" stopColor={color} stopOpacity={0.7 * brightnessMultiplier} />
                        <Stop offset="80%" stopColor={color} stopOpacity={0.4 * brightnessMultiplier} />
                        <Stop offset="100%" stopColor={color} stopOpacity={0 * brightnessMultiplier} />
                    </AnimatedLinearGradient>
                </Defs>
                <Path
                    d={triangularPath}
                    fill={`url(#${waveGradId})`}
                />
            </G>
        );
    };

    const createFlowGradient = (linkIndex: number, col: string, col2: string) => {
        const gradId = `flowGrad-${linkIndex}`;

        return (
            <LinearGradient id={gradId} x1="0" y1="0" x2="1" y2="0">
                <Stop offset="0" stopColor={col} stopOpacity={0.3} />
                <Stop offset="0.5" stopColor={col2} stopOpacity={0.9} />
                <Stop offset="1" stopColor={col} stopOpacity={0.3} />
            </LinearGradient>
        );
    };

    const nodeFillByLabel = (label: string): [string, string] => {
        switch (label.toUpperCase()) {
            case "SOLAR":
            case "PV":
                return ["#FDB52E", "#FF7A00"];
            case "BATTERY":
                return ["#12C6A9", "#09A79F"];
            case "GRID":
                return ["#9D76F2", "#7B63FF"];
            case "LOAD":
                return ["#FF6B6B", "#FF4B5C"];
            case "GENERATOR":
                return ["#FF9500", "#FF7A00"];
            case "SYSTEM":
            case "CENTER":
                return ["#FFFFFF", "#F8F9FA"];
            default:
                return ["#636972", "#545B65"];
        }
    };

    return (
        <Pressable style={[styles.wrap, { backgroundColor: bg, width, height }]} onPress={() => setFocusNode(null)}>
            <Svg width={width} height={height}>
                <Defs>
                    {links.map((l, i) => {
                        const r = Math.max(0, Math.min(1, ratioFor(l)));
                        const col = sampleColor(linkColorStops, r);
                        const col2 = sampleColor(linkColorStops, Math.min(1, r + 0.08));
                        return (
                            <React.Fragment key={`gradient-${l.source}-${l.target}-${i}`}>
                                {createFlowGradient(i, col, col2)}
                            </React.Fragment>
                        );
                    })}
                </Defs>

                {links.map((l, i) => {
                    const s = byId[l.source];
                    const t = byId[l.target];
                    if (!s || !t) return null;

                    const triangularPath = createPerfectTriangularPath(s, t);

                    const r = Math.max(0, Math.min(1, ratioFor(l)));
                    const baseCol = sampleColor(linkColorStops, r);
                    const baseCol2 = sampleColor(linkColorStops, Math.min(1, r + 0.08));

                    // Akıllı renk yoğunluğu - değer büyüklüğüne göre
                    const valueIntensity = Math.max(0.3, Math.min(1, (l.value || 0) / vMax));
                    const col = getIntelligentColor(baseCol, valueIntensity);
                    const col2 = getIntelligentColor(baseCol2, valueIntensity);
                    const gradId = `flowGrad-${i}`;

                    const related = focusedNodeId ? (l.source === focusedNodeId || l.target === focusedNodeId) : true;
                    const dim = focusedNodeId !== null && !related;
                    const alpha = dim ? 0.20 : 0.85;

                    const baseStrokeWidth = 1;
                    const valueRatio = (l.value || 0) / vMax;
                    const dynamicStrokeWidth = baseStrokeWidth + (valueRatio * 2);

                    const linkId = `${l.source}-${l.target}`;
                    const isLinkFocused = focusedLinkId === linkId;
                    const linkAlpha = isLinkFocused ? 1.0 : alpha;

                    return (
                        <G
                            key={`L${i}`}
                            onPress={(event) => handleLinkPress(l, event)}
                        >
                            <Path
                                d={triangularPath}
                                fill={col}
                                opacity={linkAlpha * 0.3}
                            />
                            <AnimatedPath
                                d={triangularPath}
                                fill={`url(#${gradId})`}
                                opacity={linkAlpha}
                            />
                            {s.id !== "GRID_L" && createElectricFlow(s, t, col, i, related && !dim)}
                        </G>
                    );
                })}

                {focusedNodeId && links.filter(l => l.source === focusedNodeId || l.target === focusedNodeId).map((l, idx) => {
                    const s = byId[l.source];
                    const t = byId[l.target];
                    if (!s || !t) return null;

                    const highlightPath = createPerfectTriangularPath(s, t);

                    const r = Math.max(0, Math.min(1, ratioFor(l)));
                    const col = sampleColor(linkColorStops, r);

                    return (
                        <G key={`HL${idx}`}>
                            <Path
                                d={highlightPath}
                                fill={col}
                                opacity={0.5}
                            />
                        </G>
                    );
                })}

                {[...layout.left, ...layout.center, ...layout.right].map((n) => {
                    const [c0, c1] = nodeFillByLabel(n.label);

                    const isLeftGrid = n.label.toUpperCase() === "GRID" && n.side === "left";
                    const isSystemCenter = n.label.toUpperCase() === "SYSTEM" && n.side === "center";
                    const isPassiveGrid = n.id === "GRID_L";
                    const nodeColors = isPassiveGrid ? ["#6B7280", "#4B5563"] : [c0, c1];

                    const inbound = links
                        .filter(l => l.target === n.id)
                        .reduce((a, b) => a + (b.value || 0), 0);
                    const outbound = links
                        .filter(l => l.source === n.id)
                        .reduce((a, b) => a + (b.value || 0), 0);

                    const nodeValue =
                        n.label.toUpperCase() === "BATTERY" && n.id.endsWith("_R")
                            ? inbound
                            : n.label.toUpperCase() === "GRID" && n.side === "left"
                                ? outbound
                                : inbound || outbound || 0;

                    let nodeOpacity = isLeftGrid ? 0.4 : 1;
                    if (isPassiveGrid) {
                        nodeOpacity = 0.3;
                    }
                    if (focusedNodeId) {
                        if (n.id === focusedNodeId) {
                            nodeOpacity = isLeftGrid ? 0.6 : 1;
                        } else {
                            const neighbor = links.some(
                                l => (l.source === focusedNodeId || l.target === focusedNodeId) && (l.source === n.id || l.target === n.id)
                            );
                            nodeOpacity = neighbor ? (isLeftGrid ? 0.5 : 0.6) : (isLeftGrid ? 0.2 : 0.2);
                        }
                    }

                    return (
                        <G
                            key={n.id}
                            onPress={isPassiveGrid ? undefined : () => setFocusNode(focusedNodeId === n.id ? null : n.id)}
                        >
                            <Rect
                                x={n.x + 2}
                                y={n.y + 4}
                                width={n.w}
                                height={n.h}
                                rx={isSystemCenter ? 8 : 18}
                                ry={isSystemCenter ? 8 : 18}
                                fill="black"
                                opacity={0.18}
                            />
                            <Defs>
                                <LinearGradient id={`node-${n.id}`} x1="0" y1="0" x2="0" y2="1">
                                    <Stop offset="0" stopColor={nodeColors[0]} />
                                    <Stop offset="1" stopColor={nodeColors[1]} />
                                </LinearGradient>
                            </Defs>
                            <Rect
                                x={n.x}
                                y={n.y}
                                width={n.w}
                                height={n.h}
                                rx={isSystemCenter ? 8 : 18}
                                ry={isSystemCenter ? 8 : 18}
                                fill={`url(#node-${n.id})`}
                                opacity={1}
                            />
                            {isSystemCenter && (
                                <G opacity={1}>
                                    <Image
                                        x={n.x}
                                        y={n.y}
                                        width={n.w}
                                        height={n.h}
                                        href={require('../assets/images/sankey-image.png')}
                                        preserveAspectRatio="none"
                                        opacity={1}
                                    />
                                </G>
                            )}
                            {!isSystemCenter && (
                                <G opacity={nodeOpacity}>
                                    <Text
                                        x={n.x + n.w / 2}
                                        y={n.y + labelFont + 5}
                                        fontSize={labelFont}
                                        fontWeight="800"
                                        fill={subtext}
                                        textAnchor="middle"
                                    >
                                        {limitText(n.label, 8)}
                                    </Text>
                                    {isPassiveGrid ? (
                                        <Text
                                            x={n.x + n.w / 2}
                                            y={n.y + n.h / 2 + valueFont / 3}
                                            fontSize={valueFont}
                                            fontWeight="900"
                                            fill={text}
                                            textAnchor="middle"
                                        >
                                            {fmtKWh(nodeValue)}
                                        </Text>
                                    ) : (
                                        <>
                                            <Text
                                                x={n.x + n.w / 2}
                                                y={n.y + n.h / 2 + valueFont / 3}
                                                fontSize={valueFont}
                                                fontWeight="900"
                                                fill={text}
                                                textAnchor="middle"
                                            >
                                                {fmtKWh(nodeValue)}
                                            </Text>
                                            <Text
                                                x={n.x + n.w / 2}
                                                y={n.y + n.h - 5}
                                                fontSize={unitFont}
                                                fontWeight="700"
                                                fill={subtext}
                                                textAnchor="middle"
                                            >
                                                kWh
                                            </Text>
                                        </>
                                    )}
                                </G>
                            )}
                        </G>
                    );
                })}

                {focusedNodeId && (() => {
                    const nd = byId[focusedNodeId];
                    if (!nd) return null;

                    const inbound = links.filter(l => l.target === focusedNodeId).reduce((a, b) => a + (b.value || 0), 0);
                    const outbound = links.filter(l => l.source === focusedNodeId).reduce((a, b) => a + (b.value || 0), 0);

                    const rawX = nd.x + nd.w / 2;
                    const rawY = nd.y - 12;
                    
                    const label = (nodes.find(n => n.id === focusedNodeId)?.label || focusedNodeId).toUpperCase();
                    const inText = `${t('dashboardFlow.in')}: ${fmtKWh(inbound)} ${valueSuffix}`;
                    const outText = `${t('dashboardFlow.out')}: ${fmtKWh(outbound)} ${valueSuffix}`;
                    
                    const labelWidth = label.length * 8 + 24;
                    const contentWidth = Math.max(inText.length, outText.length) * 9 + 24;
                    const boxW = Math.min(width - 32, Math.max(180, Math.max(labelWidth, contentWidth)));
                    const boxH = 80;
                    const pad = 12;
                    
                    const bx = clamp(rawX - boxW / 2, 16, (width - 16) - boxW);
                    const by = Math.max(16, rawY - boxH);
                    const pointerX = clamp(rawX, bx + 12, bx + boxW - 12);

                    return (
                        <G key="node-info" opacity={0.98}>
                            <Rect
                                x={bx}
                                y={by}
                                width={boxW}
                                height={boxH}
                                rx={12}
                                ry={12}
                                fill="rgba(20,24,33,0.92)"
                            />
                            <Path
                                d={`M ${pointerX - 8} ${by + boxH} L ${pointerX} ${by + boxH + 8} L ${pointerX + 8} ${by + boxH} Z`}
                                fill="rgba(20,24,33,0.92)"
                            />
                            <Text
                                x={bx + pad}
                                y={by + 20}
                                fontSize={11}
                                fontWeight="700"
                                fill="#CFE6FF"
                            >
                                {label}
                            </Text>
                            <Text
                                x={bx + pad}
                                y={by + 40}
                                fontSize={12}
                                fontWeight="600"
                                fill="#FFFFFF"
                            >
                                {inText}
                            </Text>
                            <Text
                                x={bx + pad}
                                y={by + 58}
                                fontSize={12}
                                fontWeight="600"
                                fill="#FFFFFF"
                            >
                                {outText}
                            </Text>
                        </G>
                    );
                })()}
            </Svg>

            {focusedNodeId !== null && (
                <Animated.View
                    pointerEvents="none"
                    style={[
                        {
                            position: "absolute",
                            left: 0,
                            top: 0,
                            right: 0,
                            bottom: 0,
                            backgroundColor: "rgba(0,0,0,0.25)",
                        },
                        veilStyle,
                    ]}
                />
            )}
        </Pressable>
    );
};

export default memo(FlowSankey);

const styles = StyleSheet.create({
    wrap: { borderRadius: 16, overflow: "hidden" },
});