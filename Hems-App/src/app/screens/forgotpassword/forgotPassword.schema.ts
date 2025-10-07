import * as z from 'zod';

export const forgotPasswordSchema = z.object({
  email: z.string().email('Please enter a valid email').min(1, 'Email is required'),
});

export type ForgotPasswordForm = z.infer<typeof forgotPasswordSchema>;
