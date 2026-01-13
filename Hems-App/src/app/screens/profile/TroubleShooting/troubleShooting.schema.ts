import { z } from 'zod';
import { useTranslation } from 'react-i18next';

// opsiyonel ama i18n uyumu için:
export const useTroubleShootingSchema = () => {
  const { t } = useTranslation();

  const troubleShootingSchema = z.object({
    faultType: z
      .string()
      .min(1, t('troubleshooting.validation.faultTypeRequired')),
    description: z
      .string()
      .min(10, t('troubleshooting.validation.descriptionMin'))
      .max(500, t('troubleshooting.validation.descriptionMax')),
    name: z
      .string()
      .min(2, t('troubleshooting.validation.nameMin'))
      .max(64, t('troubleshooting.validation.nameMax')),
    phone: z
      .string()
      .regex(/^[0-9]{10,15}$/, t('troubleshooting.validation.phoneInvalid')), // sadece rakam
    address: z
      .string()
      .min(5, t('troubleshooting.validation.addressMin'))
      .max(120, t('troubleshooting.validation.addressMax')),
    zip: z
      .string()
      .regex(/^\d{4,10}$/, t('troubleshooting.validation.zipInvalid')),
    email: z
      .string()
      .email(t('troubleshooting.validation.emailInvalid')),
  });

  return troubleShootingSchema;
};

export type TroubleShootingForm = z.infer<ReturnType<typeof useTroubleShootingSchema>>;
