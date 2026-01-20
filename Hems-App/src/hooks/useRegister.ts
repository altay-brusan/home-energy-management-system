import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { authService } from '../api';
import { RegisterRequest } from '../api/types/auth.types';
import { mapToAppError } from '../lib/http/error';

export type RegisterVars = {
  firstName: string;
  surname: string;
  email: string;
  password: string;
};

export default function useRegister () {
  const { t } = useTranslation();
  const [registerLoading, setRegisterLoading] = useState<boolean>(false);
  const [registerError, setRegisterError] = useState<string | null>(null);
  const [registerSuccess, setRegisterSuccess] = useState<boolean>(false);

  const register = useCallback(
    async ({ firstName, surname, email, password }: RegisterVars): Promise<void> => {
      setRegisterLoading(true);
      setRegisterError(null);
      setRegisterSuccess(false);

      try {
        const registerData: RegisterRequest = {
          firstName,
          surname,
          email,
          password,
        };

        await authService.register(registerData);
        setRegisterSuccess(true);
      } catch (err: any) {
        // Extract error message from backend response
        const responseData = err?.response?.data || err;

        // Check for validation errors format: { errors: { FieldName: ["message"] } }
        if (responseData?.errors && typeof responseData.errors === 'object') {
          const errorMessages: string[] = [];
          Object.values(responseData.errors).forEach((messages: any) => {
            if (Array.isArray(messages)) {
              errorMessages.push(...messages);
            }
          });
          if (errorMessages.length > 0) {
            setRegisterError(errorMessages.join('\n'));
            throw err;
          }
        }

        // Check for detail or message field
        if (responseData?.detail) {
          setRegisterError(responseData.detail);
          throw err;
        }

        if (responseData?.message) {
          setRegisterError(responseData.message);
          throw err;
        }

        // Fallback to generic error
        setRegisterError(t('auth.registrationError'));
        throw err;
      } finally {
        setRegisterLoading(false);
      }
    },
    []
  );

  const clearError = useCallback(() => {
    setRegisterError(null);
  }, []);

  const clearSuccess = useCallback(() => {
    setRegisterSuccess(false);
  }, []);

  return {
    register,
    registerLoading,
    registerError,
    registerSuccess,
    clearError,
    clearSuccess,
  };
}

