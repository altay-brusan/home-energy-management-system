/**
 * useDeviceSettings Hook
 * CustomModeScreen ve ayar ekranları için yüksek seviyeli hook
 * Yeni DeviceSettings API kullanır (deviceId bazlı)
 * Now uses synchronous REST API with SignalR for real-time progress
 */

import { useCallback, useEffect, useMemo } from 'react';
import { useDeviceSettingsStore, DeviceBusyInfo, DeviceRestartingInfo } from '../store/deviceSettingsStore';
import { useSelectedSiteDevice } from './useSelectedSiteDevice';
import { useRoles } from './useRoles';
import { useToastStore } from '../store/toastStore';
import { useDeviceSettingsUpdateStore } from '../store/deviceSettingsUpdateStore';
import { useInverterModeStore } from '../store/inverterModeStore';
import { useDeviceSettingsSignalR } from './useDeviceSettingsSignalR';
import { deviceSettingsSignalRService } from '../services/deviceSettingsSignalRService';
import { isGridConfigStorePath } from '../config/deviceSettingsMapping';
import {
  DeviceSettingsConfig,
  TimeSlot,
  TimeSlotVisibility,
  BatterySettingsConfig,
  SystemWorkModeSettings,
  GridStandardConfig,
  TimeOfUseConfig,
  GeneratorSmartLoadConfig,
  MaintenanceSettingsConfig,
} from '../api/types/inverterModes.types';

export interface UseDeviceSettingsReturn {
  // Data
  settings: DeviceSettingsConfig;
  batterySettings: BatterySettingsConfig;
  systemWorkMode: SystemWorkModeSettings;
  gridStandard: GridStandardConfig;
  timeOfUse: TimeOfUseConfig;
  generatorSmartLoad: GeneratorSmartLoadConfig;
  maintenanceSettings?: MaintenanceSettingsConfig;

  // Status
  isLoading: boolean;
  isSaving: boolean;
  isDirty: boolean;
  error: string | null;

  // Device busy state (when another update is in progress)
  deviceBusyInfo: DeviceBusyInfo | null;
  isDeviceBusy: boolean;

  // Device restarting state (after grid config change)
  deviceRestartingInfo: DeviceRestartingInfo | null;
  isDeviceRestarting: boolean;

  // Grid config
  hasGridConfigChanges: boolean;
  gridConfigChangesCount: number;
  revertGridConfigChanges: () => void;
  saveGridConfig: () => Promise<{
    success: boolean;
    deviceWillRestart?: boolean;
    restartGracePeriodSeconds?: number;
    estimatedAvailableAt?: string;
    error?: string;
    errorCode?: string;
  }>;

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
  const deviceBusyInfo = useDeviceSettingsStore((state) => state.deviceBusyInfo);
  const clearDeviceBusy = useDeviceSettingsStore((state) => state.clearDeviceBusy);
  const deviceRestartingInfo = useDeviceSettingsStore((state) => state.deviceRestartingInfo);
  const isDeviceRestartingFn = useDeviceSettingsStore((state) => state.isDeviceRestarting);
  const dirtyFields = useDeviceSettingsStore((state) => state.dirtyFields);
  const saveGridConfigFn = useDeviceSettingsStore((state) => state.saveGridConfig);
  const revertGridConfigChangesFn = useDeviceSettingsStore((state) => state.revertGridConfigChanges);

  // Compute isDeviceBusy - check if busy and not expired
  const isDeviceBusy = deviceBusyInfo?.isDeviceBusy &&
    deviceBusyInfo.busyUntil &&
    Date.now() < deviceBusyInfo.busyUntil;

  // Compute isDeviceRestarting
  const isDeviceRestarting = isDeviceRestartingFn();

  // Compute hasGridConfigChanges - reactive based on dirtyFields
  const hasGridConfigChanges = useMemo(() => {
    for (const storePath of dirtyFields) {
      if (isGridConfigStorePath(storePath)) {
        return true;
      }
    }
    return false;
  }, [dirtyFields]);

