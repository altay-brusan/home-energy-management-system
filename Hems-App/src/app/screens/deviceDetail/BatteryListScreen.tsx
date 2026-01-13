import { useNavigation, useRoute, useIsFocused } from "@react-navigation/native";
import { ArrowLeft } from 'lucide-react-native';
import React, { useCallback, useEffect, useRef, useState } from "react";
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  Text,
  View
} from "react-native";
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { deviceComponentsService } from "../../../api/services/deviceComponentsService";
import { BatteryIcon } from "../../../components/ui/BatteryIcon";
import { useI18n } from "../../../hooks/useI18n";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors, SPACING, TYPOGRAPHY } from "../../../theme/constants";
import { BatteryListSkeleton } from "./BatteryListSkeleton";
import type { BatteryPackDto, BatterySummaryDto } from "../../../api/types/deviceComponents.types";

interface BatteryPack {
  id: string;
  name: string;
  componentData: number; // SOC
  serialNumber: string;
  description: string;
  model: string;
  manufacturer: string;
}

export default function BatteryListScreen() {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const isDark = useThemeStore((s) => s.mode) === "dark";
  const COLORS = getThemeColors(isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();

  const deviceId = route.params?.deviceId;

  const [batteries, setBatteries] = useState<BatteryPack[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pollingIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const isFocused = useIsFocused();

  const fetchBatteries = useCallback(async (isPolling: boolean = false) => {
    if (!deviceId) {
      setBatteries([]);
      setLoading(false);
      return;
    }

    // Clear error on new fetch attempt
    if (!isPolling) {
      setError(null);
    }

    try {
      // PERFORMANCE: Cache'ten instant yükle - summary'den kaydedilen pack'leri kullan
      if (!isPolling) {
        try {
          const cacheKey = `@battery_packs_summary_${deviceId}`;
          const cachedData = await AsyncStorage.getItem(cacheKey);
          if (cachedData) {
            const parsed = JSON.parse(cachedData);
            const cachedPacks = parsed.packs || [];

            // Cache'deki pack'leri map et
            const batteryPacks: BatteryPack[] = cachedPacks.map((pack: BatteryPackDto) => ({
              id: pack.id,
              name: pack.name,
              componentData: pack.stateOfCharge || 0,
              serialNumber: pack.serialNumber || "—",
              description: '',
              model: pack.model || "—",
              manufacturer: pack.manufacturer || "—",
            }));

            setBatteries(batteryPacks);
            setLoading(false); // Cache varsa loading kapat
          } else {
            setLoading(true); // Cache yoksa skeleton göster
          }
        } catch {
          setLoading(true);
        }
      }

      // API call - use new DeviceSummary API
      const summaryData = await deviceComponentsService.getDeviceSummary(deviceId);

      // Find battery component
      const batteryComponent = summaryData.components.find(
        c => c.type === 'battery'
      ) as BatterySummaryDto | undefined;

      if (!batteryComponent) {
        setBatteries([]);
        setError(null);
        setLoading(false);
        return;
      }

      // Map battery packs
      const batteryPacks: BatteryPack[] = batteryComponent.packs.map((pack: BatteryPackDto) => ({
        id: pack.id,
        name: pack.name,
        componentData: pack.stateOfCharge || 0,
        serialNumber: pack.serialNumber || "—",
        description: '',
        model: pack.model || "—",
        manufacturer: pack.manufacturer || "—",
      }));

      setBatteries(batteryPacks);
      setError(null);

      // Save to cache (background)
      const cacheKey = `@battery_packs_summary_${deviceId}`;
      AsyncStorage.setItem(cacheKey, JSON.stringify({
        packs: batteryComponent.packs,
        timestamp: Date.now(),
      })).catch((cacheErr) => {
        console.warn('Cache write failed:', cacheErr);
      });

    } catch (err: any) {
      console.error("Error fetching batteries:", err);

      // Only show error if we have no cached data
      if (batteries.length === 0) {
        if (err?.response?.status === 404) {
          setError(t('batteryList.deviceNotFound'));
        } else if (err?.response?.status === 401) {
          // Silent - token refresh will handle
        } else if (err?.code === 'NETWORK_ERROR' || !err?.response) {
          setError(t('batteryList.networkError'));
        } else {
          setError(t('batteryList.fetchError'));
        }
      }
    } finally {
      setLoading(false);
    }
  }, [deviceId, batteries.length, t]);

  useEffect(() => {
    fetchBatteries(false);
  }, []);

  // Polling: Fetch battery data every 5 seconds (only when focused)
  useEffect(() => {
    if (!deviceId || !isFocused) {
      // Clear interval when not focused
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current);
        pollingIntervalRef.current = null;
      }
      return;
    }

    // Fetch immediately when becoming focused
    fetchBatteries(true);

    // Start polling
    const interval = setInterval(() => {
      fetchBatteries(true);
    }, 5000); // Poll every 5 seconds

    pollingIntervalRef.current = interval;

    // Cleanup on unmount or when dependencies change
    return () => {
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current);
        pollingIntervalRef.current = null;
      }
    };
  }, [deviceId, isFocused, fetchBatteries]);

  const onRefresh = async () => {
    setRefreshing(true);
    setError(null);
    await fetchBatteries(false);
    setRefreshing(false);
  };

  const handleBatteryPress = (battery: BatteryPack) => {
    navigation.navigate("BatteryPackDetail", {
      deviceId,
      componentId: battery.id,
      batteryName: battery.name,
      componentData: battery.componentData, // SOC değerini gönder
    });
  };

  const renderBatteryCard = ({ item }: { item: BatteryPack }) => {
    const socValue = item.componentData || 0;
    // SOC badge rengi için (Seçenek A: %0-20 kırmızı, %21-40 turuncu, %41+ yeşil)
    const socBadgeColor = socValue <= 20 ? COLORS.error : socValue <= 40 ? COLORS.warning : COLORS.success;

    return (
      <Pressable
        onPress={() => handleBatteryPress(item)}
        style={({ pressed }) => [
          styles.batteryCard,
          {
            backgroundColor: COLORS.surface,
            borderColor: COLORS.border,
            opacity: pressed ? 0.7 : 1,
          },
        ]}
      >
        {/* Icon */}
        <View
          style={[
            styles.iconContainer,
            {
              backgroundColor: `${COLORS.accent}15`,
            },
          ]}
        >
          <BatteryIcon
            percentage={socValue}
            size={32}
            borderColor={COLORS.textSoft}
          />
        </View>

        {/* Content */}
        <View style={styles.cardContent}>
          <Text
            style={[
              styles.batteryName,
              {
                color: COLORS.text,
              },
            ]}
            numberOfLines={1}
          >
            {item.name}
          </Text>
          {item.model && item.model !== "—" && (
            <Text
              style={[
                styles.serialNumber,
                {
                  color: COLORS.textSoft,
                },
              ]}
              numberOfLines={1}
            >
              {item.manufacturer} {item.model}
            </Text>
          )}
          <Text
            style={[
              styles.serialNumber,
              {
                color: COLORS.textSoft,
              },
            ]}
            numberOfLines={1}
          >
            {t("batteryList.serialNumber")}: {item.serialNumber}
          </Text>
          {item.description && (
            <Text
              style={[
                styles.description,
                {
                  color: COLORS.textSoft,
                },
              ]}
              numberOfLines={2}
            >
              {item.description}
            </Text>
          )}
        </View>

        {/* SOC Badge */}
        <View
          style={[
            styles.socBadge,
            {
              backgroundColor: `${socBadgeColor}15`,
            },
          ]}
        >
          <Text
            style={[
              styles.socText,
              {
                color: socBadgeColor,
              },
            ]}
          >
            {socValue.toFixed(0)}%
          </Text>
        </View>
      </Pressable>
    );
  };

  const renderEmpty = () => (
    <View style={styles.emptyContainer}>
      <BatteryIcon percentage={0} size={64} borderColor={`${COLORS.textSoft}50`} />
      <Text
        style={[
          styles.emptyText,
          {
            color: error ? COLORS.error : COLORS.textSoft,
          },
        ]}
      >
        {error || t("batteryList.empty")}
      </Text>
      {error && (
        <Pressable
          onPress={onRefresh}
          style={{
            marginTop: SPACING.md,
            paddingHorizontal: SPACING.lg,
            paddingVertical: SPACING.sm,
            backgroundColor: `${COLORS.accent}15`,
            borderRadius: 8,
          }}
        >
          <Text style={{ color: COLORS.accent, fontWeight: '600' }}>
            {t("common.retry")}
          </Text>
        </Pressable>
      )}
    </View>
  );

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
      {/* Header - Her zaman göster (cached) */}
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
          {t("batteryList.title")}
        </Text>
        <View style={{ width: 40 }} />
      </View>

      {/* Content - Skeleton veya gerçek liste */}
      {loading ? (
        <BatteryListSkeleton />
      ) : (
        <FlatList
          data={batteries}
          renderItem={renderBatteryCard}
          keyExtractor={(item) => item.id}
          contentContainerStyle={[
            styles.listContent,
            {
              paddingBottom: insets.bottom + 20,
            },
          ]}
          ListEmptyComponent={renderEmpty}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={onRefresh}
              tintColor={COLORS.accent}
            />
          }
          showsVerticalScrollIndicator={false}
        />
      )}
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
    fontWeight: '700',
  },
  listContent: {
    padding: SPACING.md,
  },
  batteryCard: {
    flexDirection: "row",
    alignItems: "center",
    padding: SPACING.md,
    borderRadius: 14,
    borderWidth: 0,
    marginBottom: SPACING.sm,
  },
  iconContainer: {
    width: 56,
    height: 56,
    borderRadius: 28,
    alignItems: "center",
    justifyContent: "center",
    marginRight: SPACING.sm,
  },
  cardContent: {
    flex: 1,
  },
  batteryName: {
    fontSize: TYPOGRAPHY.md,
    fontWeight: '700',
    marginBottom: 4,
  },
  serialNumber: {
    fontSize: TYPOGRAPHY.xs,
    fontWeight: '400',
    marginBottom: 4,
  },
  description: {
    fontSize: TYPOGRAPHY.xs,
    fontWeight: '400',
    lineHeight: 16,
  },
  socBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 12,
  },
  socText: {
    fontSize: TYPOGRAPHY.sm,
    fontWeight: '700',
  },
  emptyContainer: {
    alignItems: "center",
    justifyContent: "center",
    paddingVertical: 60,
  },
  emptyText: {
    fontSize: TYPOGRAPHY.md,
    fontWeight: '400',
    marginTop: SPACING.md,
  },
});
