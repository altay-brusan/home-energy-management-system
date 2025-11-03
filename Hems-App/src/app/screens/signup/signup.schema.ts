import * as z from 'zod';

export const createSignupSchema = (t: (key: string) => string) => z
  .object({
    firstName: z.string().min(1, t('validation.firstNameRequired')),
    surname: z.string().min(1, t('validation.surnameRequired')),
    email: z.string().email(t('validation.emailInvalid')),
    password: z
      .string()
      .min(8, t('validation.passwordMinLength'))
      .regex(/[A-Z]/, t('validation.passwordUppercase'))
      .regex(/[a-z]/, t('validation.passwordLowercase'))
      .regex(/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/, t('validation.passwordSpecialChar')),
    confirmPassword: z.string().min(1, t('validation.confirmPasswordRequired')),
  })
  .refine((data) => data.password === data.confirmPassword, {
    path: ['confirmPassword'],
    message: t('validation.passwordsNotMatch'),
  });

export type SignUpForm = z.infer<ReturnType<typeof createSignupSchema>>;
