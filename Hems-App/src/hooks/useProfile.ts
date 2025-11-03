import { useState, useCallback } from 'react';
import { authService } from '../api';
import { UserInfoDTO, InfoRequest, InfoResponse } from '../api/types/auth.types';
import { useAuthStore } from '../store/authStore';
import { saveUserData, UserData } from '../lib/storage/token';

export type UpdateProfileVars = {
  firstName: string;
  lastName: string;
  phone?: string;
  email?: string;
};

export type ChangePasswordVars = {
  oldPassword: string;
  newPassword: string;
};

export default function useProfile () {
  const [profileLoading, setProfileLoading] = useState<boolean>(false);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [profileSuccess, setProfileSuccess] = useState<boolean>(false);
  const [changePasswordLoading, setChangePasswordLoading] = useState<boolean>(false);
  const [changePasswordError, setChangePasswordError] = useState<string | null>(null);
  const [changePasswordSuccess, setChangePasswordSuccess] = useState<boolean>(false);
  const { setUser } = useAuthStore();

  const getUserInfo = useCallback(async (): Promise<UserInfoDTO> => {
    try {
      const userInfo = await authService.getUserInfo();
      return userInfo;
    } catch (err: any) {
      throw err;
    }
  }, []);

  const updateProfile = useCallback(
    async ({ firstName, lastName, phone, email }: UpdateProfileVars): Promise<void> => {
      setProfileLoading(true);
      setProfileError(null);
      setProfileSuccess(false);

      try {
        await authService.updateProfile({
          firstName,
          surname: lastName,
          phoneNumber: phone,
          email: email,
        });

        const updatedUserInfo = await authService.getUserInfo();
        const userData: UserData = {
          id: updatedUserInfo.email,
          email: updatedUserInfo.email,
          name: updatedUserInfo.name || undefined,
          surname: updatedUserInfo.surname || undefined,
          roles: updatedUserInfo.roles,
        };

        await saveUserData(userData);
        setUser(userData);
        setProfileSuccess(true);
      } catch (err: any) {
        setProfileError(err.message || 'Profile update failed');
        throw err;
      } finally {
        setProfileLoading(false);
      }
    },
    [setUser]
  );

  const changePassword = useCallback(
    async ({ oldPassword, newPassword }: ChangePasswordVars): Promise<void> => {
      setChangePasswordLoading(true);
      setChangePasswordError(null);
      setChangePasswordSuccess(false);

      try {
        const changePasswordData: InfoRequest = {
          newEmail: null,
          newPassword,
          oldPassword,
        };

        await authService.updateManageInfo(changePasswordData);
        setChangePasswordSuccess(true);
      } catch (err: any) {
        throw err;
      } finally {
        setChangePasswordLoading(false);
      }
    },
    []
  );

  const clearProfileError = useCallback(() => {
    setProfileError(null);
  }, []);

  const clearProfileSuccess = useCallback(() => {
    setProfileSuccess(false);
  }, []);

  const clearChangePasswordError = useCallback(() => {
    setChangePasswordError(null);
  }, []);

  const clearChangePasswordSuccess = useCallback(() => {
    setChangePasswordSuccess(false);
  }, []);

  return {
    getUserInfo,
    updateProfile,
    changePassword,
    profileLoading,
    profileError,
    profileSuccess,
    changePasswordLoading,
    changePasswordError,
    changePasswordSuccess,
    clearProfileError,
    clearProfileSuccess,
    clearChangePasswordError,
    clearChangePasswordSuccess,
  };
}
