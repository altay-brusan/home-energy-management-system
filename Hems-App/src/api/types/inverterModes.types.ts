/**
 * Hibrit Inverter Mod Tipleri
 * API yapısına uyumlu tip tanımlamaları
 */

// ============================================
// TEMEL TİPLER
// ============================================

export type WorkModeType = 'selling_first' | 'zero_export_to_load' | 'zero_export_to_ct';

// ============================================
// API MODE TİPLERİ
// ============================================

/**
 * Custom mode constant
 */
export const CUSTOM_MODE_NUMBER = 99;

/**
 * API response for a single mode
 */
export interface ApiModeDto {
  mode: number;
  name: string;
  description: string;
  configuration: unknown | null; // Ignored - not used in UI
}

/**
 * API response for GET /api/DeviceSettings/modes
 */
export interface GetModesResponse {
  modes: ApiModeDto[];
}

/**
 * Request body for POST /api/DeviceSettings/device/{deviceId}/mode
 */
export interface UpdateDeviceModeCommand {
  mode: number;
}

/**
 * API response for GET /api/DeviceSettings/device/{deviceId}/mode
 */
export interface GetDeviceModeResponse {
  success: boolean;
  errorMessage: string | null;
  message: string | null;
  mode: number;
  modeDescription: string;
  lastModified: string;
}

/**
 * Transformed mode for UI consumption
 */
export interface InverterMode {
  id: string;          // e.g., "mode_1", "mode_2", "custom"
  mode: number;        // API mode number (1, 2, ..., 99)
  name: string;        // Mode name from API
  description: string; // Mode description from API
  isCustom: boolean;   // true if mode === 99
}
export type EnergyPatternType = 'batt_first' | 'load_first';
export type OnOffStatus = 'on' | 'off';
export type TimeOfUsePeriodId = 'P1' | 'P2' | 'P3' | 'P4' | 'P5' | 'P6';

// ============================================
// API YAPISINA UYUMLU YENİ TİPLER
// ============================================

/**
 * SystemWorkMode - API sortOrder: 2
 */
export interface SystemWorkModeSettings {
  exportStrategy: WorkModeType;
  energyPriority: EnergyPatternType;
  solarSell: OnOffStatus;
  maxSellPower: number;
  zeroExportLimit: number;
  forceOffGrid: OnOffStatus;
  gridPeakShavingEnabled: OnOffStatus;
  gridPeakShavingLimitPower: number;
}

/**
 * BatterySettings - API sortOrder: 1
 */
export interface BatterySettingsConfig {
  gridCharging: {
    enabled: OnOffStatus;
    startSOC: number;
    current: number;
  };
  generatorCharging: {
    enabled: OnOffStatus;
    startSOC: number;
    current: number;
    maxRunTime: number;
    coolingTime: number;
  };
  protectionThresholds: {
    shutdownSOC: number;
    lowBatteryWarning: number;
    restartSOC: number;
  };
}

/**
 * TimeSlot - API TimeSlot1-6
 */
export interface TimeSlot {
  id: TimeOfUsePeriodId;
  startTime: string;
  endTime: string;
  gridCharge: OnOffStatus;
  genCharge: OnOffStatus;
  maxDischarge: number;
  minBatterySOC: number;
  minBatteryVoltage: number;
}

/**
 * TimeSlot Visibility - API'den gelen visible parametresi
 */
export interface TimeSlotVisibility {
  startTime: boolean;
  endTime: boolean;
  gridCharge: boolean;
  genCharge: boolean;
  maxDischarge: boolean;
  minBatterySOC: boolean;
  minBatteryVoltage: boolean;
}

/**
 * ActiveDays
 */
export interface ActiveDays {
  monday: boolean;
  tuesday: boolean;
  wednesday: boolean;
  thursday: boolean;
  friday: boolean;
  saturday: boolean;
  sunday: boolean;
}

/**
 * TimeOfUse - API sortOrder: 3
 */
export interface TimeOfUseConfig {
  enabled: OnOffStatus;
  activeDays: ActiveDays;
  timeSlots: [TimeSlot, TimeSlot, TimeSlot, TimeSlot, TimeSlot, TimeSlot];
}

