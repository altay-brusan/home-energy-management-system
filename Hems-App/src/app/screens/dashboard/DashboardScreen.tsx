import React, { JSX, useCallback, useState } from 'react';
import { Image, RefreshControl, SafeAreaView, ScrollView, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getDashboardStyles } from './DashboardScreen.styles';
import DashboardDevices from './ui/dashboardDevices/DashboardDevices';
import DashboardGraph from './ui/dashboardGraph/DashboardGraph';
import BatteryChart from './ui/dashboardGraph/components/BatteryChart';
import ChargeDischargeChart from './ui/dashboardGraph/components/ChargeDischargeChart';
import BatterySOCPriceChart from './ui/dashboardGraph/components/BatterySOCPriceChart';
import DashboardFlowChart from './ui/dashboardFlowChart';
import DashboardHelpButton from './ui/dashboardHelpButton/DashboardHelpButton';
import DashboardNotifications from './ui/dashboardNotifications/DashboardNotifications';
import ShareButton from './ui/dashboardShareButton/ShareButton';
import DashboardStatusRow from './ui/dashboardStatusRow/DashboardStatusRow';
import DashboardWeather from './ui/dashboardWeather/DashboardWeather';
import DashboardWelcomeCard from './ui/dashboardWelcomeCard/DashboardWelcomeCard';
import DashboardHeroImage from './ui/dashboardHeroImage';
import Sidebar from "./ui/dashboardSideBar/Sidebar";
import { DashboardModeCard } from './ui/dashboardModeCard';

type Range = 'Day' | 'Week' | 'Month' | 'Year';

export default function DashboardScreen(): JSX.Element {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = getDashboardStyles(COLORS);
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [selectedRange, setSelectedRange] = useState<Range>('Day');
  const [selectedBatteryIndex, setSelectedBatteryIndex] = useState<number | null>(null);
  const [selectedChargeIndex, setSelectedChargeIndex] = useState<number | null>(null);

  const handleHelp = (): void => {};

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 1200);
  }, []);

  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: COLORS.dark }}>
          <Sidebar />

      <ShareButton
        color={COLORS.accent}
        backgroundColor={COLORS.dark}
        top={insets.top + 8}
        right={16}
        COLORS={COLORS}
      />
      <ScrollView
        contentContainerStyle={[
          styles.container,
          { backgroundColor: COLORS.dark, paddingBottom: insets.bottom + 24 },
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
          <Image
            source={require('../../../assets/images/logo.png')}
            style={styles.screenLogo}
          />
          <Text style={styles.screenTitle}>Dashboard</Text>
        </View>
        <DashboardHeroImage COLORS={COLORS} />
        <DashboardWelcomeCard isDark={isDark} COLORS={COLORS} />

    

        <DashboardStatusRow COLORS={COLORS} />
        <DashboardModeCard COLORS={COLORS} />
        <DashboardWeather COLORS={COLORS} isDark={isDark} />

        <BatterySOCPriceChart 
          COLORS={COLORS} 
          isDark={isDark} 
        />
        
        <DashboardFlowChart 
          COLORS={COLORS} 
          isDark={isDark} 
        />
        
        <DashboardGraph
          COLORS={COLORS}
          isDark={isDark}
          selectedRange={selectedRange}
          setSelectedRange={setSelectedRange}
        />

  
        <BatteryChart
          COLORS={COLORS}
          selectedIndex={selectedBatteryIndex}
          setSelectedIndex={setSelectedBatteryIndex}
        />

        <ChargeDischargeChart
          COLORS={COLORS}
          selectedIndex={selectedChargeIndex}
          setSelectedIndex={setSelectedChargeIndex}
        />

        <DashboardDevices COLORS={COLORS} />

        <DashboardNotifications COLORS={COLORS} />

        <DashboardHelpButton COLORS={COLORS} onPress={handleHelp} />
      </ScrollView>
    </SafeAreaView>
  );
}
