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
  /**
   * Indicates if a parameter update operation is currently in progress.
   * When true, the client should disable mode/settings changes and retry later.
   */
  isDeviceBusy?: boolean;
  /**
   * Estimated time remaining for the current operation (in seconds).
   * Only populated when isDeviceBusy is true.
   */
  estimatedSecondsRemaining?: number;
  /**
   * Current progress of the operation (0-100).
   * Only populated when isDeviceBusy is true.
   */
  progressPercentage?: number;
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

// ========================================================================
// Grid Config Types (fire-and-forget operations that cause device restart)
// ========================================================================

/**
 * Grid config parameter returned from GET /grid-config
 */
export interface GridConfigParameterDto {
  parameterId: string;
  parameterName: string;
  parameterDescription: string;
  value: string;
  unit: string;
  elementType: string;
  parameterGroupId?: string;
  parameterGroupName?: string;
  lastModified?: string;
}

/**
 * Response from GET /grid-config
 */
export interface GetGridConfigResponse {
  success: boolean;
  errorCode?: string;
  errorMessage?: string;
  deviceId: string;
  parameters: GridConfigParameterDto[];
  estimatedAvailableAt?: string;
}

/**
 * Single parameter update for grid config
 */
export interface GridConfigParameterUpdate {
  parameterId: string;
  value: string;
}

/**
 * Command for PUT /grid-config
 */
export interface UpdateGridConfigCommand {
  parameters: GridConfigParameterUpdate[];
}

/**
 * Response from PUT /grid-config
 * Includes restart information since grid config changes cause device restart
 */
export interface UpdateGridConfigResponse {
  success: boolean;
  errorCode?: string;
  message?: string;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  appliedParameters: string[];
  failedParameters: FailedParameterInfo[];
  duration: string;
  /** Indicates that the device will restart */
  deviceWillRestart: boolean;
  /** Reason for device restart */
  restartReason?: string;
  /** Grace period in seconds during which device is restarting */
  restartGracePeriodSeconds: number;
  /** Estimated time when device will be available again */
  estimatedAvailableAt?: string;
}
