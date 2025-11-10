import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, ActivityIndicator, TouchableOpacity, Modal, Pressable } from 'react-native';
import Animated, { FadeInDown } from 'react-native-reanimated';
import Svg, { Path, Circle, G } from 'react-native-svg';
import { useI18n } from '../../../../../hooks/useI18n';
import { useDashboardRefresh } from '../../../../../hooks/useDashboardRefresh';
import { mobileDashboardService } from '../../../../../api/services/mobileDashboardService';
import { useThemeStore } from '../../../../../store/themeStore';
import { getThemeColors } from '../../../../../theme/constants';
import { createStyles } from './CO2Card.styles';
import { CO2CardProps } from './CO2Card.types';

// Minimalist CO2 Cloud Icon
const CO2Icon: React.FC<{ size?: number }> = ({ size = 48 }) => (
  <Svg width={size} height={size} viewBox="0 0 64 64" fill="none">
    <Path
      d="M48 32c0-8.837-7.163-16-16-16-6.906 0-12.785 4.378-15.091 10.512C13.515 27.344 11 30.395 11 34c0 4.418 3.582 8 8 8h28c5.523 0 10-4.477 10-10 0-5.185-3.947-9.449-9-9.95"
      fill="#10b981"
      opacity="0.15"
    />
    <Path
      d="M48 32c0-8.837-7.163-16-16-16-6.906 0-12.785 4.378-15.091 10.512C13.515 27.344 11 30.395 11 34c0 4.418 3.582 8 8 8h28c5.523 0 10-4.477 10-10 0-5.185-3.947-9.449-9-9.95"
      stroke="#10b981"
      strokeWidth="1.5"
      strokeLinecap="round"
      fill="none"
      opacity="0.3"
    />
  </Svg>
);

// Minimalist Tree Icon
const TreeIcon: React.FC<{ size?: number }> = ({ size = 48 }) => (
  <Svg width={size} height={size} viewBox="0 0 64 64" fill="none">
    <Path
      d="M32 8L24 20h6v8h-6l8 12h8l8-12h-6v-8h6z"
      fill="#22c55e"
      opacity="0.15"
    />
    <Path
      d="M30 40h4v16h-4z"
      fill="#22c55e"
      opacity="0.2"
    />
    <Circle cx="20" cy="18" r="3" fill="#22c55e" opacity="0.1" />
    <Circle cx="44" cy="18" r="3" fill="#22c55e" opacity="0.1" />
  </Svg>
);

// Info Icon
const InfoIcon: React.FC<{ size?: number; color?: string }> = ({ size = 16, color = '#9ca3af' }) => (
  <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
    <Circle cx="12" cy="12" r="10" stroke={color} strokeWidth="2" fill="none" />
    <Path d="M12 16v-4M12 8h.01" stroke={color} strokeWidth="2" strokeLinecap="round" />
  </Svg>
);

const CO2Card: React.FC<CO2CardProps> = ({ deviceId }) => {
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { t } = useI18n();
  const styles = createStyles(COLORS);

  const [loading, setLoading] = useState(true);
  const [co2Saved, setCo2Saved] = useState<number>(0);
  const [treesEquivalent, setTreesEquivalent] = useState<number>(0);
  const [modalVisible, setModalVisible] = useState(false);

  const fetchCO2Data = useCallback(async () => {
    if (!deviceId) {
      setLoading(false);
      return;
    }

    try {
      setLoading(true);

      // Fetch battery component to get total load (lifetime)
      const batteryComponent = await mobileDashboardService.getBatteryComponent(deviceId);

      // Formula 1: P = Total Load (lifetime) × 0.25
      const totalLoad = batteryComponent.total || 0;
      const co2SavedKg = totalLoad * 0.25;

      // Formula 2: Trees = CO2 / 22 (based on 45/22 from requirements)
      const trees = co2SavedKg / 22;

      setCo2Saved(Math.round(co2SavedKg * 100) / 100); // Round to 2 decimals
      setTreesEquivalent(Math.round(trees * 10) / 10); // Round to 1 decimal
    } catch (error) {
      console.error('Error fetching CO2 data:', error);
      setCo2Saved(0);
      setTreesEquivalent(0);
    } finally {
      setLoading(false);
    }
  }, [deviceId]);

  const { registerRefreshCallback } = useDashboardRefresh();

  // Register refresh callback
  useEffect(() => {
    const unregister = registerRefreshCallback(fetchCO2Data);
    return () => {
      unregister();
    };
  }, [registerRefreshCallback, fetchCO2Data]);

  useEffect(() => {
    fetchCO2Data();
  }, [fetchCO2Data]);

  if (loading) {
    return (
      <Animated.View entering={FadeInDown.delay(400).duration(600)} style={styles.container}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={COLORS.accent} />
        </View>
      </Animated.View>
    );
  }

  if (!deviceId) return null;

  return (
    <>
      <Animated.View entering={FadeInDown.delay(400).duration(600)} style={styles.container}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>{t('environmentalBenefits.title')}</Text>
          <TouchableOpacity
            onPress={() => setModalVisible(true)}
            hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
          >
            <InfoIcon size={20} color="#9ca3af" />
          </TouchableOpacity>
        </View>

      {/* Grid 2 Columns */}
      <View style={styles.grid}>
        {/* CO2 Card */}
        <View style={styles.benefitCard}>
          <View style={styles.valueRow}>
            <Text style={styles.value}>{co2Saved.toLocaleString()}</Text>
            <Text style={styles.unit}>kg</Text>
          </View>
          <Text style={styles.label}>{t('environmentalBenefits.co2Savings')}</Text>
        </View>

        {/* Trees Card */}
        <View style={styles.benefitCard}>
          <View style={styles.valueRow}>
            <Text style={styles.value}>{treesEquivalent.toLocaleString()}</Text>
            <Text style={styles.unit}>Trees</Text>
          </View>
          <Text style={styles.label}>{t('environmentalBenefits.equivalentTrees')}</Text>
        </View>
      </View>
      </Animated.View>

      {/* Info Modal */}
      <Modal
        animationType="fade"
        transparent={true}
        visible={modalVisible}
        onRequestClose={() => setModalVisible(false)}
      >
        <Pressable
          style={styles.modalOverlay}
          onPress={() => setModalVisible(false)}
        >
          <Pressable style={styles.modalContent} onPress={(e) => e.stopPropagation()}>
            <Text style={styles.modalTitle}>{t('environmentalBenefits.infoTitle')}</Text>

            <View style={styles.modalSection}>
              <View style={styles.modalIconRow}>
                <View style={styles.modalIconBg}>
                  <Text style={styles.modalIcon}>🌍</Text>
                </View>
                <Text style={styles.modalSectionTitle}>{t('environmentalBenefits.co2Savings')}</Text>
              </View>
              <Text style={styles.modalText}>{t('environmentalBenefits.co2Info')}</Text>
            </View>

            <View style={styles.modalSection}>
              <View style={styles.modalIconRow}>
                <View style={styles.modalIconBg}>
                  <Text style={styles.modalIcon}>🌳</Text>
                </View>
                <Text style={styles.modalSectionTitle}>{t('environmentalBenefits.equivalentTrees')}</Text>
              </View>
              <Text style={styles.modalText}>{t('environmentalBenefits.treesInfo')}</Text>
            </View>

            <TouchableOpacity
              style={styles.modalButton}
              onPress={() => setModalVisible(false)}
            >
              <Text style={styles.modalButtonText}>{t('environmentalBenefits.gotIt')}</Text>
            </TouchableOpacity>
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
};

export default CO2Card;
