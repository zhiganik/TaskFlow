import { z } from 'zod'

export const updateProfileSchema = z.object({
  displayName: z
    .string()
    .min(1, 'Display name is required.')
    .max(100, 'Display name must be 100 characters or fewer.'),
  email: z.string().min(1, 'Email is required.').email('Email must be a valid email address.'),
  avatarColor: z
    .string()
    .regex(/^#[0-9a-fA-F]{6}$/, 'Must be a valid hex color.'),
})

export type UpdateProfileFormValues = z.infer<typeof updateProfileSchema>

export const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, 'Current password is required.'),
  newPassword: z
    .string()
    .min(1, 'New password is required.')
    .min(8, 'Password must be at least 8 characters.')
    .regex(/\d/, 'Password must contain at least one number.')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.'),
})

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>
