import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, TouchableOpacity } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './TodaySummary.styles';
import { mobileDashboardService, TodayEnergySummaryDto } from '../../../../../api/services/mobileDashboardService';


// Grid Import Icon (with down arrow badge at bottom-right)
const GridImportIcon: React.FC<{ color: string; badgeBg: string }> = ({ color, badgeBg }) => (
  <View style={{ position: 'relative', width: 20, height: 20 }}>
    <Svg width={20} height={20} viewBox="0 0 15.16 24.42">
      <Path
        d="M14.94,1.35h-5.48v-2.11h.84v.83c0,.23.19.42.42.42s.42-.19.42-.42v-.83h1.26c.46,0,.84-.38.84-.84v-.85c0-.46-.38-.84-.84-.84h-2.95v-.84c0-.93-.76-1.68-1.68-1.68s-1.68.76-1.68,1.68v.84h-2.95c-.46,0-.84.38-.84.84v.85c0,.46.38.84.84.84h1.26v.83c0,.23.19.42.42.42s.42-.19.42-.42v-.83h.84v2.11H.6c-.46,0-.84.38-.84.84v.84c0,.46.38.84.84.84h1.26v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h.84v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h1.69v14.74H.17c-.23,0-.42.19-.42.42s.19.42.42.42h15.16c.23,0,.42-.19.42-.42s-.19-.42-.42-.42h-5.9V4.03h1.69v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h.84v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h1.26c.46,0,.84-.38.84-.84v-.84c0-.46-.38-.84-.84-.84Z"
        fill={color}
        transform="translate(0 6.76)"
      />
    </Svg>
    {/* Down Arrow Badge */}
    <View style={{
      position: 'absolute',
      bottom: -3,
      right: -3,
      width: 12,
      height: 12,
      borderRadius: 6,
      backgroundColor: badgeBg,
      justifyContent: 'center',
      alignItems: 'center',
    }}>
      <Svg width={8} height={8} viewBox="0 0 12 12" fill="none">
        <Path
          d="M6 2v8m0 0L3 7m3 3l3-3"
          stroke={color}
          strokeWidth={1.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </Svg>
    </View>
  </View>
);

// Grid Export Icon (with up arrow badge at bottom-right)
const GridExportIcon: React.FC<{ color: string; badgeBg: string }> = ({ color, badgeBg }) => (
  <View style={{ position: 'relative', width: 20, height: 20 }}>
    <Svg width={20} height={20} viewBox="0 0 15.16 24.42">
      <Path
        d="M14.94,1.35h-5.48v-2.11h.84v.83c0,.23.19.42.42.42s.42-.19.42-.42v-.83h1.26c.46,0,.84-.38.84-.84v-.85c0-.46-.38-.84-.84-.84h-2.95v-.84c0-.93-.76-1.68-1.68-1.68s-1.68.76-1.68,1.68v.84h-2.95c-.46,0-.84.38-.84.84v.85c0,.46.38.84.84.84h1.26v.83c0,.23.19.42.42.42s.42-.19.42-.42v-.83h.84v2.11H.6c-.46,0-.84.38-.84.84v.84c0,.46.38.84.84.84h1.26v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h.84v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h1.69v14.74H.17c-.23,0-.42.19-.42.42s.19.42.42.42h15.16c.23,0,.42-.19.42-.42s-.19-.42-.42-.42h-5.9V4.03h1.69v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h.84v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h1.26c.46,0,.84-.38.84-.84v-.84c0-.46-.38-.84-.84-.84Z"
        fill={color}
        transform="translate(0 6.76)"
      />
    </Svg>
    {/* Up Arrow Badge */}
    <View style={{
      position: 'absolute',
      bottom: -3,
      right: -3,
      width: 12,
      height: 12,
      borderRadius: 6,
      backgroundColor: badgeBg,
      justifyContent: 'center',
      alignItems: 'center',
    }}>
      <Svg width={8} height={8} viewBox="0 0 12 12" fill="none">
        <Path
          d="M6 10V2m0 0L3 5m3-3l3 3"
          stroke={color}
          strokeWidth={1.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </Svg>
    </View>
  </View>
);

// Consumption Icon (Load/Home) - From EnergyNode
const ConsumptionIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={20} height={20} viewBox="0 0 18.4 24.37">
    <Path
      d="M9.08,1.39c-.49-.12-.99-.12-1.48,0-.52.13-1.02.48-2.27,1.38l-3.88,2.8c-.93.67-1.29.94-1.55,1.27-.24.31-.42.66-.53,1.04-.12.41-.12.86-.12,2.02v5c0,1.05,0,1.8.05,2.38.05.58.14.95.28,1.24.29.58.75,1.05,1.32,1.35.29.15.65.24,1.22.29.58.05,1.31.05,2.34.05h7.76c1.03,0,1.76,0,2.34-.05.57-.05.93-.14,1.22-.29.57-.3,1.03-.77,1.32-1.35.15-.29.24-.66.28-1.24.05-.59.05-1.34.05-2.38v-5c0-1.16,0-1.61-.12-2.02-.11-.38-.29-.73-.53-1.04-.26-.33-.62-.6-1.55-1.27l-3.88-2.8c-1.25-.9-1.75-1.25-2.27-1.38Z"
      fill={color}
      fillRule="evenodd"
    />
  </Svg>
);

// Production Icon (Lightning/Electricity)
const ProductionIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={20} height={20} viewBox="0 0 24 24" fill="none">
    <Path
      d="M13 2L3 14h8l-1 8 10-12h-8l1-8z"
      fill={color}
      stroke={color}
      strokeWidth={1}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </Svg>
);

