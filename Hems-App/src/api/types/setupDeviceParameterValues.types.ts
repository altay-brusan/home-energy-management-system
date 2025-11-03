export interface SetupDeviceParameterValuesVm {
  setupDeviceParameterValues: SetupDeviceParameterValueDto[];
}

export interface SetupDeviceParameterValuesTreeVm {
  setupDeviceId: string;
  parameterGroups: ParameterGroupTreeDto2[];
}

export interface ParameterGroupTreeDto2 {
  id: string;
  name: string;
  description?: string;
  version: string;
  sortOrder: number;
  parentId?: string;
  children: ParameterGroupTreeDto2[];
  parameters: ParameterTreeDto2[];
}

export interface ParameterTreeDto2 {
  id: string;
  name: string;
  description: string;
  elementType: string;
  roleType: string;
  unit: string;
  defaultValue: string;
  sortOrder: number;
  setupDeviceParameterValue?: SetupDeviceParameterValueTreeDto;
}

export interface SetupDeviceParameterValueTreeDto {
  id: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

export interface SetupDeviceParameterValueDto {
  id: string;
  parameterGroupId?: string;
  parameterId: string;
  setupDeviceId: string;
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

export interface CreateSetupDeviceParameterValueCommand {
  parameterGroupId?: string;
  parameterId: string;
  setupDeviceId: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
}

export interface UpdateSetupDeviceParameterValueCommand {
  id: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
}

export interface GetSetupDeviceParameterValuesParams {
  setupDeviceId: string;
}
