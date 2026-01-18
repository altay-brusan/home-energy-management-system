/**
 * Device Settings Update Store
 * Manages loading state, real-time progress, and result dialog for device parameter updates.
 * Now supports SignalR-based real-time progress tracking.
 */

import { create } from 'zustand';
import { FailedParameterInfo } from '../api/types/deviceSettings.types';
import {
  OperationStartedEvent,
  ParameterProgressEvent,
  OperationCompletedEvent,
  ParameterUpdateStatus,
} from '../api/types/deviceSettingsSignalR.types';

/**
 * Update result shown in dialog/toast
 */
export interface UpdateResult {
  success: boolean;
  totalParameters: number;
  successCount: number;
  failedCount: number;
  failedParameters: FailedParameterInfo[];
  errorMessage?: string;
  duration?: string;
}

/**
 * Failed parameter info during progress (shown as they happen)
 */
export interface FailedParameterDuringProgress {
  parameterId: string;
  parameterName: string;
  errorMessage: string;
}

interface DeviceSettingsUpdateState {
  // Simple loading state
  isSaving: boolean;

  // Result state (shown in dialog/toast)
  lastResult: UpdateResult | null;
  showResultDialog: boolean;

  // Real-time progress state (SignalR)
  currentOperationId: string | null;
  totalParameters: number;
  currentIndex: number;
  successCount: number;
  failedCount: number;
  currentParameterName: string | null;
  failedParametersDuringProgress: FailedParameterDuringProgress[];

  // Actions
  startSaving: () => void;
  completeSaving: (result: UpdateResult) => void;
  clearResult: () => void;
  hideResultDialog: () => void;

  // SignalR event actions
  setOperationStarted: (event: OperationStartedEvent) => void;
  updateProgress: (event: ParameterProgressEvent) => void;
  setOperationCompleted: (event: OperationCompletedEvent) => void;

  // Reset
  reset: () => void;
}

const initialState = {
  isSaving: false,
  lastResult: null,
  showResultDialog: false,
  currentOperationId: null,
  totalParameters: 0,
  currentIndex: 0,
  successCount: 0,
  failedCount: 0,
  currentParameterName: null,
  failedParametersDuringProgress: [] as FailedParameterDuringProgress[],
};

