import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  ChangeExportStrategyCommand,
  ExportStrategyStateDto,
  DeviceSettingsTreeVm,
  UpdateDeviceParametersCommand,
  UpdateDeviceParametersResponse,
  GetGridConfigResponse,
  UpdateGridConfigCommand,
  UpdateGridConfigResponse,
} from '../types/deviceSettings.types';
import {
  GetModesResponse,
  GetDeviceModeResponse,
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
   * Update device parameters (bulk) - synchronous
   * PUT /DeviceSettings/device/{deviceId}/parameters
   * Now waits for completion and returns full result directly
   */
  async updateParameters(
    deviceId: string,
    command: UpdateDeviceParametersCommand
  ): Promise<UpdateDeviceParametersResponse> {
    const url = ENDPOINTS.DEVICE_SETTINGS.UPDATE_PARAMETERS.replace(':deviceId', deviceId);
    // Use longer timeout for parameter updates (2 minutes)
    const response = await apiClient.put<UpdateDeviceParametersResponse>(url, command, {
      timeout: 120000,
    });
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

  // ========================================================================
  // Grid Config Operations (fire-and-forget, causes device restart)
  // ========================================================================

  /**
   * Get grid configuration parameters
   * GET /DeviceSettings/device/{deviceId}/grid-config
   */
  async getGridConfig(deviceId: string): Promise<GetGridConfigResponse> {
    const url = ENDPOINTS.DEVICE_SETTINGS.GET_GRID_CONFIG.replace(':deviceId', deviceId);
    const response = await apiClient.get<GetGridConfigResponse>(url);
    return response.data;
  },

  /**
   * Update grid configuration (fire-and-forget, causes device restart)
   * PUT /DeviceSettings/device/{deviceId}/grid-config
   *
   * IMPORTANT: This operation causes the device to restart.
   * The response includes restart timing information.
   */
  async updateGridConfig(
    deviceId: string,
    command: UpdateGridConfigCommand
  ): Promise<UpdateGridConfigResponse> {
    const url = ENDPOINTS.DEVICE_SETTINGS.UPDATE_GRID_CONFIG.replace(':deviceId', deviceId);
    // Use longer timeout for grid config updates (3 minutes)
    const response = await apiClient.put<UpdateGridConfigResponse>(url, command, {
      timeout: 180000,
    });
    return response.data;
  },
};
