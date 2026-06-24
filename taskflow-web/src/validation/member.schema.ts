import { z } from 'zod'

// Mirrors InviteMemberRequestValidator (FluentValidation): valid email, role is Admin or
// Member — the backend rejects Owner here (you can't invite someone as the workspace owner).
export const inviteMemberSchema = z.object({
  email: z.string().min(1, 'Email is required.').email('Email must be a valid email address.'),
  role: z.enum(['Admin', 'Member']),
})

export type InviteMemberFormValues = z.infer<typeof inviteMemberSchema>