export const useDeviceSettingsUpdateStore = create<DeviceSettingsUpdateState>((set, get) => ({
  ...initialState,

  /**
   * Start saving operation (shows loading indicator)
   */
  startSaving: () => {
    console.log('[deviceSettingsUpdateStore] startSaving called');
    set({
      isSaving: true,
      lastResult: null,
      currentOperationId: null,
      totalParameters: 0,
      currentIndex: 0,
      successCount: 0,
      failedCount: 0,
      currentParameterName: null,
      failedParametersDuringProgress: [],
    });
  },

  /**
   * Complete saving operation with result
   * Shows dialog if there are failures OR an error occurred
   */
  completeSaving: (result: UpdateResult) => {
    console.log('[deviceSettingsUpdateStore] completeSaving called:', {
      success: result.success,
      successCount: result.successCount,
      failedCount: result.failedCount,
      hasErrorMessage: !!result.errorMessage,
      failedParametersCount: result.failedParameters?.length || 0,
    });

    // Show dialog if:
    // 1. There are failed parameters (partial or complete failure)
    // 2. There's an error message (like "Device busy", "No parameters found", etc.)
    // 3. The operation failed without success
    const shouldShowDialog =
      result.failedCount > 0 ||
      (result.failedParameters?.length || 0) > 0 ||
      (!result.success && result.errorMessage);

    set({
      isSaving: false,
      lastResult: result,
      showResultDialog: shouldShowDialog,
      currentOperationId: null,
      currentParameterName: null,
    });
  },

  /**
   * Clear last result
   */
  clearResult: () => set({ lastResult: null }),

  /**
   * Hide result dialog
   */
  hideResultDialog: () => set({ showResultDialog: false }),

  /**
   * SignalR: Operation started event
   */
  setOperationStarted: (event: OperationStartedEvent) => {
    console.log('[deviceSettingsUpdateStore] setOperationStarted:', event.operationId);
    set({
      isSaving: true,
      currentOperationId: event.operationId,
      totalParameters: event.totalParameters,
      currentIndex: 0,
      successCount: 0,
      failedCount: 0,
      currentParameterName: null,
      failedParametersDuringProgress: [],
      lastResult: null,
    });
  },

  /**
   * SignalR: Parameter progress event
   */
  updateProgress: (event: ParameterProgressEvent) => {
    console.log('[deviceSettingsUpdateStore] updateProgress:', {
      parameterName: event.parameterName,
      status: event.status,
      currentIndex: event.currentIndex,
      totalParameters: event.totalParameters,
    });

    const { failedParametersDuringProgress } = get();
    let newFailedParameters = failedParametersDuringProgress;

    // Add to failed list if this parameter failed
    if (event.status === ParameterUpdateStatus.Failed) {
      newFailedParameters = [
        ...failedParametersDuringProgress,
        {
          parameterId: event.parameterId,
          parameterName: event.parameterName,
          errorMessage: event.errorMessage || 'Unknown error',
        },
      ];
    }

    set({
      currentIndex: event.currentIndex,
      successCount: event.successCount,
      failedCount: event.failedCount,
      currentParameterName:
        event.status === ParameterUpdateStatus.InProgress ? event.parameterName : null,
      failedParametersDuringProgress: newFailedParameters,
    });
  },

  /**
   * SignalR: Operation completed event
   */
  setOperationCompleted: (event: OperationCompletedEvent) => {
    console.log('[deviceSettingsUpdateStore] setOperationCompleted:', {
      operationId: event.operationId,
      success: event.success,
      successCount: event.successCount,
      failedCount: event.failedCount,
    });

    // Convert to UpdateResult format
    const result: UpdateResult = {
      success: event.success,
      totalParameters: event.totalParameters,
      successCount: event.successCount,
      failedCount: event.failedCount,
      failedParameters: event.failedParameters,
      duration: event.duration,
    };

    // Show dialog if there are failures
    const shouldShowDialog = event.failedCount > 0 || event.failedParameters.length > 0;

    set({
      isSaving: false,
      lastResult: result,
      showResultDialog: shouldShowDialog,
      currentOperationId: null,
      currentParameterName: null,
    });
  },

  /**
   * Reset store to initial state
   */
  reset: () => set(initialState),
}));

// ============================================================================
// Selectors
// ============================================================================

export const useIsSaving = () => useDeviceSettingsUpdateStore((state) => state.isSaving);

export const useLastResult = () => useDeviceSettingsUpdateStore((state) => state.lastResult);

export const useShowResultDialog = () => useDeviceSettingsUpdateStore((state) => state.showResultDialog);

// Real-time progress selectors
export const useCurrentOperationId = () =>
  useDeviceSettingsUpdateStore((state) => state.currentOperationId);

export const useTotalParameters = () =>
  useDeviceSettingsUpdateStore((state) => state.totalParameters);

export const useCurrentIndex = () =>
  useDeviceSettingsUpdateStore((state) => state.currentIndex);

export const useProgressSuccessCount = () =>
  useDeviceSettingsUpdateStore((state) => state.successCount);

export const useProgressFailedCount = () =>
  useDeviceSettingsUpdateStore((state) => state.failedCount);

export const useCurrentParameterName = () =>
  useDeviceSettingsUpdateStore((state) => state.currentParameterName);

export const useFailedParametersDuringProgress = () =>
  useDeviceSettingsUpdateStore((state) => state.failedParametersDuringProgress);

// Combined progress selector for the modal
export const useProgressState = () =>
  useDeviceSettingsUpdateStore((state) => ({
    isSaving: state.isSaving,
    currentOperationId: state.currentOperationId,
    totalParameters: state.totalParameters,
    currentIndex: state.currentIndex,
    successCount: state.successCount,
    failedCount: state.failedCount,
    currentParameterName: state.currentParameterName,
    failedParametersDuringProgress: state.failedParametersDuringProgress,
  }));
