import React, { memo, useMemo, useState } from "react";
import { View, StyleSheet, Text as RNText, Pressable } from "react-native";
import Svg, { Defs, LinearGradient, Stop, Path, Rect, G, Text, Circle, Line } from "react-native-svg";
import Animated, { useSharedValue, withTiming, useAnimatedStyle, interpolate } from "react-native-reanimated";
import { fmtKWh, fmtPct01 } from "../utils/format";

type CatMeta = { chip: string; chip2?: string; text: string; stroke?: string };

const CAT: Record<string, CatMeta> = {
    SOLAR: { chip: "#FFE7B0", chip2: "#FFC56B", text: "#5C3B00", stroke: "#FFB23B" },
    BATTERY: { chip: "#CFFAF3", chip2: "#A6F0E3", text: "#0A5A55", stroke: "#68E5D4" },
    GRID: { chip: "#E5DAFF", chip2: "#D3C4FF", text: "#3E2A80", stroke: "#B79AFF" },
    LOAD: { chip: "#FFD8D8", chip2: "#FFC2C2", text: "#7D1421", stroke: "#FF9AA5" },
};

const getCat = (label: string): CatMeta =>
    CAT[label.toUpperCase()] ?? { chip: "#EDEFF5", chip2: "#EDEFF5", text: "#1B2333", stroke: "#D8DEE9" };

const IconSun = ({ x, y }: { x: number; y: number }) => (
    <G>
        <Circle cx={x} cy={y} r={5} fill="#FF9F0A" />
        {Array.from({ length: 8 }).map((_, i) => {
            const a = (i * Math.PI) / 4;
            return (
                <Line
                    key={i}
                    x1={x + Math.cos(a) * 7}
                    y1={y + Math.sin(a) * 7}
                    x2={x + Math.cos(a) * 11}
                    y2={y + Math.sin(a) * 11}
                    stroke="#FF9F0A"
                    strokeWidth={1.5}
                />
            );
        })}
    </G>
);

const IconBattery = ({ x, y }: { x: number; y: number }) => (
    <G>
        <Rect x={x - 9} y={y - 5} width={18} height={10} rx={2} ry={2} stroke="#0AA399" fill="none" strokeWidth={1.5} />
        <Rect x={x + 9} y={y - 2.5} width={3} height={5} rx={1} ry={1} fill="#0AA399" />
        <Line x1={x - 4} y1={y} x2={x + 2} y2={y} stroke="#0AA399" strokeWidth={2} />
    </G>
);

const IconGrid = ({ x, y }: { x: number; y: number }) => (
    <G stroke="#6A4CD6" strokeWidth={1.4}>
        {[-4, 0, 4].map(dx => (
            <Line key={"v" + dx} x1={x + dx} y1={y - 5} x2={x + dx} y2={y + 5} />
        ))}
        {[-4, 0, 4].map(dy => (
            <Line key={"h" + dy} x1={x - 5} y1={y + dy} x2={x + 5} y2={y + dy} />
        ))}
    </G>
);

const IconLoad = ({ x, y }: { x: number; y: number }) => (
    <G>
        <Rect x={x - 6} y={y - 4} width={12} height={8} rx={2} fill="none" stroke="#E03A47" strokeWidth={1.6} />
        <Line x1={x - 6} y1={y} x2={x - 10} y2={y} stroke="#E03A47" strokeWidth={1.6} />
        <Line x1={x + 6} y1={y} x2={x + 10} y2={y} stroke="#E03A47" strokeWidth={1.6} />
    </G>
);

const pickIcon = (label: string) => {
    const L = label.toUpperCase();
    if (L === "SOLAR") return IconSun;
    if (L === "BATTERY") return IconBattery;
    if (L === "GRID") return IconGrid;
    if (L === "LOAD") return IconLoad;
    return null;
};

export type SankeyNode = {
    id: string;
    label?: string;
    side: "left" | "right";
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
    badgeVariant?: "pill" | "ribbon" | "tag";
    showBadgeIcon?: boolean;
    badgeWidthRatio?: number;
    badgeFixedFontSize?: number;
    badgeUppercase?: boolean;
    badgeHPad?: number;
};

