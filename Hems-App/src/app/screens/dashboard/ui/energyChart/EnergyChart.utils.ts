/**
 * EnergyChart Utility Functions
 * Data transformation and helper functions for the Energy Chart component
 */

import {PowerMetricsResponseDto, PowerMetricsDataPoint} from '../../../../../api/types/mobileDashboard.types';
import {EnergyChartDataPoint, EnergyPeriod, TooltipData} from './EnergyChart.types';

/**
 * Transforms PowerMetrics API response to chart data format
 * Separates production sources and consumption destinations
 */
export function transformPowerMetricsToChartData(
  response: PowerMetricsResponseDto,
): EnergyChartDataPoint[] {
  return response.data.map((point: PowerMetricsDataPoint) => {
    // Production sources (positive values contribute to production)
    const solarProduction = Math.max(0, point.solarPower);
    const batteryDischarge = Math.max(0, point.batteryPower); // Positive = discharge
    const gridImport = Math.max(0, point.gridPower); // Positive = import
    const generator = 0; // Not available in PowerMetrics, set to 0

    // Consumption destinations (negative values represent consumption)
    const load = Math.max(0, point.loadPower);
    const batteryCharge = Math.abs(Math.min(0, point.batteryPower)); // Negative = charge
    const gridExport = Math.abs(Math.min(0, point.gridPower)); // Negative = export

    // Calculate totals
    const totalProduction = solarProduction + batteryDischarge + gridImport + generator;
    const totalConsumption = load + batteryCharge + gridExport;

    return {
      timestamp: point.timestamp,
      totalProduction,
      totalConsumption,
      sources: {
        solar: solarProduction,
        batteryDischarge,
        generator,
        gridImport,
      },
      destinations: {
        load,
        batteryCharge,
        gridExport,
      },
      unit: point.unit || 'kWh',
    };
  });
}

/**
 * Converts EnergyChartDataPoint to TooltipData format
 */
export function dataPointToTooltip(point: EnergyChartDataPoint): TooltipData {
  return {
    timestamp: point.timestamp,
    production: {
      total: point.totalProduction,
      solar: point.sources.solar,
      batteryDischarge: point.sources.batteryDischarge,
      generator: point.sources.generator,
      gridImport: point.sources.gridImport,
    },
    consumption: {
      total: point.totalConsumption,
      load: point.destinations.load,
      batteryCharge: point.destinations.batteryCharge,
      gridExport: point.destinations.gridExport,
    },
    unit: point.unit,
  };
}

/**
 * Formats a number to display with specified decimal places
 */
export function formatEnergyValue(value: number, decimals: number = 1): string {
  return value.toFixed(decimals);
}

/**
 * Calculates the maximum value from production and consumption for Y-axis scaling
 */
export function getMaxValue(data: EnergyChartDataPoint[]): number {
  if (data.length === 0) return 100;

  const maxProduction = Math.max(...data.map(d => d.totalProduction));
  const maxConsumption = Math.max(...data.map(d => d.totalConsumption));

  return Math.max(maxProduction, maxConsumption);
}

/**
 * Generates Y-axis tick values with nice round numbers
 */
export function generateYAxisTicks(maxValue: number, tickCount: number = 5): number[] {
  if (maxValue === 0) return [0, 20, 40, 60, 80, 100];

  // Find a nice step size
  const rawStep = maxValue / (tickCount - 1);
  const magnitude = Math.pow(10, Math.floor(Math.log10(rawStep)));
  const normalized = rawStep / magnitude;

  let niceStep: number;
  if (normalized < 1.5) {
    niceStep = magnitude;
  } else if (normalized < 3) {
    niceStep = 2 * magnitude;
  } else if (normalized < 7) {
    niceStep = 5 * magnitude;
  } else {
    niceStep = 10 * magnitude;
  }

  const ticks: number[] = [];
  for (let i = 0; i < tickCount; i++) {
    ticks.push(i * niceStep);
  }

  return ticks;
}

/**
 * Formats date for X-axis label based on period
 */
