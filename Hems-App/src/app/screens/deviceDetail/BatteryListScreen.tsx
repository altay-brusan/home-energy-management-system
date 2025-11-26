import { useNavigation, useRoute } from "@react-navigation/native";
import { ArrowLeft, Battery } from 'lucide-react-native';
import React, { useCallback, useEffect, useState } from "react";
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
import { mobileDashboardService } from "../../../api/services/mobileDashboardService";
import { useI18n } from "../../../hooks/useI18n";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors, SPACING, TYPOGRAPHY } from "../../../theme/constants";
import { BatteryListSkeleton } from "./BatteryListSkeleton";

interface BatteryPack {
  id: string;
  name: string;
  componentData: number; // SOC
  serialNumber: string;
  description: string;
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

  const fetchBatteries = useCallback(async () => {
    if (!deviceId) {
      setBatteries([]);
      setLoading(false);
      return;
    }

    try {
      // PERFORMANCE: Cache'ten instant yükle
      try {
        const cacheKey = `@battery_list_cache_${deviceId}`;
        const cachedData = await AsyncStorage.getItem(cacheKey);
        if (cachedData) {
          const parsed = JSON.parse(cachedData);
          setBatteries(parsed.batteries || []);
          setLoading(false); // Cache varsa loading kapat
        } else {
          setLoading(true); // Cache yoksa skeleton göster
        }
      } catch {
        setLoading(true);
      }

      // API çağrısı - cache varsa background'da güncelle
      const withDataRes = await mobileDashboardService.getDeviceWithData(deviceId);

      const batteryComps = withDataRes?.components
        ?.filter((c: any) =>
          c.componentName.toLowerCase().includes("battery") &&
          !c.componentName.toLowerCase().includes("bms")
        )
        .map((c: any) => ({
          id: c.componentId,
          name: c.componentName,
          componentData: c.componentData || 0,
          serialNumber: c.serialNumber || "—",
          description: c.description || "",
        })) || [];

      setBatteries(batteryComps);

      // Cache'e kaydet (background)
      const cacheKey = `@battery_list_cache_${deviceId}`;
      AsyncStorage.setItem(cacheKey, JSON.stringify({
        batteries: batteryComps,
        timestamp: Date.now(),
      })).catch(() => {});

    } catch (err) {
      console.error("Error fetching batteries:", err);
      setBatteries([]);
    } finally {
      setLoading(false);
    }
  }, [deviceId]);

  useEffect(() => {
    fetchBatteries();
  }, [fetchBatteries]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchBatteries();
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
    const socColor = socValue > 70 ? COLORS.success : socValue > 30 ? COLORS.warning : COLORS.error;

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
              backgroundColor: `${socColor}20`,
            },
          ]}
        >
          <Battery size={28} color={socColor} />
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
              backgroundColor: `${socColor}15`,
            },
          ]}
        >
          <Text
            style={[
              styles.socText,
              {
                color: socColor,
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
      <Battery size={64} color={COLORS.textSoft} opacity={0.3} />
      <Text
        style={[
          styles.emptyText,
          {
            color: COLORS.textSoft,
          },
        ]}
      >
        {t("batteryList.empty")}
      </Text>
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
