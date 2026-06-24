import { z } from 'zod'

// Mirrors CreateWorkspaceRequestValidator / UpdateWorkspaceRequestValidator (FluentValidation):
// Name is required, max 100 characters.
export const workspaceSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required.')
    .max(100, 'Name must be 100 characters or fewer.'),
})

export type WorkspaceFormValues = z.infer<typeof workspaceSchema>