  // Compute gridConfigChangesCount - count of changed grid config parameters
  const gridConfigChangesCount = useMemo(() => {
    let count = 0;
    for (const storePath of dirtyFields) {
      if (isGridConfigStorePath(storePath)) {
        count++;
      }
    }
    return count;
  }, [dirtyFields]);

  // Compute canSave
  const canSave = isDirty && !timeOfUseValidationError;

  // Get actions directly from store (stable references)
  const storeFetchSettings = useDeviceSettingsStore((state) => state.fetchSettings);
  const storeSaveSettings = useDeviceSettingsStore((state) => state.saveSettings);
  const resetToOriginal = useDeviceSettingsStore((state) => state.resetToOriginal);
  const resetToDefaults = useDeviceSettingsStore((state) => state.resetToDefaults);
  const updateSetting = useDeviceSettingsStore((state) => state.updateSetting);
  const updateBatchSettings = useDeviceSettingsStore((state) => state.updateBatchSettings);
  const updateTimeSlot = useDeviceSettingsStore((state) => state.updateTimeSlot);
  const clearError = useDeviceSettingsStore((state) => state.clearError);

  // Snapshot/rollback actions
  const capturePreSaveSnapshot = useDeviceSettingsStore((state) => state.capturePreSaveSnapshot);
  const clearPreSaveSnapshot = useDeviceSettingsStore((state) => state.clearPreSaveSnapshot);

  // Update progress store actions
  const { startSaving, completeSaving } = useDeviceSettingsUpdateStore();

  // Inverter mode store action
  const setCustomModeActive = useInverterModeStore((state) => state.setCustomModeActive);

  // Get deviceId from the device object
  const deviceId = device?.id;

  // Initialize SignalR connection for real-time progress updates
  useDeviceSettingsSignalR({ deviceId, enabled: !!deviceId });

  // NO AUTO-FETCH - Let the screen control when to fetch
  // This prevents infinite loops from useEffect dependencies

  // Fetch settings manually
  const fetchSettings = useCallback(async () => {
    if (deviceId) {
      await storeFetchSettings(deviceId);
    }
  }, [deviceId, storeFetchSettings]);

  // Save settings - now synchronous with SignalR for real-time progress
  const saveSettings = useCallback(async (): Promise<boolean> => {
    // Ensure SignalR is connected for real-time progress updates
    // Wait up to 3 seconds for connection to be ready
    if (deviceId) {
      const isConnected = await deviceSettingsSignalRService.ensureConnected(deviceId, 3000);
      if (isConnected) {
        console.log('[useDeviceSettings] SignalR connected, proceeding with save');
      } else {
        console.log('[useDeviceSettings] SignalR not connected, save will proceed without real-time progress');
      }
    }

    // Capture pre-save snapshot for potential rollback on failure
    capturePreSaveSnapshot();

    // Start saving state
    startSaving();

    try {
      // Call API - now waits for completion
      // SignalR events will be received via useDeviceSettingsSignalR hook
      const result = await storeSaveSettings();

      if (result.success) {
        // Full success - clear snapshot as all changes are saved
        clearPreSaveSnapshot();
        setCustomModeActive();
        showToast({
          type: 'success',
          message: result.successCount === 1
            ? '1 parameter updated successfully'
            : `${result.successCount} parameters updated successfully`,
        });
        completeSaving({
          success: true,
          totalParameters: result.totalParameters,
          successCount: result.successCount,
          failedCount: result.failedCount,
          failedParameters: result.failedParameters,
          duration: result.duration,
        });
        return true;
      } else if (result.errorCode === 'DEVICE_BUSY') {
        // Device is busy - another update in progress
        showToast({
          type: 'warning',
          title: 'Device Busy',
          message: 'Another update is in progress. Please wait and try again.',
        });
        completeSaving({
          success: false,
          totalParameters: 0,
          successCount: 0,
          failedCount: 0,
          failedParameters: [],
          errorMessage: 'Device busy',
        });
        return false;
      } else if (result.errorCode === 'DEVICE_RESTARTING') {
        // Device is restarting from previous grid config change
        showToast({
          type: 'warning',
          title: 'Device Restarting',
          message: 'Device is restarting. Please wait a moment and try again.',
        });
        completeSaving({
          success: false,
          totalParameters: 0,
          successCount: 0,
          failedCount: 0,
          failedParameters: [],
          errorMessage: 'Device restarting',
        });
        return false;
      } else if (result.successCount > 0 && result.failedCount > 0) {
        // Partial success - some parameters updated, some failed
        setCustomModeActive();
        showToast({
          type: 'warning',
          message: `${result.successCount} updated, ${result.failedCount} failed`,
        });
        completeSaving({
          success: false,
          totalParameters: result.totalParameters,
          successCount: result.successCount,
          failedCount: result.failedCount,
          failedParameters: result.failedParameters,
          duration: result.duration,
        });
        return false;
      } else {
        // Complete failure
        showToast({
          type: 'error',
          message: result.error || result.message || 'Failed to save settings',
        });
        completeSaving({
          success: false,
          totalParameters: result.totalParameters || 0,
          successCount: 0,
          failedCount: result.failedCount || 0,
          failedParameters: result.failedParameters || [],
          errorMessage: result.error || result.message,
        });
        return false;
      }
    } catch (error) {
      // Network or unexpected error
      console.error('[useDeviceSettings] Save error:', error);
      showToast({
        type: 'error',
        message: 'Connection error. Please try again.',
      });
      completeSaving({
        success: false,
        totalParameters: 0,
        successCount: 0,
        failedCount: 0,
        failedParameters: [],
        errorMessage: 'Connection error',
      });
      return false;
    }
  }, [deviceId, storeSaveSettings, startSaving, completeSaving, showToast, setCustomModeActive, capturePreSaveSnapshot, clearPreSaveSnapshot]);

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
    batterySettings: settings?.batterySettings,
    systemWorkMode: settings?.systemWorkMode,
    gridStandard: settings?.gridStandard,
    timeOfUse: settings?.timeOfUse,
    generatorSmartLoad: settings?.generatorAndSmartLoad,
    maintenanceSettings: settings?.maintenanceSettings,

