import MaterialIcons from '@react-native-vector-icons/material-icons';
import React, { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  Text,
  TouchableOpacity,
  View,
  Modal,
  Pressable,
  Animated,
  Dimensions,
} from 'react-native';
import Reanimated, { FadeInDown } from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useSelectedSiteDevice } from '../../../../../hooks/useSelectedSiteDevice';
import { deviceSettingsService } from '../../../../../api/services/deviceSettingsService';
import { ExportStrategy } from '../../../../../api/types/deviceSettings.types';
import { getDashboardModeCardStyles } from './DashboardModeCard.styles';

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

type ExportStrategyOption = {
  id: ExportStrategy;
  title: string;
  description: string;
};

type DashboardModeCardProps = {
  COLORS: Record<string, string>;
};

export default function DashboardModeCard({ COLORS }: DashboardModeCardProps) {
  const styles = getDashboardModeCardStyles(COLORS);
  const { t } = useTranslation();
  const { device } = useSelectedSiteDevice();
  const insets = useSafeAreaInsets();

  // Bottom sheet states
  const [modeSheetVisible, setModeSheetVisible] = useState(false);
  const [confirmSheetVisible, setConfirmSheetVisible] = useState(false);
  const [selectedStrategy, setSelectedStrategy] = useState<ExportStrategyOption | null>(null);
  const [isChanging, setIsChanging] = useState(false);

  // Current active export strategy
  const [currentStrategy, setCurrentStrategy] = useState<ExportStrategy>(ExportStrategy.ZeroExportToLoad);

  // Animation refs
  const modeSheetY = useRef(new Animated.Value(SCREEN_HEIGHT)).current;
  const modeSheetOpacity = useRef(new Animated.Value(0)).current;
  const confirmSheetY = useRef(new Animated.Value(SCREEN_HEIGHT)).current;
  const confirmSheetOpacity = useRef(new Animated.Value(0)).current;

  const exportStrategies: ExportStrategyOption[] = [
    {
      id: ExportStrategy.SellFirst,
      title: t('exportStrategy.sellFirst.title'),
      description: t('exportStrategy.sellFirst.description'),
    },
    {
      id: ExportStrategy.ZeroExportToLoad,
      title: t('exportStrategy.zeroExportToLoad.title'),
      description: t('exportStrategy.zeroExportToLoad.description'),
    },
    {
      id: ExportStrategy.ZeroExportToCt,
      title: t('exportStrategy.zeroExportToCt.title'),
      description: t('exportStrategy.zeroExportToCt.description'),
    },
  ];

  // Get current strategy display info
  const getCurrentStrategyInfo = () => {
    const strategy = exportStrategies.find((s) => s.id === currentStrategy);
    return strategy || exportStrategies[1]; // Default to ZeroExportToLoad
  };

  const currentStrategyInfo = getCurrentStrategyInfo();

  // Mode sheet animations
  useEffect(() => {
    if (modeSheetVisible) {
      modeSheetY.setValue(SCREEN_HEIGHT);
      modeSheetOpacity.setValue(0);
      Animated.parallel([
        Animated.spring(modeSheetY, {
          toValue: 0,
          useNativeDriver: true,
          bounciness: 5,
          speed: 14,
        }),
        Animated.timing(modeSheetOpacity, {
          toValue: 1,
          duration: 200,
          useNativeDriver: true,
        }),
      ]).start();
    }
  }, [modeSheetVisible]);

  // Confirm sheet animations
  useEffect(() => {
    if (confirmSheetVisible) {
      confirmSheetY.setValue(SCREEN_HEIGHT);
      confirmSheetOpacity.setValue(0);
      Animated.parallel([
        Animated.spring(confirmSheetY, {
          toValue: 0,
          useNativeDriver: true,
          bounciness: 5,
          speed: 14,
        }),
        Animated.timing(confirmSheetOpacity, {
          toValue: 1,
          duration: 200,
          useNativeDriver: true,
        }),
      ]).start();
    }
  }, [confirmSheetVisible]);

  const closeModeSheet = () => {
    Animated.parallel([
      Animated.timing(modeSheetY, {
        toValue: SCREEN_HEIGHT,
        duration: 200,
        useNativeDriver: true,
      }),
      Animated.timing(modeSheetOpacity, {
        toValue: 0,
        duration: 150,
        useNativeDriver: true,
      }),
    ]).start(() => setModeSheetVisible(false));
  };

  const closeConfirmSheet = () => {
    Animated.parallel([
      Animated.timing(confirmSheetY, {
        toValue: SCREEN_HEIGHT,
        duration: 200,
        useNativeDriver: true,
      }),
      Animated.timing(confirmSheetOpacity, {
        toValue: 0,
        duration: 150,
        useNativeDriver: true,
      }),
    ]).start(() => {
      setConfirmSheetVisible(false);
      setSelectedStrategy(null);
    });
  };

  const handleSettingsPress = () => {
    setModeSheetVisible(true);
  };

  const handleStrategySelect = (strategy: ExportStrategyOption) => {
    setSelectedStrategy(strategy);
    closeModeSheet();
    setTimeout(() => {
      setConfirmSheetVisible(true);
    }, 250);
  };

  const handleConfirm = async () => {
    if (!selectedStrategy || !device?.id) return;

    setIsChanging(true);
    try {
      const result = await deviceSettingsService.changeExportStrategy({
        deviceId: device.id,
        exportStrategy: selectedStrategy.id,
      });

      if (result.success) {
        // Update current strategy on success
        setCurrentStrategy(selectedStrategy.id);
      }
    } catch (error) {
      // Error is handled by the API client
    } finally {
      setIsChanging(false);
      closeConfirmSheet();
    }
  };

  return (
    <>
      <Reanimated.View entering={FadeInDown.delay(200).duration(600)} style={styles.widget}>
        <TouchableOpacity style={{ flex: 1 }} activeOpacity={0.8} onPress={handleSettingsPress}>
          <View style={styles.headerRow}>
            <Text style={styles.title}>{t('dashboardMode.title')}</Text>
            <MaterialIcons name="settings" size={20} color={COLORS.textSoft} />
          </View>

          <View style={styles.modeRow}>
            <Text style={styles.mode}>{currentStrategyInfo.title}</Text>
            <MaterialIcons name="check" size={20} color={COLORS.accent} style={styles.check} />
          </View>

          <Text style={styles.desc}>{currentStrategyInfo.description}</Text>
        </TouchableOpacity>
      </Reanimated.View>

      {/* Mode Selection Bottom Sheet */}
      <Modal
        transparent
        visible={modeSheetVisible}
        onRequestClose={closeModeSheet}
        statusBarTranslucent
        animationType="none"
      >
        <Animated.View
          style={{
            flex: 1,
            backgroundColor: 'rgba(0,0,0,0.5)',
            opacity: modeSheetOpacity,
          }}
        >
          <Pressable style={{ flex: 1 }} onPress={closeModeSheet} />
        </Animated.View>

        <Animated.View
          style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            backgroundColor: COLORS.surface,
            borderTopLeftRadius: 20,
            borderTopRightRadius: 20,
            paddingBottom: insets.bottom + 16,
            transform: [{ translateY: modeSheetY }],
          }}
        >
          {/* Handle */}
          <View style={{ alignItems: 'center', paddingVertical: 12 }}>
            <View
              style={{
                width: 40,
                height: 4,
                backgroundColor: COLORS.border,
                borderRadius: 2,
              }}
            />
          </View>

          {/* Header */}
          <View
            style={{
              flexDirection: 'row',
              justifyContent: 'space-between',
              alignItems: 'center',
              paddingHorizontal: 20,
              paddingBottom: 16,
              borderBottomWidth: 1,
              borderBottomColor: COLORS.border,
            }}
          >
            <Text style={{ fontSize: 18, fontWeight: '600', color: COLORS.text }}>
              {t('exportStrategy.title')}
            </Text>
            <TouchableOpacity onPress={closeModeSheet}>
              <MaterialIcons name="close" size={24} color={COLORS.textSoft} />
            </TouchableOpacity>
          </View>

          {/* Options */}
          <View style={{ paddingHorizontal: 20, paddingTop: 16 }}>
            {exportStrategies.map((strategy) => {
              const isDisabled = strategy.id === ExportStrategy.SellFirst;
              const isSelected = strategy.id === currentStrategy;
              return (
                <TouchableOpacity
                  key={strategy.id}
                  onPress={() => !isDisabled && handleStrategySelect(strategy)}
                  disabled={isDisabled}
                  style={{
                    backgroundColor: isSelected ? `${COLORS.accent}15` : COLORS.background,
                    borderRadius: 12,
                    padding: 16,
                    marginBottom: 12,
                    borderWidth: isSelected ? 2 : 1,
                    borderColor: isSelected ? COLORS.accent : COLORS.border,
                    opacity: isDisabled ? 0.5 : 1,
                  }}
                  activeOpacity={isDisabled ? 1 : 0.7}
                >
                  <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Text
                      style={{
                        fontSize: 16,
                        fontWeight: '600',
                        color: isDisabled ? COLORS.textSoft : isSelected ? COLORS.accent : COLORS.text,
                        marginBottom: 4,
                        flex: 1,
                      }}
                    >
                      {strategy.title}
                    </Text>
                    {isSelected && (
                      <MaterialIcons name="check-circle" size={22} color={COLORS.accent} />
                    )}
                  </View>
                  <Text style={{ fontSize: 14, color: COLORS.textSoft, lineHeight: 20 }}>
                    {strategy.description}
                  </Text>
                </TouchableOpacity>
              );
            })}
          </View>
        </Animated.View>
      </Modal>

      {/* Confirmation Bottom Sheet */}
      <Modal
        transparent
        visible={confirmSheetVisible}
        onRequestClose={closeConfirmSheet}
        statusBarTranslucent
        animationType="none"
      >
        <Animated.View
          style={{
            flex: 1,
            backgroundColor: 'rgba(0,0,0,0.5)',
            opacity: confirmSheetOpacity,
          }}
        >
          <Pressable style={{ flex: 1 }} onPress={closeConfirmSheet} />
        </Animated.View>

        <Animated.View
          style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            right: 0,
            backgroundColor: COLORS.surface,
            borderTopLeftRadius: 20,
            borderTopRightRadius: 20,
            paddingBottom: insets.bottom + 16,
            transform: [{ translateY: confirmSheetY }],
          }}
        >
          {/* Handle */}
          <View style={{ alignItems: 'center', paddingVertical: 12 }}>
            <View
              style={{
                width: 40,
                height: 4,
                backgroundColor: COLORS.border,
                borderRadius: 2,
              }}
            />
          </View>

          {/* Content */}
          <View style={{ paddingHorizontal: 20, paddingTop: 8 }}>
            <Text
              style={{
                fontSize: 18,
                fontWeight: '600',
                color: COLORS.text,
                textAlign: 'center',
                marginBottom: 8,
              }}
            >
              {t('exportStrategy.confirmTitle')}
            </Text>

            <Text
              style={{
                fontSize: 14,
                color: COLORS.textSoft,
                textAlign: 'center',
                marginBottom: 24,
                lineHeight: 20,
              }}
            >
              {t('exportStrategy.confirmMessage', { mode: selectedStrategy?.title })}
            </Text>

            {/* Selected mode info */}
            {selectedStrategy && (
              <View
                style={{
                  backgroundColor: COLORS.background,
                  borderRadius: 12,
                  padding: 16,
                  marginBottom: 24,
                  borderWidth: 1,
                  borderColor: COLORS.accent,
                }}
              >
                <Text style={{ fontSize: 16, fontWeight: '600', color: COLORS.accent, marginBottom: 4 }}>
                  {selectedStrategy.title}
                </Text>
                <Text style={{ fontSize: 14, color: COLORS.textSoft, lineHeight: 20 }}>
                  {selectedStrategy.description}
                </Text>
              </View>
            )}

            {/* Buttons */}
            <View style={{ flexDirection: 'row', gap: 12 }}>
              <TouchableOpacity
                onPress={closeConfirmSheet}
                disabled={isChanging}
                style={{
                  flex: 1,
                  backgroundColor: COLORS.background,
                  borderRadius: 12,
                  paddingVertical: 14,
                  alignItems: 'center',
                  borderWidth: 1,
                  borderColor: COLORS.border,
                }}
              >
                <Text style={{ fontSize: 16, fontWeight: '600', color: COLORS.textSoft }}>
                  {t('common.cancel')}
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                onPress={handleConfirm}
                disabled={isChanging}
                style={{
                  flex: 1,
                  backgroundColor: COLORS.accent,
                  borderRadius: 12,
                  paddingVertical: 14,
                  alignItems: 'center',
                }}
              >
                {isChanging ? (
                  <ActivityIndicator color="#fff" size="small" />
                ) : (
                  <Text style={{ fontSize: 16, fontWeight: '600', color: '#fff' }}>
                    {t('common.confirm')}
                  </Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </Animated.View>
      </Modal>
    </>
  );
}
