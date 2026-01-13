import apiClient from '../client/apiClient';
import { ENDPOINTS } from '../client/config';
import {
  LoginRequest,
  AccessTokenResponse,
  RefreshRequest,
  RegisterRequest,
  UserStatusDto,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  UserInfoDTO,
  InfoRequest,
  InfoResponse,
  TwoFactorRequest,
  TwoFactorResponse,
  ResendConfirmationEmailRequest,
  UpdateUserCommand,
} from '../types/auth.types';

export const authService = {
  async login(data: LoginRequest): Promise<AccessTokenResponse> {
    const response = await apiClient.post<AccessTokenResponse>(
      ENDPOINTS.AUTH.LOGIN,
      data
    );
    return response.data;
  },

  async register(data: RegisterRequest): Promise<void> {
    await apiClient.post(ENDPOINTS.AUTH.REGISTER, data);
  },

  async refreshToken(data: RefreshRequest): Promise<AccessTokenResponse> {
    const response = await apiClient.post<AccessTokenResponse>(
      ENDPOINTS.AUTH.REFRESH,
      data
    );
    return response.data;
  },

  async getUserStatus(): Promise<UserStatusDto> {
    const response = await apiClient.get<UserStatusDto>(
      ENDPOINTS.AUTH.USER_STATUS
    );
    return response.data;
  },

  async getUserStatusMobile(): Promise<UserStatusDto> {
    const response = await apiClient.get<UserStatusDto>(
      ENDPOINTS.AUTH.USER_STATUS_MOBILE
    );
    return response.data;
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<void> {
    await apiClient.post(ENDPOINTS.AUTH.FORGOT_PASSWORD, data);
  },

  async resetPassword(data: ResetPasswordRequest): Promise<void> {
    await apiClient.post(ENDPOINTS.AUTH.RESET_PASSWORD, data);
  },

  async confirmEmail(userId: string, code: string, changedEmail?: string): Promise<void> {
    await apiClient.get(ENDPOINTS.AUTH.CONFIRM_EMAIL, {
      params: { userId, code, changedEmail },
    });
  },

  async resendConfirmationEmail(data: ResendConfirmationEmailRequest): Promise<void> {
    await apiClient.post(ENDPOINTS.AUTH.RESEND_CONFIRMATION, data);
  },

  async getUserInfo(): Promise<UserInfoDTO> {
    const response = await apiClient.get<UserInfoDTO>(
      ENDPOINTS.AUTH.USER_INFO
    );
    return response.data;
  },

  async getManageInfo(): Promise<InfoResponse> {
    const response = await apiClient.get<InfoResponse>(
      ENDPOINTS.AUTH.MANAGE_INFO
    );
    return response.data;
  },

  async updateManageInfo(data: InfoRequest): Promise<InfoResponse> {
    const response = await apiClient.post<InfoResponse>(
      ENDPOINTS.AUTH.MANAGE_INFO,
      data
    );
    return response.data;
  },

  async updateProfile(data: UpdateUserCommand): Promise<void> {
    await apiClient.put(ENDPOINTS.AUTH.UPDATE_PROFILE, data);
  },

  async manage2FA(data: TwoFactorRequest): Promise<TwoFactorResponse> {
    const response = await apiClient.post<TwoFactorResponse>(
      ENDPOINTS.AUTH.MANAGE_2FA,
      data
    );
    return response.data;
  },
};

