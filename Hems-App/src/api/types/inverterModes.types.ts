/**
 * Hibrit Inverter Mod Tipleri
 * 8 ön tanımlı mod + 1 özel mod için tip tanımlamaları
 */

// Çalışma Modu Tipleri
export type WorkModeType = 'selling_first' | 'zero_export_to_load' | 'zero_export_to_ct';
export type EnergyPatternType = 'batt_first' | 'load_first';
export type BatteryType = 'lithium' | 'agm' | 'gel' | 'flooded';
export type OnOffStatus = 'on' | 'off';

// Çalışma Modu Ayarları
export interface WorkModeSettings {
  workMode: WorkModeType;
  solarSell: OnOffStatus;
  energyPattern: EnergyPatternType;
  maxSellPower: number; // 0-15000W
  zeroExportPower: number; // 0-100W
  gridPeakShaving: OnOffStatus;
  gridPeakShavingPower: number; // W
  timeOfUse: OnOffStatus;
}

// Batarya Ayarları
export interface BatterySettings {
  batteryType: BatteryType;
  batteryCapacity: number; // Ah
  maxACharge: number; // A (0-280)
  maxADischarge: number; // A (0-280)
  shutdownSOC: number; // 0-100%
  lowBattSOC: number; // 0-100%
  restartSOC: number; // 0-100%
  floatVoltage: number; // V
  absorptionVoltage: number; // V
  equalizationVoltage: number; // V
}

// Şebeke Ayarları
export interface GridSettings {
  gridCharge: OnOffStatus;
  gridChargeSOC: number; // 0-100%
  gridChargeCurrent: number; // A (0-100)
  gridSignal: OnOffStatus;
}

// Jeneratör Ayarları
export interface GeneratorSettings {
  genCharge: OnOffStatus;
  genStartSOC: number; // 0-100%
  genChargeCurrent: number; // A (0-100)
  genMaxRunTime: number; // saat (0-24)
  genDownTime: number; // saat (0-24)
  genSignal: OnOffStatus;
  genPeakShaving: OnOffStatus;
  genRatedPower: number; // W
}

// Gelişmiş Ayarlar
export interface AdvancedSettings {
  signalIslandMode: OnOffStatus;
  bmsErrStop: OnOffStatus;
  backupDelay: number; // ms (0-1000)
  ctRatio: string;
  asymmetricPhaseFeeding: OnOffStatus;
}

// Time of Use Period ID - Sabit 6 dilim
export type TimeOfUsePeriodId = 'P1' | 'P2' | 'P3' | 'P4' | 'P5' | 'P6';

// Time of Use Periyodu
export interface TimeOfUsePeriod {
  id: TimeOfUsePeriodId;
  startTime: string; // "HH:MM"
  endTime: string; // "HH:MM"
  gridCharge: OnOffStatus;
  genCharge: OnOffStatus;
  targetSOC: number; // 0-100%
  maxDischargePower: number; // 0-15000W
}

// Time of Use Ayarları - Tam olarak 6 dilim zorunlu
export interface TimeOfUseSettings {
  enabled: OnOffStatus;
  periods: [
    TimeOfUsePeriod, // P1
    TimeOfUsePeriod, // P2
    TimeOfUsePeriod, // P3
    TimeOfUsePeriod, // P4
    TimeOfUsePeriod, // P5
    TimeOfUsePeriod  // P6
  ];
  activeDays: {
    monday: boolean;
    tuesday: boolean;
    wednesday: boolean;
    thursday: boolean;
    friday: boolean;
    saturday: boolean;
    sunday: boolean;
  };
}

// Varsayılan 6 Zaman Dilimi (İnverter kullanım kılavuzuna göre)
export const DEFAULT_TIME_OF_USE_PERIODS: TimeOfUseSettings['periods'] = [
  { id: 'P1', startTime: '01:00', endTime: '05:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P2', startTime: '05:00', endTime: '09:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P3', startTime: '09:00', endTime: '13:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P4', startTime: '13:00', endTime: '17:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P5', startTime: '17:00', endTime: '21:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
  { id: 'P6', startTime: '21:00', endTime: '01:00', gridCharge: 'off', genCharge: 'off', targetSOC: 50, maxDischargePower: 5000 },
];

// Tam İnverter Konfigürasyonu
export interface InverterConfig {
  workMode: WorkModeSettings;
  battery: BatterySettings;
  grid: GridSettings;
  generator: GeneratorSettings;
  advanced: AdvancedSettings;
  timeOfUse: TimeOfUseSettings;
}

// Mod Karşılaştırma Özellikleri
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

// Ön Tanımlı Mod
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

// Mod ID'leri
export type PresetModeId =
  | 'mode_1' // Maksimum Tasarruf
  | 'mode_2' // Şebekeye Satış
  | 'mode_3' // Off-Grid (Ada)
  | 'mode_4' // Yedek Güç (UPS)
  | 'mode_5' // Gece Tarifesi Avantajı
  | 'mode_6' // Sıfır İhracat
  | 'mode_7' // Jeneratör Destekli
  | 'mode_8' // Dengeli Hibrit
  | 'custom'; // Özel Mod

// Ayar Kategorileri
export type SettingCategory =
  | 'workMode'
  | 'battery'
  | 'grid'
  | 'generator'
  | 'advanced'
  | 'timeOfUse';

// Validasyon Kuralları
export interface ValidationRules {
  batterySOC: {
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

// Default Validasyon Değerleri
export const VALIDATION_RULES: ValidationRules = {
  batterySOC: {
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

// Default Time of Use Günleri
export const DEFAULT_ACTIVE_DAYS = {
  monday: true,
  tuesday: true,
  wednesday: true,
  thursday: true,
  friday: true,
  saturday: true,
  sunday: true,
};

// Work Mode Etiketleri
export const WORK_MODE_LABELS: Record<WorkModeType, string> = {
  selling_first: 'Selling First',
  zero_export_to_load: 'Zero Export to Load',
  zero_export_to_ct: 'Zero Export to CT',
};

// Energy Pattern Etiketleri
export const ENERGY_PATTERN_LABELS: Record<EnergyPatternType, string> = {
  batt_first: 'Battery First',
  load_first: 'Load First',
};

// Battery Type Etiketleri
export const BATTERY_TYPE_LABELS: Record<BatteryType, string> = {
  lithium: 'Lithium',
  agm: 'AGM',
  gel: 'Gel',
  flooded: 'Flooded',
};
