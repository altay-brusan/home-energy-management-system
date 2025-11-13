import { GridPeriod, GridDataPoint, GridStatistics } from '../../../../../api/types/gridPerformance.types';

// Component Props
export interface GridPerformanceCardProps {
  deviceId: string | undefined;
}

// Internal Component State
export interface GridPerformanceState {
  loading: boolean;
  error: string | null;
  selectedPeriod: GridPeriod;
  gridData: GridPerformanceData | null;
  selectedPointIndex: number | null;
}

// Grid Performance Data (from API)
export interface GridPerformanceData {
  period: GridPeriod;
  dataPoints: GridDataPoint[];
  statistics: GridStatistics;
}

// Chart Props
export interface GridChartProps {
  data: GridDataPoint[];
  width: number;
  height: number;
  selectedIndex: number | null;
  onSelectPoint: (index: number | null) => void;
  isDark: boolean;
}

// Summary Card Props
export interface SummaryMetricProps {
  label: string;
  value: string;
  unit: string;
  color: string;
  icon?: string;
  isDark: boolean;
}

// Period Button Props
export interface PeriodButtonProps {
  period: GridPeriod;
  label: string;
  isSelected: boolean;
  onPress: () => void;
  isDark: boolean;
}
