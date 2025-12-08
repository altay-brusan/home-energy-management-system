import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../../../store/themeStore';
import { usePowerFlowStore } from '../../../../../store/powerFlowStore';
import { getThemeColors } from '../../../../../theme/constants';
import { usePowerFlowStream } from '../../../../../hooks/usePowerFlowStream';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { AnimatedFlowLine, CenterHub } from './components';
import { createStyles } from './PowerFlowDiagram.styles';

// SVG Icons
const SolarIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={24} height={24} viewBox="0 0 27.32 27.74">
    <Path
      d="M13.24,1.05V.41c0-.23.19-.41.42-.41s.42.19.42.41v.64c0,.23-.19.41-.42.41s-.42-.19-.42-.41ZM9.53,2.92c.08.08.19.12.3.12s.21-.04.3-.12c.16-.16.16-.43,0-.59l-.46-.45c-.16-.16-.43-.16-.59,0-.16.16-.16.43,0,.59l.46.45ZM7.6,6.83h.65c.23,0,.42-.19.42-.41s-.19-.41-.42-.41h-.65c-.23,0-.42.19-.42.41s.19.41.42.41ZM17.49,3.04c.11,0,.21-.04.3-.12l.46-.45c.16-.16.16-.43,0-.59-.16-.16-.43-.16-.59,0l-.46.45c-.16.16-.16.43,0,.59.08.08.19.12.3.12ZM19.08,6.83h.65c.23,0,.42-.19.42-.41s-.19-.41-.42-.41h-.65c-.23,0-.42.19-.42.41s.19.41.42.41ZM26.99,21.03c-.31.4-.78.62-1.28.62h-.96v.53c0,1.01-.83,1.84-1.86,1.84h-7.96v1.21h3.71c.63,0,1.14.51,1.14,1.13v.95c0,.23-.19.41-.42.41H7.97c-.23,0-.42-.19-.42-.41v-.95c0-.62.51-1.13,1.14-1.13h3.71v-1.21h-7.96c-1.02,0-1.86-.83-1.86-1.84v-.53h-.96c-.51,0-.97-.23-1.28-.62-.31-.4-.41-.9-.28-1.38l2.63-9.92c.19-.7.83-1.19,1.57-1.19h5.89c-.39-.63-.61-1.36-.61-2.12,0-2.25,1.85-4.08,4.13-4.08s4.13,1.83,4.13,4.08c0,.76-.21,1.49-.61,2.12h5.89c.73,0,1.38.49,1.57,1.19l2.63,9.92c.13.48.03.99-.28,1.38h0ZM13.24,20.83v-4.05h-5.36l-.54,4.05h5.9ZM1.9,15.95h5.24l.42-3.2H2.75l-.85,3.2ZM7.99,15.95h5.25v-3.2h-4.83l-.42,3.2h0ZM8.86,9.36l-.34,2.56h4.72v-2.56h-4.38ZM14.08,9.36v2.56h4.72l-.34-2.56h-4.38ZM14.08,20.83h5.9l-.54-4.05h-5.36v4.05ZM19.33,15.95l-.42-3.2h-4.83v3.2h5.25ZM20.18,15.95h5.24l-.85-3.2h-4.81l.42,3.2ZM23.07,9.36h-3.76l.34,2.56h4.7l-.53-1.98c-.09-.34-.4-.57-.75-.57h0ZM11.18,8.53h4.97c.52-.59.8-1.34.8-2.12,0-1.79-1.47-3.25-3.29-3.25s-3.29,1.46-3.29,3.25c0,.79.29,1.53.8,2.12h0ZM2.97,11.92h4.7l.34-2.56h-3.76c-.35,0-.66.24-.75.57l-.53,1.98h0ZM1.62,20.83h1.37s0,0,0,0,0,0,0,0h3.48l.54-4.05H1.68l-.82,3.08c-.06.23-.01.48.14.67.15.19.37.3.62.3h0ZM18.63,26.08h-4.12s0,0,0,0,0,0,0,0h-1.66s0,0,0,0,0,0,0,0h-4.12c-.17,0-.3.13-.3.3v.53h10.54v-.53c0-.16-.13-.3-.3-.3h0ZM14.08,24.03h-.84v1.21h.84v-1.21ZM23.9,21.66H3.42s0,.53,0,.53c0,.56.46,1.01,1.02,1.01h18.44c.56,0,1.02-.45,1.02-1.01v-.53h0ZM26.45,19.86l-.82-3.08h-5.35l.54,4.05h3.48s0,0,0,0,0,0,0,0h1.37c.24,0,.47-.11.62-.3.15-.19.2-.43.14-.67h0Z"
      fill={color}
    />
  </Svg>
);

const BatteryIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={24} height={24} viewBox="0 0 25.56 14.70">
    <Path
      d="M23.6,4.01h-.76v-1.14c0-1.59-1.29-2.87-2.87-2.87H2.87C1.29,0,.01,1.29,0,2.87v8.96c0,1.59,1.29,2.87,2.87,2.87h17.1c1.59,0,2.87-1.29,2.87-2.87v-1.14h.76c1.08,0,1.95-.88,1.96-1.96v-2.77c0-1.08-.88-1.95-1.96-1.96ZM21.65,11.83c0,.92-.75,1.67-1.68,1.68H2.87c-.93,0-1.67-.75-1.68-1.68V2.87c0-.93.75-1.67,1.68-1.68h17.1c.92,0,1.67.75,1.68,1.68v8.96ZM24.36,8.74c0,.42-.34.76-.76.76h-.76v-4.29h.76c.42,0,.76.34.76.76v2.77Z"
      fill={color}
    />
  </Svg>
);

const GridIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={24} height={24} viewBox="0 0 15.16 24.42">
    <Path
      d="M14.94,1.35h-5.48v-2.11h.84v.83c0,.23.19.42.42.42s.42-.19.42-.42v-.83h1.26c.46,0,.84-.38.84-.84v-.85c0-.46-.38-.84-.84-.84h-2.95v-.84c0-.93-.76-1.68-1.68-1.68s-1.68.76-1.68,1.68v.84h-2.95c-.46,0-.84.38-.84.84v.85c0,.46.38.84.84.84h1.26v.83c0,.23.19.42.42.42s.42-.19.42-.42v-.83h.84v2.11H.6c-.46,0-.84.38-.84.84v.84c0,.46.38.84.84.84h1.26v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h.84v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h1.69v14.74H.17c-.23,0-.42.19-.42.42s.19.42.42.42h15.16c.23,0,.42-.19.42-.42s-.19-.42-.42-.42h-5.9V4.03h1.69v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h.84v.84c0,.23.19.42.42.42s.42-.19.42-.42v-.84h1.26c.46,0,.84-.38.84-.84v-.84c0-.46-.38-.84-.84-.84ZM12.41-2.45v.85h-1.64s-.03,0-.04,0-.03,0-.04,0h-1.22v-.85h2.95ZM4.87-1.6s-.03,0-.04,0-.03,0-.04,0h-1.64v-.85h2.95v.85s-1.22,0-1.22,0ZM4.01,3.03s-.02,0-.03,0-.02,0-.03,0h-1.63s-.02,0-.03,0-.02,0-.03,0H.61v-.84h5.48v.84h-2.08ZM6.94,18.61V-4.13c0-.46.38-.84.84-.84s.84.38.84.84v22.74h-1.68ZM14.94,3.03h-1.65s-.02,0-.03,0-.02,0-.03,0h-1.63s-.02,0-.03,0-.02,0-.03,0h-2.08v-.84h5.48v.84Z"
      fill={color}
      transform="translate(0 6.76)"
    />
  </Svg>
);

