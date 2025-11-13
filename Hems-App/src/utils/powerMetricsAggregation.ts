/**
 * Power Metrics Aggregation Utilities
 * Converts power (W) measurements into energy (kWh) totals
 */

interface PowerDataPoint {
  timestamp: string;
  solarPower: number;
  batteryPower: number;
  gridPower: number;
  loadPower: number;
  unit: string;
}

export interface AggregatedEnergyDataPoint {
  timestamp: string; // Start of the period
  label: string; // Display label for X-axis
  solarEnergy: number; // kWh
  batteryEnergy: number; // kWh (net: positive = discharge, negative = charge)
  gridEnergy: number; // kWh (net: positive = import, negative = export)
  loadEnergy: number; // kWh (always positive)
}

/**
 * Convert power (W) to energy (kWh) for a time interval
 * @param powerWatts Power in watts
 * @param intervalHours Time interval in hours
 * @returns Energy in kWh
 */
const powerToEnergy = (powerWatts: number, intervalHours: number): number => {
  return (powerWatts * intervalHours) / 1000;
};

/**
 * Aggregate power data points by hour
 * Used for daily view (24 bars)
 * @param dataPoints Array of 15-minute interval power data points
 * @returns Array of hourly aggregated energy data
 */
export const aggregateToHourly = (dataPoints: PowerDataPoint[]): AggregatedEnergyDataPoint[] => {
  const hourlyData: Map<number, PowerDataPoint[]> = new Map();

  // Group data points by hour
  dataPoints.forEach(point => {
    const date = new Date(point.timestamp);
    const hour = date.getHours();

    if (!hourlyData.has(hour)) {
      hourlyData.set(hour, []);
    }
    hourlyData.get(hour)!.push(point);
  });

  // Aggregate each hour (sum energy from all 15-minute intervals)
  const aggregated: AggregatedEnergyDataPoint[] = [];

  for (let hour = 0; hour < 24; hour++) {
    const hourPoints = hourlyData.get(hour) || [];

    // Calculate energy for each metric by summing all 15-minute intervals
    // Each interval is 0.25 hours (15 minutes)
    const solarEnergy = hourPoints.reduce((sum, p) => sum + powerToEnergy(p.solarPower, 0.25), 0);
    const batteryEnergy = hourPoints.reduce((sum, p) => sum + powerToEnergy(p.batteryPower, 0.25), 0);
    const gridEnergy = hourPoints.reduce((sum, p) => sum + powerToEnergy(p.gridPower, 0.25), 0);
    const loadEnergy = hourPoints.reduce((sum, p) => sum + powerToEnergy(p.loadPower, 0.25), 0);

    // Use the first data point's date or create a new one for this hour
    const timestamp = hourPoints.length > 0
      ? new Date(hourPoints[0].timestamp)
      : new Date();
    timestamp.setMinutes(0, 0, 0);

    aggregated.push({
      timestamp: timestamp.toISOString(),
      label: `${hour.toString().padStart(2, '0')}`,
      solarEnergy: Math.round(solarEnergy * 100) / 100,
      batteryEnergy: Math.round(batteryEnergy * 100) / 100,
      gridEnergy: Math.round(gridEnergy * 100) / 100,
      loadEnergy: Math.round(loadEnergy * 100) / 100
    });
  }

  return aggregated;
};

/**
 * Aggregate multiple days of power data by day (for weekly view)
 * Used for weekly views - ensures all 7 days are present
 * @param dailyDataSets Array of daily power data (each with 96 15-minute intervals)
 * @param startDate Start date of the week (Monday)
 * @returns Array of daily aggregated energy data (always 7 days)
 */
export const aggregateToWeekDays = (
  dailyDataSets: Array<{ date: string; data: PowerDataPoint[] }>,
  startDate: Date
): AggregatedEnergyDataPoint[] => {
  const result: AggregatedEnergyDataPoint[] = [];
  const dataMap = new Map(dailyDataSets.map(d => [d.date, d]));

  // Always return 7 days (Monday to Sunday)
  for (let i = 0; i < 7; i++) {
    const currentDate = new Date(startDate);
    currentDate.setDate(startDate.getDate() + i);
    const dateString = currentDate.toISOString().split('T')[0];

    const dayData = dataMap.get(dateString);
    const day = currentDate.getDate();

    if (dayData) {
      // Has data - aggregate it
      const solarEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.solarPower, 0.25), 0);
      const batteryEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.batteryPower, 0.25), 0);
      const gridEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.gridPower, 0.25), 0);
      const loadEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.loadPower, 0.25), 0);

      result.push({
        timestamp: currentDate.toISOString(),
        label: day.toString(),
        solarEnergy: Math.round(solarEnergy * 100) / 100,
        batteryEnergy: Math.round(batteryEnergy * 100) / 100,
        gridEnergy: Math.round(gridEnergy * 100) / 100,
        loadEnergy: Math.round(loadEnergy * 100) / 100
      });
    } else {
      // No data (future date) - return zeros
      result.push({
        timestamp: currentDate.toISOString(),
        label: day.toString(),
        solarEnergy: 0,
        batteryEnergy: 0,
        gridEnergy: 0,
        loadEnergy: 0
      });
    }
  }

  return result;
};

/**
 * Aggregate multiple days of power data by day (for monthly view)
 * Used for monthly views - ensures all days of the month are present
 * @param dailyDataSets Array of daily power data (each with 96 15-minute intervals)
 * @param year Year of the month
 * @param month Month (0-11)
 * @returns Array of daily aggregated energy data (all days in month)
 */
