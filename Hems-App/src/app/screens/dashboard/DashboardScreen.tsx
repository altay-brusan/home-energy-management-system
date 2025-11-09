import { useFocusEffect, useNavigation, useRoute } from "@react-navigation/native";
import React, { JSX, useCallback, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Image, Platform, RefreshControl, SafeAreaView, ScrollView, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
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
import DashboardFlowChart from './ui/dashboardFlowChart';
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
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
    <SafeAreaView style={{ flex: 1, backgroundColor: COLORS.dark }}>
      {!isAdminUser && <Sidebar ref={sidebarRef} />}

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
            backgroundColor: COLORS.dark,
            paddingTop: Platform.OS === 'ios' ? SPACING.md : insets.top + SPACING.xs,
            paddingBottom: insets.bottom + 24
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
        {/* <DashboardFlowChart
          COLORS={COLORS}
          isDark={isDark}
        /> */}

        {/* <DashboardGraph
          COLORS={COLORS}
          isDark={isDark}
          selectedRange={selectedRange}
          setSelectedRange={setSelectedRange}
        /> */}


        {/* <BatteryChart
          COLORS={COLORS}
          selectedIndex={selectedBatteryIndex}
          setSelectedIndex={setSelectedBatteryIndex}
        /> */}

        <MultiLineChart
          COLORS={COLORS}
          selectedRange={selectedRange}
        />

        <ChargeDischargeChart
          COLORS={COLORS}
          selectedIndex={selectedChargeIndex}
          setSelectedIndex={setSelectedChargeIndex}
        />

        <GridPerformanceCard deviceId={device?.id} />

        <EnergyReviewCard />

        <CO2Card deviceId={device?.id} />

        <DashboardDevices COLORS={COLORS} />

        {/* SignalR Connection Status 
        <SignalRStatus COLORS={COLORS} />
*/}

        {/*  
  <DashboardNotifications COLORS={COLORS} /> */}

        <DashboardHelpButton COLORS={COLORS} onPress={handleHelp} />
      </ScrollView>
    </SafeAreaView>
  );
}
