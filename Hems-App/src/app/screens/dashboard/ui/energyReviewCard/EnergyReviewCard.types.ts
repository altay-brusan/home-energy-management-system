export interface EnergyTransaction {
  timestamp: number;
  buy: number; // kWh bought from grid
  sell: number; // kWh sold to grid
  price: number; // Price per kWh
}

export interface EnergyReviewData {
  transactions: EnergyTransaction[];
  weekStart: Date;
  weekEnd: Date;
  totalBuy: number;
  totalSell: number;
  netEnergy: number; // sell - buy
  revenue: number; // Total revenue/cost
}

export interface ChartDimensions {
  width: number;
  height: number;
  paddingTop: number;
  paddingBottom: number;
  paddingLeft: number;
  paddingRight: number;
}

export interface TooltipData {
  x: number;
  y: number;
  transaction: EnergyTransaction;
  visible: boolean;
}
