/**
 * useDeviceSettings Hook
 * CustomModeScreen ve ayar ekranları için yüksek seviyeli hook
 * Yeni DeviceSettings API kullanır (deviceId bazlı)
 * SignalR ile gerçek zamanlı progress tracking içerir
 */

import { useCallback, useRef } from 'react';
import { useDeviceSettingsStore } from '../store/deviceSettingsStore';
import { useSelectedSiteDevice } from './useSelectedSiteDevice';
import { useRoles } from './useRoles';
import { useToastStore } from '../store/toastStore';
import { useDeviceSettingsUpdateStore } from '../store/deviceSettingsUpdateStore';
import { useDeviceSettingsHub } from './useDeviceSettingsHub';
import { useInverterModeStore } from '../store/inverterModeStore';
import {
  DeviceSettingsConfig,
  TimeSlot,
  TimeSlotVisibility,
  BatterySettingsConfig,
  SystemWorkModeSettings,
  TimeOfUseConfig,
  GeneratorSmartLoadConfig,
  MaintenanceSettingsConfig,
} from '../api/types/inverterModes.types';
import { ParameterUpdateCompletedDto } from '../api/types/deviceSettingsHub.types';

export interface UseDeviceSettingsReturn {
  // Data
  settings: DeviceSettingsConfig;
  batterySettings: BatterySettingsConfig;
  systemWorkMode: SystemWorkModeSettings;
  timeOfUse: TimeOfUseConfig;
  generatorSmartLoad: GeneratorSmartLoadConfig;
  maintenanceSettings?: MaintenanceSettingsConfig;

  // Status
  isLoading: boolean;
  isSaving: boolean;
  isDirty: boolean;
  error: string | null;

  // TimeOfUse validation
  timeOfUseValidationError: string | null;
  canSave: boolean;

  // Auth
  isAdmin: boolean;
  canEditMaintenanceSettings: boolean;

  // Actions
  fetchSettings: () => Promise<void>;
  saveSettings: () => Promise<boolean>;
  resetToOriginal: () => void;
  resetToDefaults: () => void;

  // Update functions
  updateSetting: <T>(path: string, value: T) => void;
  updateBatchSettings: (updates: Record<string, any>) => void;
  updateTimeSlot: (slotIndex: number, updates: Partial<TimeSlot>) => void;

  // Category-specific updates
  updateBatterySettings: (updates: Partial<BatterySettingsConfig>) => void;
  updateSystemWorkMode: (updates: Partial<SystemWorkModeSettings>) => void;
  updateTimeOfUse: (updates: Partial<TimeOfUseConfig>) => void;
  updateGeneratorSmartLoad: (updates: Partial<GeneratorSmartLoadConfig>) => void;

  // Utility
  clearError: () => void;
}