/**
 * SmartLoad Settings - YENİ
 */
export interface SmartLoadSettings {
  enabled: OnOffStatus;
  offSOC: number;
  onSOC: number;
  pvPowerThreshold: number;
  openDelay: number;
  onGridAlwaysOn: OnOffStatus;
}

/**
 * GeneratorAndSmartLoad - API sortOrder: 4
 */
export interface GeneratorSmartLoadConfig {
  generatorSettings: {
    charge: OnOffStatus;
    gridCharge: OnOffStatus;
    startSOC: number;
    chargeCurrent: number;
  };
  generatorPeakShaving: {
    enabled: OnOffStatus;
    limitPower: number;
  };
  smartLoad: SmartLoadSettings;
}

/**
 * MaintenanceSettings - API sortOrder: 5 (Admin Only)
 */
export interface MaintenanceSettingsConfig {
  batteryMaintenance: {
    controlMode: string;
    lithiumWakeUpEnabled: OnOffStatus;
    resistance: number;
    chargeEfficiency: number;
  };
  gridCode: {
    standard: string;
    frequency: number;
    outputVoltageLevel: string;
    voltageHigh: number;
    voltageLow: number;
    frequencyHigh: number;
    frequencyLow: number;
    reconnectionTime: number;
  };
  advanced: {
    arcFaultDetection: OnOffStatus;
    islandProtection: OnOffStatus;
    parallelEnabled: OnOffStatus;
    parallelRole: string;
    externalCTRatio: number;
  };
}

/**
 * Tam Device Settings Config (API yapısına uyumlu)
 */
export interface DeviceSettingsConfig {
  batterySettings: BatterySettingsConfig;
  systemWorkMode: SystemWorkModeSettings;
  timeOfUse: TimeOfUseConfig;
  generatorAndSmartLoad: GeneratorSmartLoadConfig;
  maintenanceSettings?: MaintenanceSettingsConfig;
}

// ============================================
// DEFAULT DEĞERLER
// ============================================

export const DEFAULT_TIME_SLOTS: TimeOfUseConfig['timeSlots'] = [
  { id: 'P1', startTime: '00:00', endTime: '06:00', gridCharge: 'off', genCharge: 'off', maxDischarge: 3000, minBatterySOC: 30, minBatteryVoltage: 48 },
  { id: 'P2', startTime: '06:00', endTime: '10:00', gridCharge: 'off', genCharge: 'off', maxDischarge: 3000, minBatterySOC: 30, minBatteryVoltage: 48 },
  { id: 'P3', startTime: '10:00', endTime: '14:00', gridCharge: 'off', genCharge: 'off', maxDischarge: 3000, minBatterySOC: 30, minBatteryVoltage: 48 },
  { id: 'P4', startTime: '14:00', endTime: '18:00', gridCharge: 'off', genCharge: 'off', maxDischarge: 3000, minBatterySOC: 30, minBatteryVoltage: 48 },
  { id: 'P5', startTime: '18:00', endTime: '22:00', gridCharge: 'off', genCharge: 'off', maxDischarge: 3000, minBatterySOC: 30, minBatteryVoltage: 48 },
  { id: 'P6', startTime: '22:00', endTime: '24:00', gridCharge: 'off', genCharge: 'off', maxDischarge: 3000, minBatterySOC: 30, minBatteryVoltage: 48 },
];

export const DEFAULT_ACTIVE_DAYS: ActiveDays = {
  monday: true,
  tuesday: true,
  wednesday: true,
  thursday: true,
  friday: true,
  saturday: true,
  sunday: true,
};

