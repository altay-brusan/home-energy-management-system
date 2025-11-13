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

// Get weather condition for a day (sunny, partly cloudy, cloudy)
// Creates patterns like 2-3 cloudy days in a row
const getDayWeatherMultiplier = (date: Date): number => {
  const dailySeed = date.getFullYear() * 10000 + (date.getMonth() + 1) * 100 + date.getDate();
  const random = seededRandom(dailySeed);

  // Check if previous day was cloudy to create multi-day patterns
  const prevDate = new Date(date);
  prevDate.setDate(date.getDate() - 1);
  const prevSeed = prevDate.getFullYear() * 10000 + (prevDate.getMonth() + 1) * 100 + prevDate.getDate();
  const prevRandom = seededRandom(prevSeed);

  // If previous day was cloudy (< 0.25), higher chance this day is cloudy too
  if (prevRandom < 0.25) {
    // 60% chance to continue cloudy pattern
    if (random < 0.6) {
      return 0.55 + random * 0.2; // Cloudy: 55-75% solar
    }
  }

  // Normal weather distribution
  if (random < 0.15) {
    return 0.5 + random * 0.25; // Cloudy: 50-75% solar (15% of days)
  } else if (random < 0.35) {
    return 0.75 + random * 0.15; // Partly cloudy: 75-90% solar (20% of days)
  } else {
    return 0.95 + random * 0.1; // Sunny: 95-105% solar (65% of days)
  }
};

// Get seasonal multiplier (winter: lower solar, summer: higher solar)
const getSeasonalMultiplier = (date: Date): number => {
  const month = date.getMonth(); // 0-11

  // Northern hemisphere seasonal pattern
  if (month >= 5 && month <= 7) {
    // Summer (Jun, Jul, Aug): +5% to +15%
    return 1.05 + seededRandom(month * 100) * 0.1;
  } else if (month >= 11 || month <= 1) {
    // Winter (Dec, Jan, Feb): -5% to -15%
    return 0.85 + seededRandom(month * 100) * 0.1;
  } else if (month >= 2 && month <= 4) {
    // Spring (Mar, Apr, May): -5% to +5%
    return 0.95 + seededRandom(month * 100) * 0.1;
  } else {
    // Fall (Sep, Oct, Nov): 0% to +10%
    return 1.0 + seededRandom(month * 100) * 0.1;
  }
};

// Helper to generate realistic solar power (bell curve during daylight)
const getSolarPower = (hour: number, minute: number, seed: number, date: Date): number => {
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

  // Apply weather and seasonal variations
  const weatherMultiplier = getDayWeatherMultiplier(date);
  const seasonalMultiplier = getSeasonalMultiplier(date);

  return Math.round(peakPower * falloff * randomVariation * weatherMultiplier * seasonalMultiplier);
};

