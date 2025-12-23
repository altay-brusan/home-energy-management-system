import { useFocusEffect, useNavigation, useIsFocused } from "@react-navigation/native";
import { ArrowLeft, Sun, Wifi, Zap } from 'lucide-react-native';
import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Image,
  Pressable,
  RefreshControl,
  ScrollView,
  StatusBar,
  Text,
  TouchableOpacity,
  View
} from "react-native";
import AsyncStorage from '@react-native-async-storage/async-storage';
import LinearGradient from 'react-native-linear-gradient';
import Svg, { Path } from 'react-native-svg';
import { BatteryIcon } from "../../../components/ui/BatteryIcon";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { mobileDashboardService } from "../../../api/services/mobileDashboardService";
import { sitesService } from "../../../api/services/sitesService";
import { deviceComponentsService } from "../../../api/services/deviceComponentsService";
import { ConnectedDeviceCard } from "../../../components/ui/ConnectedDeviceCard";
import { DeviceInfoCard } from "../../../components/ui/DeviceInfoCard";
import { useDeviceLiveData } from "../../../hooks/useDeviceLiveData";
import { useI18n } from "../../../hooks/useI18n";
import { useRoles } from "../../../hooks/useRoles";
import { useSelectedSiteUtils } from "../../../hooks/useSelectedSite";
import { useThemeStore } from "../../../store/themeStore";
import { useSitesStore } from "../../../store/sitesStore";
import { getThemeColors, SPACING } from "../../../theme/constants";
import { getStyles } from "./DeviceDetail.styles";
import { DeviceDetailSkeleton } from "./DeviceDetailSkeleton";
import type { ComponentSummaryDto, InverterSummaryDto, BatterySummaryDto } from "../../../api/types/deviceComponents.types";

// PV Icon from EnergyFlowRadial
const PVIcon: React.FC<{ color: string; size?: number }> = ({ color, size = 32 }) => (
  <Svg width={size} height={size} viewBox="0 0 27.32 27.74">
    <Path
      d="M13.24,1.05V.41c0-.23.19-.41.42-.41s.42.19.42.41v.64c0,.23-.19.41-.42.41s-.42-.19-.42-.41ZM9.53,2.92c.08.08.19.12.3.12s.21-.04.3-.12c.16-.16.16-.43,0-.59l-.46-.45c-.16-.16-.43-.16-.59,0-.16.16-.16.43,0,.59l.46.45ZM7.6,6.83h.65c.23,0,.42-.19.42-.41s-.19-.41-.42-.41h-.65c-.23,0-.42.19-.42.41s.19.41.42.41ZM17.49,3.04c.11,0,.21-.04.3-.12l.46-.45c.16-.16.16-.43,0-.59-.16-.16-.43-.16-.59,0l-.46.45c-.16.16-.16.43,0,.59.08.08.19.12.3.12ZM19.08,6.83h.65c.23,0,.42-.19.42-.41s-.19-.41-.42-.41h-.65c-.23,0-.42.19-.42.41s.19.41.42.41ZM26.99,21.03c-.31.4-.78.62-1.28.62h-.96v.53c0,1.01-.83,1.84-1.86,1.84h-7.96v1.21h3.71c.63,0,1.14.51,1.14,1.13v.95c0,.23-.19.41-.42.41H7.97c-.23,0-.42-.19-.42-.41v-.95c0-.62.51-1.13,1.14-1.13h3.71v-1.21h-7.96c-1.02,0-1.86-.83-1.86-1.84v-.53h-.96c-.51,0-.97-.23-1.28-.62-.31-.4-.41-.9-.28-1.38l2.63-9.92c.19-.7.83-1.19,1.57-1.19h5.89c-.39-.63-.61-1.36-.61-2.12,0-2.25,1.85-4.08,4.13-4.08s4.13,1.83,4.13,4.08c0,.76-.21,1.49-.61,2.12h5.89c.73,0,1.38.49,1.57,1.19l2.63,9.92c.13.48.03.99-.28,1.38h0ZM13.24,20.83v-4.05h-5.36l-.54,4.05h5.9ZM1.9,15.95h5.24l.42-3.2H2.75l-.85,3.2ZM7.99,15.95h5.25v-3.2h-4.83l-.42,3.2h0ZM8.86,9.36l-.34,2.56h4.72v-2.56h-4.38ZM14.08,9.36v2.56h4.72l-.34-2.56h-4.38ZM14.08,20.83h5.9l-.54-4.05h-5.36v4.05ZM19.33,15.95l-.42-3.2h-4.83v3.2h5.25ZM20.18,15.95h5.24l-.85-3.2h-4.81l.42,3.2ZM23.07,9.36h-3.76l.34,2.56h4.7l-.53-1.98c-.09-.34-.4-.57-.75-.57h0ZM11.18,8.53h4.97c.52-.59.8-1.34.8-2.12,0-1.79-1.47-3.25-3.29-3.25s-3.29,1.46-3.29,3.25c0,.79.29,1.53.8,2.12h0ZM2.97,11.92h4.7l.34-2.56h-3.76c-.35,0-.66.24-.75.57l-.53,1.98h0ZM1.62,20.83h1.37s0,0,0,0,0,0,0,0h3.48l.54-4.05H1.68l-.82,3.08c-.06.23-.01.48.14.67.15.19.37.3.62.3h0ZM18.63,26.08h-4.12s0,0,0,0,0,0,0,0h-1.66s0,0,0,0,0,0,0,0h-4.12c-.17,0-.3.13-.3.3v.53h10.54v-.53c0-.16-.13-.3-.3-.3h0ZM14.08,24.03h-.84v1.21h.84v-1.21ZM23.9,21.66H3.42s0,.53,0,.53c0,.56.46,1.01,1.02,1.01h18.44c.56,0,1.02-.45,1.02-1.01v-.53h0ZM26.45,19.86l-.82-3.08h-5.35l.54,4.05h3.48s0,0,0,0,0,0,0,0h1.37c.24,0,.47-.11.62-.3.15-.19.2-.43.14-.67h0Z"
      fill={color}
    />
  </Svg>
);

