import { z } from 'zod';

export const deviceSetupSchema = z.object({
  deviceName: z
    .string()
    .min(2, 'Please give the device a name.')
    .max(64, 'Device name is too long.'),
  serialNumber: z
    .string()
    .min(6, 'Serial number must be at least 6 characters.')
    .max(40, 'Serial number is too long.'),
});

export type DeviceSetupForm = z.infer<typeof deviceSetupSchema>;
