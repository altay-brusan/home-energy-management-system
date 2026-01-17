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
import { useDeviceSettingsStore } from './deviceSettingsStore';

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
   * NOTE: Don't update if already completed to prevent late events from breaking UI
   */
  updateProgress: (state: BatchOperationState) => {
    const currentState = get();
    const currentStatus = currentState.batchState?.status;

    // Don't allow updates after completion - late SignalR events should be ignored
    if (currentStatus === 'completed' || currentStatus === 'failed') {
      console.log('[deviceSettingsUpdateStore] ⚠️ Ignoring updateProgress - already completed:', currentStatus);
      return;
    }

    console.log('[deviceSettingsUpdateStore] 📊 updateProgress called:', state.status, state.currentIndex, '/', state.totalParameters);

    // If incoming state is completed/failed, also update isUpdateActive
    // This handles the case where SignalR sends a progress update with completed status
    const isCompleting = state.status === 'completed' || state.status === 'failed';

    set({
      batchState: state,
      // Also update isUpdateActive if this is a completion state
      ...(isCompleting && {
        isUpdateActive: false,
        isNavigationBlocked: false,
      }),
    });

    // If completing via progress update, also finalize the main store
    if (isCompleting) {
      console.log('[deviceSettingsUpdateStore] 📊 Progress update contained completion status, finalizing...');
      const { finalizeSave } = useDeviceSettingsStore.getState();
      const failedIds = state.failedParameters?.map(p => p.parameterId) ?? [];
      finalizeSave(state.status === 'completed' && (state.failedCount ?? 0) === 0, failedIds);
    }
  },

  /**
   * Mark update as complete
   * Also finalizes the save in the main deviceSettingsStore to reset isSaving state
   */
  completeUpdate: (result: ParameterUpdateCompletedDto) => {
    const currentState = get();
    const currentStatus = currentState.batchState?.status;

    // Don't complete again if already completed
    if (currentStatus === 'completed' || currentStatus === 'failed') {
      console.log('[deviceSettingsUpdateStore] ⚠️ Ignoring completeUpdate - already completed:', currentStatus);
      return;
    }

    const hasFailures = result.failedCount > 0;
    const newStatus = hasFailures ? 'failed' : 'completed';
    console.log('[deviceSettingsUpdateStore] ✅ completeUpdate called:', newStatus, 'success:', result.successCount, 'failed:', result.failedCount);

    set({
      isUpdateActive: false,
      isNavigationBlocked: false,
      batchState: currentState.batchState
        ? {
            ...currentState.batchState,
            status: newStatus,
            successCount: result.successCount,
            failedCount: result.failedCount,
            completedAt: result.completedAt,
            duration: result.duration,
          }
        : null,
    });

    // Also finalize the save in the main store to reset isSaving state
    // This ensures the "Saving..." button text is updated regardless of completion path
    const failedIds = result.failedParameters?.map(p => p.parameterId) ?? [];
    const { finalizeSave } = useDeviceSettingsStore.getState();
    finalizeSave(!hasFailures, failedIds);
    console.log('[deviceSettingsUpdateStore] Called finalizeSave on main store, isSaving should now be false');
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
