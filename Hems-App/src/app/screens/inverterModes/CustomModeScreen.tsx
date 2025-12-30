/**
 * CustomModeScreen
 * API yapısına uyumlu cihaz ayarları ekranı
 * Toplu kaydetme (Save butonu) ile çalışır
 * SignalR ile gerçek zamanlı progress tracking içerir
 */

import React, { useEffect, useState, useRef, useCallback } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Save, RotateCcw, Clock, AlertCircle, RefreshCw } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { GestureHandlerRootView } from 'react-native-gesture-handler';

import { useThemeStore } from '../../../store/themeStore';
import { getThemeColors } from '../../../theme/constants';
import { useDeviceSettings } from '../../../hooks/useDeviceSettings';
import { useSelectedSiteDevice } from '../../../hooks/useSelectedSiteDevice';
import { useDeviceSettingsUpdateStore } from '../../../store/deviceSettingsUpdateStore';
import { UpdateProgressModal } from '../../../components/deviceSettings';

// Section Components
import {
  BatterySettingsSection,
  SystemWorkModeSection,
  GeneratorSmartLoadSection,
} from '../../../components/inverterModes/sections';

const CustomModeScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const themeMode = useThemeStore((s) => s.mode);
  const isDark = themeMode === 'dark';
  const COLORS = getThemeColors(isDark);
  const { device } = useSelectedSiteDevice();

  const {
    settings,
    isLoading,
    isSaving,
    isDirty,
    error,
    isAdmin,
    saveSettings,
    resetToOriginal,
    resetToDefaults,
    fetchSettings,
    clearError,
    canSave,
    timeOfUseValidationError,
  } = useDeviceSettings();

  // Update progress store
  const {
    isProgressModalVisible,
    isUpdateActive,
    hideProgressModal,
    confirmNavigation,
    reset: resetUpdateStore,
  } = useDeviceSettingsUpdateStore();

  const [scrollEnabled, setScrollEnabled] = useState(true);
  const shownErrorRef = useRef<string | null>(null);
  const [loadingTimeout, setLoadingTimeout] = useState(false);
  const hasFetchedRef = useRef(false);

  // Fetch settings ONCE when screen is focused
  useFocusEffect(
    useCallback(() => {
      if (device?.id && !hasFetchedRef.current) {
        console.log('[CustomModeScreen] Fetching settings for device:', device.id);
        hasFetchedRef.current = true;
        fetchSettings();
      }

      // Reset on unfocus so it fetches again next time
      return () => {
        // Don't reset here - keep the data cached
      };
    }, [device?.id, fetchSettings])
  );

  // Loading timeout - show error if loading takes too long
  useEffect(() => {
    let timeoutId: ReturnType<typeof setTimeout> | undefined;
    if (isLoading) {
      setLoadingTimeout(false);
      timeoutId = setTimeout(() => {
        setLoadingTimeout(true);
      }, 15000); // 15 seconds timeout
    }
    return () => {
      if (timeoutId) clearTimeout(timeoutId);
    };
  }, [isLoading]);

  // Show error alert only once per unique error
  useEffect(() => {
    if (error && error !== shownErrorRef.current) {
      shownErrorRef.current = error;
      Alert.alert(
        t('common.error', 'Error'),
        error,
        [{
          text: t('common.ok', 'OK'),
          onPress: () => {
            clearError();
            shownErrorRef.current = null;
          }
        }]
      );
    } else if (!error) {
      shownErrorRef.current = null;
    }
  }, [error, clearError, t]);

  // Handle retry
  const handleRetry = useCallback(() => {
    clearError();
    shownErrorRef.current = null;
    hasFetchedRef.current = false; // Allow fetch again
    fetchSettings();
  }, [clearError, fetchSettings]);

  const handleSave = async () => {
    const success = await saveSettings();
    if (success) {
      // Optionally navigate back or show success
    }
  };

  const handleReset = () => {
    Alert.alert(
      t('inverterModes.resetSettings', 'Reset Settings'),
      t('inverterModes.resetConfirm', 'Are you sure you want to reset all settings to their original values?'),
      [
        { text: t('common.cancel', 'Cancel'), style: 'cancel' },
        {
          text: t('inverterModes.reset', 'Reset'),
          style: 'destructive',
          onPress: resetToOriginal,
        },
      ]
    );
  };

  const handleBack = () => {
    // Check if update is in progress
    if (isUpdateActive) {
      Alert.alert(
        t('deviceSettings.update.inProgressTitle', 'Update In Progress'),
        t('deviceSettings.update.navigateWarning', 'Settings are being updated. Continue in background?'),
        [
          { text: t('common.cancel', 'Cancel'), style: 'cancel' },
          {
            text: t('deviceSettings.update.continueInBackground', 'Continue in Background'),
            onPress: () => {
              confirmNavigation();
              navigation.goBack();
            },
          },
        ]
      );
      return;
    }

    // Check for unsaved changes
    if (isDirty) {
      Alert.alert(
        t('inverterModes.unsavedChanges', 'Unsaved Changes'),
        t('inverterModes.unsavedChangesMessage', 'You have unsaved changes. Do you want to save before leaving?'),
        [
          {
            text: t('common.discard', 'Discard'),
            style: 'destructive',
            onPress: () => navigation.goBack(),
          },
          {
            text: t('common.save', 'Save'),
            onPress: async () => {
              const success = await saveSettings();
              if (success) {
                // Don't navigate - wait for modal to complete
              }
            },
          },
          { text: t('common.cancel', 'Cancel'), style: 'cancel' },
        ]
      );
    } else {
      navigation.goBack();
    }
  };

  // Handle continue in background from modal
  const handleContinueInBackground = () => {
    confirmNavigation();
  };

  // Handle modal close
  const handleModalClose = () => {
    if (!isUpdateActive) {
      hideProgressModal();
      resetUpdateStore();
    }
  };

  const navigateToTimeOfUse = () => {
    navigation.navigate('TimeOfUse');
  };

  const navigateToMaintenanceSettings = () => {
    navigation.navigate('MaintenanceSettings');
  };

  // No device state
  if (!device) {
    return (
      <View style={[styles.loadingContainer, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
        <AlertCircle size={48} color={COLORS.warning} />
        <Text style={[styles.loadingText, { color: COLORS.text, marginTop: 16 }]}>
          {t('inverterModes.noDeviceSelected', 'No device selected')}
        </Text>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={[styles.retryButton, { backgroundColor: COLORS.accent, marginTop: 16 }]}
        >
          <Text style={styles.retryButtonText}>{t('common.goBack', 'Go Back')}</Text>
        </TouchableOpacity>
      </View>
    );
  }

  // Loading state
  if (isLoading) {
    return (
      <View style={[styles.loadingContainer, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
        <ActivityIndicator size="large" color={COLORS.accent} />
        <Text style={[styles.loadingText, { color: COLORS.textSoft }]}>
          {t('common.loading', 'Loading...')}
        </Text>
        <TouchableOpacity
          onPress={() => navigation.goBack()}
          style={[styles.retryButton, { backgroundColor: COLORS.gray200, marginTop: 24 }]}
        >
          <Text style={[styles.retryButtonText, { color: COLORS.text }]}>{t('common.goBack', 'Go Back')}</Text>
        </TouchableOpacity>
      </View>
    );
  }

  // Error state with retry option
  if (error && !isLoading) {
    return (
      <View style={[styles.loadingContainer, { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 }]}>
        <AlertCircle size={48} color={COLORS.error || '#EF4444'} />
        <Text style={[styles.loadingText, { color: COLORS.text, marginTop: 16 }]}>
          {t('inverterModes.loadError', 'Failed to load settings')}
        </Text>
        <Text style={[styles.errorDetail, { color: COLORS.textSoft }]}>
          {error}
        </Text>
        <View style={styles.errorButtons}>
          <TouchableOpacity
            onPress={handleRetry}
            style={[styles.retryButton, { backgroundColor: COLORS.accent }]}
          >
            <RefreshCw size={16} color="#fff" />
            <Text style={styles.retryButtonText}>{t('common.retry', 'Retry')}</Text>
          </TouchableOpacity>
          <TouchableOpacity
            onPress={() => navigation.goBack()}
            style={[styles.retryButton, { backgroundColor: isDark ? COLORS.border : COLORS.gray200 }]}
          >
            <Text style={styles.retryButtonText}>{t('common.goBack', 'Go Back')}</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <View
        style={[
          styles.container,
          { backgroundColor: isDark ? COLORS.dark : COLORS.gray100 },
        ]}
      >
        {/* Header */}
        <View style={[styles.header, { paddingTop: insets.top + 12 }]}>
          <TouchableOpacity onPress={handleBack} style={styles.backButton}>
            <ArrowLeft size={24} color={COLORS.text} />
          </TouchableOpacity>
          <View style={styles.titleContainer}>
            <Text style={[styles.title, { color: COLORS.text }]}>
              {t('inverterModes.customModeSettings', 'Device Settings')}
            </Text>
            {isDirty && (
              <View style={[styles.dirtyBadge, { backgroundColor: COLORS.warning }]}>
                <Text style={styles.dirtyBadgeText}>
                  {t('common.unsaved', 'Unsaved')}
                </Text>
              </View>
            )}
          </View>
          <TouchableOpacity onPress={handleReset} style={styles.resetButton}>
            <RotateCcw size={20} color={COLORS.textSoft} />
          </TouchableOpacity>
        </View>

        <ScrollView
          contentContainerStyle={[
            styles.scrollContent,
            { paddingBottom: insets.bottom + 100 },
          ]}
          showsVerticalScrollIndicator={false}
          scrollEnabled={scrollEnabled}
        >
          {/* sortOrder: 1 - Battery Settings */}
          <BatterySettingsSection defaultExpanded />

          {/* sortOrder: 2 - System Work Mode */}
          <SystemWorkModeSection defaultExpanded />

          {/* sortOrder: 3 - Time of Use Button */}
          <TouchableOpacity
            onPress={navigateToTimeOfUse}
            style={[
              styles.navigationButton,
              {
                backgroundColor: isDark ? COLORS.surface : COLORS.white,
                borderColor: timeOfUseValidationError ? '#F44336' : 'transparent',
                borderWidth: timeOfUseValidationError ? 1 : 0,
              },
            ]}
          >
            <Clock size={20} color={timeOfUseValidationError ? '#F44336' : COLORS.accent} />
            <View style={styles.navigationButtonContent}>
              <Text style={[styles.navigationButtonText, { color: COLORS.text }]}>
                {t('inverterModes.timeOfUse', 'Time of Use')}
              </Text>
              <Text style={[styles.navigationButtonSubtext, { color: COLORS.textSoft }]}>
                {settings.timeOfUse.enabled === 'on'
                  ? t('inverterModes.enabled', 'Enabled')
                  : t('inverterModes.disabled', 'Disabled')}
              </Text>
            </View>
            <Text style={[styles.periodCount, { color: COLORS.textSoft }]}>
              {t('inverterModes.periodCount', '{{count}} periods', { count: 6 })}
            </Text>
          </TouchableOpacity>

          {/* Time of Use Validation Error */}
          {timeOfUseValidationError && (
            <View
              style={[
                styles.validationError,
                { backgroundColor: isDark ? '#FF57571A' : '#FFEBEE' },
              ]}
            >
              <AlertCircle size={16} color="#F44336" />
              <Text style={styles.validationErrorText}>
                {t(`timeOfUse.validation.${timeOfUseValidationError}`)}
              </Text>
            </View>
          )}

          {/* sortOrder: 4 - Generator & Smart Load */}
          <GeneratorSmartLoadSection />

          {/* sortOrder: 5 - Maintenance Settings (Admin Only) */}
          {isAdmin && (
            <TouchableOpacity
              onPress={navigateToMaintenanceSettings}
              style={[
                styles.navigationButton,
                styles.adminButton,
                { backgroundColor: isDark ? COLORS.surface : COLORS.white },
              ]}
            >
              <AlertCircle size={20} color={COLORS.warning} />
              <View style={styles.navigationButtonContent}>
                <Text style={[styles.navigationButtonText, { color: COLORS.text }]}>
                  {t('inverterModes.maintenanceSettings', 'Maintenance Settings')}
                </Text>
                <Text style={[styles.navigationButtonSubtext, { color: COLORS.warning }]}>
                  {t('inverterModes.adminOnly', 'Admin Only')}
                </Text>
              </View>
            </TouchableOpacity>
          )}
        </ScrollView>

        {/* Save Button */}
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
            onPress={handleSave}
            disabled={!canSave || isSaving}
            style={[
              styles.saveButton,
              {
                backgroundColor: canSave ? COLORS.accent : COLORS.gray200,
                opacity: isSaving ? 0.7 : 1,
              },
            ]}
          >
            {isSaving ? (
              <ActivityIndicator size="small" color="#fff" />
            ) : (
              <Save size={20} color="#fff" />
            )}
            <Text style={styles.saveButtonText}>
              {isSaving
                ? t('common.saving', 'Saving...')
                : t('inverterModes.saveAndApply', 'Save & Apply')}
            </Text>
          </TouchableOpacity>
        </View>

        {/* Update Progress Modal */}
        <UpdateProgressModal
          visible={isProgressModalVisible}
          onClose={handleModalClose}
          onContinueInBackground={handleContinueInBackground}
        />
      </View>
    </GestureHandlerRootView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 12,
    fontSize: 14,
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
  resetButton: {
    padding: 8,
  },
  titleContainer: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    textAlign: 'center',
  },
  dirtyBadge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 10,
  },
  dirtyBadgeText: {
    color: '#fff',
    fontSize: 10,
    fontWeight: '600',
  },
  scrollContent: {
    paddingHorizontal: 16,
  },
  navigationButton: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    gap: 12,
  },
  adminButton: {
    borderWidth: 1,
    borderColor: 'rgba(255, 193, 7, 0.3)',
  },
  navigationButtonContent: {
    flex: 1,
  },
  navigationButtonText: {
    fontSize: 14,
    fontWeight: '600',
  },
  navigationButtonSubtext: {
    fontSize: 12,
    marginTop: 2,
  },
  periodCount: {
    fontSize: 12,
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
  saveButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 16,
    borderRadius: 12,
    gap: 8,
  },
  saveButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  errorDetail: {
    fontSize: 13,
    marginTop: 8,
    textAlign: 'center',
    paddingHorizontal: 32,
  },
  errorButtons: {
    flexDirection: 'row',
    gap: 12,
    marginTop: 20,
  },
  retryButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 20,
    borderRadius: 8,
    gap: 8,
  },
  retryButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  debugText: {
    fontSize: 11,
    marginTop: 8,
    fontFamily: 'monospace',
  },
  validationError: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 8,
    marginBottom: 12,
    marginTop: -4,
  },
  validationErrorText: {
    flex: 1,
    fontSize: 13,
    color: '#F44336',
    fontWeight: '500',
  },
});

export default CustomModeScreen;
