import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation, useRoute, useIsFocused } from "@react-navigation/native";
import { ArrowLeft } from 'lucide-react-native';
import React, { useCallback, useEffect, useRef, useState } from "react";
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
import { getThemeColors, SPACING, TYPOGRAPHY } from "../../../theme/constants";
import type { DeviceDetailsResponseDto, DeviceDetailItemDto } from "../../../api/types/deviceComponents.types";

export default function BatteryPackDetailScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const isDark = useThemeStore((s) => s.mode) === "dark";
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();
  const { t, language } = useI18n();

  const {
    deviceId,
    componentId,
    bmsAddress,
    batteryName,
    componentData,
    serialNumber,
    manufacturer,
    model,
    stateOfHealth: paramSoh,
    voltage: paramVoltage,
    current: paramCurrent,
    temperature: paramTemperature,
    firmwareVersion: paramFirmwareVersion,
  } = route.params || {};

  // Debug logging
  useEffect(() => {
    console.log('BatteryPackDetail params:', {
      deviceId, componentId, bmsAddress, batteryName,
      paramSoh, paramVoltage, paramCurrent, paramTemperature
    });
  }, [deviceId, componentId, bmsAddress, batteryName, paramSoh, paramVoltage, paramCurrent, paramTemperature]);

  const [refreshing, setRefreshing] = useState(false);
  const [loading, setLoading] = useState(true);
  const [detailsData, setDetailsData] = useState<DeviceDetailsResponseDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const pollingIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const isFocused = useIsFocused();

  // Helper function to get value from details (with null safety)
  const getDetailValue = (key: string): any => {
    if (!detailsData?.details || !Array.isArray(detailsData.details)) {
      return undefined;
    }
    const item = detailsData.details.find(d => d.key === key);
    return item?.value;
  };

  // Helper function to get detail with unit
  const getDetailWithUnit = (key: string): { value: any; unit: string | null } | null => {
    const item = detailsData?.details.find(d => d.key === key);
    if (!item) return null;
    return { value: item.value, unit: item.unit };
  };

  // Fetch component details from Details API with bmsAddress
  const fetchComponentDetails = useCallback(async (isPolling: boolean = false) => {
    // If componentId is missing, we can't fetch - show error
    if (!componentId) {
      console.warn('BatteryPackDetail: componentId is missing');
      setError(t('batteryDetail.notFound'));
      setLoading(false);
      return;
    }

    // Clear error on new fetch attempt (not during polling)
    if (!isPolling) {
      setError(null);
    }

    try {
      const details = await deviceComponentsService.getComponentDetails(componentId, bmsAddress);
      setDetailsData(details);
      setError(null);
    } catch (err: any) {
      console.error('Error fetching component details:', err);

      // Only show error if we have no cached data
      if (!detailsData) {
        if (err?.response?.status === 404) {
          setError(t('batteryDetail.notFound'));
        } else if (err?.response?.status === 401) {
          // Silent - token refresh will handle
        } else if (err?.code === 'NETWORK_ERROR' || !err?.response) {
          setError(t('batteryDetail.networkError'));
        } else {
          setError(t('batteryDetail.fetchError'));
        }
      }
    } finally {
      setLoading(false);
    }
  }, [componentId, bmsAddress, detailsData, t]);

  useEffect(() => {
    fetchComponentDetails(false);
  }, [componentId, bmsAddress]);

  // Polling: Fetch details every 15 seconds (only when focused)
  useEffect(() => {
    if (!componentId || !isFocused) {
      // Clear interval when not focused
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current);
        pollingIntervalRef.current = null;
      }
      return;
    }

    // Fetch immediately when becoming focused
    fetchComponentDetails(true);

    // Start polling
    const interval = setInterval(() => {
      fetchComponentDetails(true);
    }, 15000); // Poll every 15 seconds

    pollingIntervalRef.current = interval;

    // Cleanup on unmount or when dependencies change
    return () => {
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current);
        pollingIntervalRef.current = null;
      }
    };
  }, [componentId, isFocused, fetchComponentDetails]);

  const onRefresh = async () => {
    setRefreshing(true);
    setError(null);
    await fetchComponentDetails(false);
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

  // Error state with retry button
  if (error && !detailsData) {
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
            numberOfLines={1}
          >
            {batteryName || t("batteryDetail.title")}
          </Text>
          <View style={{ width: 40 }} />
        </View>

        {/* Error content */}
        <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center', padding: 20 }}>
          <FontAwesome name="exclamation-triangle" size={48} color={COLORS.error} />
          <Text style={{ color: COLORS.error, marginTop: 16, fontSize: 16, textAlign: 'center' }}>
            {error}
          </Text>
          <Pressable
            onPress={onRefresh}
            style={{
              marginTop: 20,
              paddingHorizontal: 24,
              paddingVertical: 12,
              backgroundColor: `${COLORS.accent}15`,
              borderRadius: 8,
            }}
          >
            <Text style={{ color: COLORS.accent, fontWeight: '600' }}>
              {t("common.retry")}
            </Text>
          </Pressable>
        </View>
      </View>
    );
  }

  // Get values from Details API, with fallback to navigation params
  const soc = getDetailValue('soc') ?? componentData ?? 0;
  const soh = getDetailValue('soh') ?? paramSoh;
  const name = getDetailValue('name') || batteryName;
  const mfr = getDetailValue('manufacturer') || manufacturer;
  const mdl = getDetailValue('model') || model;
  const serial = getDetailValue('serialNumber') || serialNumber;
  const firmwareVersion = getDetailValue('firmwareVersion') ?? paramFirmwareVersion;
  const chargeStatus = getDetailValue('chargeDischargeStatus');
  const voltage = getDetailValue('voltage') ?? paramVoltage;
  const current = getDetailValue('current') ?? paramCurrent;
  const temperature = getDetailValue('temperature') ?? paramTemperature;
  const remainingCapacity = getDetailValue('remainingCapacity');
  const ratedCapacity = getDetailValue('ratedCapacity');
  const minCellVoltage = getDetailValue('minCellVoltage');
  const maxCellVoltage = getDetailValue('maxCellVoltage');
  const avgCellVoltage = getDetailValue('avgCellVoltage');
  const totalDischarge = getDetailValue('totalDischarge');
  const cycleCount = getDetailValue('cycleCount');

  const socColor = soc > 70 ? COLORS.success : soc > 30 ? COLORS.warning : COLORS.error;

  // Render a detail row
  const renderRow = (label: string, value: any, unit?: string | null) => {
    if (value === undefined || value === null) return null;
    return (
      <View style={styles.row}>
        <Text
          style={[
            styles.label,
            {
              color: COLORS.textSoft,
            },
          ]}
        >
          {label}
        </Text>
        <Text
          style={[
            styles.value,
            {
              color: COLORS.text,
            },
          ]}
        >
          {typeof value === 'number' ? value.toFixed(value % 1 === 0 ? 0 : 1) : value}
          {unit ? ` ${unit}` : ''}
        </Text>
      </View>
    );
  };

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
          numberOfLines={1}
        >
          {name || t("batteryDetail.title")}
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
        {/* Hero Card - SOC */}
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
                backgroundColor: `${socColor}15`,
              },
            ]}
          >
            <FontAwesome name="battery-full" size={48} color={socColor} />
          </View>
          <Text
            style={[
              styles.socValue,
              {
                color: socColor,
              },
            ]}
          >
            {soc?.toFixed(0) || 0}%
          </Text>
          <Text
            style={[
              styles.socLabel,
              {
                color: COLORS.textSoft,
              },
            ]}
          >
            {t("batteryDetail.stateOfCharge")}
          </Text>
          {chargeStatus && (
            <View style={[styles.statusBadge, { backgroundColor: `${COLORS.accent}15` }]}>
              <Text style={{ color: COLORS.accent, fontWeight: '600', fontSize: 12 }}>
                {chargeStatus}
              </Text>
            </View>
          )}
        </View>

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
            {t("batteryDetail.deviceInfo")}
          </Text>

          {renderRow(t("batteryDetail.name"), name)}
          {renderRow(t("batteryDetail.manufacturer"), mfr)}
          {renderRow(t("batteryDetail.model"), mdl)}
          {renderRow(t("batteryDetail.serialNumber"), serial)}
          {renderRow(t("batteryDetail.firmwareVersion") || "Firmware", firmwareVersion)}
          {renderRow(t("batteryDetail.ratedCapacity") || "Rated Capacity", ratedCapacity, "Ah")}
        </View>

        {/* Real-time Status Section */}
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
            {t("batteryDetail.realtimeStatus") || "Real-time Status"}
          </Text>

          {renderRow(t("batteryDetail.stateOfHealth") || "State of Health", soh, "%")}
          {renderRow(t("batteryDetail.voltage") || "Voltage", voltage, "V")}
          {renderRow(t("batteryDetail.current") || "Current", current, "A")}
          {renderRow(t("batteryDetail.temperature") || "Temperature", temperature, "°C")}
          {renderRow(t("batteryDetail.remainingCapacity") || "Remaining Capacity", remainingCapacity, "Ah")}
        </View>

        {/* Cell Voltage Section */}
        {(minCellVoltage || maxCellVoltage || avgCellVoltage) && (
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
              {t("batteryDetail.cellVoltage") || "Cell Voltage"}
            </Text>

            {renderRow(t("batteryDetail.minCellVoltage") || "Minimum", minCellVoltage, "mV")}
            {renderRow(t("batteryDetail.maxCellVoltage") || "Maximum", maxCellVoltage, "mV")}
            {renderRow(t("batteryDetail.avgCellVoltage") || "Average", avgCellVoltage, "mV")}
          </View>
        )}

        {/* Usage Statistics Section */}
        {(totalDischarge !== undefined || cycleCount !== undefined) && (
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
              {t("batteryDetail.usageStatistics") || "Usage Statistics"}
            </Text>

            {renderRow(t("batteryDetail.totalDischarge") || "Total Discharge", totalDischarge, "kWh")}
            {renderRow(t("batteryDetail.cycleCount") || "Cycle Count", cycleCount)}
          </View>
        )}
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
  socValue: {
    fontSize: 48,
    fontWeight: "800",
  },
  socLabel: {
    fontSize: TYPOGRAPHY.sm,
    marginTop: 4,
  },
  statusBadge: {
    marginTop: SPACING.sm,
    paddingHorizontal: 12,
    paddingVertical: 4,
    borderRadius: 12,
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
});
