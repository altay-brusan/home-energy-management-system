/**
 * Inverter Mode Store
 * API-driven mode selection and custom mode configuration
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  InverterMode,
  InverterConfig,
  WorkModeSettings,
  BatterySettings,
  GridSettings,
  GeneratorSettings,
  AdvancedSettings,
  TimeOfUseSettings,
  TimeOfUsePeriod,
  TimeOfUsePeriodId,
  CUSTOM_MODE_NUMBER,
  DEFAULT_ACTIVE_DAYS,
  DEFAULT_TIME_OF_USE_PERIODS,
} from '../api/types/inverterModes.types';
import { deviceSettingsService } from '../api/services/deviceSettingsService';

// Store State Interface
interface InverterModeState {
  // API Modes
  modes: InverterMode[];
  isLoadingModes: boolean;
  modesError: string | null;

  // Active Mode
  activeModeId: string | null;
  activeModeNumber: number | null;

  // Custom Config (for custom mode - legacy support)
  customConfig: InverterConfig;

  // Mode Application
  isApplyingMode: boolean;
  applyError: string | null;

  // Actions - Fetch Modes
  fetchModes: () => Promise<void>;

  // Actions - Mode Selection & Application
  selectAndApplyMode: (
    mode: InverterMode,
    deviceId: string,
    onCustomMode: () => void
  ) => Promise<boolean>;

  // Actions - Custom Config Updates (legacy support)
  updateCustomConfig: (config: Partial<InverterConfig>) => void;
  updateWorkModeSettings: (settings: Partial<WorkModeSettings>) => void;
  updateBatterySettings: (settings: Partial<BatterySettings>) => void;
  updateGridSettings: (settings: Partial<GridSettings>) => void;
  updateGeneratorSettings: (settings: Partial<GeneratorSettings>) => void;
  updateAdvancedSettings: (settings: Partial<AdvancedSettings>) => void;
  updateTimeOfUseSettings: (settings: Partial<TimeOfUseSettings>) => void;
  updateTimeOfUsePeriod: (periodId: TimeOfUsePeriodId, updates: Partial<TimeOfUsePeriod>) => void;

  // Actions - Custom Mode
  setCustomModeActive: () => void;

  // Actions - Error handling
  clearModesError: () => void;
  clearApplyError: () => void;
}

// Helper to transform API mode to internal format
const transformApiMode = (apiMode: {
  mode: number;
  name: string;
  description: string;
}): InverterMode => ({
  id: apiMode.mode === CUSTOM_MODE_NUMBER ? 'custom' : `mode_${apiMode.mode}`,
  mode: apiMode.mode,
  name: apiMode.name,
  description: apiMode.description,
  isCustom: apiMode.mode === CUSTOM_MODE_NUMBER,
});

// Default Custom Config
const getDefaultCustomConfig = (): InverterConfig => ({
  workMode: {
    workMode: 'zero_export_to_ct',
    solarSell: 'off',
    energyPattern: 'load_first',
    maxSellPower: 0,
    zeroExportPower: 20,
    gridPeakShaving: 'off',
    gridPeakShavingPower: 0,
    timeOfUse: 'off',
  },
  battery: {
    batteryType: 'lithium',
    batteryCapacity: 200,
    maxACharge: 40,
    maxADischarge: 100,
    chargingSOC: 100,
    shutdownSOC: 10,
    lowBattSOC: 20,
    restartSOC: 40,
    floatVoltage: 53.6,
    absorptionVoltage: 57.6,
    equalizationVoltage: 57.6,
  },
  grid: {
    gridCharge: 'off',
    gridChargeSOC: 0,
    gridChargeCurrent: 0,
    gridSignal: 'off',
  },
  generator: {
    genCharge: 'off',
    genStartSOC: 0,
    genChargeCurrent: 0,
    genMaxRunTime: 0,
    genDownTime: 0,
    genSignal: 'off',
    genPeakShaving: 'off',
    genRatedPower: 0,
  },
  advanced: {
    signalIslandMode: 'off',
    bmsErrStop: 'on',
    backupDelay: 0,
    ctRatio: '1:1',
    asymmetricPhaseFeeding: 'off',
  },
  timeOfUse: {
    enabled: 'off',
    periods: [...DEFAULT_TIME_OF_USE_PERIODS],
    activeDays: { ...DEFAULT_ACTIVE_DAYS },
  },
});

// Create Store
export const useInverterModeStore = create<InverterModeState>()(
  persist(
    (set, get) => ({
      // Initial State
      modes: [],
      isLoadingModes: false,
      modesError: null,
      activeModeId: null,
      activeModeNumber: null,
      customConfig: getDefaultCustomConfig(),
      isApplyingMode: false,
      applyError: null,

      // Fetch modes from API
      fetchModes: async () => {
        set({ isLoadingModes: true, modesError: null });
        try {
          const response = await deviceSettingsService.getModes();
          const transformedModes = response.modes.map(transformApiMode);
          set({ modes: transformedModes, isLoadingModes: false });
        } catch (error: any) {
          set({
            modesError: error.message || 'Failed to fetch modes',
            isLoadingModes: false,
          });
        }
      },

      // Select and apply mode
      selectAndApplyMode: async (mode, deviceId, onCustomMode) => {
        // If custom mode, navigate to CustomModeScreen instead
        if (mode.isCustom) {
          onCustomMode();
          return true;
        }

        set({ isApplyingMode: true, applyError: null });
        try {
          await deviceSettingsService.updateDeviceMode(deviceId, {
            mode: mode.mode,
          });
          set({
            activeModeId: mode.id,
            activeModeNumber: mode.mode,
            isApplyingMode: false,
          });
          return true;
        } catch (error: any) {
          set({
            applyError: error.message || 'Failed to apply mode',
            isApplyingMode: false,
          });
          return false;
        }
      },

      // Update entire custom config
      updateCustomConfig: (config: Partial<InverterConfig>) => {
        const { customConfig } = get();
        set({
          customConfig: { ...customConfig, ...config },
        });
      },

      // Update work mode settings
      updateWorkModeSettings: (settings: Partial<WorkModeSettings>) => {
        const { customConfig } = get();
        set({
          customConfig: {
            ...customConfig,
            workMode: { ...customConfig.workMode, ...settings },
          },
        });
      },

      // Update battery settings
      updateBatterySettings: (settings: Partial<BatterySettings>) => {
        const { customConfig } = get();
        set({
          customConfig: {
            ...customConfig,
            battery: { ...customConfig.battery, ...settings },
          },
        });
      },

      // Update grid settings
      updateGridSettings: (settings: Partial<GridSettings>) => {
        const { customConfig } = get();
        set({
          customConfig: {
            ...customConfig,
            grid: { ...customConfig.grid, ...settings },
          },
        });
      },

      // Update generator settings
      updateGeneratorSettings: (settings: Partial<GeneratorSettings>) => {
        const { customConfig } = get();
        set({
          customConfig: {
            ...customConfig,
            generator: { ...customConfig.generator, ...settings },
          },
        });
      },

      // Update advanced settings
      updateAdvancedSettings: (settings: Partial<AdvancedSettings>) => {
        const { customConfig } = get();
        set({
          customConfig: {
            ...customConfig,
            advanced: { ...customConfig.advanced, ...settings },
          },
        });
      },

      // Update time of use settings
      updateTimeOfUseSettings: (settings: Partial<TimeOfUseSettings>) => {
        const { customConfig } = get();
        set({
          customConfig: {
            ...customConfig,
            timeOfUse: { ...customConfig.timeOfUse, ...settings },
          },
        });
      },

      // Update time of use period
      updateTimeOfUsePeriod: (periodId: TimeOfUsePeriodId, updates: Partial<TimeOfUsePeriod>) => {
        const { customConfig } = get();
        const periods = customConfig.timeOfUse.periods.map((p) =>
          p.id === periodId ? { ...p, ...updates } : p
        ) as TimeOfUseSettings['periods'];
        set({
          customConfig: {
            ...customConfig,
            timeOfUse: { ...customConfig.timeOfUse, periods },
          },
        });
      },

      // Set custom mode as active (called after CustomModeScreen save)
      setCustomModeActive: () => {
        set({
          activeModeId: 'custom',
          activeModeNumber: CUSTOM_MODE_NUMBER,
        });
      },

      // Clear errors
      clearModesError: () => set({ modesError: null }),
      clearApplyError: () => set({ applyError: null }),
    }),
    {
      name: 'inverter-mode-storage',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        activeModeId: state.activeModeId,
        activeModeNumber: state.activeModeNumber,
        customConfig: state.customConfig,
      }),
      onRehydrateStorage: () => (state) => {
        // Migration: Ensure timeOfUse always has exactly 6 periods
        if (state?.customConfig?.timeOfUse) {
          const periods = state.customConfig.timeOfUse.periods;
          if (!periods || periods.length !== 6) {
            state.customConfig.timeOfUse.periods = [...DEFAULT_TIME_OF_USE_PERIODS];
          }
        }

        // Migration: Ensure chargingSOC exists
        if (state?.customConfig?.battery && state.customConfig.battery.chargingSOC === undefined) {
          state.customConfig.battery.chargingSOC = 100;
        }
      },
    }
  )
);

// Selectors
export const useModes = () => useInverterModeStore((state) => state.modes);
export const useActiveModeId = () => useInverterModeStore((state) => state.activeModeId);
export const useActiveModeNumber = () => useInverterModeStore((state) => state.activeModeNumber);
export const useIsLoadingModes = () => useInverterModeStore((state) => state.isLoadingModes);
export const useModesError = () => useInverterModeStore((state) => state.modesError);
export const useCustomConfig = () => useInverterModeStore((state) => state.customConfig);
export const useIsApplyingMode = () => useInverterModeStore((state) => state.isApplyingMode);
export const useApplyError = () => useInverterModeStore((state) => state.applyError);

// Get mode by id from fetched modes
export const useModeById = (modeId: string) =>
  useInverterModeStore((state) => state.modes.find((m) => m.id === modeId));
