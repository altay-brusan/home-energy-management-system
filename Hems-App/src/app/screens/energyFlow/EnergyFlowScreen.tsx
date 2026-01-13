import React, { useState, useEffect, useCallback } from "react";
import { Dimensions, View, Text, StyleSheet, SafeAreaView, ScrollView, TouchableOpacity, ActivityIndicator } from "react-native";
import FlowSankey, { SankeyNode, SankeyLink } from "../../../components/FlowSankey";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors } from "../../../theme/constants";
import { useI18n } from "../../../hooks/useI18n";
import { useSelectedSiteDevice } from "../../../hooks/useSelectedSiteDevice";
import { mobileDashboardService } from "../../../api/services/mobileDashboardService";
// import { Button } from "../../../components"; // Commented out for future use (day/week/month filters)

const EnergyFlowScreen = () => {
    const mode = useThemeStore(s => s.mode);
    const isDark = mode === "dark";
    const COLORS = getThemeColors(isDark);
    const width = Dimensions.get("window").width - 24;
    const { t } = useI18n();
    const { device, hasDevice } = useSelectedSiteDevice();

    const [startDate, setStartDate] = useState<string>(() => {
        const date = new Date();
        date.setDate(date.getDate() - 7);
        return date.toISOString().split('T')[0];
    });
    const [endDate, setEndDate] = useState<string>(() => {
        return new Date().toISOString().split('T')[0];
    });

    const [energyFlowData, setEnergyFlowData] = useState<any>(null);
    const [loading, setLoading] = useState(true);

    const loadEnergyFlowData = useCallback(async () => {
        if (!hasDevice || !device?.id) {
            setLoading(false);
            return;
        }

        setLoading(true);
        try {
            const data = await mobileDashboardService.getEnergyFlow(device.id, startDate, endDate);
            setEnergyFlowData(data);
        } catch (error) {setEnergyFlowData(null);
        } finally {
            setLoading(false);
        }
    }, [hasDevice, device?.id, startDate, endDate]);

    useEffect(() => {
        loadEnergyFlowData();
    }, [loadEnergyFlowData]);

    const handleStartDateChange = (date: string) => {
        setStartDate(date);
    };

    const handleEndDateChange = (date: string) => {
        setEndDate(date);
    };

    // Helper function to limit text length
    const limitText = (text: string, maxLength: number = 8) => {
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength - 3) + '...';
    };

    // Convert API data to Sankey format (fallback to static data if no API data)
    const nodes: SankeyNode[] = energyFlowData?.nodes?.map((node: any) => ({
        ...node,
        label: limitText(node.label || node.id, 8)
    })) || [
            { id: "SOLAR", label: "SOLAR", side: "left" },
            { id: "BATTERY", label: "BATTERY", side: "left" },
            { id: "GRID", label: "GRID", side: "left" },
            { id: "BATTERY_R", label: "BATTERY", side: "right" },
            { id: "LOAD", label: "LOAD", side: "right" },
            { id: "GRID_R", label: "GRID", side: "right" },
        ];

    const links: SankeyLink[] = energyFlowData?.links || [
        { source: "GRID", target: "BATTERY_R", value: 0.41 },
        { source: "BATTERY", target: "BATTERY_R", value: 0.41 },
        { source: "GRID", target: "LOAD", value: 0.01 },
        { source: "BATTERY", target: "LOAD", value: 0.0 },
    ];

    const percentByNodeId = energyFlowData?.percentByNodeId || {
        GRID: 0.9762,
        BATTERY_R: 0.9762,
        GRID_R: 0.0,
        LOAD: 0.0,
    };

    if (!hasDevice || !device?.id) {
        return (
            <SafeAreaView style={[styles.screen, { backgroundColor: COLORS.dark }]}>
                <View style={styles.noDeviceContainer}>
                    <Text style={[styles.noDeviceText, { color: COLORS.textSoft }]}>
                        {t('energyFlow.noDevice')}
                    </Text>
                </View>
            </SafeAreaView>
        );
    }

    return (
        <SafeAreaView style={[styles.screen, { backgroundColor: COLORS.dark }]}>
            <ScrollView contentContainerStyle={styles.scrollContent}>
                <Text style={[styles.title, { color: COLORS.textSoft }]}>{t('energyFlow.title')}</Text>
                <Text style={[styles.subtitle, { color: COLORS.textSoft }]}>
                    {t('energyFlow.subtitle')}
                </Text>

                {/* Date Filters */}
                <View style={[styles.filterContainer, { backgroundColor: COLORS.surface, borderColor: COLORS.border }]}>
                    <View style={styles.dateFilterRow}>
                        <View style={styles.dateInputContainer}>
                            <Text style={[styles.dateLabel, { color: COLORS.textSoft }]}>
                                {t('energyFlow.startDate')}
                            </Text>
                            <TouchableOpacity
                                style={[styles.dateButton, { backgroundColor: COLORS.white, borderColor: COLORS.border }]}
                                onPress={() => {
                                    // TODO: Implement date picker
                                }}
                            >
                                <Text style={[styles.dateButtonText, { color: COLORS.dark }]}>
                                    {startDate}
                                </Text>
                            </TouchableOpacity>
                        </View>

                        <View style={styles.dateInputContainer}>
                            <Text style={[styles.dateLabel, { color: COLORS.textSoft }]}>
                                {t('energyFlow.endDate')}
                            </Text>
                            <TouchableOpacity
                                style={[styles.dateButton, { backgroundColor: COLORS.white, borderColor: COLORS.border }]}
                                onPress={() => {
                                    // TODO: Implement date picker
                                }}
                            >
                                <Text style={[styles.dateButtonText, { color: COLORS.dark }]}>
                                    {endDate}
                                </Text>
                            </TouchableOpacity>
                        </View>
                    </View>
                </View>

                {/* Energy Flow Chart */}
                {loading ? (
                    <View style={styles.loadingContainer}>
                        <ActivityIndicator color={COLORS.accent} size="large" />
                        <Text style={[styles.loadingText, { color: COLORS.textSoft }]}>
                            {t('energyFlow.loading')}
                        </Text>
                    </View>
                ) : (
                    <FlowSankey
                        width={width}
                        height={460}
                        nodes={nodes}
                        links={links}
                        percentByNodeId={percentByNodeId}
                        theme={isDark ? "dark" : "light"}
                        layoutOpts={{
                            nodeWidth: 108,
                            minHeight: 76,
                            gapY: 12,
                            padding: 8,
                            curviness: 0.18,
                            fontScale: 0.88,
                        }}
                    />
                )}
            </ScrollView>
        </SafeAreaView>
    );
};

