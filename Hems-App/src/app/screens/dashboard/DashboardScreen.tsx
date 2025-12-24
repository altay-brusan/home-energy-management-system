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

type Range = 'Day' | 'Week' | 'Month' | 'Year';

// GRP Logo Component
const GRPLogo = ({ width = 100, height = 23, isDark }: { width?: number; height?: number; isDark: boolean }) => {
  const fillColor = isDark ? '#FFFFFF' : '#13110c';
  const accentColor = isDark ? '#FFFFFF' : '#00a270';

  return (
    <Svg width={width} height={height} viewBox="0 0 272.31 62.57">
      <G>
        <Path fill={fillColor} d="M224.05,15.6v.54c0,1.18-.41,2.16-1.22,2.94-.81.78-1.89,1.17-3.22,1.17s-2.52-.45-3.42-1.37c-.9-.91-1.35-2.02-1.35-3.33s.45-2.42,1.35-3.33c.9-.91,2.03-1.37,3.4-1.37.84,0,1.6.2,2.3.6.7.4,1.24.93,1.62,1.59l-1.04.61c-.26-.49-.65-.89-1.17-1.18-.52-.3-1.1-.45-1.73-.45-1.04,0-1.88.34-2.55,1.02-.66.68-1,1.52-1,2.52s.33,1.84,1,2.51c.67.67,1.53,1.01,2.59,1.01.91,0,1.64-.22,2.2-.67.56-.44.9-1.02,1.02-1.73h-3.33v-1.09h4.53Z"/>
        <Path fill={fillColor} d="M232.96,20.09l-2.03-3.47h-2.14v3.47h-1.2v-9.06h3.62c.78,0,1.44.27,1.99.82s.83,1.21.83,1.99c0,.59-.17,1.12-.52,1.6-.35.48-.8.82-1.35,1.02l2.14,3.62h-1.33ZM228.79,12.16v3.38h2.42c.45,0,.83-.17,1.15-.5.31-.33.47-.73.47-1.2s-.16-.86-.47-1.19c-.32-.33-.7-.49-1.15-.49h-2.42Z"/>
        <Path fill={fillColor} d="M238.77 18.95 242.97 18.95 242.97 20.09 237.56 20.09 237.56 11.04 242.91 11.04 242.91 12.17 238.77 12.17 238.77 14.96 242.58 14.96 242.58 16.08 238.77 16.08 238.77 18.95"/>
        <Path fill={fillColor} d="M247.69 18.95 251.9 18.95 251.9 20.09 246.49 20.09 246.49 11.04 251.83 11.04 251.83 12.17 247.69 12.17 247.69 14.96 251.51 14.96 251.51 16.08 247.69 16.08 247.69 18.95"/>
        <Path fill={fillColor} d="M261.05 11.04 262.25 11.04 262.25 20.09 261.28 20.09 256.62 13.33 256.62 20.09 255.42 20.09 255.42 11.04 256.35 11.04 261.05 17.84 261.05 11.04"/>
        <Path fill={fillColor} d="M218.61,26.56c.84,0,1.53.28,2.09.83s.83,1.25.83,2.08-.28,1.51-.83,2.07c-.56.56-1.25.84-2.09.84h-2.08v3.23h-1.2v-9.06h3.29ZM218.61,31.26c.5,0,.91-.17,1.24-.51.33-.34.49-.77.49-1.27s-.16-.94-.49-1.28c-.33-.34-.74-.5-1.24-.5h-2.08v3.57h2.08Z"/>
        <Path fill={fillColor} d="M232.5,34.42c-.91.91-2.02,1.37-3.33,1.37s-2.42-.46-3.33-1.37c-.91-.91-1.36-2.02-1.36-3.33s.45-2.42,1.36-3.33c.91-.91,2.01-1.37,3.33-1.37s2.42.45,3.33,1.37c.91.91,1.36,2.02,1.36,3.33s-.46,2.41-1.36,3.33M226.68,33.6c.67.68,1.5,1.02,2.49,1.02s1.82-.34,2.49-1.02c.67-.68,1-1.51,1-2.5s-.33-1.84-1-2.52c-.67-.68-1.5-1.02-2.49-1.02s-1.82.34-2.49,1.02c-.67.68-1,1.52-1,2.52s.33,1.83,1,2.5"/>
        <Path fill={fillColor} d="M238.87 35.62 236.33 26.56 237.6 26.56 239.67 34.02 241.87 26.56 243.09 26.56 245.29 34.02 247.36 26.56 248.56 26.56 246.03 35.62 244.63 35.62 242.47 28.37 240.32 35.62 238.87 35.62"/>
        <Path fill={fillColor} d="M253.19 34.48 257.4 34.48 257.4 35.62 251.99 35.62 251.99 26.56 257.33 26.56 257.33 27.7 253.19 27.7 253.19 30.48 257.01 30.48 257.01 31.61 253.19 31.61 253.19 34.48"/>
        <Path fill={fillColor} d="M266.29,35.62l-2.03-3.47h-2.14v3.47h-1.2v-9.06h3.62c.78,0,1.44.27,1.99.82.55.55.83,1.21.83,1.99,0,.59-.17,1.12-.52,1.6-.35.48-.8.82-1.35,1.02l2.14,3.62h-1.33ZM262.12,27.69v3.38h2.42c.45,0,.83-.17,1.15-.5.31-.33.47-.73.47-1.2s-.16-.86-.47-1.19c-.32-.33-.7-.49-1.15-.49h-2.42Z"/>
        <Path fill={fillColor} d="M216.52 50.01 220.73 50.01 220.73 51.15 215.32 51.15 215.32 42.09 220.66 42.09 220.66 43.23 216.52 43.23 216.52 46.01 220.34 46.01 220.34 47.14 216.52 47.14 216.52 50.01"/>
        <Path fill={fillColor} d="M229.88 42.09 231.08 42.09 231.08 51.15 230.11 51.15 225.45 44.38 225.45 51.15 224.25 51.15 224.25 42.09 225.18 42.09 229.88 48.89 229.88 42.09"/>
        <Path fill={fillColor} d="M236.3 50.01 240.5 50.01 240.5 51.15 235.09 51.15 235.09 42.09 240.44 42.09 240.44 43.23 236.3 43.23 236.3 46.01 240.11 46.01 240.11 47.14 236.3 47.14 236.3 50.01"/>
        <Path fill={fillColor} d="M249.39,51.15l-2.03-3.47h-2.14v3.47h-1.2v-9.06h3.62c.78,0,1.44.27,1.99.82s.83,1.21.83,1.99c0,.59-.17,1.12-.52,1.6-.35.48-.8.82-1.35,1.02l2.14,3.62h-1.33ZM245.22,43.22v3.38h2.42c.45,0,.83-.17,1.15-.5.31-.33.47-.73.47-1.2s-.16-.86-.47-1.19c-.32-.33-.7-.49-1.15-.49h-2.42Z"/>
        <Path fill={fillColor} d="M262.69,46.66v.54c0,1.18-.41,2.16-1.22,2.94-.81.78-1.89,1.17-3.22,1.17s-2.52-.45-3.42-1.37c-.9-.91-1.35-2.02-1.35-3.33s.45-2.42,1.35-3.33c.9-.91,2.03-1.37,3.4-1.37.84,0,1.6.2,2.3.6.7.4,1.24.93,1.62,1.59l-1.04.61c-.26-.49-.65-.89-1.17-1.18-.52-.3-1.1-.45-1.73-.45-1.04,0-1.88.34-2.55,1.02-.66.68-1,1.52-1,2.52s.33,1.84,1,2.51c.67.67,1.53,1.01,2.59,1.01.91,0,1.64-.22,2.2-.67.56-.44.9-1.02,1.02-1.73h-3.33v-1.09h4.53Z"/>
        <Path fill={fillColor} d="M272.31 42.09 269.1 47.49 269.1 51.15 267.89 51.15 267.89 47.47 264.7 42.09 266.04 42.09 268.51 46.34 270.96 42.09 272.31 42.09"/>
        <Path fill={fillColor} d="M155.77,30.93c1.97-1.98,2.95-4.72,2.95-8.2,0-3.74-1.12-6.63-3.37-8.65-2.25-2.02-5.41-3.03-9.49-3.03h-17.46v23.36h3.93V14.42h12.97c3.03,0,5.35.69,6.96,2.08,1.61,1.39,2.41,3.46,2.41,6.23s-.81,4.85-2.41,6.23c-1.61,1.39-3.93,2.08-6.96,2.08h-2.4v3.31l.04.06,11.4,16.73h4.6l-11.51-16.79c3.59-.3,6.37-1.44,8.34-3.42"/>
        <Path fill={fillColor} d="M195.86,14.14c-2.25-2.06-5.41-3.09-9.49-3.09h-16.34v40.09h3.93V14.42h11.79c3.07,0,5.41.7,7.02,2.11,1.61,1.4,2.41,3.53,2.41,6.37s-.81,4.97-2.41,6.37c-1.61,1.4-3.95,2.1-7.02,2.1h-2.64v3.37h3.25c4.08,0,7.24-1.03,9.49-3.09,2.25-2.06,3.37-4.98,3.37-8.76s-1.12-6.7-3.37-8.76"/>
        <Path fill={fillColor} d="M99.6,48.21c-9.4,0-17.04-7.64-17.04-17.04s7.64-17.04,17.04-17.04c.09,0,.19,0,.28,0v-3.91c-.09,0-.19,0-.28,0-11.55,0-20.95,9.4-20.95,20.95s9.4,20.95,20.95,20.95c10.51,0,19.23-7.78,20.72-17.88h-3.97c-1.45,7.93-8.41,13.97-16.76,13.97"/>
        <Path fill={fillColor} d="M99.88 29.13 109.67 29.13 109.67 33.06 99.88 33.06 99.88 29.13"/>
        <Path fill={accentColor} d="M31.28,0C14.01,0,0,14.01,0,31.28s14.01,31.28,31.28,31.28,31.28-14.01,31.28-31.28S48.56,0,31.28,0M31.4,52.23c-11.55,0-20.95-9.4-20.95-20.95S19.85,10.34,31.4,10.34c.09,0,.19,0,.28,0v3.91c-.09,0-.19,0-.28,0-9.4,0-17.04,7.64-17.04,17.04s7.64,17.04,17.04,17.04c8.35,0,15.31-6.03,16.76-13.97h3.97c-1.49,10.1-10.21,17.88-20.72,17.88M41.46,33.19h-9.79v-3.93h9.79v3.93Z"/>
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
      // User has no sites - go to HomeSetup
      navigation.reset({ index: 0, routes: [{ name: 'HomeSetup' }] });
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
          {/* GRP Logo */}
          <View style={{ flex: 1, alignItems: 'flex-start' }}>
            <GRPLogo width={140} height={32} isDark={isDark} />
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
