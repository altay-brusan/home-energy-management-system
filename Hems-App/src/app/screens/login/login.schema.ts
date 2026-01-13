import { z } from "zod";
import { zEmail, zPassword } from "../../../lib/validation/zod-helpers";

export const loginSchema = z.object({
  email: zEmail,
  password: zPassword,
});

export type LoginForm = z.infer<typeof loginSchema>;