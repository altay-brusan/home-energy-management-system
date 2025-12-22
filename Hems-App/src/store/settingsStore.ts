import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';

export type TemperatureUnit = 'celsius' | 'fahrenheit';
export type MeasurementSystem = 'metric' | 'imperial';
export type TimeFormat = '12h' | '24h';
export type DateFormat = 'DD/MM/YYYY' | 'MM/DD/YYYY' | 'YYYY-MM-DD';
export type PricingType = 'fixed' | 'dynamic';
export type Currency = 'EUR' | 'USD' | 'GBP' | 'TRY';

export const CURRENCY_SYMBOLS: Record<Currency, string> = {
  EUR: '€',
  USD: '$',
  GBP: '£',
  TRY: '₺',
};

export interface TimePeriodPrice {
  id: string;
  startTime: string; // HH:mm format
  endTime: string;   // HH:mm format
  buyPrice: number;  // €/kWh
  sellPrice: number; // €/kWh
}

type SettingsState = {
  // General settings
  temperatureUnit: TemperatureUnit;
  measurementSystem: MeasurementSystem;
  timeFormat: TimeFormat;
  dateFormat: DateFormat;
  // Pricing settings
  pricingType: PricingType;
  currency: Currency;
  fixedBuyPrice: number;  // per kWh
  fixedSellPrice: number; // per kWh
  dynamicPrices: TimePeriodPrice[];
  // General settings actions
  setTemperatureUnit: (unit: TemperatureUnit) => void;
  setMeasurementSystem: (system: MeasurementSystem) => void;
  setTimeFormat: (format: TimeFormat) => void;
  setDateFormat: (format: DateFormat) => void;
  // Pricing settings actions
  setPricingType: (type: PricingType) => void;
  setCurrency: (currency: Currency) => void;
  setFixedBuyPrice: (price: number) => void;
  setFixedSellPrice: (price: number) => void;
  setDynamicPrices: (prices: TimePeriodPrice[]) => void;
  addDynamicPeriod: (period: TimePeriodPrice) => void;
  updateDynamicPeriod: (id: string, period: Partial<TimePeriodPrice>) => void;
  removeDynamicPeriod: (id: string) => void;
};

const DEFAULT_DYNAMIC_PRICES: TimePeriodPrice[] = [
  { id: '1', startTime: '00:00', endTime: '06:00', buyPrice: 0.15, sellPrice: 0.08 },
  { id: '2', startTime: '06:00', endTime: '22:00', buyPrice: 0.25, sellPrice: 0.12 },
  { id: '3', startTime: '22:00', endTime: '00:00', buyPrice: 0.15, sellPrice: 0.08 },
];

export const useSettingsStore = create<SettingsState>()(
  persist(
    (set, get) => ({
      // General settings defaults
      temperatureUnit: 'celsius',
      measurementSystem: 'metric',
      timeFormat: '24h',
      dateFormat: 'DD/MM/YYYY',
      // Pricing settings defaults
      pricingType: 'fixed',
      currency: 'EUR',
      fixedBuyPrice: 0.25,
      fixedSellPrice: 0.10,
      dynamicPrices: DEFAULT_DYNAMIC_PRICES,
      // General settings actions
      setTemperatureUnit: (temperatureUnit) => set({ temperatureUnit }),
      setMeasurementSystem: (measurementSystem) => set({ measurementSystem }),
      setTimeFormat: (timeFormat) => set({ timeFormat }),
      setDateFormat: (dateFormat) => set({ dateFormat }),
      // Pricing settings actions
      setPricingType: (pricingType) => set({ pricingType }),
      setCurrency: (currency) => set({ currency }),
      setFixedBuyPrice: (fixedBuyPrice) => set({ fixedBuyPrice }),
      setFixedSellPrice: (fixedSellPrice) => set({ fixedSellPrice }),
      setDynamicPrices: (dynamicPrices) => set({ dynamicPrices }),
      addDynamicPeriod: (period) => set({ dynamicPrices: [...get().dynamicPrices, period] }),
      updateDynamicPeriod: (id, updates) => set({
        dynamicPrices: get().dynamicPrices.map((p) =>
          p.id === id ? { ...p, ...updates } : p
        ),
      }),
      removeDynamicPeriod: (id) => set({
        dynamicPrices: get().dynamicPrices.filter((p) => p.id !== id),
      }),
    }),
    {
      name: 'settings-storage',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        temperatureUnit: state.temperatureUnit,
        measurementSystem: state.measurementSystem,
        timeFormat: state.timeFormat,
        dateFormat: state.dateFormat,
        pricingType: state.pricingType,
        currency: state.currency,
        fixedBuyPrice: state.fixedBuyPrice,
        fixedSellPrice: state.fixedSellPrice,
        dynamicPrices: state.dynamicPrices,
      }),
    }
  )
);