export const DEFAULT_DEVICE_SETTINGS: DeviceSettingsConfig = {
  batterySettings: {
    gridCharging: {
      enabled: 'off',
      startSOC: 30,
      current: 40,
    },
    generatorCharging: {
      enabled: 'off',
      startSOC: 30,
      current: 40,
      maxRunTime: 24,
      coolingTime: 0,
    },
    protectionThresholds: {
      shutdownSOC: 20,
      lowBatteryWarning: 35,
      restartSOC: 50,
    },
  },
  systemWorkMode: {
    exportStrategy: 'zero_export_to_ct',
    energyPriority: 'batt_first',
    solarSell: 'off',
    maxSellPower: 12000,
    zeroExportLimit: 20,
    forceOffGrid: 'off',
    gridPeakShavingEnabled: 'off',
    gridPeakShavingLimitPower: 8000,
  },
  timeOfUse: {
    enabled: 'off',
    activeDays: { ...DEFAULT_ACTIVE_DAYS },
    timeSlots: [...DEFAULT_TIME_SLOTS],
  },
  generatorAndSmartLoad: {
    generatorSettings: {
      charge: 'off',
      gridCharge: 'off',
      startSOC: 30,
      chargeCurrent: 40,
    },
    generatorPeakShaving: {
      enabled: 'off',
      limitPower: 8000,
    },
    smartLoad: {
      enabled: 'off',
      offSOC: 10,
      onSOC: 90,
      pvPowerThreshold: 0,
      openDelay: 1,
      onGridAlwaysOn: 'off',
    },
  },
};

// ============================================
// LEGACY TİPLER (Preset modlar için korunuyor)
// ============================================

/** @deprecated Preset modlar için kullanılıyor, API için DeviceSettingsConfig kullanın */
export type BatteryType = 'lithium' | 'agm' | 'gel' | 'flooded';

/** @deprecated Preset modlar için */
export interface WorkModeSettings {
  workMode: WorkModeType;
  solarSell: OnOffStatus;
  energyPattern: EnergyPatternType;
  maxSellPower: number;
  zeroExportPower: number;
  gridPeakShaving: OnOffStatus;
  gridPeakShavingPower: number;
  timeOfUse: OnOffStatus;
}

/** @deprecated Preset modlar için */
export interface BatterySettings {
  batteryType: BatteryType;
  batteryCapacity: number;
  maxACharge: number;
  maxADischarge: number;
  chargingSOC: number;
  shutdownSOC: number;
  lowBattSOC: number;
  restartSOC: number;
  floatVoltage: number;
  absorptionVoltage: number;
  equalizationVoltage: number;
}

/** @deprecated Preset modlar için */
export interface GridSettings {
  gridCharge: OnOffStatus;
  gridChargeSOC: number;
  gridChargeCurrent: number;
  gridSignal: OnOffStatus;
}

/** @deprecated Preset modlar için */
export interface GeneratorSettings {
  genCharge: OnOffStatus;
  genStartSOC: number;
  genChargeCurrent: number;
  genMaxRunTime: number;
  genDownTime: number;
  genSignal: OnOffStatus;
  genPeakShaving: OnOffStatus;
  genRatedPower: number;
}

/** @deprecated Preset modlar için */
export interface AdvancedSettings {
  signalIslandMode: OnOffStatus;
  bmsErrStop: OnOffStatus;
  backupDelay: number;
  ctRatio: string;
  asymmetricPhaseFeeding: OnOffStatus;
}

/** @deprecated Preset modlar için */
export interface TimeOfUsePeriod {
  id: TimeOfUsePeriodId;
  startTime: string;
  endTime: string;
  gridCharge: OnOffStatus;
  genCharge: OnOffStatus;
  targetSOC: number;
  maxDischargePower: number;
}

/** @deprecated Preset modlar için */
export interface TimeOfUseSettings {
  enabled: OnOffStatus;
  periods: [TimeOfUsePeriod, TimeOfUsePeriod, TimeOfUsePeriod, TimeOfUsePeriod, TimeOfUsePeriod, TimeOfUsePeriod];
  activeDays: ActiveDays;
}

/** @deprecated Preset modlar için, API için DeviceSettingsConfig kullanın */
export interface InverterConfig {
  workMode: WorkModeSettings;
  battery: BatterySettings;
  grid: GridSettings;
  generator: GeneratorSettings;
  advanced: AdvancedSettings;
  timeOfUse: TimeOfUseSettings;
}