export default EnergyFlowScreen;

const styles = StyleSheet.create({
    screen: {
        flex: 1,
    },
    scrollContent: {
        padding: 12,
        paddingTop: 24,
    },
    title: {
        fontSize: 28,
        fontWeight: "800",
        marginBottom: 8,
        textAlign: "center",
    },
    subtitle: {
        fontSize: 14,
        fontWeight: "500",
        marginBottom: 20,
        textAlign: "center",
        opacity: 0.7,
    },
    // Date filter styles
    filterContainer: {
        marginBottom: 20,
        padding: 16,
        borderRadius: 12,
        borderWidth: 1,
    },
    dateFilterRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        gap: 12,
    },
    dateInputContainer: {
        flex: 1,
    },
    dateLabel: {
        fontSize: 14,
        fontWeight: "600",
        marginBottom: 8,
    },
    dateButton: {
        padding: 12,
        borderRadius: 8,
        borderWidth: 1,
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: 44,
    },
    dateButtonText: {
        fontSize: 14,
        fontWeight: "500",
    },
    // Loading styles
    loadingContainer: {
        height: 460,
        justifyContent: 'center',
        alignItems: 'center',
    },
    loadingText: {
        marginTop: 12,
        fontSize: 14,
        fontWeight: "500",
    },
    // No device styles
    noDeviceContainer: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        padding: 20,
    },
    noDeviceText: {
        fontSize: 16,
        fontWeight: "500",
        textAlign: 'center',
        opacity: 0.7,
    },
});