import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useCreateWorkspace, useUpdateWorkspace } from '../../hooks/useWorkspaces'
import type { WorkspaceDto } from '../../types/api.types'
import { workspaceSchema, type WorkspaceFormValues } from '../../validation/workspace.schema'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'
import { TextField } from '../ui/TextField'

interface WorkspaceFormModalProps {
  workspace?: WorkspaceDto
  onClose: () => void
  onSaved: (workspace: WorkspaceDto) => void
}

export function WorkspaceFormModal({ workspace, onClose, onSaved }: WorkspaceFormModalProps) {
  const isEdit = !!workspace
  const createMutation = useCreateWorkspace()
  const updateMutation = useUpdateWorkspace()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<WorkspaceFormValues>({
    resolver: zodResolver(workspaceSchema),
    defaultValues: { name: workspace?.name ?? '' },
  })

  const mutation = isEdit ? updateMutation : createMutation

  const onSubmit = (values: WorkspaceFormValues) => {
    setServerError(null)

    const result = isEdit
      ? updateMutation.mutateAsync({ id: workspace.id, data: values })
      : createMutation.mutateAsync(values)

    result.then(onSaved).catch((error: unknown) => {
      const fieldErrors = getFieldErrors(error)
      if (fieldErrors) {
        for (const [field, message] of Object.entries(fieldErrors)) {
          setError(field as keyof WorkspaceFormValues, { message })
        }
        return
      }
      setServerError(getErrorMessage(error))
    })
  }

  return (
    <Modal title={isEdit ? 'Rename workspace' : 'New workspace'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert variant="error">{serverError}</Alert>}

        <TextField
          label="Workspace name"
          autoFocus
          placeholder="Acme Team"
          error={errors.name?.message}
          {...register('name')}
        />

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={mutation.isPending}>
            {isEdit ? 'Save' : 'Create'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
