import { useState, useCallback, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../store/authStore';
import { useSitesStore } from '../store/sitesStore';
import { authService } from '../api/services/authService';
import { apiClient } from '../api/client/apiClient';
import { signalRService } from '../services/signalRService';
import { saveUserData, removeUserData, UserData } from '../lib/storage/token';
import { replace } from '../app/routes';
import { toast } from '../utils/toast';
import { mapToAppError } from '../lib/http/error';
import type { LoginRequest, AccessTokenResponse } from '../api/types/auth.types';

export type LoginVars = { email: string; password: string };

export default function useAuth () {
  const { t } = useTranslation();
  const { user, setUser, clearAuth } = useAuthStore();
  const clearSites = useSitesStore(s => s.clearSites);
  const [loginLoading, setLoginLoading] = useState<boolean>(false);
  const [loginError, setLoginError] = useState<string | null>(null);

  const login = useCallback(
    async ({ email, password }: LoginVars): Promise<void> => {
      setLoginLoading(true);
      setLoginError(null);

      try {
        const loginData: LoginRequest = {
          email,
          password,
          twoFactorCode: null,
          twoFactorRecoveryCode: null,
        };

        const tokenResponse: AccessTokenResponse = await authService.login(loginData);
        await apiClient.saveTokens(tokenResponse);

        const userInfo = await authService.getUserInfo();

        const userData: UserData = {
          id: email,
          email: userInfo.email,
          name: userInfo.name || userInfo.surname || 'User',
          surname: userInfo.surname || undefined,
          roles: userInfo.roles,
        };

        await saveUserData(userData);
        setUser(userData);
      } catch (err: any) {
        const appError = mapToAppError(err);// Handle specific login errors
        const detail: string | undefined = err?.response?.data?.detail || err?.response?.data?.message || err?.detail || err?.message;
        if (err?.response?.status === 401 || err?.status === 401) {
          if (typeof detail === 'string' && /email\s+not\s+confirmed/i.test(detail)) {
            setLoginError(t('errors.emailNotConfirmed'));
            toast.error(t('errors.emailNotConfirmed'), t('common.error'));

          } else {
            setLoginError(t('auth.invalidCredentials'));
            toast.error(detail || t('auth.invalidCredentials'), t('common.error'));
          }
        } else if (err?.status === 400) {
          // Check for specific error messages
          const errorData = err?.data;
          if (errorData?.message?.includes('user') || errorData?.message?.includes('kullanıcı')) {
            setLoginError(t('auth.userNotFound'));
          } else if (errorData?.message?.includes('password') || errorData?.message?.includes('şifre')) {
            setLoginError(t('auth.invalidPassword'));
          } else {
            setLoginError(appError.message);
          }
        } else {
          setLoginError(appError.message);
        }

        throw err;
      } finally {
        setLoginLoading(false);
      }
    },
    [setUser]
  );

  const clearLoginError = useCallback(() => {
    setLoginError(null);
  }, []);

  const logout = useCallback(async (): Promise<void> => {
    // Logout başladı - önce tüm pending request'leri iptal et
    apiClient.setIsLoggingOut(true);

    // SignalR bağlantısını kapat
    try {
      await signalRService.disconnect();
    } catch (error) {
      // Silent error handling
    }

    // Sites store'u temizle
    await clearSites();

    // HEMEN login sayfasına yönlendir - component'ler unmount olsun
    replace('Login');

    // Background'da temizlik işlemlerini yap
    setTimeout(async () => {
      try {
        await apiClient.clearTokens();
        await removeUserData();
        await clearAuth();
        toast.success(t('auth.logoutSuccess'), t('auth.goodbye'));
      } catch (err) {
        // Hata olsa bile login'e yönlendir
        await apiClient.clearTokens();
        await removeUserData();
        await clearAuth();
      } finally {
        apiClient.setIsLoggingOut(false);
      }
    }, 50); // Component'lerin unmount olması için kısa bir delay
  }, [clearAuth, clearSites]);

  const handleRefreshFailure = useCallback(async (): Promise<void> => {
    // Logout başladı - önce tüm pending request'leri iptal et
    apiClient.setIsLoggingOut(true);

    // SignalR bağlantısını kapat
    try {
      await signalRService.disconnect();
    } catch (error) {
      // Silent error handling
    }

    // Sites store'u temizle
    await clearSites();

    // HEMEN login sayfasına yönlendir - component'ler unmount olsun
    replace('Login');

    // Background'da temizlik işlemlerini yap
    setTimeout(async () => {
      try {
        await apiClient.clearTokens();
        await removeUserData();
        await clearAuth();
        toast.error(t('auth.sessionExpired'), t('auth.pleaseLogin'));
      } catch (error) {
        await apiClient.clearTokens();
        await removeUserData();
        await clearAuth();
      } finally {
        apiClient.setIsLoggingOut(false);
      }
    }, 50);
  }, [clearAuth, clearSites]);

  useEffect(() => {
    apiClient.registerRefreshFailureCallback(handleRefreshFailure);

    return () => {
      apiClient.unregisterRefreshFailureCallback();
    };
  }, [handleRefreshFailure]);

  return {
    user,
    isAuthenticated: !!user,
    login,
    loginLoading,
    loginError,
    clearLoginError,
    logout,
  };
}
