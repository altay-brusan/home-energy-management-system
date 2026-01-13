import { useState, useCallback } from 'react';
import { devicePriceService } from '../api/services/devicePriceService';
import {
  DevicePriceResponse,
  UpdateDevicePriceRequest,
  DevicePriceDto,
} from '../api/types/devicePrice.types';
import {
  useSettingsStore,
  TimePeriodPrice,
  Currency,
  PricingType,
} from '../store/settingsStore';

/**
 * Helper: Convert API price data to store TimePeriodPrice format
 * API: beginHour/endHour (0-24) -> Store: startTime/endTime (HH:mm)
 */
const apiPriceToStorePeriod = (
  price: DevicePriceDto,
  index: number
): TimePeriodPrice => {
  const formatHour = (hour: number): string => {
    const h = hour === 24 ? 0 : hour;
    return h.toString().padStart(2, '0') + ':00';
  };

  return {
    id: price.id || String(index + 1),
    startTime: formatHour(price.beginHour),
    endTime: formatHour(price.endHour),
    buyPrice: price.buyPrice,
    sellPrice: price.sellPrice,
  };
};

/**
 * Helper: Convert store TimePeriodPrice to API format
 * Store: startTime/endTime (HH:mm) -> API: beginHour/endHour (0-24)
 */
const storePeriodToApiPrice = (
  period: TimePeriodPrice
): { sellPrice: number; buyPrice: number; beginHour: number; endHour: number } => {
  const parseHour = (time: string): number => {
    const [hours] = time.split(':').map(Number);
    return hours;
  };

  let endHour = parseHour(period.endTime);
  // Handle midnight: if endTime is "00:00", it means 24 (end of day)
  if (endHour === 0 && period.startTime !== '00:00') {
    endHour = 24;
  }

  return {
    sellPrice: period.sellPrice,
    buyPrice: period.buyPrice,
    beginHour: parseHour(period.startTime),
    endHour: endHour,
  };
};

export const usePricing = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    pricingType,
    currency,
    fixedBuyPrice,
    fixedSellPrice,
    dynamicPrices,
    hasPricingData,
    setPricingType,
    setCurrency,
    setFixedBuyPrice,
    setFixedSellPrice,
    setDynamicPrices,
    setHasPricingData,
  } = useSettingsStore();

  /**
   * Load pricing data from API and update store
   */
  const loadPricing = useCallback(async (deviceId: string): Promise<boolean> => {
    if (!deviceId) return false;

    setIsLoading(true);
    setError(null);

    try {
      const response: DevicePriceResponse = await devicePriceService.getDevicePrice(deviceId);

      if (!response.success) {
        setError(response.errorMessage || 'Failed to load pricing data');
        setHasPricingData(false);
        return false;
      }

      // Check if user has any pricing data
      if (!response.prices || response.prices.length === 0) {
        setHasPricingData(false);
        return true; // Not an error, just no data
      }

      // Set currency from API
      const apiCurrency = response.currency?.toUpperCase() as Currency;
      if (['EUR', 'USD', 'GBP', 'TRY'].includes(apiCurrency)) {
        setCurrency(apiCurrency);
      }

      // Determine pricing type based on data
      if (response.prices.length === 1 &&
          response.prices[0].beginHour === 0 &&
          response.prices[0].endHour === 24) {
        // Single period covering entire day = Fixed pricing
        setPricingType('fixed');
        setFixedBuyPrice(response.prices[0].buyPrice);
        setFixedSellPrice(response.prices[0].sellPrice);
      } else {
        // Multiple periods = Dynamic pricing
        setPricingType('dynamic');
        const storePeriods = response.prices.map(apiPriceToStorePeriod);
        setDynamicPrices(storePeriods);
      }

      setHasPricingData(true);
      return true;
    } catch (err: any) {
      // If 404 or similar, it means no pricing data exists yet
      if (err?.response?.status === 404) {
        setHasPricingData(false);
        return true;
      }
      setError(err?.message || 'Failed to load pricing data');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [setCurrency, setPricingType, setFixedBuyPrice, setFixedSellPrice, setDynamicPrices, setHasPricingData]);

  /**
   * Save pricing data to API
   */
  const savePricing = useCallback(async (deviceId: string): Promise<boolean> => {
    if (!deviceId) return false;

    setIsSaving(true);
    setError(null);

    try {
      let prices: UpdateDevicePriceRequest['prices'];

      if (pricingType === 'fixed') {
        // Fixed pricing: single period covering entire day
        prices = [{
          sellPrice: fixedSellPrice,
          buyPrice: fixedBuyPrice,
          beginHour: 0,
          endHour: 24,
        }];
      } else {
        // Dynamic pricing: convert all periods
        prices = dynamicPrices.map(storePeriodToApiPrice);
      }

      const request: UpdateDevicePriceRequest = {
        unit: 'kWh',
        currency: currency,
        prices: prices,
      };

      await devicePriceService.updateDevicePrice(deviceId, request);
      setHasPricingData(true);
      return true;
    } catch (err: any) {
      setError(err?.message || 'Failed to save pricing data');
      return false;
    } finally {
      setIsSaving(false);
    }
  }, [pricingType, currency, fixedBuyPrice, fixedSellPrice, dynamicPrices, setHasPricingData]);

  return {
    // State
    isLoading,
    isSaving,
    error,
    hasPricingData,
    // Store values (for convenience)
    pricingType,
    currency,
    fixedBuyPrice,
    fixedSellPrice,
    dynamicPrices,
    // Actions
    loadPricing,
    savePricing,
  };
};
