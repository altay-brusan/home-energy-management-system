// Mobile Dashboard API Types

export interface HeroResponseDto {
  title: string;
  subtitle: string;
  metrics: HeroMetrics;
}

export interface HeroMetrics {
  todayEnergy: number;
  currentPower: number;
  lastSync: string;
  unit: string;
}

export interface SOCResponseDto {
  range: string;
  data: SOCDataPoint[];
  statistics: SOCStatistics;
}

export interface SOCDataPoint {
  timestamp: string;
  value: number;
  unit: string;
}

export interface SOCStatistics {
  total: number;
  peak: number;
  average: number;
  unit: string;
}

export interface SOHResponseDto {
  range: string;
  data: SOHDataPoint[];
  statistics: SOHStatistics;
}

export interface SOHDataPoint {
  timestamp: string;
  value: number;
  unit: string;
}

export interface SOHStatistics {
  peak: number;
  average: number;
  unit: string;
}

export interface EnergyFlowResponseDto {
  date: string;
  range: string;
  sources: EnergySource[];
  destinations: EnergyDestination[];
  flows: EnergyFlow[];
}

export interface EnergySource {
  id: string;
  label: string;
  value: number;
  percentage: number;
  color: string;
}

export interface EnergyDestination {
  id: string;
  label: string;
  value: number;
  percentage: number;
  color: string;
}

export interface EnergyFlow {
  source: string;
  target: string;
  value: number;
}

export interface EnergyProductionResponseDto {
  range: string;
  data: EnergyProductionDataPoint[];
  statistics: EnergyProductionStatistics;
}

export interface EnergyProductionDataPoint {
  timestamp: string;
  value: number;
  unit: string;
}

export interface EnergyProductionStatistics {
  total: number;
  peak: number;
  average: number;
  unit: string;
}

export interface EnergyConsumptionResponseDto {
  range: string;
  data: EnergyConsumptionDataPoint[];
  statistics: EnergyConsumptionStatistics;
}

export interface EnergyConsumptionDataPoint {
  timestamp: string;
  value: number;
  unit: string;
}

export interface EnergyConsumptionStatistics {
  total: number;
  peak: number;
  average: number;
  unit: string;
}

export interface PowerMetricsResponseDto {
  range: string;
  data: PowerMetricsDataPoint[];
  statistics: PowerMetricsStatistics;
}

export interface PowerMetricsDataPoint {
  timestamp: string;
  solarPower: number;
  gridPower: number;
  batteryPower: number;
  loadPower: number;
  unit: string;
}

export interface PowerMetricsStatistics {
  averageSolarPower: number;
  peakSolarPower: number;
  averageGridPower: number;
  peakGridPower: number;
  averageBatteryPower: number;
  peakBatteryPower: number;
  averageLoadPower: number;
  peakLoadPower: number;
  unit: string;
}

export interface BatteryResponseDto {
  range: string;
  data: BatteryDataPoint[];
  statistics: BatteryStatistics;
}

export interface BatteryDataPoint {
  timestamp: string;
  value: number;
  unit: string;
}

export interface BatteryStatistics {
  average: number;
  peak: number;
  current: number;
  unit: string;
}

export interface ChargeDischargeResponseDto {
  range: string;
  data: ChargeDischargeDataPoint[];
  statistics: ChargeDischargeStatistics;
}

export interface ChargeDischargeDataPoint {
  timestamp: string;
  chargeValue: number;
  dischargeValue: number;
  unit: string;
}

export interface ChargeDischargeStatistics {
  averageCharge: number;
  peakCharge: number;
  currentCharge: number;
  averageDischarge: number;
  peakDischarge: number;
  currentDischarge: number;
  unit: string;
}

export interface BatteryComponentDto {
  soc: number;
  today: number;
  total: number;
}

export interface InverterComponentDto {
  power: number;
  powerUnit: string;
  today: number;
  total: number;
}

export interface DeviceWithDataResponseDto {
  siteId: string;
  setupId: string;
  deviceId: string;
  modelNumber: string;
  deviceManufacturer: string;
  components: ComponentDto[];
}

export interface ComponentDto {
  componentId: string;
  componentName: string;
  componentDeviceType: string;
  serialNumber: string;
  componentData?: any;
}

export interface GetComponentLiveDataResponse {
  success: boolean;
  message?: string;
  data?: any;
}

export interface DeviceComponentDto {
  componentId: string;
  componentName: string;
  componentType: string;
  isActive: boolean;
  currentValue: number;
  unit: string;
  lastUpdate: string;
}

export interface DeviceStatisticsDto {
  totalComponents: number;
  activeComponents: number;
  averagePower: number;
  peakPower: number;
  totalEnergyToday: number;
  unit: string;
}

// Query Parameters
export interface HeroParams {
  deviceId: string;
}

export interface SOCParams {
  deviceId: string;
}

export interface SOHParams {
  deviceId: string;
}

export interface EnergyFlowParams {
  deviceId: string;
  startDate: string;
  endDate: string;
}

export interface EnergyProductionParams {
  deviceId: string;
  period: string;
}

export interface EnergyConsumptionParams {
  deviceId: string;
  period: string;
}

export interface PowerMetricsParams {
  deviceId: string;
  period: string;
}

export interface BatteryParams {
  deviceId: string;
}

export interface ChargeDischargeParams {
  deviceId: string;
}

export interface BatteryComponentParams {
  deviceId: string;
}

export interface GetComponentLiveDataParams {
  deviceId: string;
  componentId: string;
}
