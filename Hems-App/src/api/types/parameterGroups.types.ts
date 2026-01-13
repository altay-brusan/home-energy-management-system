export interface ParameterGroupsVm {
  parameterGroups: ParameterGroupDto[];
}

export interface ParameterGroupDto {
  id: string;
  parentId?: string;
  name: string;
  description?: string;
  version: string;
  sortOrder: number;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
  children: ParameterGroupDto[];
  parameters: ParameterDto[];
}

export interface ParameterDto {
  id: string;
  parameterGroupId: string;
  name: string;
  description: string;
  elementType: string;
  roleType: string;
  unit: string;
  defaultValue: string;
  sortOrder: number;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

export interface CreateParameterGroupCommand {
  parentId?: string;
  name: string;
  description?: string;
  version: string;
  sortOrder: number;
}

export interface UpdateParameterGroupCommand {
  id: string;
  parentId?: string;
  name: string;
  description?: string;
  version: string;
  sortOrder: number;
}

export interface GetParameterGroupParams {
  id: string;
  includeChildren?: boolean;
}