/** @deprecated Preset modlar için kullanılıyor */
export const DEFAULT_TIME_OF_USE_PERIODS: TimeOfUseSettings['periods'] = [
  { id: 'P1', startTime: '01:00', endTime: '05:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P2', startTime: '05:00', endTime: '09:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P3', startTime: '09:00', endTime: '13:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P4', startTime: '13:00', endTime: '17:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P5', startTime: '17:00', endTime: '21:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P6', startTime: '21:00', endTime: '01:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
];

// ============================================
// PRESET MOD TİPLERİ
// ============================================

export interface ModeFeatures {
  gridConnectionRequired: boolean;
  batteryRequired: boolean;
  ctSensorRequired: boolean | 'optional';
  generatorSupport: boolean;
  gridSelling: boolean;
  gridCharging: boolean;
  powerOutageOperation: boolean;
  setupDifficulty: 'easy' | 'medium' | 'hard';
  savingsPotential: 'low' | 'medium' | 'high' | 'very_high';
  energyIndependence: 'low' | 'medium' | 'high' | 'full';
}

export interface PresetMode {
  id: string;
  name: string;
  description: string;
  targetUser: string;
  recommendedScenario: string;
  config: InverterConfig;
  features: ModeFeatures;
  isCustom: boolean;
}

export type PresetModeId =
  | 'mode_1'
  | 'mode_2'
  | 'mode_3'
  | 'mode_4'
  | 'mode_5'
  | 'mode_6'
  | 'mode_7'
  | 'mode_8'
  | 'custom';

// ============================================
// VALIDASYON KURALLARI
// ============================================

export interface ValidationRule {
  min: number;
  max: number;
  step: number;
}

export interface DeviceSettingsValidation {
  batterySOC: {
    shutdown: ValidationRule;
    lowBattery: ValidationRule;
    restart: ValidationRule;
    gridChargingStart: ValidationRule;
    generatorStart: ValidationRule;
  };
  power: {
    maxSellPower: ValidationRule;
    zeroExportLimit: ValidationRule;
    gridPeakShaving: ValidationRule;
    genPeakShaving: ValidationRule;
    maxDischarge: ValidationRule;
    smartLoadPVThreshold: ValidationRule;
  };
  current: {
    gridCharge: ValidationRule;
    genCharge: ValidationRule;
  };
  time: {
    genMaxRunTime: ValidationRule;
    genCoolingTime: ValidationRule;
    smartLoadOpenDelay: ValidationRule;
  };
  voltage: {
    minBattery: ValidationRule;
  };
}

export const DEVICE_SETTINGS_VALIDATION: DeviceSettingsValidation = {
  batterySOC: {
    shutdown: { min: 0, max: 30, step: 1 },
    lowBattery: { min: 10, max: 50, step: 1 },
    restart: { min: 20, max: 100, step: 1 },
    gridChargingStart: { min: 0, max: 100, step: 1 },
    generatorStart: { min: 0, max: 100, step: 1 },
  },
  power: {
    maxSellPower: { min: 0, max: 15000, step: 100 },
    zeroExportLimit: { min: 0, max: 100, step: 1 },
    gridPeakShaving: { min: 0, max: 15000, step: 100 },
    genPeakShaving: { min: 0, max: 15000, step: 100 },
    maxDischarge: { min: 0, max: 15000, step: 100 },
    smartLoadPVThreshold: { min: 0, max: 15000, step: 100 },
  },
  current: {
    gridCharge: { min: 0, max: 100, step: 1 },
    genCharge: { min: 0, max: 100, step: 1 },
  },
  time: {
    genMaxRunTime: { min: 0, max: 24, step: 0.5 },
    genCoolingTime: { min: 0, max: 24, step: 0.5 },
    smartLoadOpenDelay: { min: 0, max: 1440, step: 1 },
  },
  voltage: {
    minBattery: { min: 40, max: 60, step: 0.1 },
  },
};

// Legacy validation (preset modlar için)
export interface ValidationRules {
  batterySOC: {
    charging: { min: number; max: number; default: number };
    shutdown: { min: number; max: number; default: number };
    lowBatt: { min: number; max: number; default: number };
    restart: { min: number; max: number; default: number };
  };
  power: {
    maxSellPower: { min: number; max: number; step: number };
    zeroExportPower: { min: number; max: number; step: number };
    gridPeakShaving: { min: number; max: number; step: number };
    maxDischargePower: { min: number; max: number; step: number };
  };
  current: {
    maxCharge: { min: number; max: number; step: number };
    maxDischarge: { min: number; max: number; step: number };
    gridCharge: { min: number; max: number; step: number };
    genCharge: { min: number; max: number; step: number };
  };
  time: {
    genMaxRunTime: { min: number; max: number; step: number };
    genDownTime: { min: number; max: number; step: number };
    backupDelay: { min: number; max: number; step: number };
  };
  voltage: {
    float: { min: number; max: number; step: number };
    absorption: { min: number; max: number; step: number };
    equalization: { min: number; max: number; step: number };
  };
  capacity: {
    battery: { min: number; max: number; step: number };
    genRated: { min: number; max: number; step: number };
  };
}

export const VALIDATION_RULES: ValidationRules = {
  batterySOC: {
    charging: { min: 50, max: 100, default: 100 },
    shutdown: { min: 0, max: 30, default: 10 },
    lowBatt: { min: 10, max: 50, default: 20 },
    restart: { min: 20, max: 100, default: 40 },
  },
  power: {
    maxSellPower: { min: 0, max: 15000, step: 100 },
    zeroExportPower: { min: 0, max: 100, step: 10 },
    gridPeakShaving: { min: 0, max: 15000, step: 100 },
    maxDischargePower: { min: 0, max: 15000, step: 100 },
  },
  current: {
    maxCharge: { min: 0, max: 280, step: 5 },
    maxDischarge: { min: 0, max: 280, step: 5 },
    gridCharge: { min: 0, max: 100, step: 5 },
    genCharge: { min: 0, max: 100, step: 5 },
  },
  time: {
    genMaxRunTime: { min: 0, max: 24, step: 0.5 },
    genDownTime: { min: 0, max: 24, step: 0.5 },
    backupDelay: { min: 0, max: 1000, step: 100 },
  },
  voltage: {
    float: { min: 48, max: 58, step: 0.1 },
    absorption: { min: 48, max: 60, step: 0.1 },
    equalization: { min: 48, max: 60, step: 0.1 },
  },
  capacity: {
    battery: { min: 0, max: 9999, step: 10 },
    genRated: { min: 0, max: 20000, step: 500 },
  },
};

// ============================================
// ETİKETLER
// ============================================

export const WORK_MODE_LABELS: Record<WorkModeType, string> = {
  selling_first: 'Selling First',
  zero_export_to_load: 'Zero Export to Load',
  zero_export_to_ct: 'Zero Export to CT',
};

export const ENERGY_PATTERN_LABELS: Record<EnergyPatternType, string> = {
  batt_first: 'Battery First',
  load_first: 'Load First',
};

export const BATTERY_TYPE_LABELS: Record<BatteryType, string> = {
  lithium: 'Lithium',
  agm: 'AGM',
  gel: 'Gel',
  flooded: 'Flooded',
};

// ============================================
// KATEGORİLER
// ============================================

export type SettingCategory =
  | 'batterySettings'
  | 'systemWorkMode'
  | 'timeOfUse'
  | 'generatorAndSmartLoad'
  | 'maintenanceSettings';

export const SETTING_CATEGORIES: { id: SettingCategory; sortOrder: number; adminOnly: boolean }[] = [
  { id: 'batterySettings', sortOrder: 1, adminOnly: false },
  { id: 'systemWorkMode', sortOrder: 2, adminOnly: false },
  { id: 'timeOfUse', sortOrder: 3, adminOnly: false },
  { id: 'generatorAndSmartLoad', sortOrder: 4, adminOnly: false },
  { id: 'maintenanceSettings', sortOrder: 5, adminOnly: true },
];