export function formatXAxisLabel(timestamp: string, period: EnergyPeriod, locale: string = 'en-US'): string {
  if (!timestamp) {
    return '';
  }

  // Try parsing the timestamp
  let date = new Date(timestamp);

  // If date is invalid, try parsing it as ISO string
  if (isNaN(date.getTime())) {
    // Try various formats
    const isoMatch = timestamp.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (isoMatch) {
      date = new Date(isoMatch[0]); // Use just the date part
    }
  }

  // Check if date is still invalid
  if (isNaN(date.getTime())) {
    return timestamp.substring(0, 10); // Return first 10 chars if invalid
  }

  switch (period) {
    case 'weekly':
      // Weekly: Show only day + month (e.g., "6 Nov") - ignore hours
      return date.toLocaleDateString(locale, {
        day: 'numeric',
        month: 'short',
      });

    case 'monthly':
      // Monthly: Show day + month (e.g., "15 Nov")
      return date.toLocaleDateString(locale, {
        day: 'numeric',
        month: 'short',
      });

    case 'yearly':
      // Yearly: Show month (e.g., "Nov")
      return date.toLocaleDateString(locale, {
        month: 'short',
      });

    default:
      return date.toLocaleDateString(locale, {
        month: 'short',
        day: 'numeric',
      });
  }
}

/**
 * Formats timestamp for tooltip display
 */
export function formatTooltipDate(timestamp: string, period: EnergyPeriod = 'weekly', locale: string = 'en-US'): string {
  if (!timestamp) {
    return 'No Date';
  }

  // Try parsing the timestamp
  let date = new Date(timestamp);

  // If date is invalid, try parsing it as ISO string
  if (isNaN(date.getTime())) {
    // Try various formats
    const isoMatch = timestamp.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (isoMatch) {
      date = new Date(isoMatch[0]); // Use just the date part
    }
  }

  // Check if date is still invalid
  if (isNaN(date.getTime())) {
    console.warn('Invalid timestamp format:', timestamp);
    return timestamp.substring(0, 20); // Return first 20 chars if invalid
  }

  // Format based on period - NO time for weekly/monthly
  switch (period) {
    case 'weekly':
      // Weekly: Show full date without time (e.g., "Monday, November 6, 2025")
      return date.toLocaleDateString(locale, {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });

    case 'monthly':
      // Monthly: Show full date without time
      return date.toLocaleDateString(locale, {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });

    case 'yearly':
      // Yearly: Show month and year only
      return date.toLocaleDateString(locale, {
        year: 'numeric',
        month: 'long',
      });

    default:
      return date.toLocaleDateString(locale, {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });
  }
}

/**
 * Maps EnergyPeriod to API period parameter
 */
export function periodToApiParam(period: EnergyPeriod): string {
  const periodMap: Record<EnergyPeriod, string> = {
    weekly: 'daily',
    monthly: 'daily',
    yearly: 'monthly',
  };

  return periodMap[period];
}

/**
 * Finds the data point at a given X position in the chart
 */
export function findDataPointAtX(
  touchX: number,
  data: EnergyChartDataPoint[],
  chartWidth: number,
  paddingLeft: number,
): EnergyChartDataPoint | null {
  if (data.length === 0) return null;

  const effectiveX = touchX - paddingLeft;
  const stepX = chartWidth / Math.max(1, data.length - 1);
  const index = Math.round(effectiveX / stepX);

  if (index < 0 || index >= data.length) return null;

  return data[index];
}

/**
 * Creates SVG path data for area chart
 * @param data Array of data points
 * @param getValue Function to extract value from data point
 * @param chartWidth Width of chart area
 * @param chartHeight Height of chart area
 * @param maxValue Maximum value for scaling
 * @returns SVG path string
 */
export function createAreaPath(
  data: EnergyChartDataPoint[],
  getValue: (point: EnergyChartDataPoint) => number,
  chartWidth: number,
  chartHeight: number,
  maxValue: number,
): string {
  if (data.length === 0) return '';

  const points = data.map((point, index) => {
    const x = (index / Math.max(1, data.length - 1)) * chartWidth;
    const value = getValue(point);
    const y = chartHeight - (value / maxValue) * chartHeight;
    return {x, y};
  });

  // Start path at bottom-left
  let path = `M 0,${chartHeight}`;

  // Draw line to first point
  if (points.length > 0) {
    path += ` L ${points[0].x},${points[0].y}`;
  }

  // Draw smooth curve through all points
  for (let i = 1; i < points.length; i++) {
    path += ` L ${points[i].x},${points[i].y}`;
  }

  // Close path at bottom-right
  if (points.length > 0) {
    path += ` L ${points[points.length - 1].x},${chartHeight}`;
  }
  path += ' Z';

  return path;
}