    // Status
    isLoading,
    isSaving,
    isDirty,
    error,

    // Device busy state
    deviceBusyInfo,
    isDeviceBusy: !!isDeviceBusy,

    // Device restarting state
    deviceRestartingInfo,
    isDeviceRestarting,

    // Grid config
    hasGridConfigChanges,
    gridConfigChangesCount,
    revertGridConfigChanges: revertGridConfigChangesFn,
    saveGridConfig: saveGridConfigFn,

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

  const updateBatteryConfiguration = useCallback(
    (updates: Partial<BatterySettingsConfig['batteryConfiguration']>) => {
      Object.entries(updates).forEach(([key, value]) => {
        updateSetting(`batterySettings.batteryConfiguration.${key}`, value);
      });
    },
    [updateSetting]
  );

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
    batteryConfiguration: batterySettings?.batteryConfiguration,
    gridCharging: batterySettings?.gridCharging,
    generatorCharging: batterySettings?.generatorCharging,
    protectionThresholds: batterySettings?.protectionThresholds,
    updateBatteryConfiguration,
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
    updateSetting('timeOfUse.enabled', timeOfUse?.enabled === 'on' ? 'off' : 'on');
  }, [timeOfUse?.enabled, updateSetting]);

  const toggleDay = useCallback(
    (day: keyof NonNullable<typeof timeOfUse>['activeDays']) => {
      updateSetting(`timeOfUse.activeDays.${day}`, !timeOfUse?.activeDays?.[day]);
    },
    [timeOfUse?.activeDays, updateSetting]
  );

  return {
    enabled: timeOfUse?.enabled,
    activeDays: timeOfUse?.activeDays,
    timeSlots: timeOfUse?.timeSlots,
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
    generatorSettings: generatorSmartLoad?.generatorSettings,
    generatorPeakShaving: generatorSmartLoad?.generatorPeakShaving,
    smartLoad: generatorSmartLoad?.smartLoad,
    updateGeneratorSettings,
    updateGeneratorPeakShaving,
    updateSmartLoad,
    isLoading,
    isDirty,
  };
};
