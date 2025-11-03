import { z } from 'zod';

export const getHomeSetupSchema = (t: (key: string) => string) => {
  const optionalText = z
    .string()
    .max(80, t('homeSetup.validation.addressLine2.max'))
    .optional()
    .or(z.literal(''));

  return z.object({
    homeName: z
      .string()
      .min(2, t('homeSetup.validation.homeName.min'))
      .max(64, t('homeSetup.validation.homeName.max')),
    addressLine1: z
      .string()
      .min(6, t('homeSetup.validation.addressLine1.min'))
      .max(120, t('homeSetup.validation.addressLine1.max')),
    addressLine2: optionalText,
    city: z
      .string()
      .max(60, t('homeSetup.validation.city.max'))
      .optional(),
    country: z
      .string()
      .max(56, t('homeSetup.validation.country.max'))
      .optional(),
    state: z
      .string()
      .max(80, t('homeSetup.validation.state.max'))
      .optional()
      .or(z.literal('')),
  });
};

export type HomeSetupForm = z.infer<ReturnType<typeof getHomeSetupSchema>>;
