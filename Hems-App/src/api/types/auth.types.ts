export interface LoginRequest {
  email: string;
  password: string;
  twoFactorCode?: string | null;
  twoFactorRecoveryCode?: string | null;
}

export interface AccessTokenResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface RegisterRequest {
  email: string;
  firstName: string;
  surname: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  resetCode: string;
  newPassword: string;
}

export interface ResendConfirmationEmailRequest {
  email: string;
}

export enum UserStatusType {
  Complete = 1,
  NeedsSite = 2,
  NeedsSetup = 3,
  Error = 4,
}

export interface UserStatusDataDto {
  siteId?: string | null;
  siteName?: string | null;
  setupId?: string | null;
  setupName?: string | null;
  deviceId?: string | null;
  deviceName?: string | null;
}

export interface UserStatusDto {
  status: UserStatusType;
  redirectTo: string;
  message: string;
  data?: UserStatusDataDto | null;
}

export interface UserInfoDTO {
  email: string;
  name?: string | null;
  surname?: string | null;
  phoneNumber?: string | null;
  roles: string[];
}

export interface InfoResponse {
  email: string;
  isEmailConfirmed: boolean;
}

export interface InfoRequest {
  newEmail?: string | null;
  newPassword?: string | null;
  oldPassword?: string | null;
}

export interface TwoFactorRequest {
  enable?: boolean | null;
  twoFactorCode?: string | null;
  resetSharedKey: boolean;
  resetRecoveryCodes: boolean;
  forgetMachine: boolean;
}

export interface TwoFactorResponse {
  sharedKey: string;
  recoveryCodesLeft: number;
  recoveryCodes?: string[] | null;
  isTwoFactorEnabled: boolean;
  isMachineRemembered: boolean;
}

export interface UpdateUserCommand {
  firstName?: string | null;
  surname?: string | null;
  email?: string | null;
  phoneNumber?: string | null;
}

