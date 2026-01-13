import { useState, useCallback } from 'react';
import { authService } from '../api';
import { ForgotPasswordRequest } from '../api/types/auth.types';

export type ForgotPasswordVars = {
  email: string;
};

export default function useForgotPassword() {
  const [forgotPasswordLoading, setForgotPasswordLoading] = useState<boolean>(false);
  const [forgotPasswordError, setForgotPasswordError] = useState<string | null>(null);
  const [forgotPasswordSuccess, setForgotPasswordSuccess] = useState<boolean>(false);

  const forgotPassword = useCallback(
    async ({ email }: ForgotPasswordVars): Promise<void> => {
      setForgotPasswordLoading(true);
      setForgotPasswordError(null);
      setForgotPasswordSuccess(false);

      try {
        const forgotPasswordData: ForgotPasswordRequest = {
          email,
        };

        await authService.forgotPassword(forgotPasswordData);
        setForgotPasswordSuccess(true);
      } catch (err: any) {
        // API client'ı zaten toast gösteriyor, sadece error'ı throw et
        throw err;
      } finally {
        setForgotPasswordLoading(false);
      }
    },
[]
  );

  const clearError = useCallback(() => {
    setForgotPasswordError(null);
  }, []);

  const clearSuccess = useCallback(() => {
    setForgotPasswordSuccess(false);
  }, []);

  return {
    forgotPassword,
    forgotPasswordLoading,
    forgotPasswordError,
    forgotPasswordSuccess,
    clearError,
    clearSuccess,
  };
}

