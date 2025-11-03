import * as z from 'zod';

export const createForgotPasswordSchema = (t: (key: string) => string) => z.object({
  email: z.string().email(t('validation.emailInvalid')).min(1, t('validation.emailRequired')),
});

export type ForgotPasswordForm = z.infer<ReturnType<typeof createForgotPasswordSchema>>;
