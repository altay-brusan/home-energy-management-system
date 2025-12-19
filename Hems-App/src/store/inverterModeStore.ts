/**
 * Inverter Mode Store
 * Hibrit inverter mod seçimi ve özel mod yapılandırması için Zustand store
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  PresetMode,
  InverterConfig,
  WorkModeSettings,
  BatterySettings,
  GridSettings,
  GeneratorSettings,
  AdvancedSettings,
  TimeOfUseSettings,
  TimeOfUsePeriod,
  TimeOfUsePeriodId,
  DEFAULT_ACTIVE_DAYS,
  DEFAULT_TIME_OF_USE_PERIODS,
} from '../api/types/inverterModes.types';
import { PRESET_MODES, getModeById, DEFAULT_MODE_ID } from '../data/presetModes';

// Store State Interface
interface InverterModeState {
  // State
  activeModeId: string | null;
  activeMode: PresetMode | null;
  customConfig: InverterConfig;
  isLoading: boolean;
  error: string | null;

  // Actions - Mode Selection
  selectMode: (modeId: string) => void;
  applyMode: () => void;
  resetToDefault: () => void;

  // Actions - Custom Config Updates
  updateCustomConfig: (config: Partial<InverterConfig>) => void;
  updateWorkModeSettings: (settings: Partial<WorkModeSettings>) => void;
  updateBatterySettings: (settings: Partial<BatterySettings>) => void;
  updateGridSettings: (settings: Partial<GridSettings>) => void;
  updateGeneratorSettings: (settings: Partial<GeneratorSettings>) => void;
  updateAdvancedSettings: (settings: Partial<AdvancedSettings>) => void;
  updateTimeOfUseSettings: (settings: Partial<TimeOfUseSettings>) => void;

  // Actions - Time of Use Periods (fixed 6 periods - no add/remove)
  updateTimeOfUsePeriod: (periodId: TimeOfUsePeriodId, updates: Partial<TimeOfUsePeriod>) => void;

  // Actions - Copy from preset
  copyPresetToCustom: (modeId: string) => void;

  // Actions - Error handling
  clearError: () => void;
}

// Default Custom Config (Dengeli Hibrit tabanlı)
const getDefaultCustomConfig = (): InverterConfig => {
  const defaultMode = getModeById(DEFAULT_MODE_ID);
  if (defaultMode) {
    return JSON.parse(JSON.stringify(defaultMode.config));
  }
  // Fallback config
  return {
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
  };
};

// Create Store
export const useInverterModeStore = create<InverterModeState>()(
  persist(
    (set, get) => ({
      // Initial State
      activeModeId: null,
      activeMode: null,
      customConfig: getDefaultCustomConfig(),
      isLoading: false,
      error: null,

      // Select a mode (doesn't apply yet)
      selectMode: (modeId: string) => {
        const mode = getModeById(modeId);
        if (mode) {
          set({
            activeModeId: modeId,
            activeMode: mode,
            error: null,
          });
        } else {
          set({ error: `Mode not found: ${modeId}` });
        }
      },

      // Apply selected mode
      applyMode: () => {
        const { activeModeId, activeMode, customConfig } = get();
        if (!activeModeId || !activeMode) {
          set({ error: 'No mode selected' });
          return;
        }

        // If custom mode, use custom config; otherwise use preset config
        if (activeMode.isCustom) {
          // Custom mode uses the customConfig
          set({ error: null });
        } else {
          // Preset mode - config is already in activeMode
          set({ error: null });
        }
      },

      // Reset to default mode
      resetToDefault: () => {
        const defaultMode = getModeById(DEFAULT_MODE_ID);
        set({
          activeModeId: DEFAULT_MODE_ID,
          activeMode: defaultMode || null,
          customConfig: getDefaultCustomConfig(),
          error: null,
        });
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

      // Update time of use period (fixed 6 periods - no add/remove)
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

      // Copy preset config to custom
      copyPresetToCustom: (modeId: string) => {
        const mode = getModeById(modeId);
        if (mode && !mode.isCustom) {
          set({
            customConfig: JSON.parse(JSON.stringify(mode.config)),
          });
        }
      },

      // Clear error
      clearError: () => {
        set({ error: null });
      },
    }),
    {
      name: 'inverter-mode-storage',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        activeModeId: state.activeModeId,
        customConfig: state.customConfig,
      }),
      onRehydrateStorage: () => (state) => {
        // Rehydrate activeMode from activeModeId
        if (state?.activeModeId) {
          const mode = getModeById(state.activeModeId);
          if (mode) {
            state.activeMode = mode;
          }
        }

        // Migration: Ensure timeOfUse always has exactly 6 periods
        if (state?.customConfig?.timeOfUse) {
          const periods = state.customConfig.timeOfUse.periods;
          if (!periods || periods.length !== 6) {
            // Reset to default 6 periods
            state.customConfig.timeOfUse.periods = [...DEFAULT_TIME_OF_USE_PERIODS];
          }
        }

        // Migration: Ensure chargingSOC exists (for existing users)
        if (state?.customConfig?.battery && state.customConfig.battery.chargingSOC === undefined) {
          state.customConfig.battery.chargingSOC = 100;
        }
      },
    }
  )
);

// Selectors
export const useActiveMode = () => useInverterModeStore((state) => state.activeMode);
export const useActiveModeId = () => useInverterModeStore((state) => state.activeModeId);
export const useCustomConfig = () => useInverterModeStore((state) => state.customConfig);
export const useInverterModeError = () => useInverterModeStore((state) => state.error);

// Get current config (active mode's config or custom config)
export const useCurrentConfig = () =>
  useInverterModeStore((state) => {
    if (state.activeMode?.isCustom) {
      return state.customConfig;
    }
    return state.activeMode?.config || state.customConfig;
  });
