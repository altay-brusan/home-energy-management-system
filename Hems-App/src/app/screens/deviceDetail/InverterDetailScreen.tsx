import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation, useRoute } from "@react-navigation/native";
import { ArrowLeft, Zap } from 'lucide-react-native';
import React, { useEffect, useState } from "react";
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View
} from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { deviceComponentsService } from "../../../api/services/deviceComponentsService";
import { useI18n } from "../../../hooks/useI18n";
import { useThemeStore } from "../../../store/themeStore";
import { useSettingsStore } from "../../../store/settingsStore";
import { getThemeColors, SPACING, TYPOGRAPHY } from "../../../theme/constants";
import { formatTemperature } from "../../../utils/temperatureUtils";
import type { DeviceDetailsResponseDto } from "../../../api/types/deviceComponents.types";

const formatDateTime = (isoString: string | null | undefined, locale: string = 'tr-TR'): string => {
  if (!isoString || isoString === "-") return "-";

  try {
    const date = new Date(isoString);
    if (isNaN(date.getTime())) return isoString;

    return date.toLocaleString(locale, {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false
    });
  } catch (error) {
    return isoString;
  }
};

export default function InverterDetailScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const isDark = useThemeStore((s) => s.mode) === "dark";
  const temperatureUnit = useSettingsStore((s) => s.temperatureUnit);
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();
  const { t, language } = useI18n();
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const { componentId, deviceId } = route.params || {};

  const [refreshing, setRefreshing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [detailsData, setDetailsData] = useState<DeviceDetailsResponseDto | null>(null);
  const [pollingInterval, setPollingInterval] = useState<NodeJS.Timeout | null>(null);

  // Helper function to get value from details
  const getDetailValue = (key: string): any => {
    const item = detailsData?.details.find(d => d.key === key);
    return item?.value;
  };

  // Fetch component details from Details API
  const fetchComponentDetails = async () => {
    if (!componentId) return;

    try {
      const details = await deviceComponentsService.getComponentDetails(componentId);
      setDetailsData(details);
    } catch (error) {
      console.error('Error fetching component details:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchComponentDetails();
  }, [componentId]);

  // Polling: Fetch details every 5 seconds
  useEffect(() => {
    if (!componentId) return;

    const interval = setInterval(() => {
      fetchComponentDetails();
    }, 15000); // Poll every 15 seconds

    setPollingInterval(interval);

    return () => {
      if (interval) {
        clearInterval(interval);
      }
    };
  }, [componentId]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchComponentDetails();
    setRefreshing(false);
  };

  if (loading && !detailsData) {
    return (
      <View
        style={[
          styles.container,
          {
            backgroundColor: COLORS.dark,
            justifyContent: "center",
            alignItems: "center",
          },
        ]}
      >
        <ActivityIndicator size="large" color={COLORS.accent} />
        <Text style={{ color: COLORS.textSoft, marginTop: 10 }}>
          {t("common.loading")}
        </Text>
      </View>
    );
  }

  // Get values from Details API (only what's actually returned)
  const runState = getDetailValue('runState');
  const outputActivePower = getDetailValue('outputActivePower') || 0;
  const phases = getDetailValue('Phases') as Array<{ "Phase Id": number; Voltage: number; Current: number; Power: number }> | null;
  const outputFrequency = getDetailValue('outputFrequency');
  const gridSideActivePower = getDetailValue('gridSideActivePower');
  const gridSidePhaseAPower = getDetailValue('gridSidePhaseAPower');
  const gridSidePhaseBPower = getDetailValue('gridSidePhaseBPower');
  const gridSidePhaseCPower = getDetailValue('gridSidePhaseCPower');

  // Load data
  const loadTotalActivePower = getDetailValue('loadTotalActivePower');
  const loadPhaseAPower = getDetailValue('loadPhaseAPower');
  const loadPhaseBPower = getDetailValue('loadPhaseBPower');
  const loadPhaseCPower = getDetailValue('loadPhaseCPower');
  const loadPhaseAVoltage = getDetailValue('loadPhaseAVoltage');
  const loadPhaseBVoltage = getDetailValue('loadPhaseBVoltage');
  const loadPhaseCVoltage = getDetailValue('loadPhaseCVoltage');
  const loadFrequency = getDetailValue('loadFrequency');

  const inverterHeatsinkTemp = getDetailValue('inverterHeatsinkTemp');
  const internalAmbientTemp = getDetailValue('internalAmbientTemp');
  const generatorSignalPresent = getDetailValue('generatorSignalPresent');
  const manufacturer = getDetailValue('manufacturer');
  const model = getDetailValue('model');
  const serialNumber = getDetailValue('serialNumber');
  const ratedPower = getDetailValue('ratedPower');

  const powerColor = outputActivePower > 0 ? COLORS.success : COLORS.textSoft;

  return (
    <View
      style={[
        styles.container,
        {
          backgroundColor: COLORS.dark,
          paddingTop: insets.top,
        },
      ]}
    >
      {/* Header */}
      <View
        style={[
          styles.header,
          {
            backgroundColor: COLORS.dark,
          },
        ]}
      >
        <Pressable
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <ArrowLeft size={24} color={COLORS.textSoft} strokeWidth={2} />
        </Pressable>
        <Text
          style={[
            styles.headerTitle,
            {
              color: COLORS.textSoft,
            },
          ]}
        >
          {t("inverterDetail.title")}
        </Text>
        <View style={{ width: 40 }} />
      </View>

      {/* Content */}
      <ScrollView
        contentContainerStyle={[
          styles.content,
          {
            paddingBottom: insets.bottom + 20,
          },
        ]}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={COLORS.accent}
          />
        }
      >
        {/* Hero Card */}
        <View
          style={[
            styles.heroCard,
            {
              backgroundColor: COLORS.surface,
              borderColor: COLORS.border,
            },
          ]}
        >
          <View
            style={[
              styles.iconContainer,
              {
                backgroundColor: `${powerColor}15`,
              },
            ]}
          >
            <Zap size={48} color={powerColor} />
          </View>
          <Text
            style={[
              styles.powerValue,
              {
                color: powerColor,
              },
            ]}
          >
            {outputActivePower?.toFixed(1) || 0} W
          </Text>
          <Text
            style={[
              styles.powerLabel,
              {
                color: COLORS.textSoft,
              },
            ]}
          >
            {t("inverterDetail.activePower")}
          </Text>
        </View>

        {/* Run State Badge */}
        {runState && (
          <View
            style={[
              styles.section,
              {
                backgroundColor: COLORS.surface,
                borderColor: COLORS.border,
                alignItems: 'center',
              },
            ]}
          >
            <View
              style={{
                paddingHorizontal: 16,
                paddingVertical: 8,
                borderRadius: 20,
                backgroundColor:
                  runState === 'normal' ? `${COLORS.success}20` :
                  runState === 'alarm' ? `${COLORS.warning}20` :
                  `${COLORS.error}20`,
              }}
            >
              <Text
                style={{
                  fontSize: 14,
                  fontWeight: '600',
                  color:
                    runState === 'normal' ? COLORS.success :
                    runState === 'alarm' ? COLORS.warning :
                    COLORS.error,
                  textTransform: 'capitalize',
                }}
              >
                {runState}
              </Text>
            </View>
          </View>
        )}

        {/* Output Section */}
        <View
          style={[
            styles.section,
            {
              backgroundColor: COLORS.surface,
              borderColor: COLORS.border,
            },
          ]}
        >
          <Text
            style={[
              styles.sectionTitle,
              {
                color: COLORS.text,
              },
            ]}
          >
            {t("inverterDetail.output")}
          </Text>

          {/* Output Frequency - single value shared across all phases */}
          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.outputFrequency")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {outputFrequency != null ? `${outputFrequency.toFixed(1)} Hz` : "-"}
            </Text>
          </View>

          {/* Per-phase output data (Voltage, Current, Power) */}
          {phases && phases.length > 0 && phases.map((phase, index) => (
            <View key={phase["Phase Id"] || index} style={[styles.phaseCard, { backgroundColor: `${COLORS.accent}08`, marginTop: index === 0 ? 12 : 8, borderRadius: 8, padding: 12 }]}>
              <Text
                style={[
                  styles.phaseTitle,
                  {
                    color: COLORS.accent,
                    marginBottom: 8,
                  },
                ]}
              >
                {t("inverterDetail.phaseLabel", { id: phase["Phase Id"] || index + 1 })}
              </Text>

              <View style={styles.row}>
                <Text style={[styles.label, { color: COLORS.textSoft }]}>
                  {t("inverterDetail.voltage")}
                </Text>
                <Text style={[styles.value, { color: COLORS.text }]}>
                  {phase.Voltage?.toFixed(1) || 0} V
                </Text>
              </View>

              <View style={styles.row}>
                <Text style={[styles.label, { color: COLORS.textSoft }]}>
                  {t("inverterDetail.current")}
                </Text>
                <Text style={[styles.value, { color: COLORS.text }]}>
                  {phase.Current?.toFixed(2) || 0} A
                </Text>
              </View>

              <View style={styles.row}>
                <Text style={[styles.label, { color: COLORS.textSoft }]}>
                  {t("inverterDetail.power")}
                </Text>
                <Text style={[styles.value, { color: COLORS.text }]}>
                  {phase.Power?.toFixed(0) || 0} W
                </Text>
              </View>
            </View>
          ))}
        </View>

        {/* Grid Section */}
        {(gridSideActivePower != null || gridSidePhaseAPower != null || gridSidePhaseBPower != null || gridSidePhaseCPower != null) && (
          <View
            style={[
              styles.section,
              {
                backgroundColor: COLORS.surface,
                borderColor: COLORS.border,
              },
            ]}
          >
            <Text
              style={[
                styles.sectionTitle,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {t("inverterDetail.grid")}
            </Text>

            {gridSideActivePower != null && (
              <View style={styles.row}>
                <Text
                  style={[
                    styles.label,
                    {
                      color: COLORS.textSoft,
                    },
                  ]}
                >
                  {t("inverterDetail.gridPower")}
                </Text>
                <Text
                  style={[
                    styles.value,
                    {
                      color: gridSideActivePower < 0 ? COLORS.success : gridSideActivePower > 0 ? COLORS.warning : COLORS.textSoft,
                    },
                  ]}
                >
                  {gridSideActivePower === 0 ? '0 W' : `${Math.abs(gridSideActivePower).toFixed(1)} W ${gridSideActivePower < 0 ? '(Export)' : '(Import)'}`}
                </Text>
              </View>
            )}

            {/* Phase A Card */}
            {gridSidePhaseAPower != null && (
              <View style={[styles.phaseCard, { backgroundColor: `${COLORS.accent}08`, marginTop: 12, borderRadius: 8, padding: 12 }]}>
                <Text style={[styles.phaseTitle, { color: COLORS.accent, marginBottom: 8 }]}>
                  {t("inverterDetail.phaseLabel", { id: "A" })}
                </Text>
                <View style={styles.row}>
                  <Text style={[styles.label, { color: COLORS.textSoft }]}>
                    {t("inverterDetail.power")}
                  </Text>
                  <Text style={[styles.value, { color: COLORS.text }]}>
                    {gridSidePhaseAPower.toFixed(1)} W
                  </Text>
                </View>
              </View>
            )}

            {/* Phase B Card */}
            {gridSidePhaseBPower != null && (
              <View style={[styles.phaseCard, { backgroundColor: `${COLORS.accent}08`, marginTop: 8, borderRadius: 8, padding: 12 }]}>
                <Text style={[styles.phaseTitle, { color: COLORS.accent, marginBottom: 8 }]}>
                  {t("inverterDetail.phaseLabel", { id: "B" })}
                </Text>
                <View style={styles.row}>
                  <Text style={[styles.label, { color: COLORS.textSoft }]}>
                    {t("inverterDetail.power")}
                  </Text>
                  <Text style={[styles.value, { color: COLORS.text }]}>
                    {gridSidePhaseBPower.toFixed(1)} W
                  </Text>
                </View>
              </View>
            )}

            {/* Phase C Card */}
            {gridSidePhaseCPower != null && (
              <View style={[styles.phaseCard, { backgroundColor: `${COLORS.accent}08`, marginTop: 8, borderRadius: 8, padding: 12 }]}>
                <Text style={[styles.phaseTitle, { color: COLORS.accent, marginBottom: 8 }]}>
                  {t("inverterDetail.phaseLabel", { id: "C" })}
                </Text>
                <View style={styles.row}>
                  <Text style={[styles.label, { color: COLORS.textSoft }]}>
                    {t("inverterDetail.power")}
                  </Text>
                  <Text style={[styles.value, { color: COLORS.text }]}>
                    {gridSidePhaseCPower.toFixed(1)} W
                  </Text>
                </View>
              </View>
            )}

            {generatorSignalPresent != null && (
              <View style={styles.row}>
                <Text
                  style={[
                    styles.label,
                    {
                      color: COLORS.textSoft,
                    },
                  ]}
                >
                  {t("inverterDetail.generator")}
                </Text>
                <Text
                  style={[
                    styles.value,
                    {
                      color: generatorSignalPresent ? COLORS.success : COLORS.textSoft,
                    },
                  ]}
                >
                  {generatorSignalPresent ? t("inverterDetail.active") : t("inverterDetail.inactive")}
                </Text>
              </View>
            )}
          </View>
        )}

        {/* Load Section */}
        {(loadTotalActivePower != null || loadPhaseAPower != null || loadPhaseBPower != null || loadPhaseCPower != null) && (
          <View
            style={[
              styles.section,
              {
                backgroundColor: COLORS.surface,
                borderColor: COLORS.border,
              },
            ]}
          >
            <Text
              style={[
                styles.sectionTitle,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {t("inverterDetail.load")}
            </Text>

            {loadTotalActivePower != null && (
              <View style={styles.row}>
                <Text
                  style={[
                    styles.label,
                    {
                      color: COLORS.textSoft,
                    },
                  ]}
                >
                  {t("inverterDetail.loadPower")}
                </Text>
                <Text
                  style={[
                    styles.value,
                    {
                      color: COLORS.text,
                    },
                  ]}
                >
                  {loadTotalActivePower.toFixed(1)} W
                </Text>
              </View>
            )}

            {loadFrequency != null && (
              <View style={styles.row}>
                <Text
                  style={[
                    styles.label,
                    {
                      color: COLORS.textSoft,
                    },
                  ]}
                >
                  {t("inverterDetail.loadFrequency")}
                </Text>
                <Text
                  style={[
                    styles.value,
                    {
                      color: COLORS.text,
                    },
                  ]}
                >
                  {loadFrequency.toFixed(2)} Hz
                </Text>
              </View>
            )}

            {/* Phase A Card */}
            {(loadPhaseAPower != null || loadPhaseAVoltage != null) && (
              <View style={[styles.phaseCard, { backgroundColor: `${COLORS.accent}08`, marginTop: 12, borderRadius: 8, padding: 12 }]}>
                <Text style={[styles.phaseTitle, { color: COLORS.accent, marginBottom: 8 }]}>
                  {t("inverterDetail.phaseLabel", { id: "A" })}
                </Text>
                {loadPhaseAVoltage != null && (
                  <View style={styles.row}>
                    <Text style={[styles.label, { color: COLORS.textSoft }]}>
                      {t("inverterDetail.voltage")}
                    </Text>
                    <Text style={[styles.value, { color: COLORS.text }]}>
                      {loadPhaseAVoltage.toFixed(1)} V
                    </Text>
                  </View>
                )}
                {loadPhaseAPower != null && (
                  <View style={styles.row}>
                    <Text style={[styles.label, { color: COLORS.textSoft }]}>
                      {t("inverterDetail.power")}
                    </Text>
                    <Text style={[styles.value, { color: COLORS.text }]}>
                      {loadPhaseAPower.toFixed(1)} W
                    </Text>
                  </View>
                )}
              </View>
            )}

            {/* Phase B Card */}
            {(loadPhaseBPower != null || loadPhaseBVoltage != null) && (
              <View style={[styles.phaseCard, { backgroundColor: `${COLORS.accent}08`, marginTop: 8, borderRadius: 8, padding: 12 }]}>
                <Text style={[styles.phaseTitle, { color: COLORS.accent, marginBottom: 8 }]}>
                  {t("inverterDetail.phaseLabel", { id: "B" })}
                </Text>
                {loadPhaseBVoltage != null && (
                  <View style={styles.row}>
                    <Text style={[styles.label, { color: COLORS.textSoft }]}>
                      {t("inverterDetail.voltage")}
                    </Text>
                    <Text style={[styles.value, { color: COLORS.text }]}>
                      {loadPhaseBVoltage.toFixed(1)} V
                    </Text>
                  </View>
                )}
                {loadPhaseBPower != null && (
                  <View style={styles.row}>
                    <Text style={[styles.label, { color: COLORS.textSoft }]}>
                      {t("inverterDetail.power")}
                    </Text>
                    <Text style={[styles.value, { color: COLORS.text }]}>
                      {loadPhaseBPower.toFixed(1)} W
                    </Text>
                  </View>
                )}
              </View>
            )}

            {/* Phase C Card */}
            {(loadPhaseCPower != null || loadPhaseCVoltage != null) && (
              <View style={[styles.phaseCard, { backgroundColor: `${COLORS.accent}08`, marginTop: 8, borderRadius: 8, padding: 12 }]}>
                <Text style={[styles.phaseTitle, { color: COLORS.accent, marginBottom: 8 }]}>
                  {t("inverterDetail.phaseLabel", { id: "C" })}
                </Text>
                {loadPhaseCVoltage != null && (
                  <View style={styles.row}>
                    <Text style={[styles.label, { color: COLORS.textSoft }]}>
                      {t("inverterDetail.voltage")}
                    </Text>
                    <Text style={[styles.value, { color: COLORS.text }]}>
                      {loadPhaseCVoltage.toFixed(1)} V
                    </Text>
                  </View>
                )}
                {loadPhaseCPower != null && (
                  <View style={styles.row}>
                    <Text style={[styles.label, { color: COLORS.textSoft }]}>
                      {t("inverterDetail.power")}
                    </Text>
                    <Text style={[styles.value, { color: COLORS.text }]}>
                      {loadPhaseCPower.toFixed(1)} W
                    </Text>
                  </View>
                )}
              </View>
            )}
          </View>
        )}

        {/* Temperature Section */}
        {(inverterHeatsinkTemp != null || internalAmbientTemp != null) && (
          <View
            style={[
              styles.section,
              {
                backgroundColor: COLORS.surface,
                borderColor: COLORS.border,
              },
            ]}
          >
            <Text
              style={[
                styles.sectionTitle,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {t("inverterDetail.temperature")}
            </Text>

            {inverterHeatsinkTemp != null && (
              <View style={styles.row}>
                <Text
                  style={[
                    styles.label,
                    {
                      color: COLORS.textSoft,
                    },
                  ]}
                >
                  {t("inverterDetail.heatsinkTemp")}
                </Text>
                <Text
                  style={[
                    styles.value,
                    {
                      color: COLORS.text,
                    },
                  ]}
                >
                  {formatTemperature(inverterHeatsinkTemp, temperatureUnit)}
                </Text>
              </View>
            )}

            {internalAmbientTemp != null && (
              <View style={styles.row}>
                <Text
                  style={[
                    styles.label,
                    {
                      color: COLORS.textSoft,
                    },
                  ]}
                >
                  {t("inverterDetail.ambientTemp")}
                </Text>
                <Text
                  style={[
                    styles.value,
                    {
                      color: COLORS.text,
                    },
                  ]}
                >
                  {formatTemperature(internalAmbientTemp, temperatureUnit)}
                </Text>
              </View>
            )}
          </View>
        )}

        {/* Device Info Section */}
        <View
          style={[
            styles.section,
            {
              backgroundColor: COLORS.surface,
              borderColor: COLORS.border,
            },
          ]}
        >
          <Text
            style={[
              styles.sectionTitle,
              {
                color: COLORS.text,
              },
            ]}
          >
            {t("inverterDetail.deviceInfo")}
          </Text>

          {manufacturer && (
            <View style={styles.row}>
              <Text
                style={[
                  styles.label,
                  {
                    color: COLORS.textSoft,
                  },
                ]}
              >
                {t("inverterDetail.manufacturer")}
              </Text>
              <Text
                style={[
                  styles.value,
                  {
                    color: COLORS.text,
                  },
                ]}
              >
                {manufacturer}
              </Text>
            </View>
          )}

          {model && (
            <View style={styles.row}>
              <Text
                style={[
                  styles.label,
                  {
                    color: COLORS.textSoft,
                  },
                ]}
              >
                {t("inverterDetail.model")}
              </Text>
              <Text
                style={[
                  styles.value,
                  {
                    color: COLORS.text,
                  },
                ]}
              >
                {model}
              </Text>
            </View>
          )}

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.serialNumber")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {serialNumber || "-"}
            </Text>
          </View>

          {ratedPower != null && (
            <View style={styles.row}>
              <Text
                style={[
                  styles.label,
                  {
                    color: COLORS.textSoft,
                  },
                ]}
              >
                {t("inverterDetail.ratedPower")}
              </Text>
              <Text
                style={[
                  styles.value,
                  {
                    color: COLORS.text,
                  },
                ]}
              >
                {ratedPower} W
              </Text>
            </View>
          )}
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: SPACING.md,
    paddingVertical: SPACING.sm,
  },
  backButton: {
    padding: 8,
  },
  headerTitle: {
    fontSize: TYPOGRAPHY.lg,
    fontWeight: "700",
    flex: 1,
    textAlign: "center",
  },
  content: {
    padding: SPACING.md,
  },
  heroCard: {
    borderRadius: 16,
    borderWidth: 0,
    padding: SPACING.lg,
    alignItems: "center",
    marginBottom: SPACING.md,
  },
  iconContainer: {
    width: 96,
    height: 96,
    borderRadius: 48,
    alignItems: "center",
    justifyContent: "center",
    marginBottom: SPACING.md,
  },
  powerValue: {
    fontSize: 48,
    fontWeight: "800",
  },
  powerLabel: {
    fontSize: TYPOGRAPHY.sm,
    marginTop: 4,
  },
  section: {
    borderRadius: 14,
    borderWidth: 0,
    padding: SPACING.md,
    marginBottom: SPACING.sm,
  },
  sectionTitle: {
    fontSize: TYPOGRAPHY.md,
    fontWeight: "700",
    marginBottom: SPACING.sm,
  },
  row: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingVertical: 8,
  },
  label: {
    fontSize: TYPOGRAPHY.sm,
    flex: 1,
  },
  value: {
    fontSize: TYPOGRAPHY.sm,
    fontWeight: "600",
    textAlign: "right",
  },
  phaseCard: {
    borderRadius: 10,
    padding: SPACING.sm,
    marginBottom: SPACING.xs,
    borderWidth: 0,
  },
  phaseTitle: {
    fontSize: TYPOGRAPHY.sm,
    fontWeight: "700",
    marginBottom: 8,
  },
});
