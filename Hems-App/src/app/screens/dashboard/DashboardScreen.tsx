import { useFocusEffect, useNavigation, useRoute } from "@react-navigation/native";
import React, { JSX, useCallback, useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Platform, RefreshControl, SafeAreaView, ScrollView, Text, View, TouchableOpacity } from 'react-native';
import Svg, { Path, G } from 'react-native-svg';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useSitesStore, useNeedsSetup } from '../../../store/sitesStore';
import LinearGradient from 'react-native-linear-gradient';
import { useDeviceNotifications } from '../../../hooks/useDeviceNotifications';
import { apiClient } from '../../../api/client/apiClient';
import { sitesService } from "../../../api/services/sitesService";
import { useDashboardRefresh } from '../../../hooks/useDashboardRefresh';
import { useRoles } from '../../../hooks/useRoles';
import { useSelectedSiteUtils } from '../../../hooks/useSelectedSite';
import { useSelectedSiteDevice } from '../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { getDashboardStyles } from './DashboardScreen.styles';
import PowerAndEnergyMetrics from './ui/powerAndEnergyMetrics/PowerAndEnergyMetrics';
import { DashboardModeCard } from './ui/dashboardModeCard';
import { InverterModeCard } from './ui/inverterModeCard';
import Sidebar from "./ui/dashboardSideBar/Sidebar";
import { BatteryStateCard } from './ui/batteryStateCard';
import { CO2Card } from './ui/co2Card';
import EnergyReviewCard from './ui/energyReviewCard';
import { GridPerformanceCard } from './ui/gridPerformanceCard';
import { PowerFlowDiagram } from './ui/powerFlowDiagram';
import { TodaySummary } from './ui/todaySummary';
import { WeatherStatus } from './ui/weatherStatus';
import { DemandBreakdownChart } from './ui/demandBreakdownChart';

type Range = 'Day' | 'Week' | 'Month' | 'Year';

// Netzr Logo Component
const NetzrLogo = ({ width = 100, height = 31, isDark }: { width?: number; height?: number; isDark: boolean }) => {
  const fillColor = isDark ? '#FFFFFF' : '#2f52a0';

  return (
    <Svg width={width} height={height} viewBox="0 0 420.25 131.85">
      <G>
        <Path fill={fillColor} d="M91.92,131.85L23.13,35.78v96.07H0V0h23.72l67.41,93.5V0h23.13v131.85h-22.34Z"/>
        <Path fill={fillColor} d="M173.8,36.1c27.32,0,44.85,20.92,44.85,49.56v4.71h-70.29c1.51,13.76,11.87,25.06,29.02,25.06,8.86,0,19.41-3.58,25.82-9.99l9.05,13c-9.05,8.67-22.43,13.19-36.94,13.19-27.32,0-47.68-19.03-47.68-47.87,0-26.38,19.22-47.68,46.17-47.68ZM148.17,76.43h51.26c-.38-10.74-7.73-24.12-25.63-24.12-16.96,0-24.69,13-25.63,24.12Z"/>
        <Path fill={fillColor} d="M236.22,100.35v-45.79h-6.45l-11.12-18.19h17.57V10.28h20.76v26.09h19.37v18.19h-19.37v44.39c0,8.23,6.67,14.91,14.91,14.91h4.46s0,17.99,0,17.99h-8.63c-17.4,0-31.5-14.1-31.5-31.5Z"/>
        <Path fill={fillColor} d="M291.16,113.86l45.43-59.3h-45.43v-18.19h70.83s0,18.19,0,18.19l-45.37,59.3h45.37v17.99h-70.83v-17.99Z"/>
        <Path fill={fillColor} d="M374.87,131.85v-65.67c0-16.46,13.34-29.8,29.8-29.8h15.58v18.19h-12.41c-6.75,0-12.22,5.47-12.22,12.22v65.07h-20.76Z"/>
      </G>
    </Svg>
  );
};

