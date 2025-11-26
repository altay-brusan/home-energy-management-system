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
import { useDeviceLiveData } from "../../../hooks/useDeviceLiveData";
import { useI18n } from "../../../hooks/useI18n";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors, SPACING, TYPOGRAPHY } from "../../../theme/constants";

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
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();
  const { t, language } = useI18n();
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const { componentId, deviceId } = route.params || {};

  const [refreshing, setRefreshing] = useState(false);
  const { data: inverterData, isFetching, fetchLiveData } = useDeviceLiveData(deviceId);

  useEffect(() => {
    if (componentId && deviceId) {
      fetchLiveData({ id: componentId, name: t('deviceNames.inverter') });
    }
  }, [componentId, deviceId]);

  const onRefresh = async () => {
    setRefreshing(true);
    if (componentId && deviceId) {
      await fetchLiveData({ id: componentId, name: t('deviceNames.inverter') });
    }
    setRefreshing(false);
  };

  if (isFetching && !inverterData) {
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

  const activePower = inverterData?.["Active Power"] || 0;
  const powerColor = activePower > 0 ? COLORS.success : COLORS.textSoft;

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
            {activePower?.toFixed(2) || 0} W
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

        {/* Power Metrics Section */}
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
            {t("inverterDetail.powerMetrics")}
          </Text>

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.reactivePower")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {inverterData?.["Reactive Power"]?.toFixed(2) || 0} VAR
            </Text>
          </View>

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.powerFactor")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {inverterData?.["Power Factor"]?.toFixed(2) || 0}
            </Text>
          </View>

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.insulationResistance")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {inverterData?.["Insulation Resistance"] || 0} MΩ
            </Text>
          </View>
        </View>

        {/* Phase Data Section */}
        {inverterData?.Phases && inverterData.Phases.length > 0 && (
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
              {t("inverterDetail.phaseData")}
            </Text>

            {inverterData.Phases.map((phase: any, index: number) => (
              <View key={index} style={styles.phaseCard}>
                <Text
                  style={[
                    styles.phaseTitle,
                    {
                      color: COLORS.text,
                    },
                  ]}
                >
                  {t("inverterDetail.phaseLabel", { id: phase["Phase Id"] || index + 1 })}
                </Text>

                <View style={styles.row}>
                  <Text
                    style={[
                      styles.label,
                      {
                        color: COLORS.textSoft,
                      },
                    ]}
                  >
                    {t("inverterDetail.voltage")}
                  </Text>
                  <Text
                    style={[
                      styles.value,
                      {
                        color: COLORS.text,
                      },
                    ]}
                  >
                    {phase.Voltage?.toFixed(1) || 0} V
                  </Text>
                </View>

                <View style={styles.row}>
                  <Text
                    style={[
                      styles.label,
                      {
                        color: COLORS.textSoft,
                      },
                    ]}
                  >
                    {t("inverterDetail.current")}
                  </Text>
                  <Text
                    style={[
                      styles.value,
                      {
                        color: COLORS.text,
                      },
                    ]}
                  >
                    {phase.Current?.toFixed(2) || 0} A
                  </Text>
                </View>

                <View style={styles.row}>
                  <Text
                    style={[
                      styles.label,
                      {
                        color: COLORS.textSoft,
                      },
                    ]}
                  >
                    {t("inverterDetail.power")}
                  </Text>
                  <Text
                    style={[
                      styles.value,
                      {
                        color: COLORS.text,
                      },
                    ]}
                  >
                    {phase.Power?.toFixed(0) || 0} W
                  </Text>
                </View>
              </View>
            ))}
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
              {inverterData?.["Device Serial Number"] || "-"}
            </Text>
          </View>

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.firmwareVersion")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {inverterData?.["Firmware Version"] || "-"}
            </Text>
          </View>

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.startupTime")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
              numberOfLines={1}
              adjustsFontSizeToFit
            >
              {formatDateTime(inverterData?.["Last Inverter Startup Time"], locale)}
            </Text>
          </View>

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("inverterDetail.shutdownTime")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
              numberOfLines={1}
              adjustsFontSizeToFit
            >
              {formatDateTime(inverterData?.["Last Inverter Shutdown Time"], locale)}
            </Text>
          </View>
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
