import MaterialIcons from '@react-native-vector-icons/material-icons';
import { useFocusEffect, useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import React, { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, Text, TouchableOpacity, View } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import { useDeviceParameters } from '../../../../../hooks/useDeviceParameters';
import { getDashboardModeCardStyles } from './DashboardModeCard.styles';

type DashboardMode = {
  mode: string;
  description: string;
};

type RootStackParamList = {
  EnergyProfile: undefined;
};

type DashboardModeCardProps = {
  COLORS: Record<string, string>;
};

export default function DashboardModeCard ({ COLORS }: DashboardModeCardProps) {
  const styles = getDashboardModeCardStyles(COLORS);
  const navigation = useNavigation<NativeStackNavigationProp<RootStackParamList>>();
  const { t } = useTranslation();
  const deviceParams = useDeviceParameters();

  const [data, setData] = useState<DashboardMode | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  const calculateMode = useCallback(() => {
    if (!deviceParams?.data || !deviceParams?.findParameterByName) {
      setData({
        mode: t('dashboardMode.modes.manual'),
        description: t('dashboardMode.descriptions.manual'),
      });
      setLoading(false);
      return;
    }

    const batteryPriorityParam = deviceParams.findParameterByName("Battery Priorty,") || deviceParams.findParameterByName("Battery Priority");
    const loadPriorityParam = deviceParams.findParameterByName("Load Priority");
    const customParam = deviceParams.findParameterByName("Custom");

    let selectedMode: string | null = null;
    let descriptionKey = 'manual';
    let modeKey = 'manual';

    if (batteryPriorityParam?.setupDeviceParameterValue?.value === "true") {
      selectedMode = 'Battery Priority';
      descriptionKey = 'battery';
      modeKey = 'batteryPriority';
    } else if (loadPriorityParam?.setupDeviceParameterValue?.value === "true") {
      selectedMode = 'Load Priority';
      descriptionKey = 'load';
      modeKey = 'loadPriority';
    } else if (customParam?.setupDeviceParameterValue?.value === "true") {
      selectedMode = 'Custom';
      descriptionKey = 'manual';
      modeKey = 'manual';
    }

    if (selectedMode) {
      setData({
        mode: t(`dashboardMode.modes.${modeKey}`),
        description: t(`dashboardMode.descriptions.${descriptionKey}`),
      });
    } else {
      setData({
        mode: t('dashboardMode.modes.manual'),
        description: t('dashboardMode.descriptions.manual'),
      });
    }
    setLoading(false);
  }, [deviceParams?.data, deviceParams?.findParameterByName, t]);

  useEffect(() => {
    calculateMode();
  }, [calculateMode]);

  useFocusEffect(
    useCallback(() => {
      if (deviceParams?.refetch) {
        deviceParams.refetch();
      }
    }, [deviceParams?.refetch])
  );

  if (loading) {
    return (
      <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.widget}>
        <ActivityIndicator color={COLORS.accent} />
      </Animated.View>
    );
  }

  return (
    <Animated.View entering={FadeInDown.delay(200).duration(600)} style={styles.widget}>
      <TouchableOpacity
        style={{ flex: 1 }}
        activeOpacity={0.8}
        onPress={() => navigation.navigate('EnergyProfile')}
      >
        <View style={styles.headerRow}>
          <Text style={styles.title}>{t('dashboardMode.title')}</Text>
          <MaterialIcons name="settings" size={20} color={COLORS.textSoft} />
        </View>

    <View style={styles.modeRow}>
  <Text style={styles.mode}>
    {data?.mode === 'Manual'
      ? t('modeSelection.operation.custom.title')
      : data?.mode === 'Battery Priority'
      ? t('modeSelection.operation.batteryPriority.title')
      : data?.mode === 'Load Priority'
      ? t('modeSelection.operation.loadPriority.title')
      : data?.mode}
  </Text>
  <MaterialIcons name="check" size={20} color={COLORS.accent} style={styles.check} />
</View>

        <Text style={styles.desc}>{data?.description}</Text>
      </TouchableOpacity>
    </Animated.View>
  );
}
