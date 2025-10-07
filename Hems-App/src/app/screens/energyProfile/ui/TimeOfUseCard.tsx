import React, { useState } from "react";
import { View, Text, TouchableOpacity, Switch, LayoutAnimation } from "react-native";
import FontAwesome from "@react-native-vector-icons/fontawesome";
import ModeSelectionPanel from "./ModeSelectionPanel";

export default function TimeOfUseCard({ styles, COLORS, setIsLoading }: any) {
    const [showTouSchedule, setShowTouSchedule] = useState(false);
    const [selectedPeriod, setSelectedPeriod] = useState("P3");
    const [selectedExport, setSelectedExport] = useState("Limited");
    const [gridCharging, setGridCharging] = useState(false);
    const [generatorCharging, setGeneratorCharging] = useState(false);
    const [showCharging, setShowCharging] = useState(false);
    const [showExportPanel, setShowExportPanel] = useState(false);

    const getPeriodTime = (p: string) => {
        const map: any = {
            P1: "00:00–04:00",
            P2: "04:00–08:00",
            P3: "08:00–12:00",
            P4: "12:00–16:00",
            P5: "16:00–20:00",
            P6: "20:00–00:00",
        };
        return map[p];
    };

    const getDischargePower = (p: string) => ({ P1: 2, P2: 2.5, P3: 3.5, P4: 3, P5: 4, P6: 2.8 }[p]);
    const getTargetSOC = (p: string) => ({ P1: 60, P2: 65, P3: 50, P4: 55, P5: 45, P6: 70 }[p]);

    return (
        <View style={styles.mainCard}>
            <Text style={styles.cardTitle}>Time-of-Use (TOU) Scheduling</Text>

            <TouchableOpacity
                activeOpacity={0.8}
                onPress={() => {
                    LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
                    setShowTouSchedule(!showTouSchedule);
                }}
            >
                <View style={styles.dropdownRow}>
                    <Text style={styles.subCardTitle}>Daily Schedule</Text>
                    <FontAwesome
                        name={showTouSchedule ? "chevron-up" : "chevron-down"}
                        size={16}
                        color={COLORS.textSoft}
                    />
                </View>
            </TouchableOpacity>

            {showTouSchedule && (
                <View style={styles.subCard}>
                    <View style={{ flexDirection: "row", justifyContent: "space-between" }}>
                        {["12AM", "6AM", "12PM", "6PM", "12AM"].map((t, index) => (
                            <Text key={`${t}-${index}`} style={[styles.valueText, { fontSize: 13 }]}>
                                {t}
                            </Text>
                        ))}
                    </View>

                    <View
                        style={{
                            flexDirection: "row",
                            borderWidth: 1,
                            borderColor: COLORS.border,
                            borderRadius: 6,
                            marginTop: 6,
                            overflow: "hidden",
                        }}
                    >
                        {["P1", "P2", "P3", "P4", "P5", "P6"].map((p) => (
                            <TouchableOpacity
                                key={p}
                                style={{
                                    flex: 1,
                                    alignItems: "center",
                                    paddingVertical: 10,
                                    backgroundColor: selectedPeriod === p ? COLORS.accent + "22" : COLORS.surface,
                                    borderRightWidth: p !== "P6" ? 1 : 0,
                                    borderColor: COLORS.border,
                                }}
                                onPress={() => setSelectedPeriod(p)}
                            >
                                <Text
                                    style={[
                                        styles.valueText,
                                        selectedPeriod === p && { color: COLORS.accent, fontWeight: "700" },
                                    ]}
                                >
                                    {p}
                                </Text>
                            </TouchableOpacity>
                        ))}
                    </View>

                    <Text style={[styles.valueAccent, { marginTop: 10 }]}>
                        {selectedPeriod} ({getPeriodTime(selectedPeriod)})
                    </Text>
                    <Text style={styles.valueText}>
                        Battery Discharge:{" "}
                        <Text style={styles.valueAccent}>{getDischargePower(selectedPeriod)} kW</Text>
                    </Text>
                    <Text style={styles.valueText}>
                        Target SOC: <Text style={styles.valueAccent}>{getTargetSOC(selectedPeriod)}%</Text>
                    </Text>

                    <Text style={[styles.subHeader, { marginTop: 20 }]}>Active Days:</Text>
                    <View
                        style={{
                            flexDirection: "row",
                            flexWrap: "wrap",
                            justifyContent: "space-between",
                            marginTop: 8,
                        }}
                    >
                        {["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"].map(
                            (day) => (
                                <View
                                    key={day}
                                    style={{
                                        width: "48%",
                                        flexDirection: "row",
                                        alignItems: "center",
                                        marginVertical: 6,
                                    }}
                                >
                                    <Switch
                                        value={false}
                                        onValueChange={() => { }}
                                        thumbColor={COLORS.surface}
                                        trackColor={{ false: COLORS.border, true: COLORS.accent + "66" }}
                                    />
                                    <Text style={[styles.label, { marginLeft: 8, fontSize: 15 }]}>{day}</Text>
                                </View>
                            )
                        )}
                    </View>

                    <TouchableOpacity
                        activeOpacity={0.8}
                        onPress={() => {
                            LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
                            setShowCharging(!showCharging);
                        }}
                    >
                        <View style={styles.dropdownRow}>
                            <Text style={styles.subCardTitle}>Charging Options</Text>
                            <FontAwesome
                                name={showCharging ? "chevron-up" : "chevron-down"}
                                size={16}
                                color={COLORS.textSoft}
                            />
                        </View>
                    </TouchableOpacity>

                    {showCharging && (
                        <View style={{ marginTop: 4 }}>
                            <View style={styles.switchRow}>
                                <Text style={styles.label}>Grid Charging</Text>
                                <Switch
                                    value={gridCharging}
                                    onValueChange={(val) => {
                                        setGridCharging(val);
                                        if (val) setGeneratorCharging(false);
                                    }}
                                    thumbColor={gridCharging ? COLORS.accent : COLORS.surface}
                                    trackColor={{ false: COLORS.border, true: COLORS.accent + "66" }}
                                />
                            </View>
                            <View style={styles.switchRow}>
                                <Text style={styles.label}>Generator Charging</Text>
                                <Switch
                                    value={generatorCharging}
                                    onValueChange={(val) => {
                                        setGeneratorCharging(val);
                                        if (val) setGridCharging(false);
                                    }}
                                    thumbColor={generatorCharging ? COLORS.accent : COLORS.surface}
                                    trackColor={{ false: COLORS.border, true: COLORS.accent + "66" }}
                                />
                            </View>
                        </View>
                    )}

                    <TouchableOpacity
                        activeOpacity={0.8}
                        onPress={() => {
                            LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
                            setShowExportPanel(!showExportPanel);
                        }}
                    >
                        <View style={styles.dropdownRow}>
                            <Text style={styles.subCardTitle}>Export Mode</Text>
                            <Text style={styles.valueAccent}>{selectedExport}</Text>
                            <FontAwesome
                                name={showExportPanel ? "chevron-up" : "chevron-down"}
                                size={16}
                                color={COLORS.textSoft}
                            />
                        </View>
                    </TouchableOpacity>
                </View>
            )}

            {showExportPanel && (
                <ModeSelectionPanel
                    type="export"
                    selectedExport={selectedExport}
                    onSelectExport={setSelectedExport}
                    onSave={() => {
                        setShowExportPanel(false);
                        setIsLoading(true);
                        setTimeout(() => setIsLoading(false), 1500);
                    }}
                    visible={showExportPanel}
                />
            )}

        </View>
    );
}
