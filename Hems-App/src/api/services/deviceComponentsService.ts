import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  DeviceComponentDto,
  CreateDeviceComponentCommand,
  UpdateDeviceComponentCommand,
  GetDeviceComponentsByDeviceIdParams,
  GetDeviceComponentParams,
  GetDeviceComponentByIdParams,
  DeviceSummaryResponseDto,
  DeviceDetailsResponseDto,
} from '../types/deviceComponents.types';

export const deviceComponentsService = {
  // Get device components by device ID
  getDeviceComponentsByDeviceId: async (
    params: GetDeviceComponentsByDeviceIdParams
  ): Promise<DeviceComponentDto[]> => {
    const { deviceId, isDeleted } = params;
    const queryParams = new URLSearchParams();
    
    if (isDeleted !== undefined) {
      queryParams.append('isDeleted', isDeleted.toString());
    }

    const url = `${ENDPOINTS.DEVICE_COMPONENTS.GET_BY_DEVICE.replace(':deviceId', deviceId)}${queryParams.toString() ? `?${queryParams.toString()}` : ''}`;
    const response = await apiClient.get<DeviceComponentDto[]>(url);
    return response.data;
  },

  // Get device component by ID
  getDeviceComponent: async (params: GetDeviceComponentByIdParams): Promise<DeviceComponentDto> => {
    const url = ENDPOINTS.DEVICE_COMPONENTS.GET_BY_ID.replace(':id', params.id);
    const response = await apiClient.get<DeviceComponentDto>(url);
    return response.data;
  },

  // Create device component
  createDeviceComponent: async (
    command: CreateDeviceComponentCommand
  ): Promise<string> => {
    const response = await apiClient.post<string>(ENDPOINTS.DEVICE_COMPONENTS.CREATE, command);
    return response.data;
  },

  // Update device component
  updateDeviceComponent: async (
    id: string,
    command: UpdateDeviceComponentCommand
  ): Promise<void> => {
    const url = ENDPOINTS.DEVICE_COMPONENTS.UPDATE.replace(':id', id);
    await apiClient.put<void>(url, command);
  },

  // Delete device component
  deleteDeviceComponent: async (id: string): Promise<void> => {
    const url = ENDPOINTS.DEVICE_COMPONENTS.DELETE.replace(':id', id);
    await apiClient.delete<void>(url);
  },

  // Get device summary (components with live data)
  getDeviceSummary: async (deviceId: string): Promise<DeviceSummaryResponseDto> => {
    const response = await apiClient.get<DeviceSummaryResponseDto>(
      ENDPOINTS.DEVICE_COMPONENTS.GET_DEVICE_SUMMARY,
      { params: { deviceId } }
    );
    return response.data;
  },

  // Get component details (realtime + parameter data)
  // For battery components, pass bmsAddress to get specific BMS data
  getComponentDetails: async (componentId: string, bmsAddress?: number): Promise<DeviceDetailsResponseDto> => {
    const url = ENDPOINTS.DEVICE_COMPONENTS.GET_COMPONENT_DETAILS.replace(':componentId', componentId);
    const params: Record<string, any> = {};
    if (bmsAddress !== undefined) {
      params.bmsAddress = bmsAddress;
    }
    const response = await apiClient.get<DeviceDetailsResponseDto>(url, { params });
    return response.data;
  },
};
