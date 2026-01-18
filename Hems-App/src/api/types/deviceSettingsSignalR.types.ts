/**
 * Device Settings SignalR Types
 * TypeScript types for real-time device parameter update progress via SignalR
 */

/**
 * Status of a parameter update within an operation
 */
export enum ParameterUpdateStatus {
  Pending = 'Pending',
  InProgress = 'InProgress',
  Success = 'Success',
  Failed = 'Failed',
}

/**
 * Event sent when a parameter update operation starts
 */
export interface OperationStartedEvent {
  operationId: string;
  deviceId: string;
  totalParameters: number;
  startedAt: string;
}

/**
 * Event sent after each parameter is processed
 */
export interface ParameterProgressEvent {
  operationId: string;
  deviceId: string;
  parameterId: string;
  parameterName: string;
  status: ParameterUpdateStatus;
  errorMessage: string | null;
  currentIndex: number;
  totalParameters: number;
  successCount: number;
  failedCount: number;
}

/**
 * Information about a failed parameter
 */
export interface FailedParameterInfo {
  parameterId: string;
  parameterName: string;
  errorMessage: string;
}

/**
 * Event sent when the entire operation completes
 */
export interface OperationCompletedEvent {
  operationId: string;
  deviceId: string;
  success: boolean;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  failedParameters: FailedParameterInfo[];
  duration: string;
}

/**
 * State of a single parameter within an operation
 */
export interface OperationParameterState {
  parameterId: string;
  parameterName: string;
  value: string;
  status: ParameterUpdateStatus;
  errorMessage: string | null;
}

/**
 * Full state of an operation (for reconnection recovery)
 */
export interface OperationState {
  operationId: string;
  deviceId: string;
  userId: string;
  startedAt: string;
  completedAt: string | null;
  isCompleted: boolean;
  success: boolean;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  currentIndex: number;
  parameters: OperationParameterState[];
}

/**
 * Callback types for SignalR event handlers
 */
export type OperationStartedCallback = (event: OperationStartedEvent) => void;
export type ParameterProgressCallback = (event: ParameterProgressEvent) => void;
export type OperationCompletedCallback = (event: OperationCompletedEvent) => void;