type LayoutNode = {
    x: number;
    y: number;
    w: number;
    h: number;
    label: string;
    id: string;
    side: "left" | "right";
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
    badgeVariant = "pill",
    showBadgeIcon = false,
    badgeWidthRatio = 0.70,
    badgeFixedFontSize = 11,
    badgeUppercase = true,
    badgeHPad = 24,
}) => {
    const {
        nodeWidth = 140,
        minHeight = 88,
        gapY = 28,
        padding = 12,
        curviness = 0.22,
        fontScale = 1,
    } = layoutOpts || {};

    const pad = padding;
    const nodeW = nodeWidth;
    const bg = theme === "dark" ? "#2B2F36" : "#F6F7FA";
    const text = theme === "dark" ? "#EAF0FF" : "#1B2333";
    const subtext = theme === "dark" ? "rgba(234,240,255,0.7)" : "rgba(27,35,51,0.6)";

    const valueFont = Math.max(22, Math.round(nodeW * 0.26 * fontScale));
    const unitFont = Math.max(10, Math.round(nodeW * 0.11 * fontScale));
    const pctFont = Math.max(9, Math.round(nodeW * 0.10 * fontScale));
    const badgeW = Math.round(nodeW * 0.53);
    const badgeH = Math.round(valueFont * 0.48);
    const badgeRad = Math.min(14, Math.round(badgeH / 2) + 6);

    const { leftNodes, rightNodes } = useMemo(() => {
        const left = nodes.filter(n => n.side === "left");
        const right = nodes.filter(n => n.side === "right");
        return { leftNodes: left, rightNodes: right };
    }, [nodes]);

    const layout = useMemo(() => {
        const placeColumn = (arr: SankeyNode[], x: number): LayoutNode[] => {
            const count = arr.length || 1;
            const usable = height - pad * 2 - gapY * (count - 1);
            const eachH = Math.max(minHeight, Math.floor(usable / count));
            return arr.map((n, i) => ({
                id: n.id,
                label: n.label ?? n.id,
                side: n.side || "left",
                x,
                y: pad + i * (eachH + gapY),
                w: nodeW,
                h: eachH,
            }));
        };
        return {
            left: placeColumn(leftNodes, pad),
            right: placeColumn(rightNodes, width - pad - nodeW),
        };
    }, [leftNodes, rightNodes, height, width, pad, gapY, minHeight, nodeW]);

    const byId: Record<string, LayoutNode> = useMemo(() => {
        const m: Record<string, LayoutNode> = {};
        [...layout.left, ...layout.right].forEach(n => (m[n.id] = n));
        return m;
    }, [layout]);

    const [tip, setTip] = useState<{ x: number; y: number; text: string } | null>(null);
    const [focusedNodeId, setFocusedNodeId] = useState<string | null>(null);
    const focusT = useSharedValue(0);

    const setFocusNode = (id: string | null) => {
        if (!enableFocusMode) return;
        setFocusedNodeId(id);
        focusT.value = withTiming(id ? 1 : 0, { duration: 220 });
        if (id && tip) setTip(null);
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

    const isLinkRelatedTo = (l: SankeyLink, id: string) => l.source === id || l.target === id;

    const sumsForNode = (id: string) => {
        const inbound = links.filter(l => l.target === id).reduce((a, b) => a + (b.value || 0), 0);
        const outbound = links.filter(l => l.source === id).reduce((a, b) => a + (b.value || 0), 0);
        return { inbound, outbound };
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

    const min = vMin;
    const max = vMax;
    const wScale = (value: number): number => {
        const domainMin = Math.max(0, min || 0);
        const domainMax = Math.max(0.01, max || 1);
        const rangeMin = 10;
        const rangeMax = 56;
        if (domainMax === domainMin) return rangeMin;
        const normalized = (value - domainMin) / (domainMax - domainMin);
        return rangeMin + normalized * (rangeMax - rangeMin);
    };

    const curv = Math.max(32, width * curviness);

    const linkPath = (s: LayoutNode, t: LayoutNode) => {
        const x0 = s.x + s.w;
        const y0 = s.y + s.h / 2;
        const x1 = t.x;
        const y1 = t.y + t.h / 2;
        return `M ${x0},${y0} C ${x0 + curv},${y0} ${x1 - curv},${y1} ${x1},${y1}`;
    };

    const approxTextWidth = (text: string, fontSize: number) => text.length * fontSize * 0.62;

    const ellipsizeToFit = (raw: string, fontSize: number, maxPx: number): string => {
        if (approxTextWidth(raw, fontSize) <= maxPx) return raw;
        const charW = fontSize * 0.62;
        const maxChars = Math.max(3, Math.floor(maxPx / charW) - 1);
        return raw.length > maxChars ? raw.slice(0, maxChars) + "…" : raw;
    };

    const drawBadge = (n: LayoutNode, variant: "pill" | "ribbon" | "tag", wantIcon: boolean) => {
        const meta = getCat(n.label);
        const chipW = clamp(Math.round(n.w * badgeWidthRatio), 64, n.w - 16);
        const chipH = 22;
        const r = 12;
        const x = n.x + 12;
        const y = n.y + 12;
        const fs = badgeFixedFontSize;
        const labelRaw = badgeUppercase ? n.label.toUpperCase() : n.label;

        const Icon = pickIcon(n.label);
        const iconOn = wantIcon && !!Icon && chipW >= 96;
        const iconSpace = iconOn ? 18 : 0;

        const maxTextPx = chipW - iconSpace - badgeHPad;
        const labelFit = ellipsizeToFit(labelRaw, fs, maxTextPx);

        if (variant === "tag") {
            return (
                <G>
                    <Rect
                        x={x}
                        y={y}
                        width={chipW}
                        height={chipH}
                        rx={r}
                        ry={r}
                        fill={meta.chip}
                        stroke={meta.stroke}
                        strokeOpacity={0.35}
                    />
                    {iconOn && Icon && <Icon x={x + 12} y={y + chipH * 0.58} />}
                    <Text
                        x={x + (iconOn ? 28 : 14)}
                        y={y + chipH * 0.72}
                        fontSize={fs}
                        fontWeight="800"
                        fill={meta.text}
                    >
                        {labelFit}
                    </Text>
                </G>
            );
        }

        if (variant === "ribbon") {
            return (
                <G>
                    <Path
                        d={`M ${x} ${y} h ${chipW - 10} a ${r} ${r} 0 0 1 ${r} ${r} v ${chipH - 10} l -10 8 l -10 -8 v -${chipH - 10} a ${r} ${r} 0 0 0 -${r} -${r} z`}
                        fill={meta.chip}
                        stroke={meta.stroke}
                        strokeOpacity={0.55}
                    />
                    {iconOn && Icon && <Icon x={x + 12} y={y + chipH * 0.58} />}
                    <Text
                        x={x + (iconOn ? 28 : 14)}
                        y={y + chipH * 0.70}
                        fontSize={fs}
                        fontWeight="800"
                        fill={meta.text}
                    >
                        {labelFit}
                    </Text>
                </G>
            );
        }

        return (
            <G>
                <Defs>
                    <LinearGradient id={`pill-${n.id}`} x1="0" y1="0" x2="1" y2="1">
                        <Stop offset="0" stopColor={meta.chip} stopOpacity={0.98} />
                        <Stop offset="1" stopColor={meta.chip2 ?? meta.chip} stopOpacity={0.98} />
                    </LinearGradient>
                </Defs>
                <Rect
                    x={x}
                    y={y}
                    width={chipW}
                    height={chipH}
                    rx={r}
                    ry={r}
                    fill={`url(#pill-${n.id})`}
                    stroke={meta.stroke}
                    strokeOpacity={0.25}
                />
                <Rect
                    x={x}
                    y={y}
                    width={chipW}
                    height={Math.max(6, chipH * 0.35)}
                    rx={r}
                    ry={r}
                    fill="rgba(255,255,255,0.35)"
                />
                {iconOn && Icon && <Icon x={x + 12} y={y + chipH * 0.56} />}
                <Text
                    x={x + (iconOn ? 28 : 14)}
                    y={y + chipH * 0.70}
                    fontSize={fs}
                    fontWeight="800"
                    fill={meta.text}
                >
                    {labelFit}
                </Text>
            </G>
        );
    };

    const nodeFillByLabel = (label: string): [string, string] => {
        switch (label.toUpperCase()) {
            case "SOLAR":
                return ["#FDB52E", "#FF7A00"];
            case "BATTERY":
                return ["#12C6A9", "#09A79F"];
            case "GRID":
                return ["#9D76F2", "#7B63FF"];
            case "LOAD":
                return ["#FF6B6B", "#FF4B5C"];
            default:
                return ["#636972", "#545B65"];
        }
    };

    return (
        <Pressable style={[styles.wrap, { backgroundColor: bg, width, height }]} onPress={() => setFocusNode(null)}>
            <Svg width={width} height={height}>
                <Defs>
                    <LinearGradient id="flowGrad" x1="0" y1="0" x2="1" y2="0">
                        <Stop offset="0" stopColor="#10D3CF" stopOpacity="0.75" />
                        <Stop offset="1" stopColor="#5B6EF5" stopOpacity="0.75" />
                    </LinearGradient>
                    <LinearGradient id="shadowGrad" x1="0" y1="0" x2="1" y2="0">
                        <Stop offset="0" stopColor="#0CC7C3" stopOpacity="0.18" />
                        <Stop offset="1" stopColor="#5868E8" stopOpacity="0.18" />
                    </LinearGradient>
                </Defs>

                {links.map((l, i) => {
                    const s = byId[l.source];
                    const t = byId[l.target];
                    if (!s || !t) return null;
                    const d = linkPath(s, t);
                    const sw = wScale(Math.max(0, l.value || 0));

                    const r = Math.max(0, Math.min(1, ratioFor(l)));
                    const col = sampleColor(linkColorStops, r);
                    const col2 = sampleColor(linkColorStops, Math.min(1, r + 0.12));
                    const gradId = `flowGrad-${i}`;

                    const related = focusedNodeId ? (l.source === focusedNodeId || l.target === focusedNodeId) : true;
                    const dim = focusedNodeId !== null && !related;
                    const alpha = dim ? 0.25 : 0.85;

                    return (
                        <G key={`L${i}`}>
                            <Defs>
                                <LinearGradient id={gradId} x1="0" y1="0" x2="1" y2="0">
                                    <Stop offset="0" stopColor={col} stopOpacity={alpha} />
                                    <Stop offset="1" stopColor={col2} stopOpacity={alpha} />
                                </LinearGradient>
                            </Defs>
                            <Path
                                d={d}
                                stroke={col}
                                strokeWidth={sw + 10}
                                opacity={alpha * 0.22}
                                fill="none"
                                strokeLinecap="round"
                            />
                            <Path
                                d={d}
                                stroke={`url(#${gradId})`}
                                strokeWidth={sw}
                                fill="none"
                                strokeLinecap="round"
                            />
                        </G>
                    );
                })}

                {focusedNodeId && links.filter(l => l.source === focusedNodeId || l.target === focusedNodeId).map((l, idx) => {
                    const s = byId[l.source];
                    const t = byId[l.target];
                    if (!s || !t) return null;
                    const d = linkPath(s, t);
                    const baseW = wScale(Math.max(0, l.value || 0));

                    const r = Math.max(0, Math.min(1, ratioFor(l)));
                    const col = sampleColor(linkColorStops, r);
                    const col2 = sampleColor(linkColorStops, Math.min(1, r + 0.12));
                    const linkIdx = links.findIndex(link => link.source === l.source && link.target === l.target && link.value === l.value);
                    const gradId = `flowGrad-${linkIdx}`;

                    return (
                        <G key={`HL${idx}`}>
                            <Path
                                d={d}
                                stroke={col}
                                strokeWidth={baseW + 24}
                                fill="none"
                                strokeLinecap="round"
                                opacity={0.35}
                            />
                            <Path
                                d={d}
                                stroke={`url(#${gradId})`}
                                strokeWidth={baseW * 1.35}
                                fill="none"
                                strokeLinecap="round"
                            />
                        </G>
                    );
                })}

                {[...layout.left, ...layout.right].map((n) => {
                    const [c0, c1] = nodeFillByLabel(n.label);

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

                    let nodeOpacity = 1;
                    if (focusedNodeId) {
                        if (n.id === focusedNodeId) {
                            nodeOpacity = 1;
                        } else {
                            const neighbor = links.some(
                                l => (l.source === focusedNodeId || l.target === focusedNodeId) && (l.source === n.id || l.target === n.id)
                            );
                            nodeOpacity = neighbor ? 0.6 : 0.2;
                        }
                    }

                    return (
                        <G
                            key={n.id}
                            opacity={nodeOpacity}
                            onPress={() => setFocusNode(focusedNodeId === n.id ? null : n.id)}
                            accessible
                            accessibilityRole="button"
                            accessibilityLabel={`${n.label} node`}
                        >
                            <Rect
                                x={n.x + 2}
                                y={n.y + 4}
                                width={n.w}
                                height={n.h}
                                rx={18}
                                ry={18}
                                fill="black"
                                opacity={0.18}
                            />
                            <Defs>
                                <LinearGradient id={`node-${n.id}`} x1="0" y1="0" x2="0" y2="1">
                                    <Stop offset="0" stopColor={c0} />
                                    <Stop offset="1" stopColor={c1} />
                                </LinearGradient>
                            </Defs>
                            <Rect
                                x={n.x}
                                y={n.y}
                                width={n.w}
                                height={n.h}
                                rx={18}
                                ry={18}
                                fill={`url(#node-${n.id})`}
                                opacity={0.95}
                            />
                            {drawBadge(n, badgeVariant, showBadgeIcon)}
                            <Text
                                x={n.x + 18}
                                y={n.y + n.h / 2 + valueFont / 3}
                                fontSize={valueFont}
                                fontWeight="800"
                                fill={text}
                            >
                                {fmtKWh(nodeValue)}
                            </Text>
                            <Text
                                x={n.x + 18}
                                y={n.y + n.h / 2 + valueFont / 3 + unitFont + 6}
                                fontSize={unitFont}
                                fill={subtext}
                            >
                                {valueSuffix}
                            </Text>
                            <Text x={n.x + 18} y={n.y + n.h - 10} fontSize={pctFont} fill={subtext}>
                                {fmtPct01(percentByNodeId?.[n.id] ?? percentByNodeId?.[n.label] ?? 0)}
                            </Text>
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
                    const boxW = 196;
                    const boxH = 64;
                    const pad = 12;
                    const bx = clamp(rawX - boxW / 2, 8, (width - 8) - boxW);
                    const by = Math.max(8, rawY - boxH);
                    const pointerX = clamp(rawX, bx + 12, bx + boxW - 12);
                    const label = (nodes.find(n => n.id === focusedNodeId)?.label || focusedNodeId).toUpperCase();

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
                                fontSize={12}
                                fontWeight="700"
                                fill="#CFE6FF"
                            >
                                {label}
                            </Text>
                            <Text
                                x={bx + pad}
                                y={by + 40}
                                fontSize={14}
                                fontWeight="800"
                                fill="#FFFFFF"
                            >
                                In: {fmtKWh(inbound)} {valueSuffix}   Out: {fmtKWh(outbound)} {valueSuffix}
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

            {tip && !enableFocusMode && (
                <View
                    style={[
                        styles.tip,
                        {
                            left: Math.min(Math.max(tip.x - 70, 8), width - 148),
                            top: Math.max(tip.y - 36, 8),
                        },
                    ]}
                    onTouchEnd={() => setTip(null)}
                >
                    <RNText style={styles.tipTxt}>{tip.text}</RNText>
                </View>
            )}
        </Pressable>
    );
};

export default memo(FlowSankey);

const styles = StyleSheet.create({
    wrap: { borderRadius: 16, overflow: "hidden" },
    tip: {
        position: "absolute",
        paddingHorizontal: 10,
        paddingVertical: 6,
        backgroundColor: "rgba(0,0,0,0.8)",
        borderRadius: 8,
    },
    tipTxt: { color: "#fff", fontSize: 12, fontWeight: "600" },
});