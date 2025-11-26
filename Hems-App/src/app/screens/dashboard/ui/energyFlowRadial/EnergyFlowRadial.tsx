import React, { useState, useCallback, useEffect } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import Svg from 'react-native-svg';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import mockPowerService, { RealTimePowerResponseDto } from '../../../../../api/services/mockPowerService';
import { dashboardService } from '../../../../../api/services/dashboardService';
import { EnergyNode, AnimatedFlowLine, CenterHub } from './components';
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

  const [loading, setLoading] = useState(true);
  const [powerData, setPowerData] = useState<RealTimePowerResponseDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [batterySoc, setBatterySoc] = useState<number | undefined>(undefined);

  // Calculate center position
  const centerX = CONTAINER_WIDTH / 2;
  const centerY = CONTAINER_HEIGHT / 2 + 12; // Add small offset for labels

  // Load power data and battery SOC
  const loadPowerData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      // Fetch mock power data and real battery SOC in parallel
      const [powerData, socValue] = await Promise.all([
        mockPowerService.getCurrentPower(device.id),
        dashboardService.getDeviceSoc(device.id).catch(() => null),
      ]);

      setPowerData(powerData);
      setBatterySoc(socValue ?? undefined);
    } catch (err) {
      console.error('Error loading power data:', err);
      setError(t('energyFlowRadial.errorLoading'));
      setPowerData(null);
      setBatterySoc(undefined);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id, t]);

  // Initial load
  useEffect(() => {
    loadPowerData();
  }, [loadPowerData]);

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(loadPowerData);
    return () => unregister();
  }, [registerRefreshCallback, loadPowerData]);

  // Auto-refresh every 5 seconds (simulating real-time updates)
  useEffect(() => {
    if (!hasDevice || !device?.id) return;

    const interval = setInterval(() => {
      loadPowerData();
    }, 5000);

    return () => clearInterval(interval);
  }, [hasDevice, device?.id, loadPowerData]);

  // Render states
  if (!hasDevice) {
    return (
      <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>{t('energyFlowRadial.noDevice')}</Text>
        </View>
      </Animated.View>
    );
  }

  if (loading && !powerData) {
    return (
      <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
          <Text style={styles.loadingText}>{t('energyFlowRadial.loading')}</Text>
        </View>
      </Animated.View>
    );
  }

  if (error || !powerData) {
    return (
      <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
        <View style={styles.emptyContainer}>
          <Text style={styles.emptyText}>
            {error || t('energyFlowRadial.noData')}
          </Text>
        </View>
      </Animated.View>
    );
  }

  // Use accent color for flow lines
  const flowColor = COLORS.accent;
  const baseLineColor = isDark ? '#374151' : '#CBD5E1'; // Dark gray for dark mode

  return (
    <Animated.View entering={FadeInDown.delay(100)} style={styles.card}>
      <View style={[styles.diagramContainer, { width: CONTAINER_WIDTH, height: CONTAINER_HEIGHT }]}>
        {/* SVG Layer for flow lines */}
        <Svg
          width={CONTAINER_WIDTH}
          height={CONTAINER_HEIGHT}
          style={styles.svgContainer}
        >
          {/* PV to Center (Top -> Center) */}
          {/* PV: power > 0 means generating (output), flow from PV to Center */}
          {powerData.pv.active && powerData.pv.power > 0 && (
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
          {/* Battery: power > 0 = discharging (Battery -> Center), power < 0 = charging (Center -> Battery) */}
          {powerData.battery.active && Math.abs(powerData.battery.power) > 0 && (
            <AnimatedFlowLine
              startX={centerX - OFFSET + NODE_SIZE / 2}
              startY={centerY}
              endX={centerX - CENTER_SIZE / 2}
              endY={centerY}
              direction={powerData.battery.power > 0 ? 'forward' : 'backward'}
              active={true}
              color={flowColor}
              baseColor={baseLineColor}
            />
          )}

          {/* Grid to/from Center (Right <-> Center) */}
          {/* Grid: power > 0 = exporting (Center -> Grid), power < 0 = importing (Grid -> Center) */}
          {powerData.grid.active && Math.abs(powerData.grid.power) > 0 && (
            <AnimatedFlowLine
              startX={centerX + CENTER_SIZE / 2}
              startY={centerY}
              endX={centerX + OFFSET - NODE_SIZE / 2}
              endY={centerY}
              direction={powerData.grid.power > 0 ? 'forward' : 'backward'}
              active={true}
              color={flowColor}
              baseColor={baseLineColor}
            />
          )}

          {/* Center to Load (Center -> Bottom) */}
          {/* Load: power < 0 means consuming (Center -> Load) */}
          {powerData.load.active && powerData.load.power < 0 && (
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
        <EnergyNode
          type="pv"
          power={Math.abs(powerData.pv.power)}
          active={powerData.pv.active}
          label={t('energyFlowRadial.labels.solar')}
          style={{
            position: 'absolute',
            left: centerX - NODE_SIZE / 2,
            top: centerY - OFFSET - NODE_SIZE / 2,
          }}
        />

        {/* Battery Node - Left */}
        <EnergyNode
          type="battery"
          power={Math.abs(powerData.battery.power)}
          active={powerData.battery.active}
          label={t('energyFlowRadial.labels.battery')}
          soc={batterySoc}
          style={{
            position: 'absolute',
            left: centerX - OFFSET - NODE_SIZE / 2,
            top: centerY - NODE_SIZE / 2,
          }}
        />

        {/* Grid Node - Right */}
        <EnergyNode
          type="grid"
          power={Math.abs(powerData.grid.power)}
          active={powerData.grid.active}
          label={t('energyFlowRadial.labels.grid')}
          style={{
            position: 'absolute',
            left: centerX + OFFSET - NODE_SIZE / 2,
            top: centerY - NODE_SIZE / 2,
          }}
        />

        {/* Load Node - Bottom */}
        <EnergyNode
          type="load"
          power={Math.abs(powerData.load.power)}
          active={powerData.load.active}
          label={t('energyFlowRadial.labels.home')}
          labelPosition="bottom"
          style={{
            position: 'absolute',
            left: centerX - NODE_SIZE / 2,
            top: centerY + OFFSET - NODE_SIZE / 2,
          }}
        />
      </View>
    </Animated.View>
  );
};

export default EnergyFlowRadial;
