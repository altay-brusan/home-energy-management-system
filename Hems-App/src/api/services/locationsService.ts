import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  CountryDto,
  CityDto,
} from '../types/locations.types';

export const locationsService = {
  async getCountries(): Promise<CountryDto[]> {
    const response = await apiClient.get<CountryDto[]>(
      ENDPOINTS.LOCATIONS.GET_COUNTRIES
    );
    return response.data;
  },

  async getCitiesByCountry(countryId: string): Promise<CityDto[]> {
    const url = ENDPOINTS.LOCATIONS.GET_CITIES_BY_COUNTRY.replace(':countryId', countryId);
    const response = await apiClient.get<CityDto[]>(url);
    return response.data;
  },
};
