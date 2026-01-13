import { useState, useCallback } from 'react';
import { authService } from '../api';
import { ResetPasswordRequest } from '../api/types/auth.types';

export type ResetPasswordVars = {
  email: string;
  resetCode: string;
  newPassword: string;
};

export default function useResetPassword() {
  const [resetPasswordLoading, setResetPasswordLoading] = useState<boolean>(false);
  const [resetPasswordError, setResetPasswordError] = useState<string | null>(null);
  const [resetPasswordSuccess, setResetPasswordSuccess] = useState<boolean>(false);

  const resetPassword = useCallback(
    async ({ email, resetCode, newPassword }: ResetPasswordVars): Promise<void> => {
      setResetPasswordLoading(true);
      setResetPasswordError(null);
      setResetPasswordSuccess(false);

      try {
        const resetPasswordData: ResetPasswordRequest = {
          email,
          resetCode,
          newPassword,
        };

        await authService.resetPassword(resetPasswordData);
        setResetPasswordSuccess(true);
      } catch (err: any) {
        // API client'ı zaten toast gösteriyor, sadece error'ı throw et
        throw err;
      } finally {
        setResetPasswordLoading(false);
      }
    },
[]
  );

  const clearError = useCallback(() => {
    setResetPasswordError(null);
  }, []);

  const clearSuccess = useCallback(() => {
    setResetPasswordSuccess(false);
  }, []);

  return {
    resetPassword,
    resetPasswordLoading,
    resetPasswordError,
    resetPasswordSuccess,
    clearError,
    clearSuccess,
  };
}