export default function DeviceDetail() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore((s) => s.mode) === "dark";
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getStyles(COLORS), [COLORS]);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();
  const { isAdmin, hasRole } = useRoles();
  const isAdminUser = isAdmin || hasRole("Administrator");
  const { selectedSite } = useSelectedSiteUtils();

  useFocusEffect(
    useCallback(() => {
      if (isAdminUser && !selectedSite) {
        navigation.navigate('Admin');
      }
    }, [isAdminUser, selectedSite, navigation])
  );


  const [components, setComponents] = useState<any[]>([]);
  const [deviceId, setDeviceId] = useState<string | null>(null);
  const [setupId, setSetupId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [inverterData, setInverterData] = useState<any>(null);

  // New state for DeviceSummary API data
  const [summaryComponents, setSummaryComponents] = useState<ComponentSummaryDto[]>([]);
  const pollingIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const isFocused = useIsFocused();

  const { fetchLiveData } = useDeviceLiveData(deviceId);

  // Fetch device summary (realtime data for inverter and battery)
  const fetchDeviceSummary = useCallback(async () => {
    if (!deviceId) return;

    try {
      const summaryData = await deviceComponentsService.getDeviceSummary(deviceId);
      setSummaryComponents(summaryData.components || []);

      // Save battery packs to localStorage for BatteryListScreen
      const batteryComponent = summaryData.components.find(c => c.type === 'battery') as BatterySummaryDto | undefined;
      if (batteryComponent?.packs) {
        const cacheKey = `@battery_packs_summary_${deviceId}`;
        await AsyncStorage.setItem(cacheKey, JSON.stringify({
          packs: batteryComponent.packs,
          timestamp: Date.now(),
        })).catch(() => {});
      }
    } catch (error) {
      console.error('Error fetching device summary:', error);
    }
  }, [deviceId]);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchDeviceComponents();
    await fetchDeviceSummary();
    setRefreshing(false);
  };

  const fetchDeviceComponents = useCallback(async () => {
    try {
      // PERFORMANCE: Cache'ten instant yükle
      try {
        const cachedData = await AsyncStorage.getItem('@device_components_cache');
        if (cachedData) {
          const parsed = JSON.parse(cachedData);
          setComponents(parsed.components || []);
          setDeviceId(parsed.deviceId || null);
          setSetupId(parsed.setupId || null);
          setLoading(false); // Cache varsa loading kapat
        } else {
          setLoading(true); // Cache yoksa skeleton göster
        }
      } catch {
        setLoading(true);
      }

      // selectedSite kontrolü
      if (!selectedSite?.id) {
        setComponents([]);
        setLoading(false);
        return;
      }

      // API çağrıları - cache varsa background'da güncelle
      const setupRes = await mobileDashboardService.getSetupDevice(selectedSite.id);
      const device = setupRes?.device;
      if (!device?.id) {
        setComponents([]);
        setLoading(false);
        return;
      }

      setDeviceId(device.id);
      setSetupId(setupRes?.setupId || null);

      const deviceData = await mobileDashboardService.getDeviceWithData(device.id);

      const comps = deviceData?.components?.map((c: any) => ({
        id: c.componentId,
        name: c.componentName,
        modelNumber: c.serialNumber || "—",
        isDeleted: false,
        componentData: c.componentData || 0,
        description: c.description || "",
      }));

      setComponents(comps || []);

      // Cache'e kaydet (background)
      AsyncStorage.setItem('@device_components_cache', JSON.stringify({
        components: comps || [],
        deviceId: device.id,
        setupId: setupRes?.setupId || null,
        timestamp: Date.now(),
      })).catch(() => {});

    } catch (err) {
      setComponents([]);
    } finally {
      setLoading(false);
    }
  }, [selectedSite]);

  useEffect(() => {
    fetchDeviceComponents();
  }, [fetchDeviceComponents]);

  // Fetch summary on mount and when deviceId changes
  useEffect(() => {
    if (deviceId) {
      fetchDeviceSummary();
    }
  }, [deviceId, fetchDeviceSummary]);

  // Polling: Fetch summary every 5 seconds (only when focused)
  useEffect(() => {
    if (!deviceId || !isFocused) {
      // Clear interval when not focused
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current);
        pollingIntervalRef.current = null;
      }
      return;
    }

    // Start polling
    const interval = setInterval(() => {
      fetchDeviceSummary();
    }, 5000); // Poll every 5 seconds

    pollingIntervalRef.current = interval;

    // Cleanup on unmount or when dependencies change
    return () => {
      if (pollingIntervalRef.current) {
        clearInterval(pollingIntervalRef.current);
        pollingIntervalRef.current = null;
      }
    };
  }, [deviceId, isFocused, fetchDeviceSummary]);

  // PERFORMANCE: Inverter live data'yı sayfa açılışında yükleme
  // Sadece gerektiğinde (manuel refresh veya kullanıcı istediğinde) yükle
  // useEffect(() => {
  //   const loadInverterLiveData = async () => {
  //     const inverterComp = components.find((c) =>
  //       c.name.toLowerCase().includes("inverter")
  //     );
  //     if (inverterComp) {
  //       const data = await fetchLiveData(inverterComp);
  //       if (data && data["Active Power"] !== undefined) {
  //         setInverterData(data);
  //       }
  //     }
  //   };
  //
  //   if (components.length > 0) {
  //     loadInverterLiveData();
  //   }
  // }, [components]);


  const navigateToInverterDetail = () => {
    const inverterComp = components.find((c) =>
      c.name.toLowerCase().includes("inverter")
    );
    if (inverterComp) {
      navigation.navigate("InverterDetail", { componentId: inverterComp.id });
    }
  };

  const navigateToBatteryList = () => {
    navigation.navigate("BatteryList", { deviceId });
  };

  const navigateToDetailScreen = (componentId: string) => {
    switch (componentId) {
      case "pvpanel":
        navigation.navigate("PVPanelDetail");
        break;
      case "gateway":
        navigation.navigate("GatewayDetail");
        break;
      case "smartmeter":
        navigation.navigate("SmartMeterDetail");
        break;
      default:
        break;
    }
  };

  // Memoized component calculations to prevent unnecessary re-renders
  const inverterComp = useMemo(() =>
    components.find(c => c.name.toLowerCase().includes("inverter")),
    [components]
  );

  const batteryComps = useMemo(() =>
    components.filter(c =>
      c.name.toLowerCase().includes("battery") && !c.name.toLowerCase().includes("bms")
    ),
    [components]
  );

  const batteryCount = batteryComps.length;
  const batterySubtitle = batteryCount > 0
    ? t("deviceDetail.battery.packCount", { count: batteryCount })
    : t("deviceDetail.battery.noBatteries");

  // Get realtime data from DeviceSummary API (memoized)
  const inverterSummary = useMemo(() =>
    summaryComponents.find(c => c.type === 'inverter') as InverterSummaryDto | undefined,
    [summaryComponents]
  );

  const batterySummary = useMemo(() =>
    summaryComponents.find(c => c.type === 'battery') as BatterySummaryDto | undefined,
    [summaryComponents]
  );

  const inverterActivePower = inverterSummary?.activePowerKw;
  const batteryStateOfCharge = batterySummary?.stateOfCharge;

  return (
    <View style={[styles.container, { backgroundColor: isDark ? 'rgba(28, 34, 43, 1)' : 'rgba(243, 243, 245, 1)' }]}>
      <StatusBar barStyle={isDark ? "light-content" : "dark-content"} />

      {/* Gradient Background at Top - Only for light mode */}
      {!isDark && (
        <LinearGradient
          colors={['rgba(118, 192, 253, 0.48)', 'rgba(243, 243, 245, 1)']}
          start={{ x: 0.5, y: 0 }}
          end={{ x: 0.5, y: 1 }}
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            height: 300,
            zIndex: 0,
          }}
        />
      )}

      {/* Header - Settings tarzı */}
      <View style={[styles.pageHeader, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.headerBackButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.pageHeaderTitle, { color: COLORS.text }]}>
          {t("deviceDetail.title")}
        </Text>
        {/* Notification Icon */}
        <TouchableOpacity
          onPress={() => navigation.navigate('Notifications')}
          style={styles.headerBackButton}
        >
          <Svg width={20} height={24} viewBox="0 0 57.03 72">
            <Path
              d="M7.53,62.8h10.2c.8,5.2,5.3,9.2,10.8,9.2s10-4,10.8-9.2h10.2c2.1,0,4.1-.9,5.6-2.5,1.4-1.6,2.1-3.7,1.9-5.8-.2-1.8-1.8-5.9-4.8-12.8-.4-1-.9-2.2-1.2-2.7v-10.8c0-11.7-8.8-21.6-20-22.8v-2.9c0-1.4-1.1-2.5-2.5-2.5s-2.5,1.1-2.5,2.5v2.9c-4.9.5-9.5,2.7-13.1,6.2-4.4,4.3-6.9,10-6.9,16.2v11.2c-.2.5-.7,1.7-1.2,2.7C1.83,48.6.23,52.6.03,54.5c-.2,2.1.5,4.2,1.9,5.8,1.5,1.6,3.5,2.5,5.6,2.5ZM28.53,67c-2.7,0-5-1.8-5.7-4.2h11.5c-.8,2.4-3.1,4.2-5.8,4.2ZM9.43,43.7c1.5-3.5,1.6-3.6,1.6-4.3v-11.6c0-4.8,1.9-9.3,5.3-12.6,3.3-3.2,7.6-4.9,12.1-4.9h.6c9.3.3,16.9,8.4,16.9,18v11.2c0,.7.1.8,1.6,4.3,1.2,2.7,4.3,9.8,4.4,11.3.1.7-.2,1.4-.6,1.9-.5.5-1.1.8-1.9.8H7.53c-.7,0-1.4-.3-1.9-.8s-.7-1.2-.6-1.9c.2-1.5,3.3-8.7,4.4-11.4Z"
              fill={COLORS.text}
            />
          </Svg>
        </TouchableOpacity>
      </View>

      {/* Content - Skeleton veya gerçek içerik */}
      {loading ? (
        <DeviceDetailSkeleton />
      ) : (

      <ScrollView
        contentContainerStyle={{
          paddingBottom: insets.bottom + 100,
        }}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={COLORS.accent}
          />
        }
      >
        {/* Top Section: Device Image + Info Cards */}
        <View style={[styles.topSection, { marginTop: 16 }]}>
          {/* Left: Device Image */}
          <View style={styles.deviceImageContainer}>
            <Image
              source={require("../../../assets/images/device-image.png")}
              style={styles.deviceImage}
              resizeMode="contain"
            />
          </View>

          {/* Right: Info Cards Stack */}
          <View style={styles.infoCardsStack}>
            {/* Inverter Card */}
            {inverterComp && (
              <DeviceInfoCard
                title={t("deviceDetail.inverter.title")}
                subtitle={
                  inverterActivePower !== null && inverterActivePower !== undefined
                    ? `${inverterActivePower.toFixed(1)} kW`
                    : inverterComp.description || t("deviceDetail.inverter.subtitle")
                }
                icon={<Zap size={24} color={COLORS.accent} />}
                status="online"
                onPress={navigateToInverterDetail}
                style={{ flex: 1 }}
              />
            )}

            {/* Battery Card */}
            <DeviceInfoCard
              title={t("deviceDetail.battery.title")}
              subtitle={
                batteryStateOfCharge !== null && batteryStateOfCharge !== undefined
                  ? `${batterySubtitle}\n${batteryStateOfCharge.toFixed(0)}% SOC`
                  : batterySubtitle
              }
              icon={
                <BatteryIcon
                  percentage={batteryStateOfCharge ?? 0}
                  size={28}
                  borderColor={COLORS.textSoft}
                />
              }
              status={batteryCount > 0 ? "online" : "offline"}
              onPress={navigateToBatteryList}
              style={{ flex: 1 }}
            />
          </View>
        </View>

        {/* Connected Devices Section */}
        <View style={styles.connectedDevicesSection}>
          <Text style={[styles.sectionTitle, { color: COLORS.text }]}>
            {t("deviceDetail.connectedDevices.title")}
          </Text>

          <View style={styles.connectedDevicesList}>
            {/* Solar Panel Card */}
            <ConnectedDeviceCard
              title={t("deviceDetail.pvPanel.title")}
              description={t("deviceDetail.pvPanel.description")}
              icon={<PVIcon color="#F59E0B" size={28} />}
              iconBackgroundColor="#FEF3C7"
              onPress={() => navigateToDetailScreen("pvpanel")}
            />

            {/* Add Device Button */}
            <Pressable
              onPress={() => navigation.navigate("DeviceAdd")}
              style={({ pressed }) => [
                {
                  alignItems: 'center',
                  justifyContent: 'center',
                  paddingVertical: SPACING.md,
                  opacity: pressed ? 0.7 : 1,
                },
              ]}
            >
              {/* Circular Plus Icon */}
              <View
                style={{
                  width: 64,
                  height: 64,
                  borderRadius: 32,
                  backgroundColor: `${COLORS.accent}15`,
                  alignItems: 'center',
                  justifyContent: 'center',
                  marginBottom: 8,
                }}
              >
                <Text style={{ fontSize: 36, color: COLORS.accent, fontWeight: '300' }}>+</Text>
              </View>

              {/* Text */}
              <Text
                style={{
                  fontSize: 15,
                  fontWeight: '600',
                  color: COLORS.text,
                }}
              >
                {t("deviceDetail.addDevice.title")}
              </Text>
            </Pressable>
          </View>
        </View>
      </ScrollView>
      )}
    </View>
  );
}
