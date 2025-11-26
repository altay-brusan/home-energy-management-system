export interface PowerMetric {
  timestamp: string;
  solar: number;
  battery: number;
  grid: number;
  load: number;
}

export interface MetricConfig {
  key: keyof Omit<PowerMetric, 'timestamp'>;
  label: string;
  color: string;
  visible: boolean;
}

export interface ChartDimensions {
  width: number;
  height: number;
  marginTop: number;
  marginRight: number;
  marginBottom: number;
  marginLeft: number;
}

export interface PowerMetricsChartProps {
  siteId?: string;
  deviceId?: string;
  refreshTrigger?: number;
}

export interface MetricsHeaderProps {
  currentValues: PowerMetric | null;
  isLoading: boolean;
}

export interface ChartLegendProps {
  metrics: MetricConfig[];
  onToggleMetric: (key: string) => void;
}

export interface LineChartProps {
  data: PowerMetric[];
  metrics: MetricConfig[];
  dimensions: ChartDimensions;
}

export interface SunInfoProps {
  sunrise: string;
  sunset: string;
}