import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  ChangeExportStrategyCommand,
  ExportStrategyStateDto,
  DeviceSettingsTreeVm,
  UpdateDeviceParametersCommand,
} from '../types/deviceSettings.types';
import {
  GetModesResponse,
  UpdateDeviceModeCommand,
} from '../types/inverterModes.types';

export const deviceSettingsService = {
  /**
   * Get device settings tree by deviceId
   * GET /DeviceSettings/device/{deviceId}/tree
   */
  async getTree(deviceId: string): Promise<DeviceSettingsTreeVm> {
    const url = ENDPOINTS.DEVICE_SETTINGS.GET_TREE.replace(':deviceId', deviceId);
    const response = await apiClient.get<DeviceSettingsTreeVm>(url);
    return response.data;
  },

  /**
   * Update device parameters (bulk)
   * PUT /DeviceSettings/device/{deviceId}/parameters
   */
  async updateParameters(
    deviceId: string,
    command: UpdateDeviceParametersCommand
  ): Promise<void> {
    const url = ENDPOINTS.DEVICE_SETTINGS.UPDATE_PARAMETERS.replace(':deviceId', deviceId);
    await apiClient.put(url, command);
  },

  /**
   * Change export strategy (legacy)
   */
  async changeExportStrategy(
    command: ChangeExportStrategyCommand
  ): Promise<ExportStrategyStateDto> {
    const response = await apiClient.post<ExportStrategyStateDto>(
      ENDPOINTS.DEVICE_SETTINGS.CHANGE_EXPORT_STRATEGY,
      command
    );
    return response.data;
  },

  /**
   * Get all available inverter modes
   * GET /DeviceSettings/modes
   */
  async getModes(): Promise<GetModesResponse> {
    const response = await apiClient.get<GetModesResponse>(
      ENDPOINTS.DEVICE_SETTINGS.GET_MODES
    );
    return response.data;
  },

  /**
   * Update device mode
   * POST /DeviceSettings/device/{deviceId}/mode
   */
  async updateDeviceMode(
    deviceId: string,
    command: UpdateDeviceModeCommand
  ): Promise<void> {
    const url = ENDPOINTS.DEVICE_SETTINGS.UPDATE_DEVICE_MODE.replace(
      ':deviceId',
      deviceId
    );
    await apiClient.post(url, command);
  },
};
