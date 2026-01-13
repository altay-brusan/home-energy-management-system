export interface ParametersVm {
  parameters: ParameterDto2[];
}

export interface ParameterDto2 {
  id: string;
  parameterGroupId: string;
  name: string;
  description: string;
  elementType: string;
  roleType: string;
  unit: string;
  defaultValue: string;
  sortOrder: number;
  parameterGroupName?: string;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

export interface CreateParameterCommand {
  parameterGroupId: string;
  name: string;
  description: string;
  elementType: string;
  roleType: string;
  unit: string;
  defaultValue: string;
  sortOrder: number;
}

export interface UpdateParameterCommand {
  id: string;
  name: string;
  description: string;
  elementType: string;
  roleType: string;
  unit: string;
  defaultValue: string;
  sortOrder: number;
}

export interface GetParametersParams {
  groupId?: string;
}

export interface GetParameterParams {
  id: string;
  includeGroup?: boolean;
}
