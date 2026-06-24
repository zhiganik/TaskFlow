import { z } from 'zod'

// Mirrors LoginRequestValidator / RegisterRequestValidator (FluentValidation) and the
// ASP.NET Identity password options in DependencyConfig.AddIdentityServices:
// RequireDigit, RequiredLength = 8, RequireUppercase — RequireNonAlphanumeric is off.
const emailField = z.string().min(1, 'Email is required.').email('Email must be a valid email address.')

export const loginSchema = z.object({
  email: emailField,
  password: z.string().min(1, 'Password is required.'),
})

export type LoginFormValues = z.infer<typeof loginSchema>

export const registerSchema = z.object({
  email: emailField,
  displayName: z
    .string()
    .min(1, 'Display name is required.')
    .max(100, 'Display name must be 100 characters or fewer.'),
  password: z
    .string()
    .min(1, 'Password is required.')
    .min(8, 'Password must be at least 8 characters.')
    .regex(/\d/, 'Password must contain at least one number.')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.'),
})

export type RegisterFormValues = z.infer<typeof registerSchema>
