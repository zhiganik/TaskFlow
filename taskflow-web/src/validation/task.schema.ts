import { z } from 'zod'

export const createTaskSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Max 200 characters'),
  columnId: z.string().min(1, 'Column is required'),
  description: z.string().max(2000, 'Max 2000 characters').optional(),
  priority: z.enum(['Low', 'Medium', 'High']),
  assigneeId: z.string().optional(),
  dueDate: z.string().optional(),
})

export const updateTaskSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Max 200 characters'),
  description: z.string().max(2000, 'Max 2000 characters').optional(),
  priority: z.enum(['Low', 'Medium', 'High']),
  assigneeId: z.string().optional(),
  dueDate: z.string().optional(),
})

export type CreateTaskFormValues = z.infer<typeof createTaskSchema>
export type UpdateTaskFormValues = z.infer<typeof updateTaskSchema>
