import React, { useMemo, useState } from 'react';
import { LayoutChangeEvent, SafeAreaView, ScrollView, Text, View, RefreshControl } from 'react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { getPVStyles } from './PVPanelDetailScreen.styles';
import { Button } from '../../../components';
import AreaChart from '../dashboard/ui/dashboardGraph/components/AreaChart';
import BarChart from '../dashboard/ui/dashboardGraph/components/BarChart';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import DeviceHeader from './ui/DeviceHeader';

type Range = 'Day' | 'Week' | 'Month' | 'Year';

export default function PVPanelDetailScreen() {
  const navigation = useNavigation<any>();
  const isDark = useThemeStore(s => s.mode) === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = useMemo(() => getPVStyles(COLORS), [COLORS]);
  const [range, setRange] = useState<Range>('Day');
  const [type, setType] = useState<'bar' | 'area'>('area');
  const [size, setSize] = useState({ w: 0, h: 0 });
  const padding = { top: 10, right: 8, bottom: 18, left: 8 };
  const insets = useSafeAreaInsets();
  const [refreshing, setRefreshing] = useState(false);
  const onRefresh = () => {
    setRefreshing(true);
    setTimeout(() => setRefreshing(false), 800);
  };

  const datasets = {
    Day: [
      { x: '00:00', y: 1 }, { x: '04:00', y: 4 }, { x: '08:00', y: 12 }, { x: '12:00', y: 18 }, { x: '16:00', y: 10 }, { x: '20:00', y: 3 }, { x: '24:00', y: 1 },
    ],
    Week: [
      { x: 'Mon', y: 320 }, { x: 'Tue', y: 420 }, { x: 'Wed', y: 380 }, { x: 'Thu', y: 460 }, { x: 'Fri', y: 520 }, { x: 'Sat', y: 610 }, { x: 'Sun', y: 540 },
    ],
    Month: Array.from({ length: 12 }).map((_, i) => ({ x: `M${i + 1}`, y: Math.round(200 + Math.random() * 600) })),
    Year: Array.from({ length: 12 }).map((_, i) => ({ x: `${i + 1}`, y: Math.round(3000 + Math.random() * 2000) })),
  } as const;

  const data = datasets[range] ?? datasets.Week;
  const onLayout = (e: LayoutChangeEvent) => setSize({ w: e.nativeEvent.layout.width, h: e.nativeEvent.layout.height });
  const innerW = Math.max(size.w - padding.left - padding.right, 1);
  const innerH = Math.max(size.h - padding.top - padding.bottom, 1);
  const stepX = innerW / Math.max(data.length, 1);
  const maxY = Math.max(...data.map(d => d.y)) * 1.1 || 1;
  const yScale = (v: number) => innerH - (v / maxY) * innerH;
  const avg = Math.round(data.reduce((s, d) => s + d.y, 0) / Math.max(data.length, 1));
  const peak = Math.max(...data.map(d => d.y));

  return (
    <SafeAreaView style={styles.screen}>
      <DeviceHeader title="PV Panel" onBack={() => navigation.goBack()} />

      <ScrollView
        contentContainerStyle={[styles.container, { paddingTop: insets.top + 60 }]}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={COLORS.accent} />
        }
      >
        <View style={styles.card}>
          <Text style={styles.title}>PV Panel</Text>
          <Text style={styles.sub}>Gerçek zamanlı üretim ve tarihsel veriler</Text>
          <View style={styles.row}>
            {(['bar', 'area'] as const).map(t => (
              <Button key={t} title={t === 'bar' ? 'Bar' : 'Area'} variant={type === t ? 'primary' : 'secondary'} style={[styles.tabBtn, type === t && { backgroundColor: COLORS.accent }]} textStyle={{ color: type === t ? COLORS.white : COLORS.textSoft }} onPress={() => setType(t)} />
            ))}
          </View>
          <View style={styles.row}>
            {(['Day', 'Week', 'Month', 'Year'] as const).map(r => (
              <Button key={r} title={r} variant={range === r ? 'primary' : 'secondary'} style={[styles.tabBtn, range === r && { backgroundColor: COLORS.accent }]} textStyle={{ color: range === r ? COLORS.white : COLORS.textSoft }} onPress={() => setRange(r)} />
            ))}
          </View>
        </View>

        <View style={[styles.card, styles.chart]} onLayout={onLayout}>
          {type === 'bar' ? (
            <BarChart width={size.w} height={size.h} padding={padding} innerW={innerW} innerH={innerH} stepX={stepX} yScale={yScale} data={data} COLORS={COLORS} accentColor={COLORS.accent} avg={avg} peak={peak} selectedIndex={null} setSelectedIndex={() => {}} />
          ) : (
            <AreaChart width={size.w} height={size.h} padding={padding} innerW={innerW} innerH={innerH} stepX={stepX} yScale={yScale} data={data} COLORS={COLORS} accentColor={COLORS.accent} avg={avg} peak={peak} selectedIndex={null} setSelectedIndex={() => {}} />
          )}
        </View>

        <View style={styles.card}>
          <Text style={styles.title}>Basic Info</Text>
          <View style={styles.statRow}><Text style={styles.statText}>Seri Numarası</Text><Text style={styles.statText}>PV-00001234</Text></View>
          <View style={styles.statRow}><Text style={styles.statText}>Bağlı Cihaz</Text><Text style={styles.statText}>SigenStor</Text></View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