export const useDeviceSettings = (): UseDeviceSettingsReturn => {
  const { device } = useSelectedSiteDevice();
  const { isAdmin } = useRoles();
  const showToast = useToastStore((state) => state.show);

  // Get state values with selectors (these are stable)
  const settings = useDeviceSettingsStore((state) => state.settings);
  const isLoading = useDeviceSettingsStore((state) => state.isLoading);
  const isSaving = useDeviceSettingsStore((state) => state.isSaving);
  const isDirty = useDeviceSettingsStore((state) => state.isDirty);
  const error = useDeviceSettingsStore((state) => state.error);
  const timeOfUseValidationError = useDeviceSettingsStore((state) => state.timeOfUseValidationError);

  // Compute canSave
  const canSave = isDirty && !timeOfUseValidationError;

  // Get actions directly from store (stable references)
  const storeFetchSettings = useDeviceSettingsStore((state) => state.fetchSettings);
  const storeSaveSettings = useDeviceSettingsStore((state) => state.saveSettings);
  const storeFinalizeSave = useDeviceSettingsStore((state) => state.finalizeSave);
  const resetToOriginal = useDeviceSettingsStore((state) => state.resetToOriginal);
  const resetToDefaults = useDeviceSettingsStore((state) => state.resetToDefaults);
  const updateSetting = useDeviceSettingsStore((state) => state.updateSetting);
  const updateBatchSettings = useDeviceSettingsStore((state) => state.updateBatchSettings);
  const updateTimeSlot = useDeviceSettingsStore((state) => state.updateTimeSlot);
  const clearError = useDeviceSettingsStore((state) => state.clearError);

  // Update progress store actions
  const { startUpdate, isProgressModalVisible } = useDeviceSettingsUpdateStore();

  // Inverter mode store action
  const setCustomModeActive = useInverterModeStore((state) => state.setCustomModeActive);

  // Ref to track if we should show toast (when modal is hidden)
  const showToastOnCompleteRef = useRef(false);

  // Handle SignalR completion callback
  const handleSignalRComplete = useCallback(
    (result: ParameterUpdateCompletedDto) => {
      // Get failed parameter IDs for partial update
      const failedParameterIds = result.failedCount > 0
        ? useDeviceSettingsUpdateStore.getState().batchState?.failedParameters.map((p) => p.parameterId) ?? []
        : [];

      // Finalize the save in the store
      storeFinalizeSave(result.failedCount === 0, failedParameterIds);

      // Set custom mode as active after successful save
      if (result.failedCount === 0) {
        setCustomModeActive();
      }

      // Show toast if modal is hidden (background mode)
      if (showToastOnCompleteRef.current || !useDeviceSettingsUpdateStore.getState().isProgressModalVisible) {
        if (result.failedCount === 0) {
          showToast({
            type: 'success',
            message: 'Settings saved successfully',
          });
        } else {
          showToast({
            type: 'warning',
            message: `${result.successCount} settings saved, ${result.failedCount} failed`,
          });
        }
      }
      showToastOnCompleteRef.current = false;
    },
    [storeFinalizeSave, showToast, setCustomModeActive]
  );

  // SignalR hook for progress tracking
  const { startBatchTracking, stopBatchTracking } = useDeviceSettingsHub({
    onCompleted: handleSignalRComplete,
  });

  // Get deviceId from the device object
  const deviceId = device?.id;

  // NO AUTO-FETCH - Let the screen control when to fetch
  // This prevents infinite loops from useEffect dependencies

  // Fetch settings manually
  const fetchSettings = useCallback(async () => {
    if (deviceId) {
      await storeFetchSettings(deviceId);
    }
  }, [deviceId, storeFetchSettings]);

  // Save settings with SignalR progress tracking
  const saveSettings = useCallback(async (): Promise<boolean> => {
    const result = await storeSaveSettings();

    if (result.success && result.correlationId) {
      // Start progress tracking via SignalR
      try {
        startUpdate(result.correlationId, result.totalParameters);
        await startBatchTracking(result.correlationId);
        // Don't show success toast here - wait for SignalR completion
        return true;
      } catch (signalRError) {
        console.log('[useDeviceSettings] SignalR tracking failed:', signalRError);
        // If SignalR fails, still consider save successful (API call succeeded)
        // Set custom mode as active
        setCustomModeActive();
        // Just show immediate toast
        showToast({
          type: 'success',
          message: 'Settings saved successfully',
        });
        storeFinalizeSave(true);
        return true;
      }
    } else if (!result.success) {
      showToast({
        type: 'error',
        message: result.error || 'Failed to save settings',
      });
      return false;
    }

    // No changes to save
    return true;
  }, [storeSaveSettings, startUpdate, startBatchTracking, showToast, storeFinalizeSave]);

  // Category-specific update helpers
  const updateBatterySettings = useCallback(
    (updates: Partial<BatterySettingsConfig>) => {
      Object.entries(updates).forEach(([category, values]) => {
        if (typeof values === 'object' && values !== null) {
          Object.entries(values).forEach(([key, value]) => {
            updateSetting(`batterySettings.${category}.${key}`, value);
          });
        }
      });
    },
    [updateSetting]
  );

  const updateSystemWorkMode = useCallback(
    (updates: Partial<SystemWorkModeSettings>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`systemWorkMode.${key}`, value);
      });
    },
    [updateSetting]
  );

  const updateTimeOfUse = useCallback(
    (updates: Partial<TimeOfUseConfig>) => {
      if (updates.enabled !== undefined) {
        updateSetting('timeOfUse.enabled', updates.enabled);
      }
      if (updates.activeDays) {
        Object.entries(updates.activeDays).forEach(([day, value]) => {
          updateSetting(`timeOfUse.activeDays.${day}`, value);
        });
      }
      // timeSlots are handled by updateTimeSlot
    },
    [updateSetting]
  );

  const updateGeneratorSmartLoad = useCallback(
    (updates: Partial<GeneratorSmartLoadConfig>) => {
      Object.entries(updates).forEach(([category, values]) => {
        if (typeof values === 'object' && values !== null) {
          Object.entries(values).forEach(([key, value]) => {
            updateSetting(`generatorAndSmartLoad.${category}.${key}`, value);
          });
        }
      });
    },
    [updateSetting]
  );

  return {
    // Data
    settings,
    batterySettings: settings.batterySettings,
    systemWorkMode: settings.systemWorkMode,
    timeOfUse: settings.timeOfUse,
    generatorSmartLoad: settings.generatorAndSmartLoad,
    maintenanceSettings: settings.maintenanceSettings,

    // Status
    isLoading,
    isSaving,
    isDirty,
    error,

    // TimeOfUse validation
    timeOfUseValidationError,
    canSave,

    // Auth
    isAdmin,
    canEditMaintenanceSettings: isAdmin,

    // Actions
    fetchSettings,
    saveSettings,
    resetToOriginal,
    resetToDefaults,

    // Update functions
    updateSetting,
    updateBatchSettings,
    updateTimeSlot,

    // Category-specific updates
    updateBatterySettings,
    updateSystemWorkMode,
    updateTimeOfUse,
    updateGeneratorSmartLoad,

    // Utility
    clearError,
  };
};

