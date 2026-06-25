import { z } from 'zod'

export const columnSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(50, 'Name must be 50 characters or fewer'),
})

export type ColumnFormValues = z.infer<typeof columnSchema>
