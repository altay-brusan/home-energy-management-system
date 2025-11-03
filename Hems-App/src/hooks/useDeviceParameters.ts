import { useState, useEffect, useCallback } from 'react';
import { setupDeviceParameterValuesService } from '../api/services/setupDeviceParameterValuesService';
import { SetupDeviceParameterValuesTreeVm, ParameterTreeDto2 } from '../api/types/setupDeviceParameterValues.types';
import { useSelectedSiteDevice } from './useSelectedSiteDevice';


export const useDeviceParameters = () => {
  const { setupInfo } = useSelectedSiteDevice();
  const [data, setData] = useState<SetupDeviceParameterValuesTreeVm | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    if (!setupInfo?.setupDeviceId) {
      setData(null);
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const response = await setupDeviceParameterValuesService.getTree({
        setupDeviceId: setupInfo.setupDeviceId,
      });
      setData(response);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch device parameters');
      setData(null);
    } finally {
      setIsLoading(false);
    }
  }, [setupInfo?.setupDeviceId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);


  const findParameterByName = useCallback(
    (name: string): ParameterTreeDto2 | null => {
      if (!data) return null;

      const searchInGroups = (groups: typeof data.parameterGroups): ParameterTreeDto2 | null => {
        for (const group of groups) {
          for (const param of group.parameters) {
            if (param.name === name) {
              return param;
            }
          }
          if (group.children.length > 0) {
            const found = searchInGroups(group.children);
            if (found) return found;
          }
        }
        return null;
      };

      return searchInGroups(data.parameterGroups);
    },
    [data]
  );

  const findParameterById = useCallback(
    (id: string): ParameterTreeDto2 | null => {
      if (!data) return null;

      const searchInGroups = (groups: typeof data.parameterGroups): ParameterTreeDto2 | null => {
        for (const group of groups) {
          for (const param of group.parameters) {
            if (param.id === id) {
              return param;
            }
          }
          if (group.children.length > 0) {
            const found = searchInGroups(group.children);
            if (found) return found;
          }
        }
        return null;
      };

      return searchInGroups(data.parameterGroups);
    },
    [data]
  );

  const getParameterValue = useCallback(
    (parameterId: string): string | null => {
      const param = findParameterById(parameterId);
      return param?.setupDeviceParameterValue?.value || null;
    },
    [findParameterById]
  );

  const hasParameter = useCallback(
    (parameterId: string): boolean => {
      const param = findParameterById(parameterId);
      return !!param?.setupDeviceParameterValue;
    },
    [findParameterById]
  );

  const getParameterValueByName = useCallback(
    (name: string): string | null => {
      const param = findParameterByName(name);
      return param?.setupDeviceParameterValue?.value || null;
    },
    [findParameterByName]
  );

  return {
    data,
    isLoading,
    error,
    refetch: fetchData,
    findParameterByName,
    findParameterById,
    getParameterValue,
    getParameterValueByName,
    hasParameter,
    setupDeviceId: setupInfo?.setupDeviceId,
  };
};
