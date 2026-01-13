export interface ParameterValueSettingSchemesVm {
  parameterValueSettingSchemes: ParameterValueSettingSchemeDto[];
}

export interface ParameterValueSettingSchemesTreeVm {
  modelNumber?: string;
  parameterGroups: ParameterGroupTreeDto[];
}

export interface ParameterGroupTreeDto {
  id: string;
  name: string;
  description?: string;
  version: string;
  sortOrder: number;
  parentId?: string;
  children: ParameterGroupTreeDto[];
  parameters: ParameterTreeDto[];
}

export interface ParameterTreeDto {
  id: string;
  name: string;
  description: string;
  elementType: string;
  roleType: string;
  unit: string;
  defaultValue: string;
  sortOrder: number;
  parameterValueSettingScheme?: ParameterValueSettingSchemeTreeDto;
}

export interface ParameterValueSettingSchemeTreeDto {
  id: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

export interface ParameterValueSettingSchemeDto {
  id: string;
  parameterId: string;
  parameterGroupId?: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
  parameterName?: string;
  parameterGroupName?: string;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

export interface CreateParameterValueSettingSchemeCommand {
  parameterId: string;
  parameterGroupId?: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
}

export interface UpdateParameterValueSettingSchemeCommand {
  id: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
}

export interface GetParameterValueSettingSchemesParams {
  modelNumber?: string;
}
