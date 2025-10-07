import React from "react";
import { Dimensions, View, Text, StyleSheet, SafeAreaView, ScrollView } from "react-native";
import FlowSankey, { SankeyNode, SankeyLink } from "../../../components/FlowSankey";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors } from "../../../theme/constants";

const EnergyFlowScreen = () => {
    const mode = useThemeStore(s => s.mode);
    const isDark = mode === "dark";
    const COLORS = getThemeColors(isDark);
    const width = Dimensions.get("window").width - 24;

    const nodes: SankeyNode[] = [
        { id: "SOLAR", label: "SOLAR", side: "left" },
        { id: "BATTERY", label: "BATTERY", side: "left" },
        { id: "GRID", label: "GRID", side: "left" },
        { id: "BATTERY_R", label: "BATTERY", side: "right" },
        { id: "LOAD", label: "LOAD", side: "right" },
        { id: "GRID_R", label: "GRID", side: "right" },
    ];

    const links: SankeyLink[] = [
        { source: "GRID", target: "BATTERY_R", value: 0.41 },
        { source: "BATTERY", target: "BATTERY_R", value: 0.41 },
        { source: "GRID", target: "LOAD", value: 0.01 },
        { source: "BATTERY", target: "LOAD", value: 0.0 },
    ];

    const percentByNodeId = {
        GRID: 0.9762,
        BATTERY_R: 0.9762,
        GRID_R: 0.0,
        LOAD: 0.0,
    };

    return (
        <SafeAreaView style={[styles.screen, { backgroundColor: COLORS.dark }]}>
            <ScrollView contentContainerStyle={styles.scrollContent}>
                <Text style={[styles.title, { color: COLORS.textSoft }]}>Energy Flow</Text>
                <Text style={[styles.subtitle, { color: COLORS.textSoft }]}>
                    Real-time energy distribution visualization
                </Text>
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
});