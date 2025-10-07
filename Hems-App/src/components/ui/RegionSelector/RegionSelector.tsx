import React, { useState, useCallback, useRef, useMemo } from 'react';
import {
  View,
  Text,
  Pressable,
  Modal,
  ScrollView,
  Dimensions,
  Animated,
  Easing,
} from 'react-native';
import { makeRegionStyles } from './RegionSelector.styles';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';

export type Region = 'Europe' | 'US' | 'Asia' | 'Australia';

interface RegionSelectorProps {
  selectedRegion: Region;
  onRegionChange: (region: Region) => void;
}

const REGIONS = [
  {
    id: 'Europe',
    label: 'Europe',
    emoji: '🇪🇺',
    timezone: 'GMT+1',
    description: 'Frankfurt, London',
  },
  {
    id: 'US',
    label: 'United States',
    emoji: '🇺🇸',
    timezone: 'GMT-5',
    description: 'Virginia, Oregon',
  },
  {
    id: 'Asia',
    label: 'Asia Pacific',
    emoji: '🌏',
    timezone: 'GMT+8',
    description: 'Singapore, Tokyo',
  },
  {
    id: 'Australia',
    label: 'Australia',
    emoji: '🇦🇺',
    timezone: 'GMT+10',
    description: 'Sydney, Melbourne',
  },
] as const;

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

export default function RegionSelector({
  selectedRegion,
  onRegionChange,
}: RegionSelectorProps) {
  const [modalVisible, setModalVisible] = useState(false);
  const backdropAnim = useRef(new Animated.Value(0)).current;
  const translateYAnim = useRef(new Animated.Value(SCREEN_HEIGHT)).current;

  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const IN = getThemeColors(isDark);
  const styles = useMemo(
    () => makeRegionStyles(COLORS, IN, isDark),
    [COLORS, IN, isDark],
  );

  const openModal = useCallback(() => {
    setModalVisible(true);
    Animated.parallel([
      Animated.timing(backdropAnim, {
        toValue: 1,
        duration: 240,
        easing: Easing.out(Easing.cubic),
        useNativeDriver: true,
      }),
      Animated.spring(translateYAnim, {
        toValue: 0,
        useNativeDriver: true,
        speed: 18,
        bounciness: 6,
      }),
    ]).start();
  }, [backdropAnim, translateYAnim]);

  const closeModal = useCallback(() => {
    Animated.parallel([
      Animated.timing(backdropAnim, {
        toValue: 0,
        duration: 180,
        easing: Easing.in(Easing.cubic),
        useNativeDriver: true,
      }),
      Animated.timing(translateYAnim, {
        toValue: SCREEN_HEIGHT,
        duration: 200,
        easing: Easing.in(Easing.cubic),
        useNativeDriver: true,
      }),
    ]).start(() => setModalVisible(false));
  }, [backdropAnim, translateYAnim]);

  const handleRegionSelect = useCallback(
    (region: Region) => {
      onRegionChange(region);
      closeModal();
    },
    [onRegionChange, closeModal],
  );

  const handleButtonPress = useCallback(() => {
    openModal();
  }, [openModal]);

  const backdropStyle = { opacity: backdropAnim };
  const sheetStyle = { transform: [{ translateY: translateYAnim }] };
  const currentRegion = REGIONS.find(r => r.id === selectedRegion);

  return (
    <>
      <Pressable onPress={handleButtonPress} style={styles.buttonContainer}>
        <View style={styles.button}>
          <View style={styles.buttonContent}>
            <Text style={styles.emoji}>{currentRegion?.emoji}</Text>
            <View>
              <Text style={styles.regionText}>{selectedRegion}</Text>
              <Text style={styles.timezoneText}>{currentRegion?.timezone}</Text>
            </View>
            <View style={styles.chevron}>
              <Text style={styles.chevronText}>▼</Text>
            </View>
          </View>
        </View>
      </Pressable>

      {modalVisible && (
        <Modal
          animationType="none"
          transparent
          visible
          onRequestClose={closeModal}
          statusBarTranslucent
        >
          <View style={styles.modalContainer}>
            <Pressable style={styles.backdrop} onPress={closeModal}>
              <Animated.View style={[styles.backdropOverlay, backdropStyle]} />
            </Pressable>

            <Animated.View style={[styles.modalContent, sheetStyle]}>
              <View style={styles.sheet}>
                <View style={styles.handleContainer}>
                  <View style={styles.handle} />
                </View>

                <View style={styles.header}>
                  <Text style={styles.title}>Select Server Region</Text>
                  <Text style={styles.subtitle}>
                    Choose the closest region for optimal performance
                  </Text>
                </View>

                <ScrollView
                  style={styles.scrollView}
                  showsVerticalScrollIndicator={false}
                  bounces={false}
                >
                  <View style={styles.regionsList}>
                    {REGIONS.map(region => {
                      const isSelected = region.id === selectedRegion;
                      return (
                        <Pressable
                          key={region.id}
                          onPress={() => handleRegionSelect(region.id)}
                          style={({ pressed }) => [
                            styles.regionItem,
                            isSelected && styles.regionItemSelected,
                            pressed && styles.regionItemPressed,
                          ]}
                        >
                          <View style={styles.regionContent}>
                            <View style={styles.regionIcon}>
                              <Text style={styles.regionEmoji}>
                                {region.emoji}
                              </Text>
                            </View>

                            <View style={styles.regionInfo}>
                              <View style={styles.regionHeader}>
                                <Text
                                  style={[
                                    styles.regionLabel,
                                    isSelected && styles.regionLabelSelected,
                                  ]}
                                >
                                  {region.label}
                                </Text>
                                {isSelected && (
                                  <View style={styles.activeBadge}>
                                    <Text style={styles.activeBadgeText}>
                                      Active
                                    </Text>
                                  </View>
                                )}
                              </View>

                              <View style={styles.regionMeta}>
                                <Text style={styles.regionMetaText}>
                                  {region.timezone}
                                </Text>
                                <View style={styles.dot} />
                                <Text style={styles.regionMetaText}>
                                  {region.description}
                                </Text>
                              </View>
                            </View>

                            {isSelected && (
                              <View style={styles.checkmark}>
                                <Text style={styles.checkmarkText}>✓</Text>
                              </View>
                            )}
                          </View>
                        </Pressable>
                      );
                    })}
                  </View>

                  <View style={styles.infoCard}>
                    <Text style={styles.infoIcon}>💡</Text>
                    <View style={styles.infoContent}>
                      <Text style={styles.infoTitle}>Pro Tip</Text>
                      <Text style={styles.infoText}>
                        Selecting a region closer to your physical location will
                        result in lower latency and better connection stability.
                      </Text>
                    </View>
                  </View>
                </ScrollView>
              </View>
            </Animated.View>
          </View>
        </Modal>
      )}
    </>
  );
}
