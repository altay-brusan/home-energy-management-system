import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import { MobileDashboardDto, WeatherForecastDto, SocResponseDto, EnergyRevenueResponseDto } from '../types/dashboard.types';
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

  async getPowerAndEnergyMetrics(
    deviceId: string,
    period: 'day' | 'week' | 'month' | 'year',
    dateOrYear: string,  // date for day (YYYY-MM-DD), year for others (YYYY)
    weekOrMonth?: number  // week number (1-52) or month number (1-12)
  ): Promise<PowerMetricsResponseDto> {
    let url: string;

    if (period === 'day') {
      // Day: ?deviceId=xxx&period=day&date=2025-12-03
      url = `${ENDPOINTS.DASHBOARD.POWER_AND_ENERGY_METRICS}?deviceId=${deviceId}&period=${period}&date=${dateOrYear}`;
    } else if (period === 'week') {
      // Week: ?deviceId=xxx&period=week&year=2025&week=49
      url = `${ENDPOINTS.DASHBOARD.POWER_AND_ENERGY_METRICS}?deviceId=${deviceId}&period=${period}&year=${dateOrYear}&week=${weekOrMonth}`;
    } else if (period === 'month') {
      // Month: ?deviceId=xxx&period=month&year=2025&month=12
      url = `${ENDPOINTS.DASHBOARD.POWER_AND_ENERGY_METRICS}?deviceId=${deviceId}&period=${period}&year=${dateOrYear}&month=${weekOrMonth}`;
    } else {
      // Year: ?deviceId=xxx&period=year&year=2025
      url = `${ENDPOINTS.DASHBOARD.POWER_AND_ENERGY_METRICS}?deviceId=${deviceId}&period=${period}&year=${dateOrYear}`;
    }

    const response = await apiClient.get<PowerMetricsResponseDto>(url);
    return response.data;
  },

  async getEnergyRevenue(
    deviceId: string,
    period: 'day' | 'week' | 'month' | 'year',
    dateOrYear: string,
    weekOrMonth?: number
  ): Promise<EnergyRevenueResponseDto> {
    let url: string;

    if (period === 'day') {
      // Day: ?deviceId=xxx&period=day&date=2025-12-03
      url = `${ENDPOINTS.DASHBOARD.ENERGY_REVENUE}?deviceId=${deviceId}&period=${period}&date=${dateOrYear}`;
    } else if (period === 'week') {
      // Week: ?deviceId=xxx&period=week&year=2025&week=49
      url = `${ENDPOINTS.DASHBOARD.ENERGY_REVENUE}?deviceId=${deviceId}&period=${period}&year=${dateOrYear}&week=${weekOrMonth}`;
    } else if (period === 'month') {
      // Month: ?deviceId=xxx&period=month&year=2025&month=12
      url = `${ENDPOINTS.DASHBOARD.ENERGY_REVENUE}?deviceId=${deviceId}&period=${period}&year=${dateOrYear}&month=${weekOrMonth}`;
    } else {
      // Year: ?deviceId=xxx&period=year&year=2025
      url = `${ENDPOINTS.DASHBOARD.ENERGY_REVENUE}?deviceId=${deviceId}&period=${period}&year=${dateOrYear}`;
    }

    const response = await apiClient.get<EnergyRevenueResponseDto>(url);
    return response.data;
  },
};


