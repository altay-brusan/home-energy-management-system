import FontAwesome from '@react-native-vector-icons/fontawesome';
import React, { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Text, View } from 'react-native';
import Animated, { FadeIn, FadeInUp } from 'react-native-reanimated';
import {
  BatteryComponentDto,
  InverterComponentDto,
  mobileDashboardService,
} from '../../../../../api/services/mobileDashboardService';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { getDashboardDevicesStyles } from './DashboardDevices.styles';
import type { DashboardDevicesProps } from './DashboardDevices.types';

type Device = {
  id: number;
  name: 'Inverter' | 'Battery' | 'EV Charger';
  status: 'Online' | 'Normal' | 'Offline';
  power?: number;
  powerUnit?: string;
  todayEnergy?: number;
  totalEnergy?: number;
  soc?: number;
  sessionEnergy?: number;
  lastSession?: string;
  componentName?: string;
  serialNumber?: string;
};

type DeviceIconName = 'plug' | 'battery-full' | 'bolt';

const deviceIcons: Record<Device['name'], { name: DeviceIconName }> = {
  Inverter: { name: 'plug' },
  Battery: { name: 'battery-full' },
  'EV Charger': { name: 'bolt' },
};

export default function DashboardDevices ({ COLORS }: DashboardDevicesProps) {
  const styles = getDashboardDevicesStyles(COLORS);
  const { t } = useTranslation();
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const [batteryData, setBatteryData] = useState<BatteryComponentDto | null>(null);
  const [inverterData, setInverterData] = useState<InverterComponentDto | null>(null);
  const [loading, setLoading] = useState(true);

  const loadDeviceData = useCallback(async () => {if (!hasDevice || !device?.id) {
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const [battery, inverter] = await Promise.all([
        mobileDashboardService.getBatteryComponent(device.id),
        mobileDashboardService.getInverterComponent(device.id),
      ]);
      setBatteryData(battery);
      setInverterData(inverter);
    } catch (error) {setBatteryData(null);
      setInverterData(null);
    } finally {
      setLoading(false);
    }
  }, [hasDevice, device?.id]);

  useEffect(() => {
    loadDeviceData();
  }, [loadDeviceData]);

  useEffect(() => {
    const unregister = registerRefreshCallback(loadDeviceData);
    return () => unregister();
  }, [registerRefreshCallback, loadDeviceData]);

  const devices: Device[] = React.useMemo(() => {
    const processedDevices: Device[] = [];
    let deviceId = 1;

    if (batteryData) {
      processedDevices.push({
        id: deviceId++,
        name: 'Battery',
        status: 'Normal',
        soc: Math.round(batteryData.soc || 0),
        todayEnergy: batteryData.today || 0,
        totalEnergy: batteryData.total || 0,
        componentName: 'Battery (BMS)',
      });
    }

    if (inverterData) {
      processedDevices.push({
        id: deviceId++,
        name: 'Inverter',
        status: 'Online',
        power: Math.round(inverterData.power || 0),
        powerUnit: inverterData.powerUnit || 'W',
        todayEnergy: inverterData.today || 0,
        totalEnergy: inverterData.total || 0,
        componentName: 'Inverter',
      });
    }
    return processedDevices;
  }, [batteryData, inverterData]);

  if (!hasDevice || !device?.id || loading || devices.length === 0) {
    return null;
  }

  return (
    <Animated.View entering={FadeIn.delay(400).duration(600)} style={styles.list}>
      <Text style={styles.sectionTitle}>{t('dashboardDevices.title')}</Text>

      {devices.map(deviceItem => {
        const iconInfo = deviceIcons[deviceItem.name];
        return (
          <Animated.View
            key={deviceItem.id}
            entering={FadeInUp.delay(450 + deviceItem.id * 80).duration(500)}
            style={styles.item}
          >
            <View style={styles.cardHeader}>
              <View style={styles.deviceIconWrap}>
                <FontAwesome
                  name={iconInfo.name}
                  size={20}
                  color={COLORS.accent}
                  style={styles.deviceIcon}
                />
              </View>
              <View style={styles.deviceInfo}>
                <Text style={styles.deviceName}>
                  {deviceItem.name === 'Inverter'
                    ? t('dashboardDevices.devices.inverter')
                    : deviceItem.name === 'Battery'
                      ? t('dashboardDevices.devices.battery')
                      : t('dashboardDevices.devices.evCharger')}
                </Text>
                {deviceItem.componentName && (
                  <Text style={styles.componentName}>{deviceItem.componentName}</Text>
                )}
              </View>
          <View style={styles.statusBadge}>
   <Text style={styles.statusText}>
     {t(`dashboardDevices.status.${deviceItem.status.toLowerCase()}`)}
   </Text>
 </View>
            </View>

            {deviceItem.name === 'Inverter' && (
              <View style={styles.inverterMetrics}>
                <View style={styles.topRow}>
                  {deviceItem.power !== undefined && deviceItem.power !== 0 && (
                    <View style={styles.metricItem}>
                     <Text style={styles.metricLabel}>{t('dashboardDevices.power')}</Text>
                      <Text style={styles.metricValue}>
                        {deviceItem.power} {deviceItem.powerUnit || 'W'}
                      </Text>
                    </View>
                  )}
                  {deviceItem.todayEnergy !== undefined && deviceItem.todayEnergy !== 0 && (
                    <View style={styles.metricItem}>
                      <Text style={styles.metricLabel}>{t('dashboardDevices.today')}</Text>
                      <Text style={styles.metricValue}>{deviceItem.todayEnergy} kWh</Text>
                    </View>
                  )}
                </View>
                {deviceItem.totalEnergy !== undefined && deviceItem.totalEnergy !== 0 && (
                  <View style={styles.totalMetricItem}>
                    <Text style={styles.totalMetricLabel}>{t('dashboardDevices.total')}</Text>
                    <Text style={styles.totalMetricValue}>{deviceItem.totalEnergy} kWh</Text>
                  </View>
                )}
              </View>
            )}

            {deviceItem.name === 'Battery' && (
              <View style={styles.metricsGrid}>
                {deviceItem.soc !== undefined && deviceItem.soc >= 0 && (
                  <View style={styles.metricItem}>
                <Text style={styles.metricLabel}>{t('dashboardDevices.soc')}</Text>
                    <Text style={styles.metricValue}>{deviceItem.soc}%</Text>
                  </View>
                )}
                {deviceItem.todayEnergy !== undefined && deviceItem.todayEnergy !== 0 && (
                  <View style={styles.metricItem}>
                   <Text style={styles.metricLabel}>{t('dashboardDevices.today')}</Text>
                    <Text style={styles.metricValue}>{deviceItem.todayEnergy} kWh</Text>
                  </View>
                )}
                {deviceItem.totalEnergy !== undefined && deviceItem.totalEnergy !== 0 && (
                  <View style={styles.metricItem}>
                   <Text style={styles.metricLabel}>{t('dashboardDevices.total')}</Text>
                    <Text style={styles.metricValue}>{deviceItem.totalEnergy} kWh</Text>
                  </View>
                )}
              </View>
            )}
          </Animated.View>
        );
      })}
    </Animated.View>
  );
}
