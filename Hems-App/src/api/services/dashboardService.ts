import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import { MobileDashboardDto,WeatherForecastDto } from '../types/dashboard.types';

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
  const url = `${ENDPOINTS.DASHBOARD.GET_WEATHER_FORECAST}/${siteId}`;
  const response = await apiClient.get<WeatherForecastDto[]>(url);
  return response.data;
},
};


