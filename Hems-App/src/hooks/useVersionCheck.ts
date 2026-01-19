import { useCallback, useEffect, useState } from 'react';
import { versionService, getAppVersion, getCurrentPlatform } from '../api/services/versionService';
import { AppVersionCheckResponse } from '../api/types/version.types';

export interface VersionCheckState {
  isLoading: boolean;
  isUpdateRequired: boolean;
  isUpdateAvailable: boolean;
  latestVersion: string | null;
  storeUrl: string | null;
  description: string | null;
  revocationReason: string | null;
  error: Error | null;
}

const initialState: VersionCheckState = {
  isLoading: true,
  isUpdateRequired: false,
  isUpdateAvailable: false,
  latestVersion: null,
  storeUrl: null,
  description: null,
  revocationReason: null,
  error: null,
};

/**
 * Hook to check app version and determine if update is required/available.
 *
 * @param autoCheck - If true, checks version on mount. Default: true
 * @returns Version check state and a manual check function
 */
export function useVersionCheck(autoCheck: boolean = true) {
  const [state, setState] = useState<VersionCheckState>(initialState);

  const checkVersion = useCallback(async () => {
    setState(prev => ({ ...prev, isLoading: true, error: null }));

    try {
      const result = await versionService.checkVersion();

      setState({
        isLoading: false,
        isUpdateRequired: result.isUpdateRequired,
        isUpdateAvailable: result.isUpdateAvailable,
        latestVersion: result.version,
        storeUrl: result.storeUrl,
        description: result.description,
        revocationReason: result.revocationReason,
        error: null,
      });

      return result;
    } catch (error) {
      console.warn('[useVersionCheck] Version check failed:', error);

      // Don't block the app if version check fails
      setState({
        isLoading: false,
        isUpdateRequired: false,
        isUpdateAvailable: false,
        latestVersion: null,
        storeUrl: null,
        description: null,
        revocationReason: null,
        error: error instanceof Error ? error : new Error('Version check failed'),
      });

      return null;
    }
  }, []);

  useEffect(() => {
    if (autoCheck) {
      checkVersion();
    }
  }, [autoCheck, checkVersion]);

  return {
    ...state,
    checkVersion,
    currentVersion: getAppVersion(),
    currentPlatform: getCurrentPlatform(),
  };
}

export default useVersionCheck;
