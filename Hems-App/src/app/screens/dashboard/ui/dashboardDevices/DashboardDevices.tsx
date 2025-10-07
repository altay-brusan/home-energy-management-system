import React from 'react';
import { View, Text } from 'react-native';
import Animated, { FadeIn, FadeInUp } from 'react-native-reanimated';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import { getDashboardDevicesStyles } from './DashboardDevices.styles';
import type { DashboardDevicesProps } from './DashboardDevices.types';
import { Badge } from '../../../../../components';

type Device = {
  id: number;
  name: 'Inverter' | 'Battery' | 'EV Charger';
  status: 'Online' | 'Normal' | 'Offline';
  power?: number;
  todayEnergy?: number;
  totalEnergy?: number;
  soc?: number;
  sessionEnergy?: number;
  lastSession?: string;
};

type DeviceIconName = 'plug' | 'battery-full' | 'bolt';

const deviceIcons: Record<Device['name'], { name: DeviceIconName }> = {
  Inverter: { name: 'plug' },
  Battery: { name: 'battery-full' },
  'EV Charger': { name: 'bolt' },
};
export default function DashboardDevices({ COLORS }: DashboardDevicesProps) {
  const styles = getDashboardDevicesStyles(COLORS);
  const devices: Device[] = [
    {
      id: 1,
      name: 'Inverter',
      status: 'Online',
      power: 4200,
      todayEnergy: 18.2,
      totalEnergy: 12500,
    },
    {
      id: 2,
      name: 'Battery',
      status: 'Normal',
      soc: 82,
      power: -1200,
      todayEnergy: 4.6,
      totalEnergy: 3200,
    },
    {
      id: 3,
      name: 'EV Charger',
      status: 'Offline',
      sessionEnergy: 0,
      lastSession: '2024-06-01 18:30',
    },
  ];
  return (
    <Animated.View
      entering={FadeIn.delay(400).duration(600)}
      style={styles.list}
    >
      <Text style={styles.sectionTitle}>Devices</Text>
      {devices.map(device => {
        const iconInfo: { name: DeviceIconName } = deviceIcons[device.name];
        return (
          <Animated.View
            key={device.id}
            entering={FadeInUp.delay(450 + device.id * 80).duration(500)}
            style={styles.item}
          >
            <View style={styles.deviceRow}>
              <View style={styles.deviceIconWrap}>
                <FontAwesome
                  name={iconInfo.name}
                  size={16}
                  color={COLORS.accent}
                  style={styles.deviceIcon}
                />
              </View>
              <Text style={styles.deviceName}>{device.name}</Text>
            </View>
            <View style={styles.badgeRow}>
              <Badge
                text={device.status}
                variant={
                  device.status === 'Online'
                    ? 'success'
                    : device.status === 'Normal'
                    ? 'info'
                    : 'error'
                }
              />
            </View>
            <View style={styles.metaRow}>
              {device.name === 'Inverter' && (
                <>
                  <Text style={styles.meta}>
                    Power:{' '}
                    <Text style={{ fontWeight: '700' }}>{device.power} W</Text>
                  </Text>
                  <Text style={styles.meta}>
                    Today:{' '}
                    <Text style={{ fontWeight: '700' }}>
                      {device.todayEnergy} kWh
                    </Text>
                  </Text>
                  <Text style={styles.meta}>
                    Total:{' '}
                    <Text style={{ fontWeight: '700' }}>
                      {device.totalEnergy} kWh
                    </Text>
                  </Text>
                </>
              )}
              {device.name === 'Battery' && (
                <>
                  <Text style={styles.meta}>
                    SOC:{' '}
                    <Text style={{ fontWeight: '700' }}>{device.soc}%</Text>
                  </Text>
                  <Text style={styles.meta}>
                    Power:{' '}
                    <Text style={{ fontWeight: '700' }}>{device.power} W</Text>
                  </Text>
                  <Text style={styles.meta}>
                    Today:{' '}
                    <Text style={{ fontWeight: '700' }}>
                      {device.todayEnergy} kWh
                    </Text>
                  </Text>
                </>
              )}
              {device.name === 'EV Charger' && (
                <>
                  <Text style={styles.meta}>
                    Last Session:{' '}
                    <Text style={{ fontWeight: '700' }}>
                      {device.lastSession}
                    </Text>
                  </Text>
                  <Text style={styles.meta}>
                    Session Energy:{' '}
                    <Text style={{ fontWeight: '700' }}>
                      {device.sessionEnergy} kWh
                    </Text>
                  </Text>
                </>
              )}
            </View>
          </Animated.View>
        );
      })}
    </Animated.View>
  );
}
