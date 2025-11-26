// Grid Performance API Types

export type GridPeriod = 'monthly' | 'yearly' | 'lifetime';

export interface GridPerformanceResponseDto {
  period: GridPeriod;
  data: GridDataPoint[];
  statistics: GridStatistics;
}

export interface GridDataPoint {
  timestamp: string;
  buyEnergy: number; // Grid'den alınan enerji (kWh)
  sellEnergy: number; // Grid'e satılan enerji (kWh)
  unit: string;
}

export interface GridStatistics {
  totalBuy: number; // Toplam grid'den alım (kWh)
  totalSell: number; // Toplam grid'e satım (kWh)
  netBalance: number; // Net denge: sellEnergy - buyEnergy (pozitif = daha fazla satış)
  averageBuy: number; // Ortalama günlük/aylık alım
  averageSell: number; // Ortalama günlük/aylık satım
  peakBuy: number; // En yüksek alım değeri
  peakSell: number; // En yüksek satım değeri
  unit: string;
}

// Request tipi (ihtiyaç halinde)
export interface GetGridPerformanceRequest {
  deviceId: string;
  period: GridPeriod;
}
