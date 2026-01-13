import type { TemperatureUnit } from '../store/settingsStore';

/**
 * Convert Celsius to Fahrenheit
 */
export const celsiusToFahrenheit = (celsius: number): number => {
  return Math.round((celsius * 9) / 5 + 32);
};

/**
 * Convert Fahrenheit to Celsius
 */
export const fahrenheitToCelsius = (fahrenheit: number): number => {
  return Math.round(((fahrenheit - 32) * 5) / 9);
};

/**
 * Format temperature based on user's preferred unit
 * @param temperatureCelsius - Temperature in Celsius (API always returns Celsius)
 * @param unit - User's preferred unit ('celsius' | 'fahrenheit')
 * @param includeSymbol - Whether to include the degree symbol (default: true)
 * @returns Formatted temperature string
 */
export const formatTemperature = (
  temperatureCelsius: number | null | undefined,
  unit: TemperatureUnit,
  includeSymbol: boolean = true
): string => {
  if (temperatureCelsius === null || temperatureCelsius === undefined) {
    return includeSymbol ? '--°' : '--';
  }

  const temp = unit === 'fahrenheit'
    ? celsiusToFahrenheit(temperatureCelsius)
    : Math.round(temperatureCelsius);

  if (!includeSymbol) {
    return `${temp}`;
  }

  return unit === 'fahrenheit' ? `${temp}°F` : `${temp}°C`;
};

/**
 * Get temperature unit symbol
 */
export const getTemperatureSymbol = (unit: TemperatureUnit): string => {
  return unit === 'fahrenheit' ? '°F' : '°C';
};
