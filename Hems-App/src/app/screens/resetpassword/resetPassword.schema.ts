import * as z from 'zod';

export const createResetPasswordSchema = (t: (key: string) => string) => z
  .object({
    resetCode: z.string().min(1, t('validation.resetCodeRequired')),
    newPassword: z
      .string()
      .min(8, t('validation.passwordMinLength'))
      .regex(/[A-Z]/, t('validation.passwordUppercase'))
      .regex(/[a-z]/, t('validation.passwordLowercase'))
      .regex(/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/, t('validation.passwordSpecialChar')),
    confirmPassword: z.string().min(1, t('validation.confirmPasswordRequired')),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    path: ['confirmPassword'],
    message: t('validation.passwordsNotMatch'),
  });

export type ResetPasswordForm = z.infer<ReturnType<typeof createResetPasswordSchema>>;

