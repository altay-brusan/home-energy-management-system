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
  const [loading, setLoading] = useState(true);
  const [detailsData, setDetailsData] = useState<DeviceDetailsResponseDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const pollingIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const isFocused = useIsFocused();

  // Helper function to get value from details
  const getDetailValue = (key: string): any => {
    const item = detailsData?.details.find(d => d.key === key);
    return item?.value;
  };

  // Fetch component details from Details API
  const fetchComponentDetails = useCallback(async (isPolling: boolean = false) => {
    if (!componentId) return;

    // Clear error on new fetch attempt (not during polling)
    if (!isPolling) {
      setError(null);
    }

    try {
      const details = await deviceComponentsService.getComponentDetails(componentId);
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
  }, [componentId, detailsData, t]);

  useEffect(() => {
    fetchComponentDetails(false);
  }, []);

  // Polling: Fetch details every 5 seconds (only when focused)
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
    }, 5000);

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

  // Get values from Details API (only what's actually returned)
  const soc = getDetailValue('soc') || 0;
  const name = getDetailValue('name');
  const manufacturer = getDetailValue('manufacturer');
  const model = getDetailValue('model');
  const serialNumber = getDetailValue('serialNumber');

  const socColor = soc > 70 ? COLORS.success : soc > 30 ? COLORS.warning : COLORS.error;

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

          {name && (
            <View style={styles.row}>
              <Text
                style={[
                  styles.label,
                  {
                    color: COLORS.textSoft,
                  },
                ]}
              >
                {t("batteryDetail.name")}
              </Text>
              <Text
                style={[
                  styles.value,
                  {
                    color: COLORS.text,
                  },
                ]}
              >
                {name}
              </Text>
            </View>
          )}

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
                {t("batteryDetail.manufacturer")}
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
                {model}
              </Text>
            </View>
          )}

          {serialNumber && (
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
                {serialNumber}
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
