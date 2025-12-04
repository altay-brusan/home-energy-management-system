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
  totalBuyPrice: number; // Total cost of buying from grid
  totalSellPrice: number; // Total revenue from selling to grid
  revenue: number; // Net revenue/cost (totalSellPrice - totalBuyPrice)
  buyPricePerKWh: number; // Price per kWh for buying from grid
  sellPricePerKWh: number; // Price per kWh for selling to grid
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
