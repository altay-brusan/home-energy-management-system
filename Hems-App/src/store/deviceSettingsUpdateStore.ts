/**
 * Device Settings Update Store (Simplified)
 * Manages simple loading state and result dialog for device parameter updates.
 * No longer handles SignalR tracking - API is now synchronous.
 */

import { create } from 'zustand';
import { FailedParameterInfo } from '../api/types/deviceSettings.types';

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

interface DeviceSettingsUpdateState {
  // Simple loading state
  isSaving: boolean;

  // Result state (shown in dialog/toast)
  lastResult: UpdateResult | null;
  showResultDialog: boolean;

  // Actions
  startSaving: () => void;
  completeSaving: (result: UpdateResult) => void;
  clearResult: () => void;
  hideResultDialog: () => void;

  // Reset
  reset: () => void;
}

const initialState = {
  isSaving: false,
  lastResult: null,
  showResultDialog: false,
};

export const useDeviceSettingsUpdateStore = create<DeviceSettingsUpdateState>((set) => ({
  ...initialState,

  /**
   * Start saving operation (shows loading indicator)
   */
  startSaving: () => {
    console.log('[deviceSettingsUpdateStore] startSaving called');
    set({
      isSaving: true,
      lastResult: null,
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
   * Reset store to initial state
   */
  reset: () => set(initialState),
}));

// ============================================================================
// Selectors
// ============================================================================

export const useIsSaving = () =>
  useDeviceSettingsUpdateStore((state) => state.isSaving);

export const useLastResult = () =>
  useDeviceSettingsUpdateStore((state) => state.lastResult);

export const useShowResultDialog = () =>
  useDeviceSettingsUpdateStore((state) => state.showResultDialog);
