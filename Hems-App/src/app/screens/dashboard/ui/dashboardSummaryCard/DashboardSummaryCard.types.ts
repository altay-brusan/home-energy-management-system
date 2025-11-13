export interface EnergySource {
  id: string;
  label: string;
  value: number;
  percentage: number;
  color: string;
}

export interface DashboardSummaryData {
  totalEnergy: number;
  sources: EnergySource[];
  status: 'onGrid' | 'off' | 'hybrid';
  savingPercentage: number;
}

export interface DashboardSummaryCardProps {
  deviceId?: string;
}