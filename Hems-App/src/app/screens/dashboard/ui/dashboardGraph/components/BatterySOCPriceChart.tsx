import React, { useState, useMemo } from 'react';
import { View, Text, Switch, ScrollView, LayoutChangeEvent, Platform } from 'react-native';
import Svg, { Defs, LinearGradient, Stop, Rect, Path, Circle, Text as SvgText } from 'react-native-svg';
import Animated, { FadeInUp, useSharedValue, useAnimatedStyle, withSpring } from 'react-native-reanimated';
import { makeBatterySOCPriceChartStyles } from './BatterySOCPriceChart.styles';

type Props = {
    COLORS: Record<string, string>;
    isDark: boolean;
};

type Datum = { x: string | number; y: number };

export default function BatterySOCPriceChart ({
    COLORS,
    isDark
}: Props) {
    const [buyingPriceEnabled, setBuyingPriceEnabled] = useState(true);
    const [sellingPriceEnabled, setSellingPriceEnabled] = useState(true);
    const [batterySOCEnabled, setBatterySOCEnabled] = useState(true);
    const [chartSize, setChartSize] = useState({ width: 0, height: 0 });

    const styles = makeBatterySOCPriceChartStyles(isDark, COLORS);
    const chartScale = useSharedValue(1);
    const padding = { top: 20, right: 20, bottom: 40, left: 40 };

    const timeLabels = ['00:00', '04:00', '08:00', '12:00', '16:00', '20:00', '24:00'];
    const buyingPriceData: Datum[] = [
        { x: '00:00', y: 0.12 },
        { x: '04:00', y: 0.15 },
        { x: '08:00', y: 0.18 },
        { x: '12:00', y: 0.22 },
        { x: '16:00', y: 0.19 },
        { x: '20:00', y: 0.16 },
        { x: '24:00', y: 0.14 },
    ];
    const sellingPriceData: Datum[] = [
        { x: '00:00', y: 0.08 },
        { x: '04:00', y: 0.11 },
        { x: '08:00', y: 0.14 },
        { x: '12:00', y: 0.18 },
        { x: '16:00', y: 0.15 },
        { x: '20:00', y: 0.12 },
        { x: '24:00', y: 0.10 },
    ];
    const batterySOCData: Datum[] = [
        { x: '00:00', y: 45 },
        { x: '04:00', y: 52 },
        { x: '08:00', y: 38 },
        { x: '12:00', y: 65 },
        { x: '16:00', y: 72 },
        { x: '20:00', y: 58 },
        { x: '24:00', y: 48 },
    ];

    const { innerW, innerH } = useMemo(() => {
        const cw = Math.max(chartSize.width - padding.left - padding.right, 1);
        const ch = Math.max(chartSize.height - padding.top - padding.bottom, 1);
        return { innerW: cw, innerH: ch };
    }, [chartSize]);

    const onChartLayout = (e: LayoutChangeEvent) => {
        const { width: w, height: h } = e.nativeEvent.layout;
        if (w !== chartSize.width || h !== chartSize.height) {
            setChartSize({ width: w, height: h });
            chartScale.value = withSpring(1, { damping: 15, stiffness: 150 });
        }
    };

    const animatedChartStyle = useAnimatedStyle(() => ({
        transform: [{ scale: chartScale.value }],
    }));

    const priceValues = [
        ...(buyingPriceEnabled ? buyingPriceData.map(d => d.y) : []),
        ...(sellingPriceEnabled ? sellingPriceData.map(d => d.y) : []),
    ];

    const batteryValues = batterySOCEnabled ? batterySOCData.map(d => d.y) : [];

    const priceMax = Math.max(...priceValues) * 1.1 || 0.25;
    const priceMin = Math.min(...priceValues) * 0.9 || 0;
    const priceRange = priceMax - priceMin;

    const batteryMax = Math.max(...batteryValues) * 1.1 || 100;
    const batteryMin = Math.min(...batteryValues) * 0.9 || 0;
    const batteryRange = batteryMax - batteryMin;

    const yScalePrice = (v: number) => innerH - ((v - priceMin) / priceRange) * innerH;
    const yScaleBattery = (v: number) => innerH - ((v - batteryMin) / batteryRange) * innerH;
    const stepX = innerW / Math.max(timeLabels.length - 1, 1);

    const createPath = (data: Datum[], isBattery = false) => {
        const points = data.map((d, i) => ({
            x: padding.left + i * stepX,
            y: padding.top + (isBattery ? yScaleBattery(d.y) : yScalePrice(d.y)),
        }));
        return points.map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`)).join(' ');
    };

    const buyingPath = buyingPriceEnabled ? createPath(buyingPriceData, false) : '';
    const sellingPath = sellingPriceEnabled ? createPath(sellingPriceData, false) : '';
    const batteryPath = batterySOCEnabled ? createPath(batterySOCData, true) : '';

    return (
        <Animated.View entering={FadeInUp.delay(200).duration(600)} style={styles.container}>
            <View style={styles.header}>
                <Text style={styles.title}>Battery SOC & Price Trend</Text>
            </View>

            <Animated.View style={[styles.chartContainer, animatedChartStyle]} onLayout={onChartLayout}>
                <Svg width={chartSize.width} height={chartSize.height}>
                    <Defs>
                        <LinearGradient id="buyingGrad" x1="0" y1="0" x2="0" y2="1">
                            <Stop offset="0%" stopColor="#22C55E" stopOpacity={0.3} />
                            <Stop offset="100%" stopColor="#22C55E" stopOpacity={0.05} />
                        </LinearGradient>
                        <LinearGradient id="sellingGrad" x1="0" y1="0" x2="0" y2="1">
                            <Stop offset="0%" stopColor="#EF4444" stopOpacity={0.3} />
                            <Stop offset="100%" stopColor="#EF4444" stopOpacity={0.05} />
                        </LinearGradient>
                        <LinearGradient id="batteryGrad" x1="0" y1="0" x2="0" y2="1">
                            <Stop offset="0%" stopColor="#3B82F6" stopOpacity={0.3} />
                            <Stop offset="100%" stopColor="#3B82F6" stopOpacity={0.05} />
                        </LinearGradient>
                    </Defs>

                    {Array.from({ length: 5 }).map((_, i) => {
                        const y = padding.top + (i * innerH) / 4;
                        return (
                            <Rect
                                key={i}
                                x={padding.left}
                                y={y}
                                width={innerW}
                                height={1}
                                fill={COLORS.border}
                                opacity={0.3}
                            />
                        );
                    })}

                    {Array.from({ length: 5 }).map((_, i) => {
                        const value = priceMin + (priceRange * (4 - i)) / 4;
                        const y = padding.top + (i * innerH) / 4;
                        return (
                            <SvgText
                                key={`price-${i}`}
                                x={padding.left - 5}
                                y={y + 4}
                                fontSize="10"
                                fill={COLORS.textSoft}
                                textAnchor="end"
                            >
                                {value.toFixed(2)}
                            </SvgText>
                        );
                    })}

                    {Array.from({ length: 5 }).map((_, i) => {
                        const value = batteryMin + (batteryRange * (4 - i)) / 4;
                        const y = padding.top + (i * innerH) / 4;
                        return (
                            <SvgText
                                key={`battery-${i}`}
                                x={padding.left + innerW + 5}
                                y={y + 4}
                                fontSize="10"
                                fill={COLORS.textSoft}
                                textAnchor="start"
                            >
                                {Math.round(value)}%
                            </SvgText>
                        );
                    })}

                    {timeLabels.map((label, i) => {
                        const x = padding.left + i * stepX;
                        return (
                            <SvgText
                                key={i}
                                x={x}
                                y={padding.top + innerH + 15}
                                fontSize="10"
                                fill={COLORS.textSoft}
                                textAnchor="middle"
                            >
                                {label}
                            </SvgText>
                        );
                    })}

                    {buyingPriceEnabled && (
                        <Path
                            d={buyingPath}
                            stroke="#22C55E"
                            strokeWidth={3}
                            fill="none"
                        />
                    )}
                    {sellingPriceEnabled && (
                        <Path
                            d={sellingPath}
                            stroke="#EF4444"
                            strokeWidth={3}
                            fill="none"
                        />
                    )}
                    {batterySOCEnabled && (
                        <Path
                            d={batteryPath}
                            stroke="#3B82F6"
                            strokeWidth={3}
                            fill="none"
                        />
                    )}

                    {buyingPriceEnabled && buyingPriceData.map((d, i) => {
                        const x = padding.left + i * stepX;
                        const y = padding.top + yScalePrice(d.y);
                        return (
                            <Circle
                                key={`buying-${i}`}
                                cx={x}
                                cy={y}
                                r={4}
                                fill="#22C55E"
                            />
                        );
                    })}
                    {sellingPriceEnabled && sellingPriceData.map((d, i) => {
                        const x = padding.left + i * stepX;
                        const y = padding.top + yScalePrice(d.y);
                        return (
                            <Circle
                                key={`selling-${i}`}
                                cx={x}
                                cy={y}
                                r={4}
                                fill="#EF4444"
                            />
                        );
                    })}
                    {batterySOCEnabled && batterySOCData.map((d, i) => {
                        const x = padding.left + i * stepX;
                        const y = padding.top + yScaleBattery(d.y);
                        return (
                            <Circle
                                key={`battery-${i}`}
                                cx={x}
                                cy={y}
                                r={4}
                                fill="#3B82F6"
                            />
                        );
                    })}
                </Svg>
            </Animated.View>

            <View style={styles.switchContainer}>
                <View style={styles.switchItem}>
                    <Text style={styles.switchLabel}>Buy</Text>
                    <Switch
                        value={buyingPriceEnabled}
                        onValueChange={setBuyingPriceEnabled}
                        trackColor={{ false: COLORS.border, true: '#22C55E' }}
                        thumbColor={buyingPriceEnabled ? '#FFFFFF' : COLORS.textSoft}
                        style={{
                            transform: [{ scaleX: Platform.OS === 'android' ? 0.8 : 0.5 }, { scaleY: Platform.OS === 'android' ? 0.8 : 0.5 }],
                            marginTop:0,
                            alignSelf: 'center'
                        }}
                    />
                </View>
                <View style={styles.switchItem}>
                    <Text style={styles.switchLabel}>Sell</Text>
                    <Switch
                        value={sellingPriceEnabled}
                        onValueChange={setSellingPriceEnabled}
                        trackColor={{ false: COLORS.border, true: '#EF4444' }}
                        thumbColor={sellingPriceEnabled ? '#FFFFFF' : COLORS.textSoft}
                        style={{
                            transform: [{ scaleX: Platform.OS === 'android' ? 0.8 : 0.5 }, { scaleY: Platform.OS === 'android' ? 0.8 : 0.5 }],
                            marginTop: 0,
                            alignSelf: 'center'
                        }}
                    />
                </View>
                <View style={styles.switchItem}>
                    <Text style={styles.switchLabel}>SOC</Text>
                    <Switch
                        value={batterySOCEnabled}
                        onValueChange={setBatterySOCEnabled}
                        trackColor={{ false: COLORS.border, true: '#3B82F6' }}
                        thumbColor={batterySOCEnabled ? '#FFFFFF' : COLORS.textSoft}
                        style={{
                            transform: [{ scaleX: Platform.OS === 'android' ? 0.8 : 0.5 }, { scaleY: Platform.OS === 'android' ? 0.8 : 0.5 }],
                            marginTop: 0,
                            alignSelf: 'center'
                        }}
                    />
                </View>
            </View>

            <View style={styles.legend}>
                {buyingPriceEnabled && (
                    <View style={styles.legendItem}>
                        <View style={[styles.legendColor, { backgroundColor: '#22C55E' }]} />
                        <Text style={styles.legendText}>Buying Price (€/kWh)</Text>
                    </View>
                )}
                {sellingPriceEnabled && (
                    <View style={styles.legendItem}>
                        <View style={[styles.legendColor, { backgroundColor: '#EF4444' }]} />
                        <Text style={styles.legendText}>Selling Price (€/kWh)</Text>
                    </View>
                )}
                {batterySOCEnabled && (
                    <View style={styles.legendItem}>
                        <View style={[styles.legendColor, { backgroundColor: '#3B82F6' }]} />
                        <Text style={styles.legendText}>Battery SOC (%)</Text>
                    </View>
                )}
            </View>
        </Animated.View>
    );
}
