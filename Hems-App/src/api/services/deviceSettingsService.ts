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
  GetDeviceModeResponse,
  UpdateDeviceModeCommand,
} from '../types/inverterModes.types';
import { UpdateParametersResponse } from '../types/deviceSettingsHub.types';

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
   * Returns correlationId for SignalR progress tracking
   */
  async updateParameters(
    deviceId: string,
    command: UpdateDeviceParametersCommand
  ): Promise<UpdateParametersResponse> {
    const url = ENDPOINTS.DEVICE_SETTINGS.UPDATE_PARAMETERS.replace(':deviceId', deviceId);
    const response = await apiClient.put<UpdateParametersResponse>(url, command);
    return response.data;
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
   * Get current active mode for a device
   * GET /DeviceSettings/device/{deviceId}/mode
   */
  async getDeviceMode(deviceId: string): Promise<GetDeviceModeResponse> {
    const url = ENDPOINTS.DEVICE_SETTINGS.GET_DEVICE_MODE.replace(':deviceId', deviceId);
    const response = await apiClient.get<GetDeviceModeResponse>(url);
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
