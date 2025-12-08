import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  ChangeExportStrategyCommand,
  ExportStrategyStateDto,
} from '../types/deviceSettings.types';

export const deviceSettingsService = {
  async changeExportStrategy(
    command: ChangeExportStrategyCommand
  ): Promise<ExportStrategyStateDto> {
    const response = await apiClient.post<ExportStrategyStateDto>(
      ENDPOINTS.DEVICE_SETTINGS.CHANGE_EXPORT_STRATEGY,
      command
    );
    return response.data;
  },
};
