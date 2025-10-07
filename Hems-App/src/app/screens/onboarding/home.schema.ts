import { z } from 'zod';

const optionalText = z
  .string()
  .max(80, 'Too long')
  .optional()
  .or(z.literal(''));

export const homeSetupSchema = z.object({
  homeName: z
    .string()
    .min(2, 'Please provide a name for your home.')
    .max(64, 'Home name is too long.'),
  addressLine1: z
    .string()
    .min(6, 'Please enter the street and number.')
    .max(120, 'Address line is too long.'),
  addressLine2: optionalText,
  city: z
    .string()
    .min(2, 'City is required.')
    .max(60, 'City name is too long.'),
  state: optionalText,
  postalCode: z
    .string()
    .min(3, 'Postal code is too short.')
    .max(20, 'Postal code is too long.'),
  country: z
    .string()
    .min(2, 'Country is required.')
    .max(56, 'Country name is too long.'),
});

export type HomeSetupForm = z.infer<typeof homeSetupSchema>;
