export enum ExportStrategy {
  SellFirst = 0,
  ZeroExportToLoad = 1,
  ZeroExportToCt = 2,
}

export interface ChangeExportStrategyCommand {
  deviceId: string;
  exportStrategy: ExportStrategy;
}

export interface ExportStrategyStateDto {
  kind?: string;
  timestampUtc?: string;
  version?: number;
  gatewayId?: string;
  success: boolean;
  message?: string;
  exportStrategy?: string;
  exportStrategyCode?: number;
}

// New DeviceSettings API Types
export interface DeviceSettingsTreeVm {
  setupDeviceId: string;
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
  visible?: boolean;
  setupDeviceParameterValue?: DeviceParameterValueDto;
}

export interface DeviceParameterValueDto {
  id: string;
  value: string;
  sortOrder: number;
  modelNumber: string;
  created: string;
  createdBy?: string;
  lastModified: string;
  lastModifiedBy?: string;
}

export interface UpdateParameterItem {
  parameterId: string;
  value: string;
}

export interface UpdateDeviceParametersCommand {
  parameters: UpdateParameterItem[];
}

/**
 * Response from PUT /api/device-settings/{deviceId}/parameters (synchronous)
 * Returns complete result directly - no SignalR tracking needed
 */
export interface UpdateDeviceParametersResponse {
  success: boolean;
  errorCode?: string;
  message?: string;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  failedParameters: FailedParameterInfo[];
  duration: string;
}

/**
 * Information about a failed parameter update
 */
export interface FailedParameterInfo {
  parameterId: string;
  parameterName: string;
  errorMessage: string;
}
