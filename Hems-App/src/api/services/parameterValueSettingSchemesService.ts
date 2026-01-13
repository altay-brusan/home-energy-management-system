import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  ParameterValueSettingSchemesVm,
  ParameterValueSettingSchemesTreeVm,
  ParameterValueSettingSchemeDto,
  CreateParameterValueSettingSchemeCommand,
  UpdateParameterValueSettingSchemeCommand,
  GetParameterValueSettingSchemesParams,
} from '../types/parameterValueSettingSchemes.types';

export const parameterValueSettingSchemesService = {
  async getAll(params?: GetParameterValueSettingSchemesParams): Promise<ParameterValueSettingSchemesVm> {
    const response = await apiClient.get<ParameterValueSettingSchemesVm>(
      ENDPOINTS.PARAMETER_VALUE_SETTING_SCHEMES.GET_ALL,
      { params }
    );
    return response.data;
  },

  async getTree(params?: GetParameterValueSettingSchemesParams): Promise<ParameterValueSettingSchemesTreeVm> {
    const response = await apiClient.get<ParameterValueSettingSchemesTreeVm>(
      ENDPOINTS.PARAMETER_VALUE_SETTING_SCHEMES.GET_TREE,
      { params }
    );
    return response.data;
  },

  async create(data: CreateParameterValueSettingSchemeCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.PARAMETER_VALUE_SETTING_SCHEMES.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateParameterValueSettingSchemeCommand): Promise<void> {
    const url = ENDPOINTS.PARAMETER_VALUE_SETTING_SCHEMES.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async delete(id: string): Promise<void> {
    const url = ENDPOINTS.PARAMETER_VALUE_SETTING_SCHEMES.DELETE.replace(':id', id);
    await apiClient.delete(url);
  },
};
