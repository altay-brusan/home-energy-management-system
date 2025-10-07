import { useState, useCallback } from 'react';
import { useAuthStore } from '../store/authStore';
import { authService, LoginResponse } from '../features/auth/service';
import { useErrorHandler } from './useErrorHandler';
import type { AppError } from '../lib/http/error';

export type LoginVars = { email: string; password: string };

export default function useAuth () {
  const { user, setUser, clearUser } = useAuthStore();
  const [loginLoading, setLoginLoading] = useState<boolean>(false);
  const [loginError, setLoginError] = useState<string | null>(null);
  const { handleError } = useErrorHandler();

  const login = useCallback(
    async ({ email, password }: LoginVars): Promise<LoginResponse | void> => {
      setLoginLoading(true);
      setLoginError(null);

      try {
        const res: LoginResponse = await authService.login(email, password);

        if (res.ok) {
          setUser(res.data.user);
          return res;
        }

        const appErr: AppError = {
          code: 'BUSINESS',
          message: res.error ?? 'Login failed, please try again.',
        };
        handleError(appErr);
        setLoginError(appErr.message);
        return res;
      } catch (err: any) {
        const appErr = handleError(err);
        setLoginError(appErr.message);
      } finally {
        setLoginLoading(false);
      }
    },
    [setUser, handleError]
  );

  const logout = useCallback(async (): Promise<void> => {
    try {
      await authService.logout();
    } catch (err) {
      handleError(err);
    } finally {
      clearUser();
    }
  }, [clearUser, handleError]);

  return {
    user,
    isAuthenticated: !!user,
    login,
    loginLoading,
    loginError,
    logout,
  };
}
