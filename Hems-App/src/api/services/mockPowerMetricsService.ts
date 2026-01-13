import type { PowerMetrics24HResponseDto, PowerMetrics24HDataPoint } from '../types/mobileDashboard.types';

/**
 * Mock service to generate realistic 24-hour power metrics data
 * Simulates solar, battery, grid, and load power for a residential energy system
 */

// Helper function to generate realistic solar power (bell curve during daylight)
const generateSolarPower = (hour: number, sunriseHour: number, sunsetHour: number): number => {
  if (hour < sunriseHour || hour > sunsetHour) {
    return 0; // No solar power at night
  }

  // Calculate solar intensity as bell curve centered at solar noon
  const dayLength = sunsetHour - sunriseHour;
  const solarNoon = sunriseHour + dayLength / 2;
  const hoursSinceSolarNoon = Math.abs(hour - solarNoon);

  // Peak solar power around 5000W, falling off as we move away from solar noon
  const peakPower = 5000;
  const falloffFactor = Math.pow(Math.cos((hoursSinceSolarNoon / (dayLength / 2)) * (Math.PI / 2)), 2);

  // Add some random variation (±10%)
  const variation = 1 + (Math.random() - 0.5) * 0.2;

  return Math.round(peakPower * falloffFactor * variation);
};

// Helper function to generate realistic load power (baseline with variations)
const generateLoadPower = (hour: number): number => {
  // Base load around 2000-2500W
  const baseLoad = 2200;

  // Higher load during morning (6-9) and evening (18-23) hours
  let multiplier = 1.0;
  if (hour >= 6 && hour <= 9) {
    multiplier = 1.3; // Morning activity
  } else if (hour >= 18 && hour <= 23) {
    multiplier = 1.4; // Evening activity (cooking, TV, etc.)
  } else if (hour >= 0 && hour <= 5) {
    multiplier = 0.8; // Night-time low usage
  }

  // Add random variation (±15%)
  const variation = 1 + (Math.random() - 0.5) * 0.3;

  return Math.round(baseLoad * multiplier * variation);
};

// Helper function to generate battery power based on solar and load
const generateBatteryPower = (solarPower: number, loadPower: number, hour: number, sunriseHour: number, sunsetHour: number): number => {
  const surplus = solarPower - loadPower;

  if (hour >= sunriseHour && hour <= sunsetHour) {
    // During daytime
    if (surplus > 500) {
      // Excess solar - charge battery (negative value)
      return -Math.round(Math.min(surplus * 0.7, 3000)); // Max 3kW charge rate
    } else if (surplus < -200) {
      // Solar insufficient - discharge battery (positive value)
      return Math.round(Math.min(Math.abs(surplus) * 0.5, 2000)); // Max 2kW discharge
    }
    return 0; // Balanced
  } else {
    // Night-time - discharge battery to support load
    const dischargeAmount = Math.min(loadPower * 0.4, 2500); // Max 2.5kW discharge
    return Math.round(dischargeAmount);
  }
};

// Helper function to generate grid power (import/export)
const generateGridPower = (solarPower: number, batteryPower: number, loadPower: number): number => {
  // Grid power = Load - Solar - Battery discharge (or + Battery charge)
  const netPower = loadPower - solarPower - batteryPower;

  // Add small variation
  const variation = 1 + (Math.random() - 0.5) * 0.1;

  return Math.round(netPower * variation);
};

/**
 * Generate mock 24-hour power metrics data
 * @param deviceId - Device identifier
 * @param date - Date for the data (defaults to today)
 * @returns PowerMetrics24HResponseDto with hourly data points
 */
export const getMockPowerMetrics24H = async (
  deviceId: string,
  date?: Date
): Promise<PowerMetrics24HResponseDto> => {
  const targetDate = date || new Date();

  // Simulate sunrise/sunset times (example: 6:30 AM, 6:45 PM)
  const sunriseHour = 6.5; // 6:30 AM
  const sunsetHour = 18.75; // 6:45 PM

  const sunriseDate = new Date(targetDate);
  sunriseDate.setHours(6, 30, 0, 0);

  const sunsetDate = new Date(targetDate);
  sunsetDate.setHours(18, 45, 0, 0);

  // Generate 24 hourly data points
  const data: PowerMetrics24HDataPoint[] = [];

  for (let hour = 0; hour < 24; hour++) {
    const timestamp = new Date(targetDate);
    timestamp.setHours(hour, 0, 0, 0);

    // Generate power values with realistic relationships
    const solarPower = generateSolarPower(hour, sunriseHour, sunsetHour);
    const loadPower = generateLoadPower(hour);
    const batteryPower = generateBatteryPower(solarPower, loadPower, hour, sunriseHour, sunsetHour);
    const gridPower = generateGridPower(solarPower, batteryPower, loadPower);

    data.push({
      timestamp: timestamp.toISOString(),
      solarPower,
      gridPower,
      batteryPower,
      loadPower,
      unit: 'W'
    });
  }

  return {
    deviceId,
    date: targetDate.toISOString().split('T')[0],
    data,
    sunriseSunset: {
      sunrise: sunriseDate.toISOString(),
      sunset: sunsetDate.toISOString()
    }
  };
};

/**
 * Simulated API delay
 */
export const mockPowerMetricsService = {
  getPowerMetrics24H: async (deviceId: string, date?: Date): Promise<PowerMetrics24HResponseDto> => {
    // Simulate network delay
    await new Promise<void>(resolve => setTimeout(() => resolve(), 500 + Math.random() * 500));
    return getMockPowerMetrics24H(deviceId, date);
  }
};
