/**
 * 24-hour power metrics service with 15-minute intervals
 * Generates realistic power data for solar, battery, grid, and load
 */

interface PowerDataPoint {
  timestamp: string;
  solarPower: number;
  batteryPower: number;
  gridPower: number;
  loadPower: number;
  unit: string;
}

interface PowerMetrics24HData {
  deviceId: string;
  date: string;
  data: PowerDataPoint[];
  sunriseTime: string;
  sunsetTime: string;
}

// Seeded random number generator for consistent daily data
const seededRandom = (seed: number): number => {
  const x = Math.sin(seed) * 10000;
  return x - Math.floor(x);
};

// Helper to generate realistic solar power (bell curve during daylight)
const getSolarPower = (hour: number, minute: number, seed: number): number => {
  const sunriseHour = 6.5; // 6:30
  const sunsetHour = 18.75; // 18:45
  const currentTime = hour + minute / 60;

  if (currentTime < sunriseHour || currentTime > sunsetHour) {
    return 0;
  }

  const dayLength = sunsetHour - sunriseHour;
  const solarNoon = sunriseHour + dayLength / 2;
  const hoursSinceNoon = Math.abs(currentTime - solarNoon);

  const peakPower = 5000;
  const falloff = Math.pow(Math.cos((hoursSinceNoon / (dayLength / 2)) * (Math.PI / 2)), 2);
  const randomVariation = 0.9 + seededRandom(seed + hour * 100 + minute) * 0.2;

  return Math.round(peakPower * falloff * randomVariation);
};

// Helper to generate realistic load power
const getLoadPower = (hour: number, seed: number): number => {
  const baseLoad = 2200;

  let multiplier = 1.0;
  if (hour >= 6 && hour <= 9) {
    multiplier = 1.3; // Morning peak
  } else if (hour >= 18 && hour <= 23) {
    multiplier = 1.4; // Evening peak
  } else if (hour >= 0 && hour <= 5) {
    multiplier = 0.8; // Night low
  }

  const randomVariation = 0.85 + seededRandom(seed + hour * 50) * 0.3;
  return Math.round(baseLoad * multiplier * randomVariation);
};

// Helper to generate battery power
const getBatteryPower = (solarPower: number, loadPower: number, hour: number): number => {
  const surplus = solarPower - loadPower;
  const sunriseHour = 6.5;
  const sunsetHour = 18.75;

  if (hour >= sunriseHour && hour <= sunsetHour) {
    // Daytime
    if (surplus > 500) {
      // Charging (negative)
      return -Math.round(Math.min(surplus * 0.7, 3000));
    } else if (surplus < -200) {
      // Discharging (positive)
      return Math.round(Math.min(Math.abs(surplus) * 0.5, 2000));
    }
    return 0;
  } else {
    // Night - discharge to support load
    return Math.round(Math.min(loadPower * 0.4, 2500));
  }
};

// Helper to generate grid power
const getGridPower = (solarPower: number, batteryPower: number, loadPower: number, seed: number): number => {
  const netPower = loadPower - solarPower - batteryPower;
  const randomVariation = 0.95 + seededRandom(seed) * 0.1;
  return Math.round(netPower * randomVariation);
};

/**
 * Generate 24-hour power metrics with 15-minute intervals (96 data points)
 * Uses daily seed to ensure consistent data throughout the day
 */
export const generate24HourPowerMetrics = (deviceId: string, date?: Date): PowerMetrics24HData => {
  const data: PowerDataPoint[] = [];
  const targetDate = date ? new Date(date) : new Date();
  targetDate.setHours(0, 0, 0, 0);

  // Daily seed based on date - ensures same data for the entire day
  const dailySeed = targetDate.getFullYear() * 10000 + (targetDate.getMonth() + 1) * 100 + targetDate.getDate();

  // Generate data for every 15 minutes (96 points per day)
  for (let hour = 0; hour < 24; hour++) {
    for (let minute = 0; minute < 60; minute += 15) {
      const timestamp = new Date(targetDate);
      timestamp.setHours(hour, minute, 0, 0);

      const seed = dailySeed + hour * 100 + minute;
      const solarPower = getSolarPower(hour, minute, seed);
      const loadPower = getLoadPower(hour, seed);
      const batteryPower = getBatteryPower(solarPower, loadPower, hour);
      const gridPower = getGridPower(solarPower, batteryPower, loadPower, seed);

      data.push({
        timestamp: timestamp.toISOString(),
        solarPower,
        batteryPower,
        gridPower,
        loadPower,
        unit: 'W'
      });
    }
  }

  const sunriseDate = new Date(targetDate);
  sunriseDate.setHours(6, 30, 0, 0);

  const sunsetDate = new Date(targetDate);
  sunsetDate.setHours(18, 45, 0, 0);

  return {
    deviceId,
    date: targetDate.toISOString().split('T')[0],
    data,
    sunriseTime: sunriseDate.toISOString(),
    sunsetTime: sunsetDate.toISOString()
  };
};

/**
 * Get power metrics for a specific date
 * If date is today, filter up to current time
 * If date is in the past, show full day (96 data points)
 */
export const getPowerMetricsForDate = (deviceId: string, selectedDate?: Date): PowerMetrics24HData => {
  const targetDate = selectedDate ? new Date(selectedDate) : new Date();
  targetDate.setHours(0, 0, 0, 0);

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const fullData = generate24HourPowerMetrics(deviceId, targetDate);

  // If selected date is today, filter up to current time
  if (targetDate.getTime() === today.getTime()) {
    const now = new Date();
    const currentHour = now.getHours();
    const currentMinute = now.getMinutes();
    const currentTotalMinutes = currentHour * 60 + currentMinute;

    const filteredData = fullData.data.filter(point => {
      const pointTime = new Date(point.timestamp);
      const pointHour = pointTime.getHours();
      const pointMinute = pointTime.getMinutes();
      const pointTotalMinutes = pointHour * 60 + pointMinute;

      return pointTotalMinutes <= currentTotalMinutes;
    });

    return {
      ...fullData,
      data: filteredData
    };
  }

  // For past dates, return full day data
  return fullData;
};

export const powerMetrics24HService = {
  /**
   * Get 24-hour power metrics for a specific date
   * If date is today, updates every 15 minutes
   * If date is in the past, shows full day
   */
  getPowerMetrics24H: async (deviceId: string, selectedDate?: Date): Promise<PowerMetrics24HData> => {
    // Simulate API delay
    await new Promise(resolve => setTimeout(resolve, 300));
    return getPowerMetricsForDate(deviceId, selectedDate);
  }
};
