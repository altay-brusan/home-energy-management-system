/**
 * Device Settings Update Store
 * Manages the state of ongoing device parameter update operations
 * Handles progress tracking, modal visibility, and navigation blocking
 */

import { create } from 'zustand';
import {
  BatchOperationState,
  ParameterUpdateCompletedDto,
} from '../api/types/deviceSettingsHub.types';

interface DeviceSettingsUpdateState {
  // Progress tracking
  isUpdateActive: boolean;
  correlationId: string | null;
  batchState: BatchOperationState | null;

  // Modal visibility
  isProgressModalVisible: boolean;

  // Navigation blocking
  isNavigationBlocked: boolean;
  pendingNavigation: (() => void) | null;

  // Actions
  startUpdate: (correlationId: string, totalParameters?: number) => void;
  updateProgress: (state: BatchOperationState) => void;
  completeUpdate: (result: ParameterUpdateCompletedDto) => void;
  cancelUpdate: () => void;

  // Modal controls
  showProgressModal: () => void;
  hideProgressModal: () => void;

  // Navigation controls
  setPendingNavigation: (callback: (() => void) | null) => void;
  confirmNavigation: () => void;
  cancelNavigation: () => void;

  // Reset
  reset: () => void;
}

const initialState = {
  isUpdateActive: false,
  correlationId: null,
  batchState: null,
  isProgressModalVisible: false,
  isNavigationBlocked: false,
  pendingNavigation: null,
};

export const useDeviceSettingsUpdateStore = create<DeviceSettingsUpdateState>((set, get) => ({
  ...initialState,

  /**
   * Start a new update operation
   */
  startUpdate: (correlationId: string, totalParameters?: number) => {
    console.log('[deviceSettingsUpdateStore] 🚀 startUpdate called:', correlationId, 'params:', totalParameters);
    set({
      isUpdateActive: true,
      correlationId,
      isProgressModalVisible: true,
      isNavigationBlocked: true,
      batchState: {
        correlationId,
        deviceId: '',
        status: 'pending',
        totalParameters: totalParameters ?? 0,
        currentIndex: 0,
        successCount: 0,
        failedCount: 0,
        skippedCount: 0,
        wasCancelled: false,
        failedParameters: [],
        failedParametersInfo: [],
        startedAt: null,
        completedAt: null,
        duration: null,
      },
    });
  },

  /**
   * Update progress from SignalR events
   */
  updateProgress: (state: BatchOperationState) => {
    console.log('[deviceSettingsUpdateStore] 📊 updateProgress called:', state.status, state.currentIndex, '/', state.totalParameters);
    set({
      batchState: state,
    });
  },

  /**
   * Mark update as complete
   */
  completeUpdate: (result: ParameterUpdateCompletedDto) => {
    const hasFailures = result.failedCount > 0;
    const currentBatchState = get().batchState;

    set({
      isUpdateActive: false,
      isNavigationBlocked: false,
      batchState: currentBatchState
        ? {
            ...currentBatchState,
            status: hasFailures ? 'failed' : 'completed',
            successCount: result.successCount,
            failedCount: result.failedCount,
            completedAt: result.completedAt,
            duration: result.duration,
          }
        : null,
    });
  },

  /**
   * Cancel update (cleanup without completion)
   */
  cancelUpdate: () =>
    set({
      isUpdateActive: false,
      isNavigationBlocked: false,
    }),

  /**
   * Show progress modal
   */
  showProgressModal: () => set({ isProgressModalVisible: true }),

  /**
   * Hide progress modal
   */
  hideProgressModal: () => set({ isProgressModalVisible: false }),

  /**
   * Set pending navigation callback
   */
  setPendingNavigation: (callback: (() => void) | null) =>
    set({ pendingNavigation: callback }),

  /**
   * Confirm navigation - continue update in background
   */
  confirmNavigation: () => {
    const { pendingNavigation } = get();
    // Hide modal but keep tracking in background
    set({
      isProgressModalVisible: false,
      pendingNavigation: null,
    });
    // Execute the pending navigation
    if (pendingNavigation) {
      pendingNavigation();
    }
  },

  /**
   * Cancel pending navigation
   */
  cancelNavigation: () => set({ pendingNavigation: null }),

  /**
   * Reset store to initial state
   */
  reset: () => set(initialState),
}));

// ============================================================================
// Selectors
// ============================================================================

export const useIsUpdateActive = () =>
  useDeviceSettingsUpdateStore((state) => state.isUpdateActive);

export const useBatchProgress = () =>
  useDeviceSettingsUpdateStore((state) => state.batchState);

export const useIsProgressModalVisible = () =>
  useDeviceSettingsUpdateStore((state) => state.isProgressModalVisible);

export const useIsNavigationBlocked = () =>
  useDeviceSettingsUpdateStore((state) => state.isNavigationBlocked);
