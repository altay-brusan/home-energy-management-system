import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  DeviceDto,
  CreateDeviceCommand,
  UpdateDeviceCommand,
  DeviceLoginCommand,
  DeviceLoginResult,
} from '../types/devices.types';
import {
  DeviceComponentDto,
  CreateDeviceComponentCommand,
  UpdateDeviceComponentCommand,
  GetDeviceComponentParams,
} from '../types/deviceComponents.types';

export const devicesService = {
  async getById(id: string): Promise<DeviceDto> {
    const url = ENDPOINTS.DEVICES.GET_BY_ID.replace(':id', id);
    const response = await apiClient.get<DeviceDto>(url);
    return response.data;
  },

  async create(data: CreateDeviceCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.DEVICES.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateDeviceCommand): Promise<void> {
    const url = ENDPOINTS.DEVICES.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async delete(id: string): Promise<void> {
    const url = ENDPOINTS.DEVICES.DELETE.replace(':id', id);
    await apiClient.delete(url);
  },

  async login(data: DeviceLoginCommand): Promise<DeviceLoginResult> {
    const response = await apiClient.post<DeviceLoginResult>(
      ENDPOINTS.DEVICES.LOGIN,
      data
    );
    return response.data;
  },
};

export const deviceComponentsService = {
  async getByDeviceId(params: GetDeviceComponentParams): Promise<DeviceComponentDto[]> {
    const url = ENDPOINTS.DEVICE_COMPONENTS.GET_BY_DEVICE.replace(
      ':deviceId',
      params.deviceId
    );
    const response = await apiClient.get<DeviceComponentDto[]>(url, {
      params: { isDeleted: params.isDeleted },
    });
    return response.data;
  },

  async getById(id: string): Promise<DeviceComponentDto> {
    const url = ENDPOINTS.DEVICE_COMPONENTS.GET_BY_ID.replace(':id', id);
    const response = await apiClient.get<DeviceComponentDto>(url);
    return response.data;
  },

  async create(data: CreateDeviceComponentCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.DEVICE_COMPONENTS.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateDeviceComponentCommand): Promise<void> {
    const url = ENDPOINTS.DEVICE_COMPONENTS.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async delete(id: string): Promise<void> {
    const url = ENDPOINTS.DEVICE_COMPONENTS.DELETE.replace(':id', id);
    await apiClient.delete(url);
  },
};

