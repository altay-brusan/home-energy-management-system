import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  SetupDeviceParameterValuesVm,
  SetupDeviceParameterValuesTreeVm,
  SetupDeviceParameterValueDto,
  CreateSetupDeviceParameterValueCommand,
  UpdateSetupDeviceParameterValueCommand,
  GetSetupDeviceParameterValuesParams,
} from '../types/setupDeviceParameterValues.types';

export const setupDeviceParameterValuesService = {
  async getAll(params: GetSetupDeviceParameterValuesParams): Promise<SetupDeviceParameterValuesVm> {
    const response = await apiClient.get<SetupDeviceParameterValuesVm>(
      ENDPOINTS.SETUP_DEVICE_PARAMETER_VALUES.GET_ALL,
      { params }
    );
    return response.data;
  },

  async getTree(params: GetSetupDeviceParameterValuesParams): Promise<SetupDeviceParameterValuesTreeVm> {
    const response = await apiClient.get<SetupDeviceParameterValuesTreeVm>(
      ENDPOINTS.SETUP_DEVICE_PARAMETER_VALUES.GET_TREE,
      { params }
    );
    return response.data;
  },

  async create(data: CreateSetupDeviceParameterValueCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.SETUP_DEVICE_PARAMETER_VALUES.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateSetupDeviceParameterValueCommand): Promise<void> {
    const url = ENDPOINTS.SETUP_DEVICE_PARAMETER_VALUES.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async delete(id: string): Promise<void> {
    const url = ENDPOINTS.SETUP_DEVICE_PARAMETER_VALUES.DELETE.replace(':id', id);
    await apiClient.delete(url);
  },
};
