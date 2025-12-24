/**
 * Device Settings Store
 * API ayarlarını cache'ler, local state yönetir ve bulk save sağlar
 * Yeni DeviceSettings API kullanır (deviceId bazlı)
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  DeviceSettingsConfig,
  DEFAULT_DEVICE_SETTINGS,
  TimeSlot,
} from '../api/types/inverterModes.types';
import {
  DeviceSettingsTreeVm,
  ParameterGroupTreeDto,
  ParameterTreeDto,
  UpdateParameterItem,
} from '../api/types/deviceSettings.types';
import { UpdateParametersResponse } from '../api/types/deviceSettingsHub.types';
import { deviceSettingsService } from '../api/services/deviceSettingsService';
import {
  findMappingByApiName,
  ALL_MAPPINGS,
  ParameterMapping,
} from '../config/deviceSettingsMapping';
import { transformerRegistry } from '../utils/deviceSettingsTransformers';

// Parameter metadata (API'den gelen ID'ler ve değerler)
export interface ParameterMetadata {
  parameterId: string;
  valueId?: string; // deviceParameterValue.id (info için)
  apiName: string;
  originalValue: string;
  currentValue: string;
  modelNumber?: string;
  parameterGroupId?: string;
}

// Store State
interface DeviceSettingsState {
  // API tree cache
  apiTree: DeviceSettingsTreeVm | null;
  deviceId: string | null;

  // Local settings state (app format)
  settings: DeviceSettingsConfig;

  // Parameter metadata (ID'ler ve original değerler)
  parameterMetadata: Record<string, ParameterMetadata>;

  // Status
  isLoading: boolean;
  isSaving: boolean;
  error: string | null;
  lastFetchedAt: number | null;

  // Dirty tracking
  isDirty: boolean;
  dirtyFields: Set<string>;

  // Actions
  fetchSettings: (deviceId: string) => Promise<void>;
  saveSettings: () => Promise<{ success: boolean; correlationId?: string; totalParameters?: number; error?: string }>;
  finalizeSave: (success: boolean, failedParameterIds?: string[]) => void;
  resetToOriginal: () => void;
  resetToDefaults: () => void;

  // Update actions (nested paths)
  updateSetting: <T>(path: string, value: T) => void;
  updateBatchSettings: (updates: Record<string, any>) => void;

  // TimeSlot specific
  updateTimeSlot: (slotIndex: number, updates: Partial<TimeSlot>) => void;

  // Utility
  clearError: () => void;
  setDeviceId: (id: string | null) => void;
}

// Helper: Get nested value from object by dot path
const getNestedValue = (obj: any, path: string): any => {
  return path.split('.').reduce((current, key) => current?.[key], obj);
};

// Helper: Set nested value in object by dot path
const setNestedValue = <T>(obj: T, path: string, value: any): T => {
  const result = JSON.parse(JSON.stringify(obj));
  const keys = path.split('.');
  let current = result;
  for (let i = 0; i < keys.length - 1; i++) {
    if (current[keys[i]] === undefined) {
      current[keys[i]] = {};
    }
    current = current[keys[i]];
  }
  current[keys[keys.length - 1]] = value;
  return result;
};

// Helper: Transform API tree to app settings
const transformApiTreeToSettings = (
  tree: DeviceSettingsTreeVm
): { settings: DeviceSettingsConfig; metadata: Record<string, ParameterMetadata> } => {
  const settings: DeviceSettingsConfig = JSON.parse(JSON.stringify(DEFAULT_DEVICE_SETTINGS));
  const metadata: Record<string, ParameterMetadata> = {};

  const processParameter = (param: ParameterTreeDto, groupId?: string) => {
    const mapping = findMappingByApiName(param.name);
    if (!mapping) {
      if (__DEV__ && param.name.includes('day') || param.name.includes('Monday') || param.name.includes('Tuesday')) {
        console.warn('[DeviceSettings] No mapping found for parameter:', param.name);
      }
      return;
    }

    const apiValue = param.setupDeviceParameterValue?.value ?? param.defaultValue;
    const transformer = transformerRegistry.toApp[mapping.transformerType as keyof typeof transformerRegistry.toApp];
    const appValue = transformer ? transformer(apiValue) : apiValue;

    // Debug log for ActiveDays
    if (__DEV__ && mapping.storePath.includes('activeDays')) {
      console.log('[DeviceSettings] Processing ActiveDay:', {
        apiName: param.name,
        storePath: mapping.storePath,
        apiValue,
        appValue,
        parameterId: param.id,
      });
    }

    // Set value in settings
    const newSettings = setNestedValue(settings, mapping.storePath, appValue);
    Object.assign(settings, newSettings);

    // Store metadata
    metadata[mapping.storePath] = {
      parameterId: param.id,
      valueId: param.setupDeviceParameterValue?.id,
      apiName: param.name,
      originalValue: apiValue,
      currentValue: apiValue,
      modelNumber: param.setupDeviceParameterValue?.modelNumber,
      parameterGroupId: groupId,
    };
  };

  const processGroup = (group: ParameterGroupTreeDto) => {
    group.parameters.forEach((param) => processParameter(param, group.id));
    group.children.forEach(processGroup);
  };

  tree.parameterGroups.forEach(processGroup);

  return { settings, metadata };
};

// Helper: Get changed parameters for saving
const getChangedParameters = (
  settings: DeviceSettingsConfig,
  metadata: Record<string, ParameterMetadata>,
  dirtyFields: Set<string>
): Array<{
  mapping: ParameterMapping;
  meta: ParameterMetadata;
  newValue: string;
}> => {
  const changes: Array<{
    mapping: ParameterMapping;
    meta: ParameterMetadata;
    newValue: string;
  }> = [];

  dirtyFields.forEach((storePath) => {
    const meta = metadata[storePath];
    const mapping = ALL_MAPPINGS.find((m) => m.storePath === storePath);

    // Debug logging
    if (__DEV__) {
      console.log('[DeviceSettings] Processing dirty field:', {
        storePath,
        hasMeta: !!meta,
        hasMapping: !!mapping,
        metaParameterId: meta?.parameterId,
        mappingApiName: mapping?.apiName,
      });
    }

    if (meta && mapping) {
      const appValue = getNestedValue(settings, storePath);
      const transformerKey = mapping.transformerType as keyof typeof transformerRegistry.toAPI;
      const transformer = transformerRegistry.toAPI[transformerKey] as ((v: any) => string) | undefined;
      const apiValue = transformer ? transformer(appValue) : String(appValue);

      if (__DEV__) {
        console.log('[DeviceSettings] Value comparison:', {
          storePath,
          appValue,
          apiValue,
          originalValue: meta.originalValue,
          changed: apiValue !== meta.originalValue,
        });
      }

      if (apiValue !== meta.originalValue) {
        changes.push({
          mapping,
          meta,
          newValue: apiValue,
        });
      }
    } else if (__DEV__) {
      console.warn('[DeviceSettings] ⚠️ Missing meta or mapping for:', storePath);
    }
  });

  return changes;
};

// Create Store
export const useDeviceSettingsStore = create<DeviceSettingsState>()(
  persist(
    (set, get) => ({
      // Initial State
      apiTree: null,
      deviceId: null,
      settings: JSON.parse(JSON.stringify(DEFAULT_DEVICE_SETTINGS)),
      parameterMetadata: {},
      isLoading: false,
      isSaving: false,
      error: null,
      lastFetchedAt: null,
      isDirty: false,
      dirtyFields: new Set<string>(),

      // Fetch settings from API
      fetchSettings: async (deviceId: string) => {
        // Prevent duplicate fetches
        const { isLoading } = get();
        if (isLoading) {
          console.log('[DeviceSettings] Already loading, skipping fetch');
          return;
        }

        set({ isLoading: true, error: null, deviceId });

        try {
          console.log('[DeviceSettings] Fetching tree for deviceId:', deviceId);
          const tree = await deviceSettingsService.getTree(deviceId);
          const { settings, metadata } = transformApiTreeToSettings(tree);

          set({
            apiTree: tree,
            settings,
            parameterMetadata: metadata,
            isLoading: false,
            lastFetchedAt: Date.now(),
            isDirty: false,
            dirtyFields: new Set<string>(),
          });
          console.log('[DeviceSettings] Fetch completed successfully');
        } catch (err: any) {
          console.error('[DeviceSettings] Fetch error:', err);
          set({
            isLoading: false,
            error: err.message || 'Failed to fetch device settings',
          });
        }
      },

      // Save changed settings to API (bulk update)
      // Returns correlationId for SignalR progress tracking
      saveSettings: async () => {
        const { settings, parameterMetadata, dirtyFields, deviceId } = get();

        if (!deviceId || dirtyFields.size === 0) {
          return { success: true };
        }

        set({ isSaving: true, error: null });

        try {
          const changes = getChangedParameters(settings, parameterMetadata, dirtyFields);

          if (changes.length === 0) {
            set({ isSaving: false });
            return { success: true };
          }

          // Build bulk update payload
          const parameters: UpdateParameterItem[] = changes.map((change) => ({
            parameterId: change.meta.parameterId,
            value: change.newValue,
          }));

          // Send bulk update - now returns correlationId for SignalR tracking
          const response = await deviceSettingsService.updateParameters(deviceId, { parameters });

          if (response.success && response.correlationId) {
            // Don't update metadata yet - wait for SignalR completion
            // Keep isSaving true until finalizeSave is called
            return {
              success: true,
              correlationId: response.correlationId,
              totalParameters: response.totalParameters,
            };
          } else {
            set({ isSaving: false, error: response.errorMessage || response.message });
            return { success: false, error: response.errorMessage || response.message };
          }
        } catch (err: any) {
          set({
            isSaving: false,
            error: err.message || 'Failed to save settings',
          });
          return { success: false, error: err.message };
        }
      },

      // Finalize save after SignalR completion
      // Called when ParameterUpdateCompleted event is received
      finalizeSave: (success: boolean, failedParameterIds?: string[]) => {
        const { settings, parameterMetadata, dirtyFields } = get();

        if (success) {
          const changes = getChangedParameters(settings, parameterMetadata, dirtyFields);

          // Update metadata with new original values (excluding failed)
          const updatedMeta = { ...parameterMetadata };
          const remainingDirty = new Set(dirtyFields);

          changes.forEach((change) => {
            const isFailed = failedParameterIds?.includes(change.meta.parameterId);
            if (!isFailed) {
              // Update metadata for successful parameters
              updatedMeta[change.mapping.storePath] = {
                ...change.meta,
                originalValue: change.newValue,
                currentValue: change.newValue,
              };
              // Remove from dirty fields
              remainingDirty.delete(change.mapping.storePath);
            }
          });

          set({
            parameterMetadata: updatedMeta,
            isSaving: false,
            isDirty: remainingDirty.size > 0,
            dirtyFields: remainingDirty,
            error: null,
          });
        } else {
          // Just stop saving state on failure
          set({ isSaving: false });
        }
      },

      // Reset to original API values
      resetToOriginal: () => {
        const { parameterMetadata } = get();
        let settings = JSON.parse(JSON.stringify(DEFAULT_DEVICE_SETTINGS));

        Object.entries(parameterMetadata).forEach(([storePath, meta]) => {
          const mapping = ALL_MAPPINGS.find((m) => m.storePath === storePath);
          if (mapping) {
            const transformer =
              transformerRegistry.toApp[mapping.transformerType as keyof typeof transformerRegistry.toApp];
            const appValue = transformer ? transformer(meta.originalValue) : meta.originalValue;
            settings = setNestedValue(settings, storePath, appValue);
          }
        });

        set({
          settings,
          isDirty: false,
          dirtyFields: new Set<string>(),
        });
      },

      // Reset to default values
      resetToDefaults: () => {
        set({
          settings: JSON.parse(JSON.stringify(DEFAULT_DEVICE_SETTINGS)),
          isDirty: true,
          dirtyFields: new Set(ALL_MAPPINGS.map((m) => m.storePath)),
        });
      },

      // Update single setting
      updateSetting: <T>(path: string, value: T) => {
        const { settings, dirtyFields } = get();

        // Debug logging for activeDays
        if (__DEV__ && path.includes('activeDays')) {
          console.log('[DeviceSettings] updateSetting called:', {
            path,
            value,
            currentValue: getNestedValue(settings, path),
          });
        }

        const newSettings = setNestedValue(settings, path, value);
        const newDirtyFields = new Set(dirtyFields);
        newDirtyFields.add(path);

        set({
          settings: newSettings,
          isDirty: true,
          dirtyFields: newDirtyFields,
        });
      },

      // Update multiple settings at once
      updateBatchSettings: (updates: Record<string, any>) => {
        const { settings, dirtyFields } = get();
        let newSettings = { ...settings };
        const newDirtyFields = new Set(dirtyFields);

        Object.entries(updates).forEach(([path, value]) => {
          newSettings = setNestedValue(newSettings, path, value);
          newDirtyFields.add(path);
        });

        set({
          settings: newSettings,
          isDirty: true,
          dirtyFields: newDirtyFields,
        });
      },

      // Update TimeSlot
      updateTimeSlot: (slotIndex: number, updates: Partial<TimeSlot>) => {
        const { settings, dirtyFields } = get();
        const newTimeSlots = [...settings.timeOfUse.timeSlots] as DeviceSettingsConfig['timeOfUse']['timeSlots'];
        newTimeSlots[slotIndex] = { ...newTimeSlots[slotIndex], ...updates };

        const newDirtyFields = new Set(dirtyFields);
        Object.keys(updates).forEach((key) => {
          newDirtyFields.add(`timeOfUse.timeSlots.${slotIndex}.${key}`);
        });

        set({
          settings: {
            ...settings,
            timeOfUse: {
              ...settings.timeOfUse,
              timeSlots: newTimeSlots,
            },
          },
          isDirty: true,
          dirtyFields: newDirtyFields,
        });
      },

      // Clear error
      clearError: () => set({ error: null }),

      // Set device ID
      setDeviceId: (id: string | null) => set({ deviceId: id }),
    }),
    {
      name: 'device-settings-storage',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        settings: state.settings,
        deviceId: state.deviceId,
        lastFetchedAt: state.lastFetchedAt,
      }),
    }
  )
);

// Selectors
export const useDeviceSettings = () => useDeviceSettingsStore((state) => state.settings);
export const useDeviceSettingsLoading = () => useDeviceSettingsStore((state) => state.isLoading);
export const useDeviceSettingsSaving = () => useDeviceSettingsStore((state) => state.isSaving);
export const useDeviceSettingsError = () => useDeviceSettingsStore((state) => state.error);
export const useDeviceSettingsDirty = () => useDeviceSettingsStore((state) => state.isDirty);

// Category-specific selectors
export const useBatterySettings = () =>
  useDeviceSettingsStore((state) => state.settings.batterySettings);
export const useSystemWorkModeSettings = () =>
  useDeviceSettingsStore((state) => state.settings.systemWorkMode);
export const useTimeOfUseSettings = () =>
  useDeviceSettingsStore((state) => state.settings.timeOfUse);
export const useGeneratorSmartLoadSettings = () =>
  useDeviceSettingsStore((state) => state.settings.generatorAndSmartLoad);
export const useMaintenanceSettings = () =>
  useDeviceSettingsStore((state) => state.settings.maintenanceSettings);

// TimeSlot selector
export const useTimeSlot = (index: number) =>
  useDeviceSettingsStore((state) => state.settings.timeOfUse.timeSlots[index]);
