import { z } from "zod";

export const zEmail = z
  .string({ required_error: "Email is required" })
  .trim()
  .min(1, "Email is required")
  .email("Please enter a valid email")
  .transform(v => v.toLowerCase());

export const zPassword = z
  .string({ required_error: "Password is required" })
  .min(6, "Password must be at least 6 characters");

export const zRegion = z.enum(
  ["Europe", "America", "Asia", "Africa", "Oceania"],
  { required_error: "Please select a region" }
);

export function safeParseOrThrow<T extends z.ZodTypeAny>(schema: T, data: unknown) {
  const res = schema.safeParse(data);
  if (!res.success) {
    throw res.error;
  }
  return res.data as z.infer<T>;
}
