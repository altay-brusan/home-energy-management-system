import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  SiteDto,
  CreateSiteCommand,
  UpdateSiteCommand,
  GetSitesParams,
  PaginatedSiteList,
  SitesDashboardDto,
} from '../types/sites.types';

export const sitesService = {
  async getAll(params: GetSitesParams): Promise<PaginatedSiteList> {
    console.log('🟢 [SitesService] GET /Sites with params:', params);
    console.log('🟢 [SitesService] Full URL:', ENDPOINTS.SITES.GET_ALL);

    const response = await apiClient.get<PaginatedSiteList>(
      ENDPOINTS.SITES.GET_ALL,
      { params }
    );

    console.log('🟢 [SitesService] Response status:', response.status);
    console.log('🟢 [SitesService] Response data:', response.data);

    return response.data;
  },

  async getById(id: string): Promise<SiteDto> {
    const url = ENDPOINTS.SITES.GET_BY_ID.replace(':id', id);
    const response = await apiClient.get<SiteDto>(url);
    return response.data;
  },

  async getUserSites(): Promise<SitesDashboardDto> {
    const response = await apiClient.get<SitesDashboardDto>(
      ENDPOINTS.SITES.GET_USER_SITES
    );
    return response.data;
  },

  async create(data: CreateSiteCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.SITES.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateSiteCommand): Promise<void> {
    const url = ENDPOINTS.SITES.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async deleteMobile(id: string): Promise<void> {
    const url = ENDPOINTS.SITES.DELETE_MOBILE.replace(':id', id);
    await apiClient.delete(url);
  },

  async deleteAdmin(id: string): Promise<void> {
    const url = ENDPOINTS.SITES.DELETE_ADMIN.replace(':id', id);
    await apiClient.delete(url);
  },
};

