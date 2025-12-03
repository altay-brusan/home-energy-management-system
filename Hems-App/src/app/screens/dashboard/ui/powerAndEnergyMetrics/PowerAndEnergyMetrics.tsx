import MaterialIcons from '@react-native-vector-icons/material-icons';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, LayoutChangeEvent, Platform, Text, TouchableOpacity, Vibration, View } from 'react-native';
import Animated, { FadeInUp, useAnimatedStyle, useSharedValue, withSpring } from 'react-native-reanimated';
import { EnergyConsumptionResponseDto, EnergyProductionResponseDto, mobileDashboardService, SOCResponseDto } from '../../../../../api/services/mobileDashboardService';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { getPowerAndEnergyMetricsStyles } from './PowerAndEnergyMetrics.styles';
import type { PowerAndEnergyMetricsProps } from './PowerAndEnergyMetrics.types';
import PowerMetricsChart from './components/PowerMetricsChart';

export default function PowerAndEnergyMetrics ({
  COLORS,
  isDark,
  selectedRange,
  setSelectedRange,
}: PowerAndEnergyMetricsProps) {
  const { t } = useTranslation();
  const styles = getPowerAndEnergyMetricsStyles(COLORS);

  return (
    <>
      <PowerMetricsChart COLORS={COLORS} />
    </>
  );
}
