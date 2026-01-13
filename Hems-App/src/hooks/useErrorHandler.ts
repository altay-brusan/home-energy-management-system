import { useCallback } from 'react';
import { useErrorStore } from '../store/errorStore';
import { mapToAppError, AppError } from '../lib/http/error';

export function useErrorHandler() {
  const show = useErrorStore((s) => s.show);

  const handleError = useCallback((err: unknown): AppError => {
    const appErr = mapToAppError(err);
    
    if (appErr.status === 401) {
      return appErr;
    }
    
    show(appErr);
    return appErr;
  }, [show]);

  return { handleError };
}