interface MetricCardProps {
  label: string;
  value: string;
  unit: string;
  color: string;
  icon: React.ReactNode;
}

const MetricCard: React.FC<MetricCardProps> = ({ label, value, unit, color, icon }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = createStyles(COLORS);

  return (
    <View style={styles.metricCard}>
      <View style={[styles.iconContainer, { backgroundColor: `${color}15` }]}>
        {icon}
      </View>
      <View style={styles.metricContent}>
        <View style={styles.metricValueContainer}>
          <Text style={[styles.metricValue, { color: COLORS.textSoft }]}>{value}</Text>
          <Text style={styles.metricUnit}>{unit}</Text>
        </View>
        <Text style={styles.metricLabel}>{label}</Text>
      </View>
    </View>
  );
};

export const TodaySummary: React.FC = () => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();

  const [loading, setLoading] = useState(false);
  const [summaryData, setSummaryData] = useState<TodayEnergySummaryDto | null>(null);

  const formatValue = (value: number): string => {
    if (value >= 1000) {
      return (value / 1000).toFixed(1);
    }
    return value.toFixed(1);
  };

  const getUnit = (value: number): string => {
    return value >= 1000 ? 'MWh' : 'kWh';
  };

  const loadTodayData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      return;
    }

    try {
      setLoading(true);
      const data = await mobileDashboardService.getTodaySummary(device.id);
      setSummaryData(data);
    } catch (err) {
      console.error('Error loading today summary:', err);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  // Initial load
  useEffect(() => {
    loadTodayData();
  }, [loadTodayData]);

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(loadTodayData);
    return () => unregister();
  }, [registerRefreshCallback, loadTodayData]);



  if (!hasDevice) {
    return null;
  }

  return (
    <Animated.View entering={FadeInDown.delay(200)} style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <Text style={styles.title}>{t('todaySummary.title')}</Text>
       
      </View>

      {/* Grid Layout - 2x2 */}
      <View style={styles.gridContainer}>
        {/* Row 1 */}
        <View style={styles.row}>
          <MetricCard
            label={t('todaySummary.gridImport')}
            value={formatValue(summaryData?.gridImport ?? 0)}
            unit={getUnit(summaryData?.gridImport ?? 0)}
            color="#5EB2F7"
            icon={<GridImportIcon color="#5EB2F7" badgeBg={COLORS.surface} />}
          />
          <MetricCard
            label={t('todaySummary.gridExport')}
            value={formatValue(summaryData?.gridExport ?? 0)}
            unit={getUnit(summaryData?.gridExport ?? 0)}
            color="#5EB2F7"
            icon={<GridExportIcon color="#5EB2F7" badgeBg={COLORS.surface} />}
          />
        </View>

        {/* Row 2 */}
        <View style={styles.row}>
          <MetricCard
            label={t('todaySummary.consumption')}
            value={formatValue(summaryData?.consumption ?? 0)}
            unit={getUnit(summaryData?.consumption ?? 0)}
            color="#5EB2F7"
            icon={<ConsumptionIcon color="#5EB2F7" />}
          />
          <MetricCard
            label={t('todaySummary.production')}
            value={formatValue(summaryData?.production ?? 0)}
            unit={getUnit(summaryData?.production ?? 0)}
            color="#5EB2F7"
            icon={<ProductionIcon color="#5EB2F7" />}
          />
        </View>
      </View>
    </Animated.View>
  );
};

export default TodaySummary;
