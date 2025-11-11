import FontAwesome from "@react-native-vector-icons/fontawesome";
import { useFocusEffect, useNavigation } from "@react-navigation/native";
import { ArrowLeft } from 'lucide-react-native';
import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  Animated,
  Image,
  Pressable,
  RefreshControl,
  ScrollView,
  StatusBar,
  Text,
  TouchableOpacity,
  View
} from "react-native";
import Svg, { Path } from 'react-native-svg';
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { mobileDashboardService } from "../../../api/services/mobileDashboardService";
import { sitesService } from "../../../api/services/sitesService";
import { useDeviceLiveData } from "../../../hooks/useDeviceLiveData";
import { useI18n } from "../../../hooks/useI18n";
import { useRoles } from "../../../hooks/useRoles";
import { useSelectedSiteUtils } from "../../../hooks/useSelectedSite";
import { useThemeStore } from "../../../store/themeStore";
import { getThemeColors, SPACING } from "../../../theme/constants";
import { getStyles } from "./DeviceDetail.styles";
import BatteryDetailBottomSheet from "./ui/BatteryDetailBottomSheet";
import DeviceBadge from "./ui/DeviceBadge";
import GridCard from "./ui/GridCard";

export default function DeviceDetail () {
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

  const headerOpacity = useRef(new Animated.Value(0)).current;
  const headerY = useRef(new Animated.Value(-50)).current;

  const [components, setComponents] = useState<any[]>([]);
  const [deviceId, setDeviceId] = useState<string | null>(null);
  const [setupId, setSetupId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [batterySheetVisible, setBatterySheetVisible] = useState(false);
  const [selectedBattery, setSelectedBattery] = useState<any>(null);
  const [inverterData, setInverterData] = useState<any>(null);

  const { data: batteryData, isFetching: isFetchingLive, fetchLiveData } =
    useDeviceLiveData(deviceId);

  const onRefresh = async () => {
    setRefreshing(true);
    await fetchDeviceComponents();
    setRefreshing(false);
  };

  const fetchDeviceComponents = useCallback(async () => {
    try {
      setLoading(true);

      const sitesData = await sitesService.getUserSites();
      const siteId = sitesData?.siteList?.[0]?.id;

      if (!siteId) {setComponents([]);
        return;
      }

      const setupRes = await mobileDashboardService.getSetupDevice(siteId);
      const device = setupRes?.device;
      if (!device?.id) {setComponents([]);
        return;
      }

      setDeviceId(device.id);
      setSetupId(setupRes?.setupId || null);

      const withDataRes = await mobileDashboardService.getDeviceWithData(device.id);

      const comps = withDataRes?.components?.map((c: any) => ({
        id: c.componentId,
        name: c.componentName,
        modelNumber: c.serialNumber || "—",
        isDeleted: false,
        componentData: c.componentData || 0,
      }));

      setComponents(comps || []);
    } catch (err) {setComponents([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchDeviceComponents();
  }, [fetchDeviceComponents]);
  useEffect(() => {
    const loadInverterLiveData = async () => {
      const inverterComp = components.find((c) =>
        c.name.toLowerCase().includes("inverter")
      );
      if (inverterComp) {
        const data = await fetchLiveData(inverterComp);
        if (data && data["Active Power"] !== undefined) {
          setInverterData(data);
        }
      }
    };

    if (components.length > 0) {
      loadInverterLiveData();
    }
  }, [components]);

  useEffect(() => {
    Animated.parallel([
      Animated.timing(headerOpacity, {
        toValue: 1,
        duration: 800,
        useNativeDriver: true,
      }),
      Animated.spring(headerY, {
        toValue: 0,
        useNativeDriver: true,
        tension: 50,
        friction: 8,
      }),
    ]).start();
  }, []);

  const handleOpenBattery = async (component: any) => {
    setSelectedBattery(component);
    setBatterySheetVisible(true);
    await fetchLiveData(component);
  };

  const handleRestartDevice = () => {
    Alert.alert(
      t("deviceDetail.battery.restart"),
      t("deviceDetail.confirmRestart"),
      [
        { text: t("common.cancel"), style: "cancel" },
        {
          text: t("deviceDetail.confirmStart"),
          style: "destructive",
          onPress: () => {
            Alert.alert(t("deviceDetail.actionStarted"), t("deviceDetail.deviceRestarting"));
          },
        },
      ]
    );
  };

  const navigateToDetailScreen = (componentId: string) => {
    switch (componentId) {
      case "pvpanel":
        navigation.navigate("PVPanelDetail");
        break;
      case "gateway":
        navigation.navigate("GatewayDetail");
        break;
      default:
        break;
    }
  };

  if (loading) {
    return (
      <View
        style={[
          styles.container,
          { justifyContent: "center", alignItems: "center" },
        ]}
      >
        <ActivityIndicator size="large" color={COLORS.accent} />
        <Text style={{ color: COLORS.textSoft, marginTop: 10 }}>
          {t("common.loading")}
        </Text>
      </View>
    );
  }
  const bmsComp = components.find(c =>
    c.name.toLowerCase().includes("bms")
  );

  const inverterComp = components.find(c =>
    c.name.toLowerCase().includes("inverter")
  );

  // 🔹 SOC ve total power değerleri
  const socValue = bmsComp?.componentData || 0;

  const totalPower = inverterData?.["Active Power"] || 0;

  return (
    <View style={styles.container}>
      <StatusBar barStyle={isDark ? "light-content" : "dark-content"} />
      <Animated.View
        style={[
          styles.header,
          {
            paddingTop: insets.top,
            opacity: headerOpacity,
            transform: [{ translateY: headerY }],
          },
        ]}
      >
        <View style={[styles.headerContent, { flexDirection: "row", alignItems: "center" }]}>
          <Pressable
            style={{ paddingVertical: 8, paddingRight: 8, zIndex: 2 }}
            onPress={() => navigation.goBack()}
          >
            <ArrowLeft size={24} color={COLORS.textSoft} strokeWidth={2} />
          </Pressable>

          <View style={{ flex: 1, alignItems: "center" }}>
            <Text style={[styles.headerTitle, { textAlign: "center" }]}>
              {t("deviceDetail.title")}
            </Text>
          </View>

          {/* Notification Icon */}
          <TouchableOpacity
            onPress={() => navigation.navigate('Notifications')}
            style={{ padding: 8, zIndex: 2 }}
          >
            <View style={{ position: 'relative' }}>
              <Svg width={20} height={25} viewBox="0 0 57.03 72">
                <Path
                  d="M7.53,62.8h10.2c.8,5.2,5.3,9.2,10.8,9.2s10-4,10.8-9.2h10.2c2.1,0,4.1-.9,5.6-2.5,1.4-1.6,2.1-3.7,1.9-5.8-.2-1.8-1.8-5.9-4.8-12.8-.4-1-.9-2.2-1.2-2.7v-10.8c0-11.7-8.8-21.6-20-22.8v-2.9c0-1.4-1.1-2.5-2.5-2.5s-2.5,1.1-2.5,2.5v2.9c-4.9.5-9.5,2.7-13.1,6.2-4.4,4.3-6.9,10-6.9,16.2v11.2c-.2.5-.7,1.7-1.2,2.7C1.83,48.6.23,52.6.03,54.5c-.2,2.1.5,4.2,1.9,5.8,1.5,1.6,3.5,2.5,5.6,2.5ZM28.53,67c-2.7,0-5-1.8-5.7-4.2h11.5c-.8,2.4-3.1,4.2-5.8,4.2ZM9.43,43.7c1.5-3.5,1.6-3.6,1.6-4.3v-11.6c0-4.8,1.9-9.3,5.3-12.6,3.3-3.2,7.6-4.9,12.1-4.9h.6c9.3.3,16.9,8.4,16.9,18v11.2c0,.7.1.8,1.6,4.3,1.2,2.7,4.3,9.8,4.4,11.3.1.7-.2,1.4-.6,1.9-.5.5-1.1.8-1.9.8H7.53c-.7,0-1.4-.3-1.9-.8s-.7-1.2-.6-1.9c.2-1.5,3.3-8.7,4.4-11.4Z"
                  fill={COLORS.textSoft}
                />
              </Svg>
              {/* Red Badge */}
              <View
                style={{
                  position: 'absolute',
                  top: -2,
                  right: -2,
                  width: 8,
                  height: 8,
                  borderRadius: 4,
                  backgroundColor: '#FF3B30',
                  borderWidth: 1.5,
                  borderColor: COLORS.dark,
                }}
              />
            </View>
          </TouchableOpacity>
        </View>
      </Animated.View>

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
        <View style={[styles.heroCard, { marginTop: insets.top + 72 }]}>
          <View style={styles.headerRow}>
            <View>
              <Text style={styles.title}>NetZr</Text>
              <Text style={[styles.statLabel, { marginTop: 4 }]}>
                {t("deviceDetail.totalComponents", { count: components.length })}
              </Text>
            </View>
          </View>

          <View style={styles.visualWrap}>
            <Image
              source={require("../../../assets/images/device-image.png")}
              style={styles.visualImg}
              resizeMode="contain"
            />
          </View>

          <View style={styles.badgesBelowRow}>
            {components.map((comp) => (
              <DeviceBadge
                key={comp.id}
                COLORS={COLORS}
                title={String(t(`deviceNames.${comp.name.toLowerCase()}`, { defaultValue: comp.name }))}
                subtitle={""}
                value={comp.componentData ? comp.componentData.toFixed(2).toString() : "0"}
                icon={
                  comp.name.toLowerCase().includes("inverter")
                    ? "bolt"
                    : comp.name.toLowerCase().includes("bms")
                      ? "microchip"
                      : "battery-full"
                }
                status={comp.isDeleted ? "inactive" : "normal"}
                onPress={() => handleOpenBattery(comp)}
              />
            ))}
          </View>
        </View>

        <View style={styles.statsSection}>
          <View style={styles.statsSectionHeader}>
            <Text style={styles.statsSectionTitle}>{t("deviceDetail.informations")}</Text>
          </View>

          <View style={styles.statsContainer}>
            <View style={styles.statCard}>
              <FontAwesome name="bolt" size={18} color={COLORS.accent} />
              <Text style={styles.statValue}>{(totalPower).toFixed(2)} kW</Text>
              <Text style={styles.statLabel}>{t("deviceDetail.dailyProduction")}</Text>
            </View>

            <View style={styles.statCard}>
              <FontAwesome name="battery-full" size={18} color={COLORS.success} />
              <Text style={styles.statValue}>{socValue.toFixed(0)}%</Text>
              <Text style={styles.statLabel}>{t("deviceDetail.batteryCharge")}</Text>
            </View>

            <View style={styles.statCard}>
              <FontAwesome name="plug" size={18} color={COLORS.textSoft} />

              <Text
                style={[styles.statValue, { textAlign: 'center', marginTop: 6 }]}
                numberOfLines={1}
                adjustsFontSizeToFit
                minimumFontScale={0.75}
              >
                {t("deviceList.onGrid")}
              </Text>

              <Text
                style={[
                  styles.statLabel,
                  { textAlign: 'center', alignSelf: 'center', marginTop: 6 },
                ]}
              >
                {t("deviceDetail.connection")}
              </Text>
            </View>
          </View>
        </View>


        <View style={styles.sectionContainer}>
          <Text style={styles.sectionTitle}>{t("deviceDetail.devicesTitle")}</Text>
          <View style={styles.gridRow}>
            <GridCard
              COLORS={COLORS}
              title={t("deviceDetail.pvPanel.title")}
              subtitle="—"
              icon="sun-o"
              onPress={() => navigateToDetailScreen("pvpanel")}
            />
            <GridCard
              COLORS={COLORS}
              title={t("deviceDetail.gateway.title")}
              subtitle={t("deviceDetail.gateway.subtitle")}
              icon="exchange"
              onPress={() => navigateToDetailScreen("gateway")}
            />
          </View>
        </View>
      </ScrollView>

      <BatteryDetailBottomSheet
        visible={batterySheetVisible}
        onClose={() => setBatterySheetVisible(false)}
        onRestart={handleRestartDevice}
        loading={isFetchingLive}
        deviceName={selectedBattery?.name || "Battery"}
        batteryData={batteryData || {}}
        
      />
    </View>
  );
}
