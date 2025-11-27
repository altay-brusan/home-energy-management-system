import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import Svg from 'react-native-svg';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { usePowerFlowStream } from '../../../../../hooks/usePowerFlowStream';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { AnimatedFlowLine, CenterHub } from './components';
import { createStyles } from './EnergyFlowRadial.styles';

// Layout constants
const CONTAINER_WIDTH = 350;
const CONTAINER_HEIGHT = 450; // Increased for labels
const NODE_SIZE = 100;
const CENTER_SIZE = 80; // Increased to fit logo
const OFFSET = 140; // Distance from center to nodes

/**
 * EnergyFlowRadial Component
 *
 * Displays a radial energy flow diagram showing real-time power flow between:
 * - PV (Solar) - Top
 * - Battery - Left
 * - Grid - Right
 * - Load (House) - Bottom
 *
 * All energy flows through a central hub (inverter).
 */
export const EnergyFlowRadial: React.FC = () => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();

  const [batterySoc, setBatterySoc] = useState<number | undefined>(undefined);
  const [dataToRender, setDataToRender] = useState<any>(null);

  // Use static device ID for now
  const deviceIdToUse = '725dabc0-c6bf-11f0-a84b-2f34a5f564b4';

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
      console.log('🔵 [EnergyFlowRadial] Updating dataToRender:', {
        solar: powerFlow.solarPowerKw,
        battery: powerFlow.batteryPowerKw,
        grid: powerFlow.gridPowerKw,
        load: powerFlow.loadPowerKw,
      });
      setDataToRender({ ...powerFlow });
    }
  }, [powerFlow]);

  // Calculate center position
  const centerX = CONTAINER_WIDTH / 2;
  const centerY = CONTAINER_HEIGHT / 2 + 12; // Add small offset for labels

  // Load battery SOC (separate from power flow stream)
  const loadBatterySoc = useCallback(async () => {
    try {
      const socValue = await dashboardService.getDeviceSoc(deviceIdToUse);
      setBatterySoc(socValue ?? undefined);
    } catch (err) {
      // Silent error - SOC is optional
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

  // Render states
  // Disabled for testing with static device ID
  // if (!hasDevice) {
  //   return (
  //     <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
  //       <View style={styles.emptyContainer}>
  //         <Text style={styles.emptyText}>{t('energyFlowRadial.noDevice')}</Text>
  //       </View>
  //     </Animated.View>
  //   );
  // }

  // Use default values if no data yet
  const finalData = dataToRender || {
    solarPowerKw: 0,
    batteryPowerKw: 0,
    gridPowerKw: 0,
    loadPowerKw: 0,
    generatorPowerKw: 0,
    deviceId: '',
  };

  // Use accent color for flow lines
  const flowColor = COLORS.accent;
  const baseLineColor = isDark ? '#374151' : '#CBD5E1'; // Dark gray for dark mode

  console.log('🎨 [EnergyFlowRadial] Rendering with dataToRender:', {
    solar: finalData.solarPowerKw,
    battery: finalData.batteryPowerKw,
    grid: finalData.gridPowerKw,
    load: finalData.loadPowerKw,
    batteryAbs: Math.abs(finalData.batteryPowerKw),
    loadAbs: Math.abs(finalData.loadPowerKw),
  });

  console.log('🎨 [EnergyFlowRadial] About to render nodes with keys:', {
    pvKey: `pv-${finalData.solarPowerKw}`,
    batteryKey: `battery-${finalData.batteryPowerKw}`,
    gridKey: `grid-${finalData.gridPowerKw}`,
    loadKey: `load-${finalData.loadPowerKw}`,
  });

  return (
    <View style={styles.card}>
      {/* Live Streaming Indicator */}
      {isStreaming && (
        <View
          style={{
            position: 'absolute',
            top: 12,
            right: 12,
            flexDirection: 'row',
            alignItems: 'center',
            backgroundColor: isDark ? 'rgba(0, 0, 0, 0.3)' : 'rgba(255, 255, 255, 0.8)',
            paddingHorizontal: 8,
            paddingVertical: 4,
            borderRadius: 12,
            zIndex: 10,
          }}
        >
          <View
            style={{
              width: 6,
              height: 6,
              borderRadius: 3,
              backgroundColor: '#22C55E', // Green
              marginRight: 6,
            }}
          />
          <Text
            style={{
              fontSize: 10,
              color: COLORS.textSoft,
              fontWeight: '600',
            }}
          >
            LIVE
          </Text>
        </View>
      )}

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
              direction={finalData.gridPowerKw > 0 ? 'forward' : 'backward'}
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
            <Text style={{ fontSize: 18, fontWeight: '700', color: finalData.solarPowerKw >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {finalData.solarPowerKw.toFixed(3)}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: finalData.solarPowerKw >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              kW
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
            <Text style={{ fontSize: 18, fontWeight: '700', color: Math.abs(finalData.batteryPowerKw) >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {Math.abs(finalData.batteryPowerKw).toFixed(3)}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: Math.abs(finalData.batteryPowerKw) >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              kW
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
            <Text style={{ fontSize: 18, fontWeight: '700', color: Math.abs(finalData.gridPowerKw) >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {Math.abs(finalData.gridPowerKw).toFixed(3)}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: Math.abs(finalData.gridPowerKw) >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              kW
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
            <Text style={{ fontSize: 18, fontWeight: '700', color: Math.abs(finalData.loadPowerKw) >= 0.001 ? COLORS.text : COLORS.slate400 }}>
              {Math.abs(finalData.loadPowerKw).toFixed(3)}
            </Text>
            <Text style={{ fontSize: 12, marginTop: 2, color: Math.abs(finalData.loadPowerKw) >= 0.001 ? COLORS.textSoft : COLORS.border }}>
              kW
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

export default EnergyFlowRadial;