// Hook for individual category (for section components)
export const useBatterySettingsSection = () => {
  const { batterySettings, updateSetting, isLoading, isDirty } = useDeviceSettings();

  const updateGridCharging = useCallback(
    (updates: Partial<BatterySettingsConfig['gridCharging']>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`batterySettings.gridCharging.${key}`, value);
      });
    },
    [updateSetting]
  );

  const updateGeneratorCharging = useCallback(
    (updates: Partial<BatterySettingsConfig['generatorCharging']>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`batterySettings.generatorCharging.${key}`, value);
      });
    },
    [updateSetting]
  );

  const updateProtectionThresholds = useCallback(
    (updates: Partial<BatterySettingsConfig['protectionThresholds']>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`batterySettings.protectionThresholds.${key}`, value);
      });
    },
    [updateSetting]
  );

  return {
    gridCharging: batterySettings.gridCharging,
    generatorCharging: batterySettings.generatorCharging,
    protectionThresholds: batterySettings.protectionThresholds,
    updateGridCharging,
    updateGeneratorCharging,
    updateProtectionThresholds,
    isLoading,
    isDirty,
  };
};

export const useSystemWorkModeSection = () => {
  const { systemWorkMode, updateSetting, isLoading, isDirty } = useDeviceSettings();

  const updateField = useCallback(
    <K extends keyof SystemWorkModeSettings>(key: K, value: SystemWorkModeSettings[K]) => {
      updateSetting(`systemWorkMode.${key}`, value);
    },
    [updateSetting]
  );

  return {
    ...systemWorkMode,
    updateField,
    isLoading,
    isDirty,
  };
};

export const useTimeOfUseSection = () => {
  const { timeOfUse, updateSetting, updateTimeSlot, isLoading, isDirty, timeOfUseValidationError } = useDeviceSettings();
  const timeSlotVisibility = useDeviceSettingsStore((state) => state.timeSlotVisibility);

  const toggleEnabled = useCallback(() => {
    updateSetting('timeOfUse.enabled', timeOfUse.enabled === 'on' ? 'off' : 'on');
  }, [timeOfUse.enabled, updateSetting]);

  const toggleDay = useCallback(
    (day: keyof typeof timeOfUse.activeDays) => {
      updateSetting(`timeOfUse.activeDays.${day}`, !timeOfUse.activeDays[day]);
    },
    [timeOfUse.activeDays, updateSetting]
  );

  return {
    enabled: timeOfUse.enabled,
    activeDays: timeOfUse.activeDays,
    timeSlots: timeOfUse.timeSlots,
    timeSlotVisibility,
    toggleEnabled,
    toggleDay,
    updateTimeSlot,
    isLoading,
    isDirty,
    timeOfUseValidationError,
  };
};

export const useGeneratorSmartLoadSection = () => {
  const { generatorSmartLoad, updateSetting, isLoading, isDirty } = useDeviceSettings();

  const updateGeneratorSettings = useCallback(
    (updates: Partial<GeneratorSmartLoadConfig['generatorSettings']>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`generatorAndSmartLoad.generatorSettings.${key}`, value);
      });
    },
    [updateSetting]
  );

  const updateGeneratorPeakShaving = useCallback(
    (updates: Partial<GeneratorSmartLoadConfig['generatorPeakShaving']>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`generatorAndSmartLoad.generatorPeakShaving.${key}`, value);
      });
    },
    [updateSetting]
  );

  const updateSmartLoad = useCallback(
    (updates: Partial<GeneratorSmartLoadConfig['smartLoad']>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`generatorAndSmartLoad.smartLoad.${key}`, value);
      });
    },
    [updateSetting]
  );

  return {
    generatorSettings: generatorSmartLoad.generatorSettings,
    generatorPeakShaving: generatorSmartLoad.generatorPeakShaving,
    smartLoad: generatorSmartLoad.smartLoad,
    updateGeneratorSettings,
    updateGeneratorPeakShaving,
    updateSmartLoad,
    isLoading,
    isDirty,
  };
};