export default function DashboardScreen (): JSX.Element {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = getDashboardStyles(COLORS);
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [selectedRange, setSelectedRange] = useState<Range>('Day');
  const route = useRoute();
  const navigation = useNavigation<any>();
  const { isAdmin, hasRole } = useRoles();
  const isAdminUser = isAdmin || hasRole("Administrator");
  const siteIdFromParam = (route.params as any)?.siteId;

  const [siteData, setSiteData] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  const { selectedSite, getSelectedSiteName } = useSelectedSiteUtils();
  const needsSetup = useNeedsSetup();
  const { sites } = useSitesStore();

  const { selectedSiteDevice, hasDevice, device, externalDevices, setupInfo, loadSelectedSiteDevice } = useSelectedSiteDevice();

  const { refreshAll } = useDashboardRefresh();

  const sidebarRef = React.useRef<any>(null);

  // Get unread notification count
  const { notifications, refreshNotifications } = useDeviceNotifications({ isProcessed: false });
  const unreadCount = notifications.filter(n => !n.read).length;

  React.useEffect(() => {
    if (selectedSite && selectedSite.hasDevice && !selectedSiteDevice) {loadSelectedSiteDevice().catch(error => {});
    }
  }, [selectedSite, selectedSiteDevice, loadSelectedSiteDevice]);

  useFocusEffect(
    useCallback(() => {
      if (isAdminUser && !selectedSite && !siteIdFromParam) {
        navigation.navigate('Admin');
      }
      // Refresh notifications when screen is focused
      refreshNotifications();
    }, [isAdminUser, selectedSite, siteIdFromParam, navigation, refreshNotifications])
  );

  // Redirect users without valid site/device to appropriate setup screen
  useEffect(() => {
    // Skip for admin users - they have different flow
    if (isAdminUser) return;

    if (needsSetup === 'no_sites') {
      // User has no sites - go to JoinOrCreate (can create home or enter invitation code)
      navigation.reset({ index: 0, routes: [{ name: 'JoinOrCreate' }] });
    } else if (needsSetup === 'no_selected_site') {
      // User has sites but none selected - go to Home to select one
      navigation.reset({ index: 0, routes: [{ name: 'Home' }] });
    } else if (needsSetup === 'no_device') {
      // User has site but no device - go to DeviceSetup
      navigation.reset({
        index: 0,
        routes: [{ name: 'DeviceSetup', params: { siteId: selectedSite?.id } }],
      });
    }
  }, [needsSetup, isAdminUser, navigation, selectedSite?.id]);

  React.useEffect(() => {
    if (isAdminUser) {
      return;
    }

    if (!hasDevice || !device?.id) {return;
    }

    setTimeout(() => {refreshAll();
    }, 100);
  }, [hasDevice, device?.id, isAdminUser]);
  React.useEffect(() => {
    const fetchSite = async () => {
      if (!siteIdFromParam) return;
      try {
        setLoading(true);const data = await sitesService.getById(siteIdFromParam);
        setSiteData(data);} catch (error) {} finally {
        setLoading(false);
      }
    };

    fetchSite();
  }, [siteIdFromParam]);

  // Register global refresh callback for token refresh (only once)
  React.useEffect(() => {
    // Admin kullanıcılar için callback kaydı yapma
    if (isAdminUser) {
      return;
    }

    apiClient.registerGlobalRefreshCallback(refreshAll);
    return () => {
      apiClient.unregisterGlobalRefreshCallback();
    };
  }, []);

  React.useEffect(() => {
    // Admin kullanıcılar için callback güncellemesi yapma
    if (isAdminUser) {
      return;
    }

    apiClient.updateGlobalRefreshCallback(refreshAll);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAdminUser]);


  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    try {
      // Componentlerin callback'leri register olması için kısa bir delay
      setTimeout(async () => {
        await refreshAll();
        setRefreshing(false);
      }, 100);
    } catch (error) {setRefreshing(false);
    }
     
  }, [refreshAll]);

  // Loading state - device bilgisi yüklenene kadar bekle
  if (!isAdminUser && hasDevice && !device?.id && selectedSite) {
    return (
      <SafeAreaView style={{ flex: 1, backgroundColor: COLORS.dark, justifyContent: 'center', alignItems: 'center' }}>
        <Text style={{ color: COLORS.white, fontSize: 16 }}>Loading device...</Text>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: isDark ? 'rgba(28, 34, 43, 1)' : 'rgba(243, 243, 245, 1)' }}>
      {!isAdminUser && <Sidebar ref={sidebarRef} />}

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

      <ScrollView
        contentContainerStyle={[
          styles.container,
          {
            backgroundColor: 'transparent',
            paddingTop: Platform.OS === 'ios' ? SPACING.md : insets.top + SPACING.xs,
            paddingBottom: insets.bottom + 100
          },
        ]}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={[COLORS.accent]}
            tintColor={COLORS.accent}
          />
        }
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.titleRow}>
          {/* Netzr Logo */}
          <View style={{ flex: 1, alignItems: 'flex-start' }}>
            <NetzrLogo width={80} height={25} isDark={isDark} />
          </View>

          {/* Site Selector Button */}
          {selectedSite && (
            <TouchableOpacity
              onPress={() => sidebarRef.current?.toggleSidebar()}
              style={{
                backgroundColor: isDark ? COLORS.surface : COLORS.white,
                paddingHorizontal: 12,
                paddingVertical: 8,
                borderRadius: 20,
                borderWidth: 1,
                borderColor: COLORS.border,
                marginRight: 8,
                flexDirection: 'row',
                alignItems: 'center',
                maxWidth: 150,
              }}
            >
              <Svg width={16} height={16} viewBox="0 0 24 24" style={{ marginRight: 6 }}>
                <Path
                  d="M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"
                  fill={COLORS.accent}
                />
              </Svg>
              <Text
                style={{
                  color: COLORS.accent,
                  fontSize: 13,
                  fontWeight: '600',
                  flexShrink: 1,
                  maxWidth: 100,
                }}
                numberOfLines={1}
                ellipsizeMode="tail"
              >
                {getSelectedSiteName()}
              </Text>
            </TouchableOpacity>
          )}

          {/* Notification Icon */}
          <TouchableOpacity
            onPress={() => navigation.navigate('Notifications')}
            style={{
              padding: 8,
              position: 'relative',
            }}
          >
            <Svg width={20} height={25} viewBox="0 0 57.03 72">
              <Path
                d="M7.53,62.8h10.2c.8,5.2,5.3,9.2,10.8,9.2s10-4,10.8-9.2h10.2c2.1,0,4.1-.9,5.6-2.5,1.4-1.6,2.1-3.7,1.9-5.8-.2-1.8-1.8-5.9-4.8-12.8-.4-1-.9-2.2-1.2-2.7v-10.8c0-11.7-8.8-21.6-20-22.8v-2.9c0-1.4-1.1-2.5-2.5-2.5s-2.5,1.1-2.5,2.5v2.9c-4.9.5-9.5,2.7-13.1,6.2-4.4,4.3-6.9,10-6.9,16.2v11.2c-.2.5-.7,1.7-1.2,2.7C1.83,48.6.23,52.6.03,54.5c-.2,2.1.5,4.2,1.9,5.8,1.5,1.6,3.5,2.5,5.6,2.5ZM28.53,67c-2.7,0-5-1.8-5.7-4.2h11.5c-.8,2.4-3.1,4.2-5.8,4.2ZM9.43,43.7c1.5-3.5,1.6-3.6,1.6-4.3v-11.6c0-4.8,1.9-9.3,5.3-12.6,3.3-3.2,7.6-4.9,12.1-4.9h.6c9.3.3,16.9,8.4,16.9,18v11.2c0,.7.1.8,1.6,4.3,1.2,2.7,4.3,9.8,4.4,11.3.1.7-.2,1.4-.6,1.9-.5.5-1.1.8-1.9.8H7.53c-.7,0-1.4-.3-1.9-.8s-.7-1.2-.6-1.9c.2-1.5,3.3-8.7,4.4-11.4Z"
                fill={COLORS.textSoft}
              />
            </Svg>
            {unreadCount > 0 && (
              <View
                style={{
                  position: 'absolute',
                  top: 4,
                  right: 4,
                  backgroundColor: '#EF4444',
                  borderRadius: 10,
                  minWidth: 18,
                  height: 18,
                  justifyContent: 'center',
                  alignItems: 'center',
                  paddingHorizontal: 4,
                }}
              >
                <Text
                  style={{
                    color: '#FFFFFF',
                    fontSize: 10,
                    fontWeight: '700',
                  }}
                >
                  {unreadCount > 99 ? '99+' : unreadCount}
                </Text>
              </View>
            )}
          </TouchableOpacity>
        </View>

        <WeatherStatus deviceId={device?.id} />

        <PowerFlowDiagram />
        <TodaySummary />
        <BatteryStateCard deviceId={device?.id} />
        <InverterModeCard COLORS={COLORS} />
        {/* <DashboardModeCard COLORS={COLORS} /> */}
        <DemandBreakdownChart />
        <PowerAndEnergyMetrics
          COLORS={COLORS}
          isDark={isDark}
          selectedRange={selectedRange}
          setSelectedRange={setSelectedRange}
        />
        <GridPerformanceCard deviceId={device?.id} />

        <EnergyReviewCard />

        <CO2Card deviceId={device?.id} />
      </ScrollView>
    </SafeAreaView>
  );
}
