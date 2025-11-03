import AsyncStorage from '@react-native-async-storage/async-storage';
import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import i18n from '../../lib/i18n';
import { toast } from '../../utils/toast';
import { AccessTokenResponse, RefreshRequest } from '../types/auth.types';
import { ApiError } from '../types/common.types';
import { API_CONFIG, ENDPOINTS } from './config';

const STORAGE_KEYS = {
  ACCESS_TOKEN: 'access_token',
  REFRESH_TOKEN: 'refresh_token',
  TOKEN_EXPIRY: 'token_expiry',
} as const;

class ApiClient {
  private client: AxiosInstance;
  private isRefreshing = false;
  private refreshSubscribers: ((token: string) => void)[] = [];
  private globalRefreshCallback: (() => Promise<void>) | null = null;
  private refreshPromise: Promise<string> | null = null;
  private onRefreshFailureCallback: (() => Promise<void>) | null = null;
  private isLoggingOut = false;
  private abortControllers: AbortController[] = [];
  private refreshFailed = false; // Flag to prevent infinite refresh loops
  private lastLoginError: AxiosError | null = null; // Track last login error to check for email not confirmed

  constructor() {
    this.client = axios.create({
      baseURL: API_CONFIG.BASE_URL,
      timeout: API_CONFIG.TIMEOUT,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors () {
    this.client.interceptors.request.use(
      async (config: InternalAxiosRequestConfig) => {
        if (this.isLoggingOut) {
          const abortController = new AbortController();
          abortController.abort();
          config.signal = abortController.signal;
          return config;
        }

        const token = await this.getAccessToken();
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        if (!config.signal) {
          const abortController = new AbortController();
          config.signal = abortController.signal;
          this.abortControllers.push(abortController);
        }


        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    this.client.interceptors.response.use(
      (response) => {
        // AbortController'ı temizle
        const signal = response.config?.signal;
        if (signal && signal instanceof AbortSignal) {
          this.abortControllers = this.abortControllers.filter(ac => ac.signal !== signal);
        }


        this.showSuccessToast(response);
        return response;
      },
      async (error: AxiosError) => {
        // Logout sırasında hiçbir işlem yapma
        if (this.isLoggingOut) {
          return Promise.reject(error);
        }

        // Aborted request veya CanceledError - sessizce handle et
        if (error.name === 'CanceledError' || error.code === 'ERR_CANCELED' || error.message?.includes('canceled')) {
          return Promise.reject(error);
        }

        // AbortController'ı temizle
        const signal = error.config?.signal;
        if (signal && signal instanceof AbortSignal) {
          this.abortControllers = this.abortControllers.filter(ac => ac.signal !== signal);
        }


        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        // Login endpoint'inden gelen 401 hatası için token refresh yapma (email not confirmed gibi durumlar için)
        const requestUrl = originalRequest.url || '';
        const fullUrl = `${originalRequest.baseURL || ''}${requestUrl}`;
        const isLoginEndpoint = requestUrl.includes(ENDPOINTS.AUTH.LOGIN) || fullUrl.includes(ENDPOINTS.AUTH.LOGIN) || requestUrl.includes('/Users/login') || fullUrl.includes('/Users/login');
        
        if (error.response?.status === 401 && isLoginEndpoint) {
          this.lastLoginError = error;
          return Promise.reject(this.handleError(error));
        }

        if (error.response?.status === 401 && !originalRequest._retry && !this.refreshFailed && !this.isLoggingOut) {
          originalRequest._retry = true;

          const refreshToken = await this.getRefreshToken();
          if (!refreshToken) {
            if (this.onRefreshFailureCallback && !this.isLoggingOut) {
              if (!this.isEmailNotConfirmedError(error)) {
                this.onRefreshFailureCallback().catch(() => {});
              }
            }
            return Promise.reject(error);
          }

          if (this.isRefreshing && this.refreshPromise) {
            return this.refreshPromise.then((token) => {
              if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${token}`;
              }
              return this.client(originalRequest);
            }).catch((_refreshError) => {
              if (this.onRefreshFailureCallback && !this.isLoggingOut) {
                if (!this.isEmailNotConfirmedError(error)) {
                  this.onRefreshFailureCallback().catch(() => {});
                }
              }
              return Promise.reject(error);
            });
          }

          if (!this.isRefreshing) {
            this.isRefreshing = true;
            this.refreshPromise = this.performTokenRefresh();
          }

          return this.refreshPromise!.then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return this.client(originalRequest);
          }).catch((_refreshError) => {
            if (this.onRefreshFailureCallback && !this.isLoggingOut) {
              if (!this.isEmailNotConfirmedError(error)) {
                this.onRefreshFailureCallback().catch(() => {});
              }
            }
            return Promise.reject(error);
          });
        }

        // Eğer 401 ve _retry true ise, token refresh zaten başarısız oldu
        if (error.response?.status === 401 && originalRequest._retry) {
          const apiError = this.handleError(error);
          // Login endpoint'inden gelen email not confirmed hatası için toast gösterme
          if (this.isEmailNotConfirmedError(error)) {
            return Promise.reject(apiError);
          }
          // Session expired için onRefreshFailureCallback çağrılacak
          if (this.onRefreshFailureCallback && !this.isLoggingOut) {
            if (!this.isEmailNotConfirmedError(error)) {
              this.onRefreshFailureCallback().catch(() => {});
            }
          }
          return Promise.reject(apiError);
        }

        // Eğer refreshFailed ise, hiçbir yeni 401'i retry etme
        if (error.response?.status === 401 && this.refreshFailed) {
          const apiError = this.handleError(error);
          // Login endpoint'inden gelen email not confirmed hatası için toast gösterme
          if (this.isEmailNotConfirmedError(error)) {
            return Promise.reject(apiError);
          }
          // Session expired için onRefreshFailureCallback çağrılacak
          if (this.onRefreshFailureCallback && !this.isLoggingOut) {
            if (!this.isEmailNotConfirmedError(error)) {
              this.onRefreshFailureCallback().catch(() => {});
            }
          }
          return Promise.reject(apiError);
        }

        return Promise.reject(this.handleError(error));
      }
    );
  }

  private async refreshTokenRequest (refreshToken: string): Promise<AccessTokenResponse> {
    const response = await axios.post<AccessTokenResponse>(
      `${API_CONFIG.BASE_URL}/Users/refresh`,
      { refreshToken } as RefreshRequest
    );
    return response.data;
  }

  private async performTokenRefresh (): Promise<string> {
    try {
      const refreshToken = await this.getRefreshToken();
      if (!refreshToken) {
        await this.clearTokens();

        if (this.onRefreshFailureCallback && !this.isLoggingOut) {
          if (!this.isEmailNotConfirmedError(this.lastLoginError as any)) {
            this.onRefreshFailureCallback().catch(() => {});
          }
          this.lastLoginError = null;
        }

        return Promise.reject(new Error('No refresh token available'));
      }

      const response = await this.refreshTokenRequest(refreshToken);
      await this.saveTokens(response);

      if (this.globalRefreshCallback) {
        this.globalRefreshCallback().catch(() => {});
      }

      return response.accessToken;
    } catch (error) {
      this.refreshFailed = true; // Set flag to prevent infinite refresh loops
      await this.clearTokens();

      if (this.onRefreshFailureCallback && !this.isLoggingOut) {
        if (!this.isEmailNotConfirmedError(this.lastLoginError as any)) {
          this.onRefreshFailureCallback().catch(() => {});
        }
        this.lastLoginError = null;
      }

      throw error;
    } finally {
      this.isRefreshing = false;
      this.refreshPromise = null;
    }
  }

  public registerGlobalRefreshCallback (callback: () => Promise<void>): void {
    this.globalRefreshCallback = callback;
  }

  public updateGlobalRefreshCallback (callback: () => Promise<void>): void {
    if (this.globalRefreshCallback) {
      this.globalRefreshCallback = callback;
    }
  }

  public unregisterGlobalRefreshCallback (): void {
    this.globalRefreshCallback = null;
  }

  public registerRefreshFailureCallback (callback: () => Promise<void>): void {
    this.onRefreshFailureCallback = callback;
  }

  public updateRefreshFailureCallback (callback: () => Promise<void>): void {
    if (this.onRefreshFailureCallback) {
      this.onRefreshFailureCallback = callback;
    }
  }

  public unregisterRefreshFailureCallback (): void {
    this.onRefreshFailureCallback = null;
  }

  public setIsLoggingOut (value: boolean): void {
    this.isLoggingOut = value;
    if (value) {
      this.cancelAllPendingRequests();
    }
  }

  public getIsLoggingOut (): boolean {
    return this.isLoggingOut;
  }

  private cancelAllPendingRequests (): void {
    this.abortControllers.forEach(controller => {
      controller.abort();
    });
    this.abortControllers = [];
  }

  private isEmailNotConfirmedError (error: AxiosError | null): boolean {
    if (!error) return false;
    const data = error.response?.data as any;
    const detail = data?.detail || data?.message;
    const requestUrl = error.config?.url || '';
    const fullUrl = `${error.config?.baseURL || ''}${requestUrl}`;
    const isLoginEndpoint = requestUrl.includes(ENDPOINTS.AUTH.LOGIN) || fullUrl.includes(ENDPOINTS.AUTH.LOGIN) || requestUrl.includes('/Users/login') || fullUrl.includes('/Users/login');
    return isLoginEndpoint && typeof detail === 'string' && /email\s+not\s+confirmed/i.test(detail);
  }

  private handleError (error: AxiosError): ApiError {
    let apiError: ApiError;

    if (error.response) {
      const responseError = error.response.data as ApiError;
      apiError = {
        status: error.response.status,
        title: responseError.title || i18n.t('api.error.defaultTitle'),
        detail: responseError.detail,
        message: responseError.message,
        type: responseError.type,
        errors: responseError.errors,
        url: error.config?.url || '',
      };
    } else if (error.request) {
      apiError = {
        status: 0,
        title: i18n.t('api.error.networkError'),
        detail: i18n.t('api.error.networkErrorMessage'),
        message: i18n.t('api.error.networkErrorMessage'),
        url: error.config?.url || '',
      };
    } else {
      apiError = {
        status: 0,
        title: i18n.t('api.error.unknownError'),
        detail: error.message || i18n.t('api.error.unknownErrorMessage'),
        message: error.message || i18n.t('api.error.unknownErrorMessage'),
        url: error.config?.url || '',
      };
    }

    if (apiError.status === 401) {
      // Login endpoint'inden gelen email not confirmed hatası için toast gösterme
      if (this.isEmailNotConfirmedError(error)) {
        return apiError;
      }
      return apiError;
    }

    // Logout sırasında toast gösterme
    if (!this.isLoggingOut) {
      this.showErrorToast(apiError);
    }

    return apiError;
  }

  private showErrorToast (apiError: ApiError) {
    if (apiError.status === 401) {
      return;
    }

    if (apiError.status === 403) {
      return;
    }

    let title = i18n.t('api.error.defaultTitle');
    let message = i18n.t('api.error.defaultMessage');

    if (apiError.detail) {
      message = this.translateBackendMessage(apiError.detail, apiError.url);
      title = this.translateBackendTitle(apiError.title || '', apiError.url) || i18n.t('common.error');
    } else if (apiError.message) {
      message = this.translateBackendMessage(apiError.message, apiError.url);
      title = this.translateBackendTitle(apiError.title || '', apiError.url) || i18n.t('common.error');
    } else {
      title = this.translateBackendTitle(apiError.title || '', apiError.url) || i18n.t('common.error');
      if (apiError.status === 400) {
        title = i18n.t('api.error.invalidRequest');
        message = i18n.t('api.error.invalidRequestMessage');
      } else if (apiError.status === 404) {
        title = i18n.t('api.error.notFound');
        message = i18n.t('api.error.notFoundMessage');
      } else if (apiError.status === 409) {
        title = i18n.t('api.error.conflict');
        message = i18n.t('api.error.conflictMessage');
      } else if (apiError.status === 422) {
        title = i18n.t('api.error.validationError');
        message = i18n.t('api.error.validationErrorMessage');
      } else if (apiError.status === 500) {
        title = i18n.t('api.error.serverError');
        message = i18n.t('api.error.serverErrorMessage');
      } else if (apiError.status === 0) {
        title = i18n.t('api.error.connectionError');
        message = i18n.t('api.error.connectionErrorMessage');
      }
    }

    toast.error(message, title);
  }

  private translateBackendMessage (backendMessage: string, url?: string): string {
    const message = backendMessage.toLowerCase().trim();

    if (url && (url.includes(ENDPOINTS.AUTH.LOGIN) || url.includes(ENDPOINTS.AUTH.FORGOT_PASSWORD))) {
      if (message === 'lockedout') {
        return i18n.t('errors.lockedOut');
      }

      if (message === 'failed') {
        return i18n.t('errors.loginFailed');
      }

      if (message === 'not found') {
        return i18n.t('errors.loginNotFound');
      }

      if (message === 'invalidcredentials') {
        return i18n.t('errors.invalidCredentials');
      }

      if (message === 'usernotfound') {
        return i18n.t('errors.loginNotFound');
      }
    }

    if (message === 'lockedout') {
      return i18n.t('errors.lockedOut');
    }

    if (message === 'failed') {
      return i18n.t('errors.failed');
    }

    if (message === 'not found') {
      return i18n.t('errors.notFound');
    }

    if (message === 'invalidcredentials') {
      return i18n.t('errors.invalidCredentials');
    }

    if (message === 'usernotfound') {
      return i18n.t('errors.userNotFound');
    }

    if (message === 'emailalreadyexists') {
      return i18n.t('errors.emailAlreadyExists');
    }

    if (message === 'passwordtooweak') {
      return i18n.t('errors.passwordTooWeak');
    }

    if (message === 'tokenexpired') {
      return i18n.t('errors.tokenExpired');
    }

    if (message === 'networkerror') {
      return i18n.t('errors.networkError');
    }

    if (message === 'servererror') {
      return i18n.t('errors.serverError');
    }

    if (message === 'validationerror') {
      return i18n.t('errors.validationError');
    }

    if (message === 'unauthorized') {
      return i18n.t('errors.unauthorized');
    }

    if (message === 'forbidden') {
      return i18n.t('errors.forbidden');
    }

    if (message === 'timeout') {
      return i18n.t('errors.timeout');
    }

    if (message === 'ratelimit') {
      return i18n.t('errors.rateLimit');
    }

    return backendMessage;
  }

  private translateBackendTitle (backendTitle: string, url?: string): string {
    if (!backendTitle) return '';

    const title = backendTitle.toLowerCase().trim();

    if (url && (url.includes(ENDPOINTS.AUTH.LOGIN) || url.includes(ENDPOINTS.AUTH.FORGOT_PASSWORD))) {
      if (title === 'not found') {
        return i18n.t('common.error');
      }

      if (title === 'unauthorized') {
        return i18n.t('common.error');
      }

      if (title === 'forbidden') {
        return i18n.t('common.error');
      }

      if (title === 'bad request') {
        return i18n.t('common.error');
      }
    }

    if (title === 'not found') {
      return i18n.t('errors.notFound');
    }

    if (title === 'unauthorized') {
      return i18n.t('errors.unauthorized');
    }

    if (title === 'forbidden') {
      return i18n.t('errors.forbidden');
    }

    if (title === 'bad request') {
      return i18n.t('errors.validationError');
    }

    if (title === 'server error') {
      return i18n.t('errors.serverError');
    }

    if (title === 'network error') {
      return i18n.t('errors.networkError');
    }

    if (title === 'timeout') {
      return i18n.t('errors.timeout');
    }

    if (title === 'rate limit') {
      return i18n.t('errors.rateLimit');
    }

    return backendTitle;
  }

  private showSuccessToast (response: any) {
    const url = response.config?.url || '';
    const method = response.config?.method?.toUpperCase() || '';

    if (url.includes(ENDPOINTS.AUTH.REGISTER)) {
      toast.success(i18n.t('auth.accountCreated'), i18n.t('common.success'));
    } else if (url.includes(ENDPOINTS.AUTH.FORGOT_PASSWORD)) {
      toast.success(i18n.t('auth.resetLinkSent'), i18n.t('common.success'));
    } else if (url.includes(ENDPOINTS.AUTH.RESET_PASSWORD)) {
      toast.success(i18n.t('auth.passwordResetSuccess'), i18n.t('common.success'));
    } else if (url.includes(ENDPOINTS.AUTH.MANAGE_INFO) && method === 'PUT') {
      toast.success(i18n.t('profileInfo.profileUpdated'), i18n.t('common.success'));
    } else if (url.includes(ENDPOINTS.AUTH.LOGIN)) {
      toast.success(i18n.t('auth.loginSuccess'), i18n.t('auth.welcome'));
      return;
    }

    if (method === 'POST' && response.status === 201) {
      toast.success(i18n.t('api.success.created'), i18n.t('api.success.createdTitle'));
    } else if (method === 'PUT' && response.status === 200) {
      toast.success(i18n.t('api.success.updated'), i18n.t('api.success.updatedTitle'));
    } else if (method === 'DELETE' && response.status === 200) {
      toast.success(i18n.t('api.success.deleted'), i18n.t('api.success.deletedTitle'));
    }
  }

  async saveTokens (tokens: AccessTokenResponse): Promise<void> {
    const expiryTime = Date.now() + tokens.expiresIn * 1000;
    await AsyncStorage.multiSet([
      [STORAGE_KEYS.ACCESS_TOKEN, tokens.accessToken],
      [STORAGE_KEYS.REFRESH_TOKEN, tokens.refreshToken],
      [STORAGE_KEYS.TOKEN_EXPIRY, expiryTime.toString()],
    ]);
    this.refreshFailed = false; // Reset flag on successful token save
  }

  async getAccessToken (): Promise<string | null> {
    return await AsyncStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
  }

  async getRefreshToken (): Promise<string | null> {
    return await AsyncStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN);
  }

  async clearTokens (): Promise<void> {
    await AsyncStorage.multiRemove([
      STORAGE_KEYS.ACCESS_TOKEN,
      STORAGE_KEYS.REFRESH_TOKEN,
      STORAGE_KEYS.TOKEN_EXPIRY,
    ]);
    this.refreshFailed = false;
  }

  async isTokenExpired (): Promise<boolean> {
    const expiryTime = await AsyncStorage.getItem(STORAGE_KEYS.TOKEN_EXPIRY);
    if (!expiryTime) return true;

    const now = Date.now();
    const expiry = parseInt(expiryTime, 10);

    return now >= expiry;
  }

  async shouldRefreshToken (): Promise<boolean> {
    const expiryTime = await AsyncStorage.getItem(STORAGE_KEYS.TOKEN_EXPIRY);
    if (!expiryTime) return true;

    const now = Date.now();
    const expiry = parseInt(expiryTime, 10);

    return now >= expiry - API_CONFIG.TOKEN_REFRESH_THRESHOLD;
  }

  getClient (): AxiosInstance {
    return this.client;
  }
}

const apiClientInstance = new ApiClient();

export const apiClient = apiClientInstance;
export default apiClientInstance.getClient();

