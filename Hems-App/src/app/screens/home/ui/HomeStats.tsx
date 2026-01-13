import React from 'react';
import { View, Text } from 'react-native';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useTranslation } from 'react-i18next';
import { SitesDashboardDto } from '../../../../api/types/sites.types';

type Props = {
  homes: { id: string; name: string; address: string; hasDevice: boolean; power: string }[];
  sitesDashboard: SitesDashboardDto | null;
  styles: any;
  COLORS: any;
};

export function HomeStats({ homes, sitesDashboard, styles, COLORS }: Props) {
  const { t } = useTranslation();

  // Use dashboard data if available, otherwise fallback to homes array
  const totalHomes = sitesDashboard?.totalHomes ?? homes.length;
  const totalDevices = sitesDashboard?.totalDevices ?? 0;
  const totalPower = sitesDashboard?.totalPower ?? '0kW';

  return (
    <View style={styles.statsSection}>
      <View style={styles.statCard}>
        <MaterialIcons name="home" size={20} color={COLORS.accent} />
        <Text style={styles.statNumber}>{totalHomes}</Text>
        <Text style={styles.statLabel}>{t('homeStats.homes')}</Text>
      </View>
      <View style={styles.statCard}>
        <MaterialIcons name="device-hub" size={20} color={COLORS.success} />
        <Text style={styles.statNumber}>{totalDevices}</Text>
        <Text style={styles.statLabel}>{t('homeStats.devices')}</Text>
      </View>
      <View style={styles.statCard}>
        <MaterialIcons name="flash-on" size={20} color={COLORS.warning} />
        <Text style={styles.statNumber}>{totalPower}</Text>
        <Text style={styles.statLabel}>{t('homeStats.totalPower')}</Text>
      </View>
    </View>
  );
}
