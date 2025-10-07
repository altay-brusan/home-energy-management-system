import * as z from 'zod';

export const signupSchema = z
  .object({
    name: z.string().min(1, 'Name is required'),
    email: z.string().email('Please enter a valid email'),
    password: z.string().min(6, 'Password must be at least 6 characters'),
    confirmPassword: z.string().min(6, 'Please confirm your password'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match',
  });

export type SignUpForm = z.infer<typeof signupSchema>;
