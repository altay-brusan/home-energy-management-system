import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import { MobileDashboardDto, WeatherForecastDto, SocResponseDto } from '../types/dashboard.types';
import { PowerMetricsResponseDto } from './mobileDashboardService';

export const dashboardService = {
  async getDashboard(): Promise<MobileDashboardDto> {
    const response = await apiClient.get<MobileDashboardDto>(
      ENDPOINTS.DASHBOARD.GET_MOBILE
    );
    return response.data;
  },

  async getDeviceComponentData(deviceId: string, componentId: string): Promise<any> {
    const url = ENDPOINTS.DASHBOARD.GET_COMPONENT_DATA
      .replace(':deviceId', deviceId)
      .replace(':componentId', componentId);
    const response = await apiClient.get(url);
    return response.data;
  },
  async getWeatherForecast(siteId: string): Promise<WeatherForecastDto[]> {
  const url = ENDPOINTS.DASHBOARD.GET_WEATHER_FORECAST.replace(':siteId', siteId);
  const response = await apiClient.get<WeatherForecastDto[]>(url);
  return response.data;
},

  async getDeviceSoc(deviceId: string): Promise<number | null> {
    const response = await apiClient.get<SocResponseDto>(
      ENDPOINTS.DASHBOARD.SOC,
      {
        params: { deviceId }
      }
    );

    // inverterBatChannelSoc array'inden SOC değerini döndür
    if (response.data.inverterBatChannelSoc && response.data.inverterBatChannelSoc.length > 0) {
      return response.data.inverterBatChannelSoc[0].socPercent;
    }

    return null;
  },

  async getPowerAndEnergyMetrics(deviceId: string, period: string): Promise<PowerMetricsResponseDto> {
    const url = `${ENDPOINTS.DASHBOARD.POWER_AND_ENERGY_METRICS}?deviceId=${deviceId}&period=${period}`;
    const response = await apiClient.get<PowerMetricsResponseDto>(url);
    return response.data;
  },
};


