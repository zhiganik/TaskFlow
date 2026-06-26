import { zodResolver } from '@hookform/resolvers/zod'
import { forwardRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useCreateTask } from '../../hooks/useTasks'
import { useMembers } from '../../hooks/useMembers'
import { createTaskSchema, type CreateTaskFormValues } from '../../validation/task.schema'
import type { WorkspaceColumnDto } from '../../types/api.types'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'
import { Select } from '../ui/Select'
import { TextField } from '../ui/TextField'
import { LabelPicker } from './LabelPicker'

interface CreateTaskModalProps {
  workspaceId: string
  columns: WorkspaceColumnDto[]
  defaultColumnId?: string
  onClose: () => void
}

// Textarea styled to match TextField
const TextArea = forwardRef<
  HTMLTextAreaElement,
  React.TextareaHTMLAttributes<HTMLTextAreaElement> & { label: string; error?: string }
>(({ label, error, id, ...props }, ref) => {
  const textareaId = id ?? (props.name as string | undefined)
  return (
    <div>
      <label htmlFor={textareaId} className="mb-1.5 block text-sm font-medium text-gray-700">
        {label}
      </label>
      <textarea
        {...props}
        id={textareaId}
        ref={ref}
        rows={3}
        className={`block w-full resize-none rounded-md border px-3 py-2.5 text-sm text-gray-900 placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-brand-500/30 ${
          error ? 'border-red-400 focus:border-red-400' : 'border-gray-200 focus:border-brand-500'
        }`}
      />
      {error && <p className="mt-1.5 text-xs text-[#a32d2d]">{error}</p>}
    </div>
  )
})
TextArea.displayName = 'TextArea'

export function CreateTaskModal({
  workspaceId,
  columns,
  defaultColumnId,
  onClose,
}: CreateTaskModalProps) {
  const createMutation = useCreateTask(workspaceId)
  const { data: members } = useMembers(workspaceId)
  const [serverError, setServerError] = useState<string | null>(null)
  const [selectedLabelIds, setSelectedLabelIds] = useState<string[]>([])

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<CreateTaskFormValues>({
    resolver: zodResolver(createTaskSchema),
    defaultValues: {
      title: '',
      columnId: defaultColumnId ?? columns[0]?.id ?? '',
      priority: 'Medium',
      assigneeId: '',
      dueDate: '',
      description: '',
    },
  })

  const onSubmit = (values: CreateTaskFormValues) => {
    setServerError(null)
    createMutation
      .mutateAsync({
        title: values.title,
        columnId: values.columnId,
        description: values.description || null,
        priority: values.priority,
        assigneeId: values.assigneeId || null,
        dueDate: values.dueDate ? new Date(values.dueDate + 'T00:00:00Z').toISOString() : null,
        labelIds: selectedLabelIds.length ? selectedLabelIds : undefined,
      })
      .then(onClose)
      .catch((error: unknown) => {
        const fieldErrors = getFieldErrors(error)
        if (fieldErrors) {
          for (const [field, message] of Object.entries(fieldErrors)) {
            setError(field as keyof CreateTaskFormValues, { message })
          }
          return
        }
        setServerError(getErrorMessage(error))
      })
  }

  return (
    <Modal title="New task" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3" noValidate>
        {serverError && <Alert variant="error">{serverError}</Alert>}

        <TextField
          label="Title"
          autoFocus
          placeholder="What needs to be done?"
          error={errors.title?.message}
          {...register('title')}
        />

        <Select label="Column" error={errors.columnId?.message} {...register('columnId')}>
          {columns.map((col) => (
            <option key={col.id} value={col.id}>
              {col.name}
            </option>
          ))}
        </Select>

        <Select label="Priority" error={errors.priority?.message} {...register('priority')}>
          <option value="Low">Low</option>
          <option value="Medium">Medium</option>
          <option value="High">High</option>
        </Select>

        <Select label="Assignee" error={errors.assigneeId?.message} {...register('assigneeId')}>
          <option value="">Unassigned</option>
          {members?.map((m) => (
            <option key={m.userId} value={m.userId}>
              {m.displayName}
            </option>
          ))}
        </Select>

        <div>
          <label htmlFor="dueDate" className="mb-1.5 block text-sm font-medium text-gray-700">
            Due date
          </label>
          <input
            id="dueDate"
            type="date"
            className="block w-full rounded-md border border-gray-200 px-3 py-2.5 text-sm text-gray-900 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
            {...register('dueDate')}
          />
          {errors.dueDate && (
            <p className="mt-1.5 text-xs text-[#a32d2d]">{errors.dueDate.message}</p>
          )}
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">Labels</label>
          <LabelPicker
            workspaceId={workspaceId}
            selectedIds={selectedLabelIds}
            onChange={setSelectedLabelIds}
          />
        </div>

        <TextArea
          label="Description"
          placeholder="Optional details…"
          error={errors.description?.message}
          {...register('description')}
        />

        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={createMutation.isPending}>
            Create task
          </Button>
        </div>
      </form>
    </Modal>
  )
}
