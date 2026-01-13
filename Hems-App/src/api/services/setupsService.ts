import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  SetupDto,
  CreateSetupCommand,
  UpdateSetupCommand,
  GetSetupsParams,
  GetSetupDeviceParams,
  UserSetupDeviceDto,
  PaginatedSetupList,
} from '../types/setups.types';

export const setupsService = {
  async getAll(params: GetSetupsParams): Promise<PaginatedSetupList> {
    const response = await apiClient.get<PaginatedSetupList>(
      ENDPOINTS.SETUPS.GET_ALL,
      { params }
    );
    return response.data;
  },

  async getById(id: string): Promise<SetupDto> {
    const url = ENDPOINTS.SETUPS.GET_BY_ID.replace(':id', id);
    const response = await apiClient.get<SetupDto>(url);
    return response.data;
  },

  async getSetupDevice(params: GetSetupDeviceParams): Promise<UserSetupDeviceDto> {
    const response = await apiClient.get<UserSetupDeviceDto>(
      ENDPOINTS.SETUPS.GET_SETUP_DEVICE,
      { params }
    );
    return response.data;
  },

  async create(data: CreateSetupCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.SETUPS.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateSetupCommand): Promise<void> {
    const url = ENDPOINTS.SETUPS.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async deleteMobile(id: string): Promise<void> {
    const url = ENDPOINTS.SETUPS.DELETE_MOBILE.replace(':id', id);
    await apiClient.delete(url);
  },

  async deleteAdmin(id: string): Promise<void> {
    const url = ENDPOINTS.SETUPS.DELETE_ADMIN.replace(':id', id);
    await apiClient.delete(url);
  },
};

