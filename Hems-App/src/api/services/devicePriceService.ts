import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  DevicePriceResponse,
  UpdateDevicePriceRequest,
} from '../types/devicePrice.types';

export const devicePriceService = {
  /**
   * Get device price settings
   * GET /DeviceSettings/device/{deviceId}/price
   */
  async getDevicePrice(deviceId: string): Promise<DevicePriceResponse> {
    const url = ENDPOINTS.DEVICE_SETTINGS.GET_DEVICE_PRICE.replace(
      ':deviceId',
      deviceId
    );
    const response = await apiClient.get<DevicePriceResponse>(url);
    return response.data;
  },

  /**
   * Update device price settings
   * PUT /DeviceSettings/device/{deviceId}/price
   */
  async updateDevicePrice(
    deviceId: string,
    data: UpdateDevicePriceRequest
  ): Promise<void> {
    const url = ENDPOINTS.DEVICE_SETTINGS.UPDATE_DEVICE_PRICE.replace(
      ':deviceId',
      deviceId
    );
    await apiClient.put(url, data);
  },
};
