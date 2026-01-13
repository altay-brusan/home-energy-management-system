import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  ParametersVm,
  ParameterDto2,
  CreateParameterCommand,
  UpdateParameterCommand,
  GetParametersParams,
  GetParameterParams,
} from '../types/parameters.types';

export const parametersService = {
  async getAll(params?: GetParametersParams): Promise<ParametersVm> {
    const response = await apiClient.get<ParametersVm>(
      ENDPOINTS.PARAMETERS.GET_ALL,
      { params }
    );
    return response.data;
  },

  async getById(params: GetParameterParams): Promise<ParameterDto2> {
    const url = ENDPOINTS.PARAMETERS.GET_BY_ID.replace(':id', params.id);
    const response = await apiClient.get<ParameterDto2>(url, {
      params: { includeGroup: params.includeGroup },
    });
    return response.data;
  },

  async create(data: CreateParameterCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.PARAMETERS.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateParameterCommand): Promise<void> {
    const url = ENDPOINTS.PARAMETERS.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async delete(id: string): Promise<void> {
    const url = ENDPOINTS.PARAMETERS.DELETE.replace(':id', id);
    await apiClient.delete(url);
  },
};
