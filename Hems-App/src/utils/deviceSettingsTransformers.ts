/**
 * Device Settings Transformers
 * API değerlerini uygulama değerlerine ve tersi dönüştüren fonksiyonlar
 */

import { OnOffStatus } from '../api/types/inverterModes.types';

// Work Mode Types
export type APIWorkModeType = 'SellingFirst' | 'ZeroExportToLoad' | 'ZeroExportToCT';
export type AppWorkModeType = 'selling_first' | 'zero_export_to_load' | 'zero_export_to_ct';

// Energy Pattern Types
export type APIEnergyPatternType = 'BatteryFirst' | 'LoadFirst';
export type AppEnergyPatternType = 'batt_first' | 'load_first';

/**
 * Export Strategy Transformers
 */
export const exportStrategyToApp = (apiValue: string): AppWorkModeType => {
  const map: Record<string, AppWorkModeType> = {
    'SellingFirst': 'selling_first',
    'ZeroExportToLoad': 'zero_export_to_load',
    'ZeroExportToCT': 'zero_export_to_ct',
  };
  return map[apiValue] || 'zero_export_to_ct';
};

export const exportStrategyToAPI = (appValue: AppWorkModeType): APIWorkModeType => {
  const map: Record<AppWorkModeType, APIWorkModeType> = {
    'selling_first': 'SellingFirst',
    'zero_export_to_load': 'ZeroExportToLoad',
    'zero_export_to_ct': 'ZeroExportToCT',
  };
  return map[appValue];
};

/**
 * Energy Priority Transformers
 */
export const energyPriorityToApp = (apiValue: string): AppEnergyPatternType => {
  return apiValue === 'BatteryFirst' ? 'batt_first' : 'load_first';
};

export const energyPriorityToAPI = (appValue: AppEnergyPatternType): APIEnergyPatternType => {
  return appValue === 'batt_first' ? 'BatteryFirst' : 'LoadFirst';
};

/**
 * Boolean / OnOff Transformers
 */
export const booleanToOnOff = (apiValue: string): OnOffStatus => {
  return apiValue === 'true' ? 'on' : 'off';
};

export const onOffToBoolean = (appValue: OnOffStatus): string => {
  return appValue === 'on' ? 'true' : 'false';
};

/**
 * Numeric Transformers
 */
export const stringToNumber = (apiValue: string): number => {
  const num = parseFloat(apiValue);
  return isNaN(num) ? 0 : num;
};

export const numberToString = (appValue: number): string => {
  return String(appValue);
};

/**
 * Time String Transformer (HH:MM format)
 */
export const timeStringToApp = (apiValue: string): string => {
  // API zaten HH:MM formatında geliyor
  return apiValue || '00:00';
};

export const timeStringToAPI = (appValue: string): string => {
  return appValue || '00:00';
};

/**
 * Transformer Registry
 * Her transformer tipi için fonksiyon referansları
 */
export const transformerRegistry = {
  // API -> App
  toApp: {
    exportStrategy: exportStrategyToApp,
    energyPriority: energyPriorityToApp,
    boolean: booleanToOnOff,
    number: stringToNumber,
    time: timeStringToApp,
    string: (v: string) => v,
  },
  // App -> API
  toAPI: {
    exportStrategy: exportStrategyToAPI,
    energyPriority: energyPriorityToAPI,
    boolean: onOffToBoolean,
    number: numberToString,
    time: timeStringToAPI,
    string: (v: string) => v,
  },
};

/**
 * Generic Transform Helper
 */
export const transformValue = (
  value: string,
  transformerType: keyof typeof transformerRegistry.toApp,
  direction: 'toApp' | 'toAPI' = 'toApp'
): any => {
  const registry = transformerRegistry[direction];
  const transformer = registry[transformerType as keyof typeof registry] as ((v: any) => any) | undefined;
  if (transformer) {
    return transformer(value);
  }
  return value;
};
