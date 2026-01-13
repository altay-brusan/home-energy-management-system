import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  ParameterGroupsVm,
  ParameterGroupDto,
  CreateParameterGroupCommand,
  UpdateParameterGroupCommand,
  GetParameterGroupParams,
} from '../types/parameterGroups.types';

export const parameterGroupsService = {
  async getAll(): Promise<ParameterGroupsVm> {
    const response = await apiClient.get<ParameterGroupsVm>(
      ENDPOINTS.PARAMETER_GROUPS.GET_ALL
    );
    return response.data;
  },

  async getById(params: GetParameterGroupParams): Promise<ParameterGroupDto> {
    const url = ENDPOINTS.PARAMETER_GROUPS.GET_BY_ID.replace(':id', params.id);
    const response = await apiClient.get<ParameterGroupDto>(url, {
      params: { includeChildren: params.includeChildren },
    });
    return response.data;
  },

  async create(data: CreateParameterGroupCommand): Promise<string> {
    const response = await apiClient.post<string>(
      ENDPOINTS.PARAMETER_GROUPS.CREATE,
      data
    );
    return response.data;
  },

  async update(id: string, data: UpdateParameterGroupCommand): Promise<void> {
    const url = ENDPOINTS.PARAMETER_GROUPS.UPDATE.replace(':id', id);
    await apiClient.put(url, data);
  },

  async delete(id: string): Promise<void> {
    const url = ENDPOINTS.PARAMETER_GROUPS.DELETE.replace(':id', id);
    await apiClient.delete(url);
  },
};
