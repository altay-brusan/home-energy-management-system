/**
 * Device Settings Hub Types
 * SignalR hub types for real-time device parameter update tracking
 */

// ============================================================================
// API Response Types (Updated)
// ============================================================================

/**
 * Response from PUT /api/device-settings/{deviceId}/parameters
 * Now returns correlationId for SignalR tracking
 */
export interface UpdateParametersResponse {
  success: boolean;
  correlationId: string;
  totalParameters: number;
  message: string;
  errorMessage?: string;
}

// ============================================================================
// SignalR Event DTOs
// ============================================================================

/**
 * Fired when batch parameter processing begins
 */
export interface ParameterUpdateStartedDto {
  correlationId: string;
  deviceId: string;
  totalParameters: number;
  startedAt: string; // ISO date
}

/**
 * Fired for each successfully updated parameter
 */
export interface ParameterUpdatedDto {
  correlationId: string;
  deviceId: string;
  parameterId: string;
  parameterName: string;
  newValue: string;
  rpcMethod?: string;
  currentIndex: number; // 1-based
  totalParameters: number;
  updatedAt: string; // ISO date
}

/**
 * Fired when a parameter update fails
 */
export interface ParameterUpdateFailedDto {
  correlationId: string;
  deviceId: string;
  parameterId: string;
  parameterName: string;
  errorMessage: string;
  rpcMethod?: string;
  currentIndex: number; // 1-based
  totalParameters: number;
  failedAt: string; // ISO date
}

/**
 * Failed parameter info included in completion message
 */
export interface FailedParameterInfo {
  parameterId: string;
  parameterName: string;
  errorMessage: string;
  retryAttempts: number;
}

/**
 * Fired when all parameters have been processed
 */
export interface ParameterUpdateCompletedDto {
  correlationId: string;
  deviceId: string;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  completedAt: string; // ISO date
  duration: string; // TimeSpan format: "00:00:05.123"
  wasCancelled: boolean;
  failedParameters: FailedParameterInfo[];
}

// ============================================================================
// Batch Operation State
// ============================================================================

export type BatchOperationStatus = 'pending' | 'in_progress' | 'completed' | 'failed';

/**
 * State for tracking a batch parameter update operation
 */
export interface BatchOperationState {
  correlationId: string;
  deviceId: string;
  status: BatchOperationStatus;
  totalParameters: number;
  currentIndex: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  wasCancelled: boolean;
  failedParameters: ParameterUpdateFailedDto[];
  failedParametersInfo: FailedParameterInfo[];
  startedAt: string | null;
  completedAt: string | null;
  duration: string | null;
}

// ============================================================================
// Hub Constants
// ============================================================================

/**
 * SignalR hub event names
 */
export const DEVICE_SETTINGS_HUB_EVENTS = {
  PARAMETER_UPDATE_STARTED: 'ParameterUpdateStarted',
  PARAMETER_UPDATED: 'ParameterUpdated',
  PARAMETER_UPDATE_FAILED: 'ParameterUpdateFailed',
  PARAMETER_UPDATE_COMPLETED: 'ParameterUpdateCompleted',
} as const;

/**
 * SignalR hub method names (server-side)
 */
export const DEVICE_SETTINGS_HUB_METHODS = {
  SUBSCRIBE_TO_BATCH: 'SubscribeToBatchOperation',
  UNSUBSCRIBE_FROM_BATCH: 'UnsubscribeFromBatchOperation',
  SUBSCRIBE_TO_DEVICE: 'SubscribeToDeviceSettings',
  UNSUBSCRIBE_FROM_DEVICE: 'UnsubscribeFromDeviceSettings',
} as const;

// ============================================================================
// Listener Types
// ============================================================================

export type BatchOperationListener = (state: BatchOperationState) => void;
export type CompletionListener = (result: ParameterUpdateCompletedDto) => void;
export type ConnectionStateListener = (state: string) => void;
