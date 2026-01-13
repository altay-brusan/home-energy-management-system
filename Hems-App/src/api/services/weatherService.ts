import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  WeatherForecast,
  GetWeatherForecastsParams,
} from '../types/weather.types';

export const weatherService = {
  async getForecasts(params: GetWeatherForecastsParams): Promise<WeatherForecast[]> {
    const url = ENDPOINTS.WEATHER_FORECASTS.GET_BY_SITE.replace(':siteId', params.siteId);
    const response = await apiClient.get<WeatherForecast[]>(url);
    return response.data;
  },
};
