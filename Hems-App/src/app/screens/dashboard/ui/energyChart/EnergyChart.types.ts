/**
 * EnergyChart Component Types
 * Defines TypeScript interfaces for the Energy Production/Consumption chart
 */

/**
 * Time period selection for the energy chart
 */
export type EnergyPeriod = 'weekly' | 'monthly' | 'yearly';

/**
 * Energy source breakdown for a single data point
 */
export interface EnergySourceBreakdown {
  solar: number;
  batteryDischarge: number;
  generator: number;
  gridImport: number;
}

/**
 * Energy destination/consumption breakdown for a single data point
 */
export interface EnergyDestinationBreakdown {
  load: number;
  batteryCharge: number;
  gridExport: number;
}

/**
 * Complete energy data point for a single timestamp
 * Contains both production and consumption with source breakdowns
 */
export interface EnergyChartDataPoint {
  timestamp: string; // ISO 8601 timestamp
  totalProduction: number; // Sum of all production sources (kWh)
  totalConsumption: number; // Sum of all consumption destinations (kWh)
  sources: EnergySourceBreakdown;
  destinations: EnergyDestinationBreakdown;
  unit: string; // e.g., "kWh"
}

/**
 * Data structure for the tooltip info box
 */
export interface TooltipData {
  timestamp: string;
  production: {
    total: number;
    solar: number;
    batteryDischarge: number;
    generator: number;
    gridImport: number;
  };
  consumption: {
    total: number;
    load: number;
    batteryCharge: number;
    gridExport: number;
  };
  unit: string;
}

/**
 * Chart dimension configuration
 */
export interface ChartDimensions {
  width: number;
  height: number;
  padding: {
    top: number;
    right: number;
    bottom: number;
    left: number;
  };
}

/**
 * Props for the EnergyChart component
 */
export interface EnergyChartProps {
  /**
   * Optional initial period selection
   * @default 'weekly'
   */
  initialPeriod?: EnergyPeriod;

  /**
   * Optional height for the chart container
   * @default 320
   */
  chartHeight?: number;
}
