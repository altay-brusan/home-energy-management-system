/**
 * ModeDetailScreen
 * API-driven mode detail view and application
 */

import React from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, CheckCircle } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useInverterModeStore } from '../../../store/inverterModeStore';
import { useSitesStore } from '../../../store/sitesStore';

const ModeDetailScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const themeMode = useThemeStore((s) => s.mode);
  const isDark = themeMode === 'dark';
  const COLORS = getThemeColors(isDark);

  const {
    modes,
    activeModeId,
    selectAndApplyMode,
    isApplyingMode,
    applyError,
    clearApplyError,
  } = useInverterModeStore();

  const { selectedSiteDevice } = useSitesStore();
  const deviceId = selectedSiteDevice?.device?.id;

  const modeId = route.params?.modeId;
  const mode = modes.find((m) => m.id === modeId);

  if (!mode) {
    return (
      <View
        style={[
          styles.container,
          styles.centerContent,
          { backgroundColor: isDark ? COLORS.dark : COLORS.gray100, paddingTop: insets.top },
        ]}
      >
        <Text style={{ color: COLORS.text }}>{t('inverterModes.modeNotFound')}</Text>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={[styles.retryButton, { backgroundColor: COLORS.accent, marginTop: 16 }]}
        >
          <Text style={styles.retryText}>{t('common.goBack')}</Text>
        </TouchableOpacity>
      </View>
    );
  }

  const isActive = activeModeId === modeId;

  const handleApplyMode = async () => {
    if (!deviceId) {
      Alert.alert(
        t('common.error'),
        t('inverterModes.noDeviceSelected', { defaultValue: 'No device selected. Please select a site with a device first.' })
      );
      return;
    }

    Alert.alert(
      t('inverterModes.applyModeTitle'),
      t('inverterModes.applyModeConfirm', { mode: mode.name }),
      [
        { text: t('inverterModes.cancel'), style: 'cancel' },
        {
          text: t('inverterModes.apply'),
          onPress: async () => {
            const success = await selectAndApplyMode(
              mode,
              deviceId,
              () => navigation.navigate('CustomMode')
            );

            if (success) {
              Alert.alert(
                t('inverterModes.success'),
                t('inverterModes.modeAppliedSuccess', { mode: mode.name }),
                [{ text: t('inverterModes.ok'), onPress: () => navigation.goBack() }]
              );
            } else {
              Alert.alert(
                t('common.error'),
                applyError || t('inverterModes.applyFailed', { defaultValue: 'Failed to apply mode. Please try again.' }),
                [{ text: t('inverterModes.ok'), onPress: clearApplyError }]
              );
            }
          },
        },
      ]
    );
  };

  return (
    <View
      style={[
        styles.container,
        { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 },
      ]}
    >
      {/* Header */}
      <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={styles.backButton}
        >
          <ArrowLeft size={24} color={COLORS.text} />
        </TouchableOpacity>
        <Text style={[styles.title, { color: COLORS.text }]} numberOfLines={1}>
          {mode.name}
        </Text>
        <View style={{ width: 40 }} />
      </View>

      <ScrollView
        contentContainerStyle={[
          styles.scrollContent,
          { paddingBottom: insets.bottom + 100 },
        ]}
        showsVerticalScrollIndicator={false}
      >
        {/* Mode Badge and Status */}
        <View style={styles.badgeRow}>
          <View
            style={[
              styles.badge,
              { backgroundColor: COLORS.accent },
            ]}
          >
            <Text style={styles.badgeText}>
              {`${t('inverterModes.mode')} ${mode.mode}`}
            </Text>
          </View>
          {isActive && (
            <View style={[styles.activeBadge, { backgroundColor: '#28A745' }]}>
              <CheckCircle size={14} color="#fff" />
              <Text style={styles.activeBadgeText}>{t('inverterModes.active')}</Text>
            </View>
          )}
        </View>

        {/* Description from API */}
        <Text style={[styles.description, { color: COLORS.text }]}>
          {mode.description}
        </Text>

        {/* Info Box */}
        <View
          style={[
            styles.infoBox,
            { backgroundColor: isDark ? `${COLORS.accent}15` : '#E3F2FD' },
          ]}
        >
          <Text style={[styles.infoText, { color: COLORS.textSoft }]}>
            {t('inverterModes.modeInfoText', {
              defaultValue: 'This mode will configure your inverter settings automatically for optimal performance based on the selected profile.'
            })}
          </Text>
        </View>
      </ScrollView>

      {/* Apply Button */}
      <View
        style={[
          styles.footer,
          {
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
            paddingBottom: insets.bottom + 16,
          },
        ]}
      >
        <TouchableOpacity
          onPress={handleApplyMode}
          disabled={isActive || isApplyingMode}
          style={[
            styles.applyButton,
            {
              backgroundColor: isActive || isApplyingMode ? COLORS.border : COLORS.accent,
            },
          ]}
        >
          {isApplyingMode ? (
            <ActivityIndicator size="small" color="#fff" />
          ) : isActive ? (
            <>
              <CheckCircle size={20} color={COLORS.textSoft} />
              <Text style={[styles.applyButtonText, { color: COLORS.textSoft }]}>
                {t('inverterModes.thisModeActive')}
              </Text>
            </>
          ) : (
            <Text style={styles.applyButtonText}>{t('inverterModes.applyThisMode')}</Text>
          )}
        </TouchableOpacity>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  centerContent: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  backButton: {
    padding: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    flex: 1,
    textAlign: 'center',
    marginHorizontal: 8,
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  badgeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 12,
  },
  badge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
  },
  badgeText: {
    color: '#fff',
    fontSize: 13,
    fontWeight: '700',
  },
  activeBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 16,
    gap: 4,
  },
  activeBadgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
  description: {
    fontSize: 15,
    lineHeight: 22,
    marginBottom: 24,
  },
  infoBox: {
    borderRadius: 12,
    padding: 16,
  },
  infoText: {
    fontSize: 13,
    lineHeight: 18,
  },
  retryButton: {
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderRadius: 8,
  },
  retryText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  footer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    paddingHorizontal: 16,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: 'rgba(0,0,0,0.1)',
  },
  applyButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 16,
    borderRadius: 12,
    gap: 8,
  },
  applyButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});

export default ModeDetailScreen;
