import { z } from 'zod';

export const getHomeSetupSchema = (t: (key: string) => string) => {
  return z.object({
    homeName: z
      .string()
      .min(2, t('homeSetup.validation.homeName.min'))
      .max(64, t('homeSetup.validation.homeName.max')),
    addressLine: z
      .string()
      .max(200, t('homeSetup.validation.addressLine.max'))
      .optional()
      .or(z.literal('')),
    countryId: z.string().optional(),
    cityId: z.string().optional(),
  });
};

export type HomeSetupForm = z.infer<ReturnType<typeof getHomeSetupSchema>>;