const HomeIcon: React.FC<{ color: string }> = ({ color }) => (
  <Svg width={24} height={24} viewBox="0 0 18.4 24.37">
    <Path
      d="M9.08,1.39c-.49-.12-.99-.12-1.48,0-.52.13-1.02.48-2.27,1.38l-3.88,2.8c-.93.67-1.29.94-1.55,1.27-.24.31-.42.66-.53,1.04-.12.41-.12.86-.12,2.02v5c0,1.05,0,1.8.05,2.38.05.58.14.95.28,1.24.29.58.75,1.05,1.32,1.35.29.15.65.24,1.22.29.58.05,1.31.05,2.34.05h7.76c1.03,0,1.76,0,2.34-.05.57-.05.93-.14,1.22-.29.57-.3,1.03-.77,1.32-1.35.15-.29.24-.66.28-1.24.05-.59.05-1.34.05-2.38v-5c0-1.16,0-1.61-.12-2.02-.11-.38-.29-.73-.53-1.04-.26-.33-.62-.6-1.55-1.27l-3.88-2.8c-1.25-.9-1.75-1.25-2.27-1.38ZM7.3,.2c.68-.17,1.39-.17,2.07,0,.77.2,1.46.7,2.57,1.49.04.03.07.05.11.08l3.88,2.8s.05.04.08.06c.82.59,1.34.97,1.72,1.46.34.43.59.93.74,1.46.17.6.17,1.24.17,2.27v5.13c0,1.01,0,1.81-.05,2.46-.05.66-.16,1.2-.41,1.7-.41.81-1.06,1.47-1.85,1.89-.49.25-1.03.36-1.67.42-.63.05-1.42.05-2.41.05h-7.81c-1,0-1.78,0-2.41-.05-.64-.05-1.18-.16-1.67-.42-.8-.41-1.45-1.08-1.85-1.89-.25-.5-.36-1.05-.41-1.7-.05-.64-.05-1.44-.05-2.46v-5.03s0-.07,0-.1c0-1.03,0-1.67.17-2.27.15-.53.4-1.02.74-1.46.38-.49.9-.86,1.72-1.46.03-.02.05-.04.08-.06l3.88-2.8s.07-.05.11-.08c1.1-.8,1.8-1.3,2.57-1.49Z"
      fill={color}
      fillRule="evenodd"
    />
    <Path
      d="M8.68,6.54c.28.19.35.57.17.86l-1.8,2.74h3.72c.22,0,.43.13.53.33.11.2.09.44-.03.63l-2.42,3.7c-.19.28-.56.36-.84.17-.28-.19-.35-.57-.17-.86l1.8-2.74h-3.72c-.22,0-.43-.13-.53-.33-.11-.2-.09-.44.03-.63l2.42-3.7c.19-.28.56-.36.84-.17Z"
      fill={color}
      fillRule="evenodd"
    />
  </Svg>
);

// Layout constants
const CONTAINER_WIDTH = 350;
const CONTAINER_HEIGHT = 450; // Increased for labels
const NODE_SIZE = 100;
const CENTER_SIZE = 80; // Increased to fit logo
const OFFSET = 140; // Distance from center to nodes

/**
 * Format power value for display
 * - Below 1 kW: show as Watts (e.g., 854 W)
 * - 1 kW and above: show as kW with 2 decimals (e.g., 1.85 kW)
 * - Preserves negative sign when showSign is true (for battery charging)
 */
const formatPower = (kw: number, showSign: boolean = false): { value: string; unit: string } => {
  const absKw = Math.abs(kw);
  const sign = showSign && kw < 0 ? '-' : '';
  if (absKw < 1) {
    const watts = Math.round(absKw * 1000);
    return { value: `${sign}${watts}`, unit: 'W' };
  } else {
    return { value: `${sign}${absKw.toFixed(2)}`, unit: 'kW' };
  }
};

/**
 * PowerFlowDiagram Component
 *
 * Displays a radial energy flow diagram showing real-time power flow between:
 * - PV (Solar) - Top
 * - Battery - Left
 * - Grid - Right
 * - Load (House) - Bottom
 *
 * All energy flows through a central hub (inverter).
 */
