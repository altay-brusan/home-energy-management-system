import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';

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
  statistics?: PowerMetricsStatistics;
}

export interface PowerMetricsDataPoint {
  timestamp: string;
  solarPower: number;
  gridPower: number;
  batteryPower: number;
  loadPower: number;
  generatorPower: number;
  gridImportEnergyKWh: number | null;
  gridExportEnergyKWh: number | null;
  generatorEnergyKWh: number | null;
  batteryDischargeEnergyKWh: number | null;
  batteryChargeEnergyKWh: number | null;
  loadEnergyKWh: number | null;
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

export interface TodayEnergySummaryDto {
  deviceId: string;
  dataDateTime: string;
  gridImport: number;
  gridExport: number;
  consumption: number;
  production: number;
}

export const mobileDashboardService = {
  // Get hero metrics
  async getHero (deviceId: string): Promise<HeroResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.HERO}?deviceId=${deviceId}`;
    const response = await apiClient.get<HeroResponseDto>(url);
    return response.data;
  },

  // Get SOC data
  async getSOC (deviceId: string): Promise<SOCResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.SOC}?deviceId=${deviceId}`;
    const response = await apiClient.get<SOCResponseDto>(url);
    return response.data;
  },

  // Get SOH data
  async getSOH (deviceId: string): Promise<SOHResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.SOH}?deviceId=${deviceId}`;
    const response = await apiClient.get<SOHResponseDto>(url);
    return response.data;
  },

  // Get device with data
  async getDeviceWithData (deviceId: string): Promise<DeviceWithDataResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.GET_DEVICE_WITH_DATA}?deviceId=${deviceId}`;
    const response = await apiClient.get<DeviceWithDataResponseDto>(url);
    return response.data;
  },

  // Get component live data
  async getComponentLiveData (deviceId: string, componentId: string): Promise<GetComponentLiveDataResponse> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.GET_COMPONENT_LIVE_DATA}?deviceId=${deviceId}&componentId=${componentId}`;
    const response = await apiClient.get<GetComponentLiveDataResponse>(url);
    return response.data;
  },

  // Get energy flow data
  async getEnergyFlow (deviceId: string, startDate: string, endDate: string): Promise<EnergyFlowResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.ENERGY_FLOW}?deviceId=${deviceId}&startDate=${startDate}&endDate=${endDate}`;
    const response = await apiClient.get<EnergyFlowResponseDto>(url);
    return response.data;
  },

  // Get energy production data
  async getEnergyProduction (deviceId: string, period: string): Promise<EnergyProductionResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.ENERGY_PRODUCTION}?deviceId=${deviceId}&period=${period}`;
    const response = await apiClient.get<EnergyProductionResponseDto>(url);
    return response.data;
  },

  // Get energy consumption data
  async getEnergyConsumption (deviceId: string, period: string): Promise<EnergyConsumptionResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.ENERGY_CONSUMPTION}?deviceId=${deviceId}&period=${period}`;
    const response = await apiClient.get<EnergyConsumptionResponseDto>(url);
    return response.data;
  },

  // Get power metrics
  async getPowerMetrics (deviceId: string, period: string): Promise<PowerMetricsResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.POWER_METRICS}?deviceId=${deviceId}&period=${period}`;
    const response = await apiClient.get<PowerMetricsResponseDto>(url);
    return response.data;
  },

  // Get battery data
  async getBattery (deviceId: string): Promise<BatteryResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.BATTERY}?deviceId=${deviceId}`;
    const response = await apiClient.get<BatteryResponseDto>(url);
    return response.data;
  },

  // Get charge discharge data
  async getChargeDischarge (deviceId: string): Promise<ChargeDischargeResponseDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.CHARGE_DISCHARGE}?deviceId=${deviceId}`;
    const response = await apiClient.get<ChargeDischargeResponseDto>(url);
    return response.data;
  },

  // Get battery component data
  async getBatteryComponent (deviceId: string): Promise<BatteryComponentDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.BATTERY_COMPONENT}?deviceId=${deviceId}`;
    const response = await apiClient.get<BatteryComponentDto>(url);
    return response.data;
  },

  // Get inverter component data
  async getInverterComponent (deviceId: string): Promise<InverterComponentDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.INVERTER_COMPONENT}?deviceId=${deviceId}`;
    const response = await apiClient.get<InverterComponentDto>(url);
    return response.data;
  },
  async getSetupDevice (siteId: string): Promise<any> {
    if (!siteId) throw new Error('siteId parametresi gerekli');

    const url = `${ENDPOINTS.SETUPS.GET_SETUP_DEVICE}?siteId=${siteId}`;
    const response = await apiClient.get(url);
    return response.data;
  },

  // Get today's energy summary
  async getTodaySummary (deviceId: string): Promise<TodayEnergySummaryDto> {
    const url = `${ENDPOINTS.MOBILE_DASHBOARD.TODAY_SUMMARY}?deviceId=${deviceId}`;
    const response = await apiClient.get<TodayEnergySummaryDto>(url);
    return response.data;
  },

};