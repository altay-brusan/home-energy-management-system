/**
 * Power Metrics Types
 * Types for power and energy data across different time periods
 */

/**
 * Raw power data point (15-minute intervals)
 * Power values are in Watts (W)
 */
export interface PowerDataPoint {
  timestamp: string;
  solarPower: number; // Watts
  batteryPower: number; // Watts (positive = discharge, negative = charge)
  gridPower: number; // Watts (positive = import, negative = export)
  loadPower: number; // Watts
  unit: string; // "W"
}

/**
 * 24-hour power metrics data
 * Contains 96 data points (15-minute intervals) for a full day
 */
export interface PowerMetrics24HData {
  deviceId: string;
  date: string; // ISO date string
  data: PowerDataPoint[];
  sunriseTime: string; // ISO datetime
  sunsetTime: string; // ISO datetime
}

/**
 * Aggregated energy data point
 * Energy values are in kilowatt-hours (kWh)
 * Used for displaying charts with time-aggregated data
 */
export interface AggregatedEnergyDataPoint {
  timestamp: string; // Start of the period (ISO datetime)
  label: string; // Display label for X-axis (e.g., "10", "Mon", "Jan")
  solarEnergy: number; // kWh (always positive)
  batteryEnergy: number; // kWh (net: positive = discharge, negative = charge)
  gridEnergy: number; // kWh (net: positive = import, negative = export)
  loadEnergy: number; // kWh (always positive)
}

/**
 * Daily power data for aggregation
 */
export interface DailyPowerData {
  date: string; // ISO date string
  data: PowerDataPoint[];
}

/**
 * Monthly power data for yearly aggregation
 */
export interface MonthlyPowerData {
  month: number; // 0-11 (January = 0)
  year: number;
  dailyData: DailyPowerData[];
}

/**
 * Time period type for chart range selection
 */
export type PowerMetricsTimePeriod = 'Day' | 'Week' | 'Month' | 'Year';

/**
 * Chart data for different time periods
 */
export interface PowerMetricsChartData {
  period: PowerMetricsTimePeriod;
  data: AggregatedEnergyDataPoint[];
  startDate: string; // ISO date string
  endDate: string; // ISO date string
}

/**
 * Power metric visibility configuration
 */
export interface MetricVisibility {
  solar: boolean;
  battery: boolean;
  grid: boolean;
  load: boolean;
}

/**
 * Power metric colors
 */
export interface PowerMetricColors {
  solar: string;
  battery: string;
  grid: string;
  load: string;
}