export const aggregateToMonthDays = (
  dailyDataSets: Array<{ date: string; data: PowerDataPoint[] }>,
  year: number,
  month: number
): AggregatedEnergyDataPoint[] => {
  const result: AggregatedEnergyDataPoint[] = [];
  const dataMap = new Map(dailyDataSets.map(d => [d.date, d]));
  const daysInMonth = new Date(year, month + 1, 0).getDate();

  // Always return all days in the month
  for (let day = 1; day <= daysInMonth; day++) {
    const currentDate = new Date(year, month, day);
    const dateString = currentDate.toISOString().split('T')[0];

    const dayData = dataMap.get(dateString);

    if (dayData) {
      // Has data - aggregate it
      const solarEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.solarPower, 0.25), 0);
      const batteryEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.batteryPower, 0.25), 0);
      const gridEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.gridPower, 0.25), 0);
      const loadEnergy = dayData.data.reduce((sum, p) => sum + powerToEnergy(p.loadPower, 0.25), 0);

      result.push({
        timestamp: currentDate.toISOString(),
        label: day.toString(),
        solarEnergy: Math.round(solarEnergy * 100) / 100,
        batteryEnergy: Math.round(batteryEnergy * 100) / 100,
        gridEnergy: Math.round(gridEnergy * 100) / 100,
        loadEnergy: Math.round(loadEnergy * 100) / 100
      });
    } else {
      // No data (future date) - return zeros
      result.push({
        timestamp: currentDate.toISOString(),
        label: day.toString(),
        solarEnergy: 0,
        batteryEnergy: 0,
        gridEnergy: 0,
        loadEnergy: 0
      });
    }
  }

  return result;
};

/**
 * Aggregate multiple days of power data by day (legacy - for backward compatibility)
 * @deprecated Use aggregateToWeekDays or aggregateToMonthDays instead
 */
export const aggregateToDays = (
  dailyDataSets: Array<{ date: string; data: PowerDataPoint[] }>
): AggregatedEnergyDataPoint[] => {
  return dailyDataSets.map(({ date, data }) => {
    // Sum energy from all data points in the day
    const solarEnergy = data.reduce((sum, p) => sum + powerToEnergy(p.solarPower, 0.25), 0);
    const batteryEnergy = data.reduce((sum, p) => sum + powerToEnergy(p.batteryPower, 0.25), 0);
    const gridEnergy = data.reduce((sum, p) => sum + powerToEnergy(p.gridPower, 0.25), 0);
    const loadEnergy = data.reduce((sum, p) => sum + powerToEnergy(p.loadPower, 0.25), 0);

    const dateObj = new Date(date);
    const day = dateObj.getDate();

    return {
      timestamp: dateObj.toISOString(),
      label: day.toString(),
      solarEnergy: Math.round(solarEnergy * 100) / 100,
      batteryEnergy: Math.round(batteryEnergy * 100) / 100,
      gridEnergy: Math.round(gridEnergy * 100) / 100,
      loadEnergy: Math.round(loadEnergy * 100) / 100
    };
  });
};

/**
 * Aggregate multiple months of power data by month
 * Used for yearly view (12 bars) - ensures all 12 months are present
 * @param monthlyDataSets Array of monthly power data grouped by month
 * @param year Year to aggregate
 * @returns Array of monthly aggregated energy data (always 12 months)
 */
export const aggregateToMonths = (
  monthlyDataSets: Array<{
    month: number;
    year: number;
    dailyData: Array<{ date: string; data: PowerDataPoint[] }>
  }>,
  year: number
): AggregatedEnergyDataPoint[] => {
  const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  const result: AggregatedEnergyDataPoint[] = [];
  const dataMap = new Map(monthlyDataSets.map(m => [m.month, m]));

  // Always return all 12 months
  for (let month = 0; month < 12; month++) {
    const monthData = dataMap.get(month);
    const monthDate = new Date(year, month, 1);

    if (monthData && monthData.dailyData.length > 0) {
      // Has data - aggregate it
      let solarEnergy = 0;
      let batteryEnergy = 0;
      let gridEnergy = 0;
      let loadEnergy = 0;

      monthData.dailyData.forEach(({ data }) => {
        solarEnergy += data.reduce((sum, p) => sum + powerToEnergy(p.solarPower, 0.25), 0);
        batteryEnergy += data.reduce((sum, p) => sum + powerToEnergy(p.batteryPower, 0.25), 0);
        gridEnergy += data.reduce((sum, p) => sum + powerToEnergy(p.gridPower, 0.25), 0);
        loadEnergy += data.reduce((sum, p) => sum + powerToEnergy(p.loadPower, 0.25), 0);
      });

      result.push({
        timestamp: monthDate.toISOString(),
        label: monthNames[month],
        solarEnergy: Math.round(solarEnergy * 100) / 100,
        batteryEnergy: Math.round(batteryEnergy * 100) / 100,
        gridEnergy: Math.round(gridEnergy * 100) / 100,
        loadEnergy: Math.round(loadEnergy * 100) / 100
      });
    } else {
      // No data (future month) - return zeros
      result.push({
        timestamp: monthDate.toISOString(),
        label: monthNames[month],
        solarEnergy: 0,
        batteryEnergy: 0,
        gridEnergy: 0,
        loadEnergy: 0
      });
    }
  }

  return result;
};