export const PowerFlowDiagram: React.FC = () => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const { setBatteryPowerKw } = usePowerFlowStore();

  const [batterySoc, setBatterySoc] = useState<number | undefined>(undefined);
  const [dataToRender, setDataToRender] = useState<any>(null);

  // Get device ID from selected site's device
  const deviceIdToUse = device?.id;

  // Clear cached data and log when device ID changes
  useEffect(() => {
    /* console.log('🔌 [PowerFlowDiagram] Device ID:', deviceIdToUse, '| Has Device:', hasDevice); */

    // Clear old data when device changes
    setDataToRender(null);
    setBatterySoc(undefined);
  }, [deviceIdToUse, hasDevice]);

  // PowerFlow streaming hook
  const {
    powerFlow,
    isStreaming,
    streamState,
    error: streamError,
  } = usePowerFlowStream({
    deviceId: deviceIdToUse,
    intervalSeconds: 5,
    enabled: true,
  });

  // Force update when powerFlow changes
  useEffect(() => {
    if (powerFlow) {
      /* console.log('🔵 [PowerFlowDiagram] Updating dataToRender:', {
        solar: powerFlow.solarPowerKw,
        battery: powerFlow.batteryPowerKw,
        grid: powerFlow.gridPowerKw,
        load: powerFlow.loadPowerKw,
      }); */
      setDataToRender({ ...powerFlow });
      // Update battery power in store for BatteryStateCard
      setBatteryPowerKw(powerFlow.batteryPowerKw || 0);
    }
  }, [powerFlow, setBatteryPowerKw]);

  // Calculate center position
  const centerX = CONTAINER_WIDTH / 2;
  const centerY = CONTAINER_HEIGHT / 2 + 12; // Add small offset for labels

  // Load battery SOC (separate from power flow stream)
  const loadBatterySoc = useCallback(async () => {
    if (!deviceIdToUse) return;

    try {
      const socValue = await dashboardService.getDeviceSoc(deviceIdToUse);
      setBatterySoc(socValue ?? undefined);
    } catch (err) {
      console.error('Error fetching battery SOC:', err);
      setBatterySoc(undefined);
    }
  }, [deviceIdToUse]);

  // Load SOC initially and on refresh
  useEffect(() => {
    loadBatterySoc();
  }, [loadBatterySoc]);

  // Register refresh callback for SOC
  useEffect(() => {
    const unregister = registerRefreshCallback(loadBatterySoc);
    return () => unregister();
  }, [registerRefreshCallback, loadBatterySoc]);

  // Render states - show empty state if no device
  if (!hasDevice || !deviceIdToUse) {
    return (
      <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>{t('energyFlowRadial.noDevice')}</Text>
        </View>
      </Animated.View>
    );
  }

  // Show error state if stream failed
  if (streamError) {
    return (
      <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>{t('energyFlowRadial.connectionError')}</Text>
        </View>
      </Animated.View>
    );
  }

  // Show connecting/loading state if no data received yet
  if (!dataToRender) {
    return (
      <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
        <View style={styles.emptyContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
          <Text style={[styles.emptyText, { marginTop: 12 }]}>{t('energyFlowRadial.connecting')}</Text>
        </View>
      </Animated.View>
    );
  }

  // Use received data
  const finalData = dataToRender;

  // Use accent color for flow lines
  const flowColor = COLORS.accent;
  const baseLineColor = isDark ? '#374151' : '#CBD5E1'; // Dark gray for dark mode

  /* console.log('🎨 [PowerFlowDiagram] Rendering with dataToRender:', {
    solar: finalData.solarPowerKw,
    battery: finalData.batteryPowerKw,
    grid: finalData.gridPowerKw,
    load: finalData.loadPowerKw,
    batteryAbs: Math.abs(finalData.batteryPowerKw),
    loadAbs: Math.abs(finalData.loadPowerKw),
  }); */

 /*  console.log('🎨 [PowerFlowDiagram] About to render nodes with keys:', {
    pvKey: `pv-${finalData.solarPowerKw}`,
    batteryKey: `battery-${finalData.batteryPowerKw}`,
    gridKey: `grid-${finalData.gridPowerKw}`,
    loadKey: `load-${finalData.loadPowerKw}`,
  }); */

  return (
    <View style={styles.card}>
      <View style={[styles.diagramContainer, { width: CONTAINER_WIDTH, height: CONTAINER_HEIGHT }]}>
        {/* SVG Layer for flow lines */}
        <Svg
          width={CONTAINER_WIDTH}
          height={CONTAINER_HEIGHT}
          style={styles.svgContainer}
        >
          {/* PV to Center (Top -> Center) */}
          {finalData.solarPowerKw > 0.001 && (
            <AnimatedFlowLine
              startX={centerX}
              startY={centerY - OFFSET + NODE_SIZE / 2}
              endX={centerX}
              endY={centerY - CENTER_SIZE / 2}
              direction="forward"
              active={true}
              color={flowColor}
              baseColor={baseLineColor}
            />
          )}

          {/* Battery to/from Center (Left <-> Center) */}
          {Math.abs(finalData.batteryPowerKw) > 0.001 && (
            <AnimatedFlowLine
              startX={centerX - OFFSET + NODE_SIZE / 2}
              startY={centerY}
              endX={centerX - CENTER_SIZE / 2}
              endY={centerY}
              direction={finalData.batteryPowerKw > 0 ? 'forward' : 'backward'}
              active={true}
              color={flowColor}
              baseColor={baseLineColor}
            />
          )}

          {/* Grid to/from Center (Right <-> Center) */}
          {Math.abs(finalData.gridPowerKw) > 0.001 && (
            <AnimatedFlowLine
              startX={centerX + CENTER_SIZE / 2}
              startY={centerY}
              endX={centerX + OFFSET - NODE_SIZE / 2}
              endY={centerY}
              direction={finalData.gridPowerKw > 0 ? 'backward' : 'forward'}
              active={true}
              color={flowColor}
              baseColor={baseLineColor}
            />
          )}

          {/* Center to Load (Center -> Bottom) */}
          {finalData.loadPowerKw > 0.001 && (
            <AnimatedFlowLine
              startX={centerX}
              startY={centerY + CENTER_SIZE / 2}
              endX={centerX}
              endY={centerY + OFFSET - NODE_SIZE / 2}
              direction="forward"
              active={true}
              color={flowColor}
              baseColor={baseLineColor}
            />
          )}
        </Svg>

        {/* Center Hub (Inverter) */}
        <CenterHub
          size={CENTER_SIZE}
          style={{
            left: centerX - CENTER_SIZE / 2,
            top: centerY - CENTER_SIZE / 2,
          }}
        />

        {/* PV Node - Top */}
        <View style={{ position: 'absolute', left: centerX - NODE_SIZE / 2, top: centerY - OFFSET - NODE_SIZE / 2 }}>
          <Text style={{ position: 'absolute', top: -24, left: 0, right: 0, fontSize: 14, fontWeight: '500', textAlign: 'center', color: COLORS.textSoft }}>
            {t('energyFlowRadial.labels.solar')}
          </Text>
          <View
            style={{
              width: 100,
              height: 100,
              borderRadius: 16,
              backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
              borderColor: finalData.solarPowerKw >= 0.001 ? `${COLORS.accent}40` : COLORS.border,
              borderWidth: 4,
              opacity: finalData.solarPowerKw >= 0.001 ? 1 : 0.4,
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <SolarIcon color={finalData.solarPowerKw >= 0.001 ? COLORS.accent : COLORS.slate400} />
            <Text style={{ fontSize: 18, fontWeight: '700', marginTop: 4, color: finalData.solarPowerKw >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {formatPower(finalData.solarPowerKw).value}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: finalData.solarPowerKw >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              {formatPower(finalData.solarPowerKw).unit}
            </Text>
          </View>
        </View>

        {/* Battery Node - Left */}
        <View style={{ position: 'absolute', left: centerX - OFFSET - NODE_SIZE / 2, top: centerY - NODE_SIZE / 2 }}>
          <Text style={{ position: 'absolute', top: -24, left: 0, right: 0, fontSize: 14, fontWeight: '500', textAlign: 'center', color: COLORS.textSoft }}>
            {t('energyFlowRadial.labels.battery')}
          </Text>
          <View
            style={{
              width: 100,
              height: 100,
              borderRadius: 16,
              backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
              borderColor: Math.abs(finalData.batteryPowerKw) >= 0.001 ? `${COLORS.accent}40` : COLORS.border,
              borderWidth: 4,
              opacity: Math.abs(finalData.batteryPowerKw) >= 0.001 ? 1 : 0.4,
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <BatteryIcon color={Math.abs(finalData.batteryPowerKw) >= 0.001 ? COLORS.accent : COLORS.slate400} />
            <Text style={{ fontSize: 18, fontWeight: '700', marginTop: 4, color: Math.abs(finalData.batteryPowerKw) >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {formatPower(finalData.batteryPowerKw, true).value}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: Math.abs(finalData.batteryPowerKw) >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              {formatPower(finalData.batteryPowerKw, true).unit}
            </Text>
          </View>
          {batterySoc !== undefined && (
            <Text style={{ position: 'absolute', bottom: -26, left: 0, right: 0, fontSize: 12, fontWeight: '600', textAlign: 'center', color: COLORS.textSoft, opacity: 0.8 }}>
              SOC: {Math.round(batterySoc)}%
            </Text>
          )}
        </View>

        {/* Grid Node - Right */}
        <View style={{ position: 'absolute', left: centerX + OFFSET - NODE_SIZE / 2, top: centerY - NODE_SIZE / 2 }}>
          <Text style={{ position: 'absolute', top: -24, left: 0, right: 0, fontSize: 14, fontWeight: '500', textAlign: 'center', color: COLORS.textSoft }}>
            {t('energyFlowRadial.labels.grid')}
          </Text>
          <View
            style={{
              width: 100,
              height: 100,
              borderRadius: 16,
              backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
              borderColor: Math.abs(finalData.gridPowerKw) >= 0.001 ? `${COLORS.accent}40` : COLORS.border,
              borderWidth: 4,
              opacity: Math.abs(finalData.gridPowerKw) >= 0.001 ? 1 : 0.4,
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <GridIcon color={Math.abs(finalData.gridPowerKw) >= 0.001 ? COLORS.accent : COLORS.slate400} />
            <Text style={{ fontSize: 18, fontWeight: '700', marginTop: 4, color: Math.abs(finalData.gridPowerKw) >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {formatPower(finalData.gridPowerKw).value}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: Math.abs(finalData.gridPowerKw) >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              {formatPower(finalData.gridPowerKw).unit}
            </Text>
          </View>
        </View>

        {/* Load Node - Bottom */}
        <View style={{ position: 'absolute', left: centerX - NODE_SIZE / 2, top: centerY + OFFSET - NODE_SIZE / 2 }}>
          <View
            style={{
              width: 100,
              height: 100,
              borderRadius: 16,
              backgroundColor: isDark ? COLORS.surface : '#FFFFFF',
              borderColor: Math.abs(finalData.loadPowerKw) >= 0.001 ? `${COLORS.accent}40` : COLORS.border,
              borderWidth: 4,
              opacity: Math.abs(finalData.loadPowerKw) >= 0.001 ? 1 : 0.4,
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <HomeIcon color={Math.abs(finalData.loadPowerKw) >= 0.001 ? COLORS.accent : COLORS.slate400} />
            <Text style={{ fontSize: 18, fontWeight: '700', marginTop: 4, color: Math.abs(finalData.loadPowerKw) >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {formatPower(finalData.loadPowerKw).value}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: Math.abs(finalData.loadPowerKw) >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              {formatPower(finalData.loadPowerKw).unit}
            </Text>
          </View>
          <Text style={{ position: 'absolute', bottom: -24, left: 0, right: 0, fontSize: 14, fontWeight: '500', textAlign: 'center', color: COLORS.textSoft }}>
            {t('energyFlowRadial.labels.home')}
          </Text>
        </View>
      </View>
    </View>
  );
};

export default PowerFlowDiagram;
