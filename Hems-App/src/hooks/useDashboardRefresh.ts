import { useCallback, useRef, useEffect } from 'react';
import { useSelectedSiteDevice } from './useSelectedSiteDevice';

// Global callback store - survives component re-renders
const globalRefreshCallbacks = new Set<() => Promise<void>>();

/**
 * Hook to manage dashboard refresh functionality
 * Provides centralized refresh mechanism for all dashboard components
 */
export const useDashboardRefresh = () => {
  const { device, hasDevice } = useSelectedSiteDevice();

  useEffect(() => {
    return () => {
    };
  }, []);

  const registerRefreshCallback = useCallback((callback: () => Promise<void>) => {
    if (!callback || typeof callback !== 'function') {return () => { };
    }

    globalRefreshCallbacks.add(callback);// Return cleanup function
    return () => {
      globalRefreshCallbacks.delete(callback);};
  }, []);

  const refreshAll = useCallback(async () => {const validCallbacks = Array.from(globalRefreshCallbacks)
      .filter(callback => callback && typeof callback === 'function');const refreshPromises = validCallbacks.map((callback, index) => {return callback().catch(error => {});
    });

    await Promise.allSettled(refreshPromises);}, []);

  return {
    registerRefreshCallback,
    refreshAll,
    deviceId: device?.id,
    hasDevice,
  };
};
