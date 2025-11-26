import { useFocusEffect, useNavigation, useRoute } from "@react-navigation/native";
import React, { JSX, useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Image, Platform, RefreshControl, SafeAreaView, ScrollView, Text, View, TouchableOpacity } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import LinearGradient from 'react-native-linear-gradient';
import { apiClient } from '../../../api/client/apiClient';
import { sitesService } from "../../../api/services/sitesService";
import { useDashboardRefresh } from '../../../hooks/useDashboardRefresh';
import { useRoles } from '../../../hooks/useRoles';
import { useSelectedSiteUtils } from '../../../hooks/useSelectedSite';
import { useSelectedSiteDevice } from '../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors, SPACING } from '../../../theme/constants';
import { getDashboardStyles } from './DashboardScreen.styles';
import SignalRStatus from './ui/SignalRStatus';
import DashboardDevices from './ui/dashboardDevices/DashboardDevices';
import DashboardGraph from './ui/dashboardGraph/DashboardGraph';
import BatteryChart from './ui/dashboardGraph/components/BatteryChart';
import BatterySOCPriceChart from './ui/dashboardGraph/components/BatterySOCPriceChart';
import BatterySOHChart from './ui/dashboardGraph/components/BatterySOHChart';
import ChargeDischargeChart from './ui/dashboardGraph/components/ChargeDischargeChart';
import MultiLineChart from './ui/dashboardGraph/components/MultiLineChart';
import DashboardHelpButton from './ui/dashboardHelpButton/DashboardHelpButton';
import DashboardHeroImage from './ui/dashboardHeroImage';
import { DashboardModeCard } from './ui/dashboardModeCard';
import Sidebar from "./ui/dashboardSideBar/Sidebar";
import { DashboardSummaryCard } from './ui/dashboardSummaryCard';
import { BatteryStateCard } from './ui/batteryStateCard';
import { CO2Card } from './ui/co2Card';
import EnergyReviewCard from './ui/energyReviewCard';
import EnergyChart from './ui/energyChart/EnergyChart';
import { GridPerformanceCard } from './ui/gridPerformanceCard';
import DashboardWelcomeCard from './ui/dashboardWelcomeCard/DashboardWelcomeCard';
import { EnergyFlowRadial } from './ui/energyFlowRadial';
import { TodaySummary } from './ui/todaySummary';
import { WeatherStatus } from './ui/weatherStatus';

type Range = 'Day' | 'Week' | 'Month' | 'Year';

export default function DashboardScreen (): JSX.Element {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = getDashboardStyles(COLORS);
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [selectedRange, setSelectedRange] = useState<Range>('Day');
  const [selectedBatteryIndex, setSelectedBatteryIndex] = useState<number | null>(null);
  const [selectedChargeIndex, setSelectedChargeIndex] = useState<number | null>(null);
  const route = useRoute();
  const navigation = useNavigation<any>();
  const { isAdmin, hasRole } = useRoles();
  const isAdminUser = isAdmin || hasRole("Administrator");
  const siteIdFromParam = (route.params as any)?.siteId;

  const [siteData, setSiteData] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  const { selectedSite, getSelectedSiteName } = useSelectedSiteUtils();

  const { selectedSiteDevice, hasDevice, device, externalDevices, setupInfo, loadSelectedSiteDevice } = useSelectedSiteDevice();

  const { refreshAll } = useDashboardRefresh();

  const sidebarRef = React.useRef<any>(null);

  React.useEffect(() => {
    if (selectedSite && selectedSite.hasDevice && !selectedSiteDevice) {loadSelectedSiteDevice().catch(error => {});
    }
  }, [selectedSite, selectedSiteDevice, loadSelectedSiteDevice]);

  useFocusEffect(
    useCallback(() => {
      if (isAdminUser && !selectedSite && !siteIdFromParam) {
        navigation.navigate('Admin');
      }
    }, [isAdminUser, selectedSite, siteIdFromParam, navigation])
  );

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

  const handleHelp = (): void => { };

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

      {/* <ShareButton
        color={COLORS.accent}
        backgroundColor={COLORS.dark}
        top={insets.top + 8}
        right={16}
        COLORS={COLORS}
      /> */}
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
          <View style={{ flex: 1, alignItems: 'center' }}>
            <Text style={[styles.screenTitle, { fontSize: 18 }]}>{t('dashboardScreen.welcome')}</Text>
            {selectedSite && (
              <Text style={[styles.screenTitle, { fontSize: 14, opacity: 0.7, marginTop: 4 }]}>
                {getSelectedSiteName()}
              </Text>
            )}
          </View>
          {/* Notification Icon */}
          <TouchableOpacity
            onPress={() => navigation.navigate('Notifications')}
            style={{
              padding: 8,
              position: 'absolute',
              right: 0,
              top: 0,
            }}
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
        <WeatherStatus deviceId={device?.id} />
        <EnergyFlowRadial />
        <TodaySummary />
        {/* <DashboardSummaryCard deviceId={device?.id} /> */}
        <BatteryStateCard deviceId={device?.id} />
        {/* <EnergyChart /> */}
        {/* <DashboardWelcomeCard isDark={isDark} COLORS={COLORS} /> */}
        {/* <DashboardHeroImage COLORS={COLORS} /> */}
        <DashboardModeCard COLORS={COLORS} />

        {/* <BatterySOCPriceChart
          COLORS={COLORS}
          isDark={isDark}
        /> */}
        {/* <BatterySOHChart
          COLORS={COLORS}
          isDark={isDark}
        /> */}

        <DashboardGraph
          COLORS={COLORS}
          isDark={isDark}
          selectedRange={selectedRange}
          setSelectedRange={setSelectedRange}
        />


        {/* <BatteryChart
          COLORS={COLORS}
          selectedIndex={selectedBatteryIndex}
          setSelectedIndex={setSelectedBatteryIndex}
        /> */}

        {/* <MultiLineChart
          COLORS={COLORS}
          selectedRange={selectedRange}
        /> */}

        <GridPerformanceCard deviceId={device?.id} />

        <EnergyReviewCard />

        <ChargeDischargeChart
          COLORS={COLORS}
          selectedIndex={selectedChargeIndex}
          setSelectedIndex={setSelectedChargeIndex}
        />

        <CO2Card deviceId={device?.id} />

        {/* <DashboardDevices COLORS={COLORS} /> */}

        {/* SignalR Connection Status 
        <SignalRStatus COLORS={COLORS} />
*/}

        {/*
  <DashboardNotifications COLORS={COLORS} /> */}

        {/* <DashboardHelpButton COLORS={COLORS} onPress={handleHelp} /> */}
      </ScrollView>
    </SafeAreaView>
  );
}
