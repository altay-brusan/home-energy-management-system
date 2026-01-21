/**
 * Device Settings Mapping Configuration
 * API parametre isimlerini uygulama state path'lerine eşleştirir
 */

export type TransformerType =
  | 'exportStrategy'
  | 'energyPriority'
  | 'boolean'
  | 'nativeBoolean'
  | 'number'
  | 'time'
  | 'string';

export interface ParameterMapping {
  apiName: string;
  storePath: string;
  transformerType: TransformerType;
  unit?: string;
  min?: number;
  max?: number;
  step?: number;
  roleType?: 'User' | 'Admin';
}

/**
 * BatterySettings Mappings
 */
export const BATTERY_SETTINGS_MAPPING: ParameterMapping[] = [
  // BatteryConfiguration
  {
    apiName: 'BatteryCapacity',
    storePath: 'batterySettings.batteryConfiguration.capacity',
    transformerType: 'number',
    unit: 'Ah',
    min: 0,
    max: 2000,
    step: 10,
    roleType: 'User',
  },
  {
    apiName: 'MaxBatteryChargeCurrent',
    storePath: 'batterySettings.batteryConfiguration.maxChargeCurrent',
    transformerType: 'number',
    unit: 'A',
    min: 0,
    max: 240,
    step: 5,
    roleType: 'User',
  },
  {
    apiName: 'MaxBatteryDischargeCurrent',
    storePath: 'batterySettings.batteryConfiguration.maxDischargeCurrent',
    transformerType: 'number',
    unit: 'A',
    min: 0,
    max: 240,
    step: 5,
    roleType: 'User',
  },
  // ChargeSources -> GridCharging
  {
    apiName: 'GridChargingEnabled',
    storePath: 'batterySettings.gridCharging.enabled',
    transformerType: 'boolean',
    roleType: 'User',
  },
  {
    apiName: 'GridChargingStartSOC',
    storePath: 'batterySettings.gridCharging.startSOC',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'GridChargingCurrent',
    storePath: 'batterySettings.gridCharging.current',
    transformerType: 'number',
    unit: 'A',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  // ChargeSources -> GeneratorCharging
  {
    apiName: 'GeneratorChargingEnabled',
    storePath: 'batterySettings.generatorCharging.enabled',
    transformerType: 'boolean',
    roleType: 'User',
  },
  {
    apiName: 'GeneratorChargingStartSOC',
    storePath: 'batterySettings.generatorCharging.startSOC',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'GeneratorChargingCurrent',
    storePath: 'batterySettings.generatorCharging.current',
    transformerType: 'number',
    unit: 'A',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'GeneratorMaxRunTime',
    storePath: 'batterySettings.generatorCharging.maxRunTime',
    transformerType: 'number',
    unit: 'hrs',
    min: 0,
    max: 24,
    step: 0.5,
    roleType: 'User',
  },
  {
    apiName: 'GeneratorCoolingTime',
    storePath: 'batterySettings.generatorCharging.coolingTime',
    transformerType: 'number',
    unit: 'hrs',
    min: 0,
    max: 24,
    step: 0.5,
    roleType: 'User',
  },
  // ProtectionThresholds
  {
    apiName: 'ShutdownSOC',
    storePath: 'batterySettings.protectionThresholds.shutdownSOC',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 30,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'LowBatteryWarning',
    storePath: 'batterySettings.protectionThresholds.lowBatteryWarning',
    transformerType: 'number',
    unit: '%',
    min: 10,
    max: 50,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'RestartSOC',
    storePath: 'batterySettings.protectionThresholds.restartSOC',
    transformerType: 'number',
    unit: '%',
    min: 20,
    max: 100,
    step: 1,
    roleType: 'User',
  },
];

/**
 * SystemWorkMode Mappings
 */
export const SYSTEM_WORK_MODE_MAPPING: ParameterMapping[] = [
  // ExportStrategy
  {
    apiName: 'ExportStrategySelection',
    storePath: 'systemWorkMode.exportStrategy',
    transformerType: 'exportStrategy',
    roleType: 'User',
  },
  // EnergyPriority
  {
    apiName: 'EnergyPrioritySelection',
    storePath: 'systemWorkMode.energyPriority',
    transformerType: 'energyPriority',
    roleType: 'User',
  },
  // InverterMode
  {
    apiName: 'SolarSell',
    storePath: 'systemWorkMode.solarSell',
    transformerType: 'boolean',
    roleType: 'User',
  },
  {
    apiName: 'MaxSolarSellPower',
    storePath: 'systemWorkMode.maxSolarSellPower',
    transformerType: 'number',
    unit: 'W',
    min: 0,
    max: 15000,
    step: 100,
    roleType: 'User',
  },
  {
    apiName: 'MaxSellPower',
    storePath: 'systemWorkMode.maxSellPower',
    transformerType: 'number',
    unit: 'W',
    min: 0,
    max: 15000,
    step: 100,
    roleType: 'User',
  },
  {
    apiName: 'ZeroExportLimit',
    storePath: 'systemWorkMode.zeroExportLimit',
    transformerType: 'number',
    unit: 'W',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'ForceOffGrid',
    storePath: 'systemWorkMode.forceOffGrid',
    transformerType: 'boolean',
    roleType: 'User',
  },
  // GridPeakShaving
  {
    apiName: 'GridPeakShavingEnabled',
    storePath: 'systemWorkMode.gridPeakShavingEnabled',
    transformerType: 'boolean',
    roleType: 'User',
  },
  {
    apiName: 'GridPeakShavingLimitPower',
    storePath: 'systemWorkMode.gridPeakShavingLimitPower',
    transformerType: 'number',
    unit: 'W',
    min: 0,
    max: 15000,
    step: 100,
    roleType: 'User',
  },
];

/**
 * TimeOfUse Mappings
 */
export const TIME_OF_USE_MAPPING: ParameterMapping[] = [
  {
    apiName: 'TimeOfUseEnabled',
    storePath: 'timeOfUse.enabled',
    transformerType: 'boolean',
    roleType: 'User',
  },
  // Active Days - uses nativeBoolean because ActiveDays type is JavaScript boolean
  {
    apiName: 'Monday',
    storePath: 'timeOfUse.activeDays.monday',
    transformerType: 'nativeBoolean',
    roleType: 'User',
  },
  {
    apiName: 'Tuesday',
    storePath: 'timeOfUse.activeDays.tuesday',
    transformerType: 'nativeBoolean',
    roleType: 'User',
  },
  {
    apiName: 'Wednesday',
    storePath: 'timeOfUse.activeDays.wednesday',
    transformerType: 'nativeBoolean',
    roleType: 'User',
  },
  {
    apiName: 'Thursday',
    storePath: 'timeOfUse.activeDays.thursday',
    transformerType: 'nativeBoolean',
    roleType: 'User',
  },
  {
    apiName: 'Friday',
    storePath: 'timeOfUse.activeDays.friday',
    transformerType: 'nativeBoolean',
    roleType: 'User',
  },
  {
    apiName: 'Saturday',
    storePath: 'timeOfUse.activeDays.saturday',
    transformerType: 'nativeBoolean',
    roleType: 'User',
  },
  {
    apiName: 'Sunday',
    storePath: 'timeOfUse.activeDays.sunday',
    transformerType: 'nativeBoolean',
    roleType: 'User',
  },
];

/**
 * TimeSlot Mapping Generator
 * Her TimeSlot için mapping oluşturur (1-6)
 */
export const generateTimeSlotMappings = (slotNumber: number): ParameterMapping[] => {
  const prefix = `TimeSlot${slotNumber}`;
  const storePath = `timeOfUse.timeSlots.${slotNumber - 1}`;

  return [
    {
      apiName: `${prefix}Start`,
      storePath: `${storePath}.startTime`,
      transformerType: 'time',
      roleType: 'User',
    },
    {
      apiName: `${prefix}End`,
      storePath: `${storePath}.endTime`,
      transformerType: 'time',
      roleType: 'User',
    },
    {
      apiName: `${prefix}GridCharge`,
      storePath: `${storePath}.gridCharge`,
      transformerType: 'boolean',
      roleType: 'User',
    },
    {
      apiName: `${prefix}GenCharge`,
      storePath: `${storePath}.genCharge`,
      transformerType: 'boolean',
      roleType: 'User',
    },
    {
      apiName: `${prefix}MaxDischarge`,
      storePath: `${storePath}.maxDischarge`,
      transformerType: 'number',
      unit: 'W',
      min: 0,
      max: 15000,
      step: 100,
      roleType: 'User',
    },
    {
      apiName: `${prefix}MinBatterySOC`,
      storePath: `${storePath}.minBatterySOC`,
      transformerType: 'number',
      unit: '%',
      min: 0,
      max: 100,
      step: 1,
      roleType: 'User',
    },
    {
      apiName: `${prefix}MinBatteryVoltage`,
      storePath: `${storePath}.minBatteryVoltage`,
      transformerType: 'number',
      unit: 'V',
      min: 40,
      max: 60,
      step: 0.1,
      roleType: 'User',
    },
  ];
};

// Tüm TimeSlot mappings (1-6)
export const TIME_SLOT_MAPPINGS: ParameterMapping[] = [
  ...generateTimeSlotMappings(1),
  ...generateTimeSlotMappings(2),
  ...generateTimeSlotMappings(3),
  ...generateTimeSlotMappings(4),
  ...generateTimeSlotMappings(5),
  ...generateTimeSlotMappings(6),
];

/**
 * GeneratorAndSmartLoad Mappings
 */
export const GENERATOR_SMART_LOAD_MAPPING: ParameterMapping[] = [
  // GeneratorSettings
  {
    apiName: 'GeneratorCharge',
    storePath: 'generatorAndSmartLoad.generatorSettings.charge',
    transformerType: 'boolean',
    roleType: 'User',
  },
  {
    apiName: 'GeneratorGridCharge',
    storePath: 'generatorAndSmartLoad.generatorSettings.gridCharge',
    transformerType: 'boolean',
    roleType: 'User',
  },
  {
    apiName: 'GeneratorStartSOC',
    storePath: 'generatorAndSmartLoad.generatorSettings.startSOC',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'GeneratorChargeCurrent',
    storePath: 'generatorAndSmartLoad.generatorSettings.chargeCurrent',
    transformerType: 'number',
    unit: 'A',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  // GeneratorPeakShaving
  {
    apiName: 'GeneratorPeakShavingEnabled',
    storePath: 'generatorAndSmartLoad.generatorPeakShaving.enabled',
    transformerType: 'boolean',
    roleType: 'User',
  },
  {
    apiName: 'GeneratorPeakShavingLimitPower',
    storePath: 'generatorAndSmartLoad.generatorPeakShaving.limitPower',
    transformerType: 'number',
    unit: 'W',
    min: 0,
    max: 15000,
    step: 100,
    roleType: 'User',
  },
  // SmartLoad (SmartLoadEnabled is an enum: 0=Generator, 1=SmartLoad, 2=Microinverter)
  {
    apiName: 'SmartLoadEnabled',
    storePath: 'generatorAndSmartLoad.smartLoad.enabled',
    transformerType: 'number',
    roleType: 'User',
  },
  {
    apiName: 'SmartLoadOffSOC',
    storePath: 'generatorAndSmartLoad.smartLoad.offSOC',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'SmartLoadOnSOC',
    storePath: 'generatorAndSmartLoad.smartLoad.onSOC',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'SmartLoadPVPowerThreshold',
    storePath: 'generatorAndSmartLoad.smartLoad.pvPowerThreshold',
    transformerType: 'number',
    unit: 'W',
    min: 0,
    max: 15000,
    step: 100,
    roleType: 'User',
  },
  {
    apiName: 'SmartLoadOpenDelay',
    storePath: 'generatorAndSmartLoad.smartLoad.openDelay',
    transformerType: 'number',
    unit: 'min',
    min: 0,
    max: 1440,
    step: 1,
    roleType: 'User',
  },
  {
    apiName: 'SmartLoadOnGridAlwaysOn',
    storePath: 'generatorAndSmartLoad.smartLoad.onGridAlwaysOn',
    transformerType: 'boolean',
    roleType: 'User',
  },
];

/**
 * GridStandard Mappings (User Level)
 */
export const GRID_STANDARD_MAPPING: ParameterMapping[] = [
  {
    apiName: 'GridStandard',
    storePath: 'gridStandard.standard',
    transformerType: 'string',
    roleType: 'User',
  },
  {
    apiName: 'GridFrequency',
    storePath: 'gridStandard.frequency',
    transformerType: 'number',
    unit: 'Hz',
    min: 45,
    max: 65,
    step: 0.1,
    roleType: 'User',
  },
  {
    apiName: 'PhaseType',
    storePath: 'gridStandard.phaseType',
    transformerType: 'string',
    roleType: 'User',
  },
  {
    apiName: 'GridLevel',
    storePath: 'gridStandard.gridLevel',
    transformerType: 'string',
    roleType: 'User',
  },
  {
    apiName: 'ITSystemNeutralGrounded',
    storePath: 'gridStandard.itSystemNeutralGrounded',
    transformerType: 'boolean',
    roleType: 'User',
  },
];

/**
 * MaintenanceSettings Mappings (Admin Only)
 */
export const MAINTENANCE_SETTINGS_MAPPING: ParameterMapping[] = [
  // BatteryMaintenance
  {
    apiName: 'BatteryControlMode',
    storePath: 'maintenanceSettings.batteryMaintenance.controlMode',
    transformerType: 'string',
    roleType: 'Admin',
  },
  {
    apiName: 'LithiumBatteryWakeUpEnabled',
    storePath: 'maintenanceSettings.batteryMaintenance.lithiumWakeUpEnabled',
    transformerType: 'boolean',
    roleType: 'Admin',
  },
  {
    apiName: 'BatteryResistance',
    storePath: 'maintenanceSettings.batteryMaintenance.resistance',
    transformerType: 'number',
    unit: 'mO',
    min: 0,
    max: 500,
    step: 1,
    roleType: 'Admin',
  },
  {
    apiName: 'ChargeEfficiency',
    storePath: 'maintenanceSettings.batteryMaintenance.chargeEfficiency',
    transformerType: 'number',
    unit: '%',
    min: 80,
    max: 100,
    step: 0.1,
    roleType: 'Admin',
  },
  // GridCodeConfiguration - Protection Parameters
  {
    apiName: 'GridVoltageHigh',
    storePath: 'maintenanceSettings.gridCode.voltageHigh',
    transformerType: 'number',
    unit: 'V',
    min: 240,
    max: 280,
    step: 1,
    roleType: 'Admin',
  },
  {
    apiName: 'GridVoltageLow',
    storePath: 'maintenanceSettings.gridCode.voltageLow',
    transformerType: 'number',
    unit: 'V',
    min: 180,
    max: 220,
    step: 1,
    roleType: 'Admin',
  },
  {
    apiName: 'GridFrequencyHigh',
    storePath: 'maintenanceSettings.gridCode.frequencyHigh',
    transformerType: 'number',
    unit: 'Hz',
    min: 50,
    max: 55,
    step: 0.1,
    roleType: 'Admin',
  },
  {
    apiName: 'GridFrequencyLow',
    storePath: 'maintenanceSettings.gridCode.frequencyLow',
    transformerType: 'number',
    unit: 'Hz',
    min: 45,
    max: 50,
    step: 0.1,
    roleType: 'Admin',
  },
  {
    apiName: 'ReconnectionTime',
    storePath: 'maintenanceSettings.gridCode.reconnectionTime',
    transformerType: 'number',
    unit: 's',
    min: 0,
    max: 600,
    step: 10,
    roleType: 'Admin',
  },
  // Power Factor Settings
  {
    apiName: 'PowerFactorTarget',
    storePath: 'maintenanceSettings.gridCode.powerFactorTarget',
    transformerType: 'number',
    unit: '',
    min: 0.8,
    max: 1.0,
    step: 0.01,
    roleType: 'Admin',
  },
  {
    apiName: 'ActivePowerRegulation',
    storePath: 'maintenanceSettings.gridCode.activePowerRegulation',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'Admin',
  },
  {
    apiName: 'ReactivePowerRegulation',
    storePath: 'maintenanceSettings.gridCode.reactivePowerRegulation',
    transformerType: 'number',
    unit: '%',
    min: 0,
    max: 100,
    step: 1,
    roleType: 'Admin',
  },
  // AdvancedFunctions
  {
    apiName: 'ArcFaultDetectionEnabled',
    storePath: 'maintenanceSettings.advanced.arcFaultDetection',
    transformerType: 'boolean',
    roleType: 'Admin',
  },
  {
    apiName: 'IslandProtectionEnabled',
    storePath: 'maintenanceSettings.advanced.islandProtection',
    transformerType: 'boolean',
    roleType: 'Admin',
  },
  {
    apiName: 'LithiumBatteryLossFaultEnabled',
    storePath: 'maintenanceSettings.advanced.lithiumBatteryLossFaultEnabled',
    transformerType: 'boolean',
    roleType: 'Admin',
  },
  // Parallel Settings
  {
    apiName: 'ParallelEnabled',
    storePath: 'maintenanceSettings.advanced.parallelEnabled',
    transformerType: 'boolean',
    roleType: 'Admin',
  },
  {
    apiName: 'ParallelRole',
    storePath: 'maintenanceSettings.advanced.parallelRole',
    transformerType: 'string',
    roleType: 'Admin',
  },
  {
    apiName: 'ParallelPhase',
    storePath: 'maintenanceSettings.advanced.parallelPhase',
    transformerType: 'string',
    roleType: 'Admin',
  },
  {
    apiName: 'ModbusAddress',
    storePath: 'maintenanceSettings.advanced.modbusAddress',
    transformerType: 'number',
    unit: '',
    min: 0,
    max: 63,
    step: 1,
    roleType: 'Admin',
  },
  // CT Settings
  {
    apiName: 'ExternalCTRatio',
    storePath: 'maintenanceSettings.advanced.externalCTRatio',
    transformerType: 'number',
    unit: '',
    min: 1,
    max: 5000,
    step: 1,
    roleType: 'Admin',
  },
  {
    apiName: 'ExternalMeterCTRatio',
    storePath: 'maintenanceSettings.advanced.externalMeterCTRatio',
    transformerType: 'number',
    unit: '',
    min: 1,
    max: 100,
    step: 1,
    roleType: 'Admin',
  },
  // External Relay Configuration
  {
    apiName: 'ExternalRelayFeatureEnabled',
    storePath: 'maintenanceSettings.externalRelay.enabled',
    transformerType: 'boolean',
    roleType: 'Admin',
  },
  {
    apiName: 'RelayBitmap',
    storePath: 'maintenanceSettings.externalRelay.bitmap',
    transformerType: 'number',
    unit: '',
    min: 0,
    max: 65535,
    step: 1,
    roleType: 'Admin',
  },
  // Generator Override
  {
    apiName: 'ForceSmartLoadOn',
    storePath: 'maintenanceSettings.generatorOverride.forceSmartLoadOn',
    transformerType: 'boolean',
    roleType: 'Admin',
  },
];

/**
 * All Parameter Mappings (User level only)
 */
export const ALL_USER_MAPPINGS: ParameterMapping[] = [
  ...BATTERY_SETTINGS_MAPPING,
  ...SYSTEM_WORK_MODE_MAPPING,
  ...GRID_STANDARD_MAPPING,
  ...TIME_OF_USE_MAPPING,
  ...TIME_SLOT_MAPPINGS,
  ...GENERATOR_SMART_LOAD_MAPPING,
];

/**
 * All Parameter Mappings (including Admin)
 */
export const ALL_MAPPINGS: ParameterMapping[] = [
  ...ALL_USER_MAPPINGS,
  ...MAINTENANCE_SETTINGS_MAPPING,
];

/**
 * Find mapping by API parameter name
 */
export const findMappingByApiName = (apiName: string): ParameterMapping | undefined => {
  return ALL_MAPPINGS.find(m => m.apiName === apiName);
};

/**
 * Find mapping by store path
 */
export const findMappingByStorePath = (storePath: string): ParameterMapping | undefined => {
  return ALL_MAPPINGS.find(m => m.storePath === storePath);
};

/**
 * Get mappings by role
 */
export const getMappingsByRole = (role: 'User' | 'Admin'): ParameterMapping[] => {
  if (role === 'Admin') {
    return ALL_MAPPINGS;
  }
  return ALL_USER_MAPPINGS;
};

/**
 * Group names for display
 */
export const PARAMETER_GROUPS = {
  BATTERY_SETTINGS: 'BatterySettings',
  SYSTEM_WORK_MODE: 'SystemWorkMode',
  GRID_STANDARD: 'GridStandard',
  TIME_OF_USE: 'TimeOfUse',
  GENERATOR_SMART_LOAD: 'GeneratorAndSmartLoad',
  MAINTENANCE_SETTINGS: 'MaintenanceSettings',
} as const;

/**
 * Group display order (sortOrder from API)
 */
export const GROUP_SORT_ORDER: Record<string, number> = {
  [PARAMETER_GROUPS.BATTERY_SETTINGS]: 1,
  [PARAMETER_GROUPS.SYSTEM_WORK_MODE]: 2,
  [PARAMETER_GROUPS.GRID_STANDARD]: 3,
  [PARAMETER_GROUPS.TIME_OF_USE]: 4,
  [PARAMETER_GROUPS.GENERATOR_SMART_LOAD]: 5,
  [PARAMETER_GROUPS.MAINTENANCE_SETTINGS]: 6,
};

/**
 * Grid Config Parameters - These cause device restart when changed
 * IMPORTANT: Changing any of these parameters will cause the device to restart.
 * The frontend should warn the user before saving and show a lockout overlay
 * during the restart period (~2 minutes).
 */
export const GRID_CONFIG_PARAMETERS = new Set([
  // From setGridCodeConfig RPC
  'GridStandard',
  'GridFrequency',
  'GridVoltageHigh',
  'GridVoltageLow',
  'GridFrequencyHigh',
  'GridFrequencyLow',
  'ReconnectionTime',
  'PowerFactorTarget',
  'ActivePowerRegulation',
  'ReactivePowerRegulation',
  // From setSystemConnectionStructure RPC
  'PhaseType',
  'GridLevel',
  'ITSystemNeutralGrounded',
]);

/**
 * Store paths that correspond to grid config parameters
 * Used to detect if any dirty fields are grid config related
 */
export const GRID_CONFIG_STORE_PATHS = new Set([
  'gridStandard.standard',
  'gridStandard.frequency',
  'gridStandard.phaseType',
  'gridStandard.gridLevel',
  'gridStandard.itSystemNeutralGrounded',
  // Admin-level grid code settings
  'maintenanceSettings.gridCode.voltageHigh',
  'maintenanceSettings.gridCode.voltageLow',
  'maintenanceSettings.gridCode.frequencyHigh',
  'maintenanceSettings.gridCode.frequencyLow',
  'maintenanceSettings.gridCode.reconnectionTime',
  'maintenanceSettings.gridCode.powerFactorTarget',
  'maintenanceSettings.gridCode.activePowerRegulation',
  'maintenanceSettings.gridCode.reactivePowerRegulation',
]);

/**
 * Check if a parameter API name is a grid config parameter
 */
export const isGridConfigParameter = (apiName: string): boolean => {
  return GRID_CONFIG_PARAMETERS.has(apiName);
};

/**
 * Check if a store path corresponds to a grid config parameter
 */
export const isGridConfigStorePath = (storePath: string): boolean => {
  return GRID_CONFIG_STORE_PATHS.has(storePath);
};
