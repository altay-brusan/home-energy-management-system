import React, { useMemo, useState } from 'react';
import { LayoutChangeEvent, SafeAreaView, ScrollView, Text, TouchableOpacity, View, RefreshControl, Vibration, Platform } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getPVStyles } from './PVPanelDetailScreen.styles';
import AreaChart from '../dashboard/ui/dashboardGraph/components/AreaChart';
import BarChart from '../dashboard/ui/dashboardGraph/components/BarChart';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import DeviceHeader from './ui/DeviceHeader';
import { useI18n } from '../../../hooks/useI18n';
import Animated, { useSharedValue, withSpring } from 'react-native-reanimated';
import MaterialIcons from '@react-native-vector-icons/material-icons';

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

export default function PVPanelDetailScreen() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getPVStyles(COLORS), [COLORS]);
  const { t } = useI18n();
  const [range, setRange] = useState<Range>('Day');
  const [type, setType] = useState<'bar' | 'area'>('area');
  const [size, setSize] = useState({ w: 0, h: 0 });
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const padding = { top: 20, right: 8, bottom: 80, left: 8 };
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState(false);
  
  const segmentedIndicatorPosition = useSharedValue(type === 'bar' ? 0 : 1);

  const safeVibrate = (duration: number) => {
    try {
      if (Platform.OS === 'android') {
        Vibration.vibrate(duration);
      } else if (Platform.OS === 'ios') {
        Vibration.vibrate();
      }
    } catch (error) {
      // Silent
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 800);
  };

  // Generate demo data based on selected range
  const data = useMemo(() => generateDemoData(range), [range]);
  
  const onLayout = (e: LayoutChangeEvent) => setSize({ w: e.nativeEvent.layout.width, h: e.nativeEvent.layout.height });
  const innerW = Math.max(size.w - padding.left - padding.right, 1);
  const innerH = Math.max(size.h - padding.top - padding.bottom, 1);
  const stepX = innerW / Math.max(data.length, 1);
  const maxY = Math.max(...data.map(d => d.y)) * 1.1 || 1;
  const yScale = (v: number) => innerH - (v / maxY) * innerH;
  const avg = Math.round(data.reduce((s, d) => s + d.y, 0) / Math.max(data.length, 1));
  const peak = Math.max(...data.map(d => d.y));

  // Reset selectedIndex when range or type changes
  React.useEffect(() => {
    setSelectedIndex(null);
  }, [range, type]);

  return (
    <SafeAreaView style={styles.screen}>
      <DeviceHeader title={t('deviceDetail.pvPanel.title')} onBack={() => navigation.goBack()} />

      <ScrollView
        contentContainerStyle={[styles.container, { paddingTop: insets.top + 60 }]}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.accent} />
        }
      >
        <View style={styles.card}>
          <Text style={styles.title}>{t('deviceDetail.pvPanel.title')}</Text>
          <Text style={styles.sub}>{t('deviceDetail.pvPanel.subtitle')}</Text>
          
          {/* Stat Pills */}
          <View style={styles.statPillsContainer}>
            <View style={styles.statPillContainer}>
              <View style={styles.statPill}>
                <Text style={styles.statPillLabel}>{t('dashboardGraph.peak')}</Text>
                <Text style={styles.statPillValue}>{peak.toFixed(1)}</Text>
              </View>
            </View>
            <View style={styles.statPillContainer}>
              <View style={styles.statPill}>
                <Text style={styles.statPillLabel}>{t('dashboardGraph.average')}</Text>
                <Text style={styles.statPillValue}>{avg.toFixed(1)}</Text>
              </View>
            </View>
          </View>
        </View>

        <View style={styles.card}>
          <View style={[styles.chart]} onLayout={onLayout}>
            {size.w > 0 && size.h > 0 && data.length > 0 ? (
              type === 'bar' ? (
                <BarChart 
                  width={size.w} 
                  height={size.h} 
                  padding={padding} 
                  innerW={innerW} 
                  innerH={innerH} 
                  stepX={stepX} 
                  yScale={yScale} 
                  data={data} 
                  COLORS={COLORS} 
                  accentColor={COLORS.accent} 
                  avg={avg} 
                  peak={peak} 
                  selectedIndex={selectedIndex} 
                  setSelectedIndex={setSelectedIndex}
                  selectedRange={range}
                />
              ) : (
                <AreaChart 
                  width={size.w} 
                  height={size.h} 
                  padding={padding} 
                  innerW={innerW} 
                  innerH={innerH} 
                  stepX={stepX} 
                  yScale={yScale} 
                  data={data} 
                  COLORS={COLORS} 
                  accentColor={COLORS.accent} 
                  avg={avg} 
                  peak={peak} 
                  selectedIndex={selectedIndex} 
                  setSelectedIndex={setSelectedIndex}
                  selectedRange={range}
                />
              )
            ) : (
              <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
                <Text style={{ color: COLORS.textSoft }}>{t('common.noData')}</Text>
              </View>
            )}
          </View>

          {/* Segmented Control for Chart Type */}
          <View style={styles.segmentedControl}>
            <View style={styles.segmentedTrack}>
              <Animated.View
                style={[
                  styles.segmentedIndicator,
                  {
                    backgroundColor: COLORS.accent,
                    left: type === 'bar' ? 2 : '50%',
                    right: type === 'bar' ? '50%' : 2
                  }
                ]}
              />
              {(['bar', 'area'] as const).map((chartType, index) => (
                <TouchableOpacity
                  key={chartType}
                  style={styles.segmentedButton}
                  onPress={() => {
                    safeVibrate(50);
                    segmentedIndicatorPosition.value = withSpring(index, {
                      damping: 15,
                      stiffness: 150,
                    });
                    setType(chartType);
                    setSelectedIndex(null);
                  }}
                  activeOpacity={0.7}
                >
                  <View style={styles.segmentedContent}>
                    <MaterialIcons
                      name={chartType === 'bar' ? 'bar-chart' : 'show-chart'}
                      size={16}
                      color={type === chartType ? COLORS.white : COLORS.textSoft}
                    />
                    <Text style={[
                      styles.segmentedText,
                      {
                        color: type === chartType ? COLORS.white : COLORS.textSoft,
                        fontWeight: type === chartType ? '600' : '500'
                      }
                    ]}>
                      {t(`dashboardGraph.chartType.${chartType}`)}
                    </Text>
                  </View>
                </TouchableOpacity>
              ))}
            </View>
          </View>

          {/* Pill Tabs for Range */}
          <View style={styles.pillTabs}>
            {(['Day', 'Week', 'Month', 'Year'] as const).map((r) => {
              const isActive = range === r;
              return (
                <TouchableOpacity
                  key={r}
                  style={[
                    styles.pillTab,
                    {
                      backgroundColor: isActive ? COLORS.accent : COLORS.bgShadeLight,
                      borderColor: isActive ? COLORS.accent : COLORS.border,
                      shadowColor: isActive ? COLORS.accent : 'transparent',
                      shadowOffset: { width: 0, height: 2 },
                      shadowOpacity: isActive ? 0.2 : 0,
                      shadowRadius: 3,
                      elevation: isActive ? 2 : 0,
                    }
                  ]}
                  onPress={() => {
                    safeVibrate(30);
                    setRange(r);
                    setSelectedIndex(null);
                  }}
                  activeOpacity={0.7}
                >
                  <Text style={[
                    styles.pillText,
                    {
                      color: isActive ? COLORS.white : COLORS.textSoft,
                      fontWeight: isActive ? '600' : '500',
                    }
                  ]}>
                    {t(`dashboardGraph.range.${r.toLowerCase()}`)}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>{t('deviceDetail.pvPanel.basicInfo')}</Text>
          <View style={styles.statRow}><Text style={styles.statText}>{t('deviceDetail.pvPanel.serialNumber')}</Text><Text style={styles.statText}>—</Text></View>
          <View style={styles.statRow}><Text style={styles.statText}>{t('deviceDetail.pvPanel.connectedDevice')}</Text><Text style={styles.statText}>—</Text></View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
