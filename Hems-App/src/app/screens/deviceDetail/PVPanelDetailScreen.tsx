import React, { useMemo, useState } from 'react';
import { LayoutChangeEvent, Pressable, ScrollView, Text, View, RefreshControl } from 'react-native';
import { ArrowLeft, Sun } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getPVStyles } from './PVPanelDetailScreen.styles';
import { SolarLineChart } from './ui/SolarLineChart';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { useI18n } from '../../../hooks/useI18n';

type Range = 'Day' | 'Week' | 'Month' | 'Year';

// Demo data generator based on range
const generateDemoData = (range: Range): { x: string; y: number }[] => {
  switch (range) {
    case 'Day':
      // 24 hours
      return Array.from({ length: 24 }, (_, i) => {
        const hour = i.toString().padStart(2, '0');
        // Solar panel produces energy during daylight hours (6-18) with peak around noon
        let value = 0;
        if (i >= 6 && i <= 18) {
          const peakHour = 12;
          const distanceFromPeak = Math.abs(i - peakHour);
          value = Math.max(0, 5 - (distanceFromPeak * 0.4)) + Math.random() * 1.5;
        }
        return { x: `${hour}:00`, y: Math.round(value * 10) / 10 };
      });
    
    case 'Week':
      // 7 days
      const dayNames = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
      return dayNames.map((day, i) => ({
        x: day,
        y: Math.round((3 + Math.random() * 4 + (i % 2 === 0 ? 1 : 0)) * 10) / 10,
      }));
    
    case 'Month':
      // 30 days
      return Array.from({ length: 30 }, (_, i) => ({
        x: `${i + 1}`,
        y: Math.round((2 + Math.random() * 5 + (i % 7 === 0 ? 1.5 : 0)) * 10) / 10,
      }));
    
    case 'Year':
      // 12 months
      const monthNames = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
      return monthNames.map((month, i) => {
        // Higher production in summer months (June-August: index 5-7)
        const isSummer = i >= 5 && i <= 7;
        const baseValue = isSummer ? 4 : 2;
        return {
          x: month,
          y: Math.round((baseValue + Math.random() * 3) * 10) / 10,
        };
      });
    
    default:
      return [];
  }
};

const SOLAR_COLOR = '#FACC15';

export default function PVPanelDetailScreen() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getPVStyles(COLORS), [COLORS]);
  const { t } = useI18n();
  const [range, setRange] = useState<Range>('Day');
  const [size, setSize] = useState({ w: 0, h: 0 });
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState(false);

  const onRefresh = () => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 800);
  };

  // Generate demo data based on selected range
  const data = useMemo(() => generateDemoData(range), [range]);

  const onLayout = (e: LayoutChangeEvent) => setSize({ w: e.nativeEvent.layout.width, h: e.nativeEvent.layout.height });

  // Calculate statistics
  const totalProduction = data.reduce((s, d) => s + d.y, 0);
  const avg = totalProduction / Math.max(data.length, 1);
  const peak = Math.max(...data.map(d => d.y));

  return (
    <View style={[styles.screen, { paddingTop: insets.top }]}>
      {/* Header - BatteryListScreen style */}
      <View style={styles.header}>
        <Pressable style={styles.backButton} onPress={() => navigation.goBack()}>
          <ArrowLeft size={24} color={COLORS.textSoft} strokeWidth={2} />
        </Pressable>
        <Text style={styles.headerTitle}>{t('deviceDetail.pvPanel.title')}</Text>
        <View style={styles.headerPlaceholder} />
      </View>

      <ScrollView
        contentContainerStyle={styles.container}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.accent} />
        }
      >
        {/* Summary Card - Dashboard style */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <View style={[styles.cardIconContainer, { backgroundColor: `${SOLAR_COLOR}20` }]}>
              <Sun size={22} color={SOLAR_COLOR} strokeWidth={2} />
            </View>
            <View>
              <Text style={styles.cardTitle}>{t('deviceDetail.pvPanel.title')}</Text>
              <Text style={styles.cardSubtitle}>{t('deviceDetail.pvPanel.subtitle')}</Text>
            </View>
          </View>

          {/* Metrics Row - TodaySummary style */}
          <View style={styles.metricsRow}>
            <View style={styles.metricItem}>
              <Text style={styles.metricValue}>
                {peak.toFixed(1)}
                <Text style={styles.metricUnit}> kW</Text>
              </Text>
              <Text style={styles.metricLabel}>{t('dashboardGraph.peak')}</Text>
            </View>
            <View style={styles.metricDivider} />
            <View style={styles.metricItem}>
              <Text style={styles.metricValue}>
                {avg.toFixed(1)}
                <Text style={styles.metricUnit}> kW</Text>
              </Text>
              <Text style={styles.metricLabel}>{t('dashboardGraph.average')}</Text>
            </View>
            <View style={styles.metricDivider} />
            <View style={styles.metricItem}>
              <Text style={styles.metricValue}>
                {totalProduction.toFixed(1)}
                <Text style={styles.metricUnit}> kWh</Text>
              </Text>
              <Text style={styles.metricLabel}>{t('dashboardGraph.total')}</Text>
            </View>
          </View>
        </View>

        {/* Chart Card */}
        <View style={styles.chartCard}>
          <View style={styles.chartHeader}>
            <Text style={styles.chartTitle}>{t('dashboardGraph.production')}</Text>
          </View>

          <View style={styles.chart} onLayout={onLayout}>
            {size.w > 0 && size.h > 0 && data.length > 0 ? (
              <SolarLineChart
                data={data}
                range={range}
                COLORS={COLORS}
                width={size.w}
                height={size.h}
              />
            ) : (
              <View style={styles.emptyContainer}>
                <Text style={styles.emptyText}>{t('common.noData')}</Text>
              </View>
            )}
          </View>

          {/* Range Tabs - Dashboard style */}
          <View style={styles.rangeTabs}>
            {(['Day', 'Week', 'Month', 'Year'] as const).map((r) => {
              const isActive = range === r;
              return (
                <Pressable
                  key={r}
                  style={[
                    styles.rangeTab,
                    isActive && styles.rangeTabActive,
                  ]}
                  onPress={() => setRange(r)}
                >
                  <Text style={[
                    styles.rangeTabText,
                    isActive && styles.rangeTabTextActive,
                  ]}>
                    {t(`dashboardGraph.range.${r.toLowerCase()}`)}
                  </Text>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Info Card */}
        <View style={styles.infoCard}>
          <Text style={styles.infoTitle}>{t('deviceDetail.pvPanel.basicInfo')}</Text>
          <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>{t('deviceDetail.pvPanel.serialNumber')}</Text>
            <Text style={styles.infoValue}>—</Text>
          </View>
          <View style={[styles.infoRow, styles.infoRowLast]}>
            <Text style={styles.infoLabel}>{t('deviceDetail.pvPanel.connectedDevice')}</Text>
            <Text style={styles.infoValue}>—</Text>
          </View>
        </View>
      </ScrollView>
    </View>
  );
}