// Helper to generate realistic load power
const getLoadPower = (hour: number, seed: number, date: Date): number => {
  const baseLoad = 2200;

  let multiplier = 1.0;
  if (hour >= 6 && hour <= 9) {
    multiplier = 1.3; // Morning peak
  } else if (hour >= 18 && hour <= 23) {
    multiplier = 1.4; // Evening peak
  } else if (hour >= 0 && hour <= 5) {
    multiplier = 0.8; // Night low
  }

  // Daily variation: some days use more power (weekends, holidays, etc.)
  const dailySeed = date.getFullYear() * 10000 + (date.getMonth() + 1) * 100 + date.getDate();
  const dayVariation = 0.9 + seededRandom(dailySeed + 500) * 0.2; // 90% to 110%

  // Weekend tends to have different usage pattern
  const dayOfWeek = date.getDay();
  const weekendMultiplier = (dayOfWeek === 0 || dayOfWeek === 6) ? 1.1 : 1.0; // +10% on weekends

  const randomVariation = 0.85 + seededRandom(seed + hour * 50) * 0.3;
  return Math.round(baseLoad * multiplier * randomVariation * dayVariation * weekendMultiplier);
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
      const solarPower = getSolarPower(hour, minute, seed, targetDate);
      const loadPower = getLoadPower(hour, seed, targetDate);
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

/**
 * Get power metrics for a week (7 days)
 * Returns array of daily data starting from the given date (or Monday of the week)
 * Only returns data up to today (no future dates)
 */
export const getPowerMetricsForWeek = (deviceId: string, selectedDate?: Date): Array<{ date: string; data: PowerDataPoint[] }> => {
  const targetDate = selectedDate ? new Date(selectedDate) : new Date();
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  // Find Monday of the week
  const dayOfWeek = targetDate.getDay();
  const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek; // Sunday = 0, Monday = 1
  const monday = new Date(targetDate);
  monday.setDate(targetDate.getDate() + mondayOffset);
  monday.setHours(0, 0, 0, 0);

  const weekData: Array<{ date: string; data: PowerDataPoint[] }> = [];

  // Generate data for 7 days (Monday to Sunday), but only up to today
  for (let i = 0; i < 7; i++) {
    const currentDay = new Date(monday);
    currentDay.setDate(monday.getDate() + i);
    currentDay.setHours(0, 0, 0, 0);

    // Only include days that are today or in the past
    if (currentDay.getTime() <= today.getTime()) {
      const dayData = generate24HourPowerMetrics(deviceId, currentDay);
      weekData.push({
        date: currentDay.toISOString().split('T')[0],
        data: dayData.data
      });
    }
  }

  return weekData;
};

/**
 * Get power metrics for a month
 * Returns array of daily data for all days in the month
 * Only returns data up to today (no future dates)
 */
export const getPowerMetricsForMonth = (deviceId: string, selectedDate?: Date): Array<{ date: string; data: PowerDataPoint[] }> => {
  const targetDate = selectedDate ? new Date(selectedDate) : new Date();
  targetDate.setHours(0, 0, 0, 0);

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const year = targetDate.getFullYear();
  const month = targetDate.getMonth();

  // Get first and last day of the month
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const daysInMonth = lastDay.getDate();

  const monthData: Array<{ date: string; data: PowerDataPoint[] }> = [];

  // Generate data for each day in the month, but only up to today
  for (let day = 1; day <= daysInMonth; day++) {
    const currentDay = new Date(year, month, day);
    currentDay.setHours(0, 0, 0, 0);

    // Only include days that are today or in the past
    if (currentDay.getTime() <= today.getTime()) {
      const dayData = generate24HourPowerMetrics(deviceId, currentDay);

      monthData.push({
        date: currentDay.toISOString().split('T')[0],
        data: dayData.data
      });
    }
  }

  return monthData;
};

/**
 * Get power metrics for a year (12 months)
 * Returns array of monthly data, where each month contains daily data
 * Only returns data up to today (no future months/days)
 */
export const getPowerMetricsForYear = (
  deviceId: string,
  selectedDate?: Date
): Array<{
  month: number;
  year: number;
  dailyData: Array<{ date: string; data: PowerDataPoint[] }>
}> => {
  const targetDate = selectedDate ? new Date(selectedDate) : new Date();
  const year = targetDate.getFullYear();

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const currentMonth = today.getMonth();
  const currentYear = today.getFullYear();

  const yearData: Array<{
    month: number;
    year: number;
    dailyData: Array<{ date: string; data: PowerDataPoint[] }>
  }> = [];

  // Generate data for each month (0-11), but only up to current month if it's current year
  for (let month = 0; month < 12; month++) {
    // Skip future months in the current year
    if (year === currentYear && month > currentMonth) {
      continue;
    }

    const monthlyDailyData: Array<{ date: string; data: PowerDataPoint[] }> = [];

    // Get number of days in this month
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    // Generate data for each day in the month, but only up to today
    for (let day = 1; day <= daysInMonth; day++) {
      const currentDay = new Date(year, month, day);
      currentDay.setHours(0, 0, 0, 0);

      // Only include days that are today or in the past
      if (currentDay.getTime() <= today.getTime()) {
        const dayData = generate24HourPowerMetrics(deviceId, currentDay);

        monthlyDailyData.push({
          date: currentDay.toISOString().split('T')[0],
          data: dayData.data
        });
      }
    }

    // Only add the month if it has data
    if (monthlyDailyData.length > 0) {
      yearData.push({
        month,
        year,
        dailyData: monthlyDailyData
      });
    }
  }

  return yearData;
};

export const powerMetrics24HService = {
  /**
   * Get 24-hour power metrics for a specific date
   * If date is today, updates every 15 minutes
   * If date is in the past, shows full day
   */
  getPowerMetrics24H: async (deviceId: string, selectedDate?: Date): Promise<PowerMetrics24HData> => {
    // Simulate API delay
    await new Promise<void>(resolve => setTimeout(() => resolve(), 300));
    return getPowerMetricsForDate(deviceId, selectedDate);
  },

  /**
   * Get power metrics for a week
   * Returns 7 days of data starting from Monday of the selected week
   */
  getPowerMetricsForWeek: async (
    deviceId: string,
    selectedDate?: Date
  ): Promise<Array<{ date: string; data: PowerDataPoint[] }>> => {
    // Simulate API delay
    await new Promise<void>(resolve => setTimeout(() => resolve(), 400));
    return getPowerMetricsForWeek(deviceId, selectedDate);
  },

  /**
   * Get power metrics for a month
   * Returns all days in the selected month
   */
  getPowerMetricsForMonth: async (
    deviceId: string,
    selectedDate?: Date
  ): Promise<Array<{ date: string; data: PowerDataPoint[] }>> => {
    // Simulate API delay
    await new Promise<void>(resolve => setTimeout(() => resolve(), 500));
    return getPowerMetricsForMonth(deviceId, selectedDate);
  },

  /**
   * Get power metrics for a year
   * Returns 12 months of data, each containing daily data
   */
  getPowerMetricsForYear: async (
    deviceId: string,
    selectedDate?: Date
  ): Promise<Array<{
    month: number;
    year: number;
    dailyData: Array<{ date: string; data: PowerDataPoint[] }>
  }>> => {
    // Simulate API delay
    await new Promise<void>(resolve => setTimeout(() => resolve(), 600));
    return getPowerMetricsForYear(deviceId, selectedDate);
  }
};
