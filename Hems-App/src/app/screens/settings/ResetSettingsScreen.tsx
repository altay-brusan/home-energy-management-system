import React, { useState } from 'react';
import { View, Text, ScrollView, TouchableOpacity, Alert } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ArrowLeft, RefreshCw, RotateCcw, AlertTriangle } from 'lucide-react-native';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useI18n } from '../../../hooks/useI18n';
import { makeResetSettingsScreenStyles } from './ResetSettingsScreen.styles';
import { toast } from '../../../utils/toast';

export default function ResetSettingsScreen({ navigation }: any) {
  const mode = useThemeStore((s) => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = makeResetSettingsScreenStyles(COLORS, isDark);
  const insets = useSafeAreaInsets();
  const { t } = useI18n();

  const [isRestarting, setIsRestarting] = useState(false);
  const [isResetting, setIsResetting] = useState(false);

  const handleRestartDevice = () => {
    Alert.alert(
      t('settings.restartDevice'),
      t('settings.confirmRestart'),
      [
        {
          text: t('common.cancel'),
          style: 'cancel',
        },
        {
          text: t('common.confirm'),
          style: 'default',
          onPress: async () => {
            setIsRestarting(true);
            try {
              // TODO: Call API to restart device
              // await deviceSettingsService.restartDevice();
              toast.success(t('settings.restartSuccess'), t('common.success'));
            } catch (error) {
              toast.error(t('common.error'), t('common.error'));
            } finally {
              setIsRestarting(false);
            }
          },
        },
      ],
      { cancelable: true }
    );
  };

  const handleFactoryReset = () => {
    Alert.alert(
      t('settings.factoryReset'),
      t('settings.confirmFactoryReset'),
      [
        {
          text: t('common.cancel'),
          style: 'cancel',
        },
        {
          text: t('common.confirm'),
          style: 'destructive',
          onPress: async () => {
            setIsResetting(true);
            try {
              // TODO: Call API to factory reset device
              // await deviceSettingsService.factoryReset();
              toast.success(t('settings.factoryResetSuccess'), t('common.success'));
            } catch (error) {
              toast.error(t('common.error'), t('common.error'));
            } finally {
              setIsResetting(false);
            }
          },
        },
      ],
      { cancelable: true }
    );
  };

  return (
    <View style={[styles.container, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]}>{t('settings.resetOptions')}</Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[styles.scrollContent, { paddingBottom: insets.bottom + 24 }]}
        showsVerticalScrollIndicator={false}
      >
        {/* Restart Device Card */}
        <TouchableOpacity
          style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
          onPress={handleRestartDevice}
          disabled={isRestarting}
          activeOpacity={0.7}
        >
          <View style={[styles.iconContainer, { backgroundColor: `${COLORS.accent}15` }]}>
            <RefreshCw size={22} color={COLORS.accent} />
          </View>
          <View style={styles.cardContent}>
            <Text style={[styles.cardTitle, { color: COLORS.text }]}>
              {t('settings.restartDevice')}
            </Text>
            <Text style={[styles.cardSubtitle, { color: COLORS.textSoft }]}>
              {t('settings.restartDeviceDesc')}
            </Text>
          </View>
          {isRestarting && (
            <Text style={[styles.loadingText, { color: COLORS.accent }]}>...</Text>
          )}
        </TouchableOpacity>

        {/* Factory Reset Card */}
        <TouchableOpacity
          style={[styles.card, { backgroundColor: isDark ? COLORS.surface : COLORS.white }]}
          onPress={handleFactoryReset}
          disabled={isResetting}
          activeOpacity={0.7}
        >
          <View style={[styles.iconContainer, { backgroundColor: `${COLORS.error}15` }]}>
            <RotateCcw size={22} color={COLORS.error} />
          </View>
          <View style={styles.cardContent}>
            <Text style={[styles.cardTitle, { color: COLORS.text }]}>
              {t('settings.factoryReset')}
            </Text>
            <Text style={[styles.cardSubtitle, { color: COLORS.textSoft }]}>
              {t('settings.factoryResetDesc')}
            </Text>
          </View>
          {isResetting && (
            <Text style={[styles.loadingText, { color: COLORS.error }]}>...</Text>
          )}
        </TouchableOpacity>

        {/* Warning Banner */}
        <View style={[styles.warningBanner, { backgroundColor: `${COLORS.warning}15` }]}>
          <AlertTriangle size={20} color={COLORS.warning} style={styles.warningIcon} />
          <Text style={[styles.warningText, { color: COLORS.warning }]}>
            {t('settings.factoryResetDesc')}
          </Text>
        </View>
      </ScrollView>
    </View>
  );
}
