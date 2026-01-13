import { useState, useCallback, useEffect } from 'react';
import { mobileDashboardService, SOHResponseDto } from '../api/services/mobileDashboardService';
import { useSelectedSiteDevice } from './useSelectedSiteDevice';
import { useDashboardRefresh } from './useDashboardRefresh';

export function useSOH() {
  const { device, hasDevice } = useSelectedSiteDevice();
  const { registerRefreshCallback } = useDashboardRefresh();
  const [sohData, setSohData] = useState<SOHResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadSOHData = useCallback(async () => {
    if (!hasDevice || !device?.id) {
      setIsLoading(false);
      setSohData(null);
      setError(null);
      return;
    }

    setIsLoading(true);
    setError(null);
    
    try {
      const data = await mobileDashboardService.getSOH(device.id);
      setSohData(data);
    } catch (err: any) {setError(err?.message || 'Failed to load SOH data');
      setSohData(null);
    } finally {
      setIsLoading(false);
    }
  }, [device?.id, hasDevice]);

  useEffect(() => {
    loadSOHData();
  }, [loadSOHData]);

  const refreshSOHData = useCallback(async () => {
    await loadSOHData();
  }, [loadSOHData]);

  useEffect(() => {
    const unregisterSOH = registerRefreshCallback(refreshSOHData);
    return () => {
      unregisterSOH();
    };
  }, [registerRefreshCallback, refreshSOHData]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    sohData,
    isLoading,
    error,
    refreshSOHData,
    clearError,
    hasDevice,
    deviceId: device?.id,
  };
}
