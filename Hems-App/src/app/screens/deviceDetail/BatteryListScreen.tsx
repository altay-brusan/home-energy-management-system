import { useNavigation, useRoute } from "@react-navigation/native";
import { ArrowLeft } from 'lucide-react-native';
import React, { useCallback, useEffect, useState } from "react";
import {
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
  componentId: string;
  bmsAddress: number;
  stateOfHealth: number | null;
  voltage: number | null;
  current: number | null;
  temperature: number | null;
  firmwareVersion: string | null;
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

  // Fallback: fetch from API if cache is not available
  const fetchBatteriesFromApi = useCallback(async () => {
    if (!deviceId) return;

    try {
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
      // Use batteryComponent.id as fallback for componentId (for backwards compatibility)
      const batteryPacks: BatteryPack[] = batteryComponent.packs.map((pack: BatteryPackDto, index: number) => ({
        id: pack.id,
        name: pack.name,
        componentData: pack.stateOfCharge || 0,
        serialNumber: pack.serialNumber || "—",
        description: '',
        model: pack.model || "—",
        manufacturer: pack.manufacturer || "—",
        componentId: pack.componentId || batteryComponent.id,
        bmsAddress: pack.bmsAddress || (index + 1),
        stateOfHealth: pack.stateOfHealth,
        voltage: pack.voltage,
        current: pack.current,
        temperature: pack.temperature,
        firmwareVersion: pack.firmwareVersion,
      }));

      setBatteries(batteryPacks);
      setError(null);

      // Save to cache (include parent component ID for fallback)
      const cacheKey = `@battery_packs_summary_${deviceId}`;
      await AsyncStorage.setItem(cacheKey, JSON.stringify({
        packs: batteryComponent.packs,
        parentComponentId: batteryComponent.id,
        timestamp: Date.now(),
      }));
    } catch (err: any) {
      console.error("Error fetching batteries:", err);

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

  // Load batteries from cache (populated by DeviceDetail screen)
  const loadBatteriesFromCache = useCallback(async () => {
    if (!deviceId) {
      setBatteries([]);
      setLoading(false);
      return;
    }

    setError(null);

    try {
      const cacheKey = `@battery_packs_summary_${deviceId}`;
      const cachedData = await AsyncStorage.getItem(cacheKey);

      if (cachedData) {
        const parsed = JSON.parse(cachedData);
        const cachedPacks = parsed.packs || [];
        const parentComponentId = parsed.parentComponentId;

        // Check if cache is outdated (missing componentId info) - if so, fetch fresh data
        const firstPack = cachedPacks[0];
        if (cachedPacks.length > 0 && !firstPack?.componentId && !parentComponentId) {
          console.log('Cache missing componentId, fetching fresh data');
          await fetchBatteriesFromApi();
          return;
        }

        // Map cached packs to BatteryPack format
        // Use parentComponentId as fallback for componentId (for backwards compatibility)
        const batteryPacks: BatteryPack[] = cachedPacks.map((pack: BatteryPackDto, index: number) => ({
          id: pack.id,
          name: pack.name,
          componentData: pack.stateOfCharge || 0,
          serialNumber: pack.serialNumber || "—",
          description: '',
          model: pack.model || "—",
          manufacturer: pack.manufacturer || "—",
          componentId: pack.componentId || parentComponentId,
          bmsAddress: pack.bmsAddress || (index + 1),
          stateOfHealth: pack.stateOfHealth,
          voltage: pack.voltage,
          current: pack.current,
          temperature: pack.temperature,
          firmwareVersion: pack.firmwareVersion,
        }));

        setBatteries(batteryPacks);
        setLoading(false);
      } else {
        // No cache available - fetch from API as fallback
        await fetchBatteriesFromApi();
      }
    } catch {
      // Cache read failed - try API
      await fetchBatteriesFromApi();
    }
  }, [deviceId, fetchBatteriesFromApi]);

  // Load from cache on mount (no polling - DeviceDetail handles data refresh)
  useEffect(() => {
    loadBatteriesFromCache();
  }, [loadBatteriesFromCache]);

  const onRefresh = async () => {
    setRefreshing(true);
    setError(null);
    await fetchBatteriesFromApi();
    setRefreshing(false);
  };

  const handleBatteryPress = (battery: BatteryPack) => {
    navigation.navigate("BatteryPackDetail", {
      deviceId,
      componentId: battery.componentId,
      bmsAddress: battery.bmsAddress,
      batteryName: battery.name,
      componentData: battery.componentData, // SOC
      serialNumber: battery.serialNumber,
      manufacturer: battery.manufacturer,
      model: battery.model,
      stateOfHealth: battery.stateOfHealth,
      voltage: battery.voltage,
      current: battery.current,
      temperature: battery.temperature,
      firmwareVersion: battery.firmwareVersion,
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
