import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useNavigation, useRoute } from "@react-navigation/native";
import { ArrowLeft } from 'lucide-react-native';
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

export default function BatteryPackDetailScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const isDark = useThemeStore((s) => s.mode) === "dark";
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();
  const { t, language } = useI18n();
  const locale = language === 'tr' ? 'tr-TR' : 'en-US';

  const { deviceId, componentId, batteryName, componentData } = route.params || {};

  const [refreshing, setRefreshing] = useState(false);
  const { data: batteryData, isFetching, fetchLiveData } = useDeviceLiveData(deviceId);

  useEffect(() => {
    if (componentId && deviceId) {
      fetchLiveData({
        id: componentId,
        name: batteryName || t('deviceNames.battery'),
        componentData: componentData || 0 // SOC değerini hook'a gönder
      });
    }
  }, [componentId, deviceId, componentData]);

  const onRefresh = async () => {
    setRefreshing(true);
    if (componentId && deviceId) {
      await fetchLiveData({
        id: componentId,
        name: batteryName || t('deviceNames.battery'),
        componentData: componentData || 0 // SOC değerini hook'a gönder
      });
    }
    setRefreshing(false);
  };

  if (isFetching && !batteryData) {
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

  const socColor = batteryData?.soc > 70 ? COLORS.success : batteryData?.soc > 30 ? COLORS.warning : COLORS.error;

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
            {batteryData?.soc?.toFixed(0) || 0}%
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
        </View>

        {/* Stats Section */}
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
            {t("batteryDetail.stats")}
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
              {t("batteryDetail.soh")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.soh || "-"}
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
              {t("batteryDetail.voltage")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.voltage?.toFixed(2) || 0} V
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
              {t("batteryDetail.current")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.current?.toFixed(2) || 0} A
            </Text>
          </View>
        </View>

        {/* Cell Voltage Section */}
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
            {t("batteryDetail.cellVoltage")}
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
              {t("batteryDetail.maxCellVoltage")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.maxCellVoltage?.toFixed(3) || 0} V
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
              {t("batteryDetail.minCellVoltage")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.minCellVoltage?.toFixed(3) || 0} V
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
              {t("batteryDetail.avgCellVoltage")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.avgCellVoltage?.toFixed(3) || 0} V
            </Text>
          </View>
        </View>

        {/* Temperature Section */}
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
            {t("batteryDetail.temperature")}
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
              {t("batteryDetail.maxCellTemp")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.maxCellTemp?.toFixed(1) || 0} °C
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
              {t("batteryDetail.minCellTemp")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.minCellTemp?.toFixed(1) || 0} °C
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
              {t("batteryDetail.avgTemp")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.avgTemp?.toFixed(1) || 0} °C
            </Text>
          </View>
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

          <View style={styles.row}>
            <Text
              style={[
                styles.label,
                {
                  color: COLORS.textSoft,
                },
              ]}
            >
              {t("batteryDetail.model")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.model || "-"}
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
              {t("batteryDetail.serialNumber")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.sn || "-"}
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
              {t("batteryDetail.firmwareVersion")}
            </Text>
            <Text
              style={[
                styles.value,
                {
                  color: COLORS.text,
                },
              ]}
            >
              {batteryData?.version || "-"}
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
              {t("batteryDetail.lastUpdate")}
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
              {formatDateTime(batteryData?.dateTime, locale)}
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
  socValue: {
    fontSize: 48,
    fontWeight: "800",
  },
  socLabel: {
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
});
