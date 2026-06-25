import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useRenameColumn } from '../../hooks/useColumns'
import type { WorkspaceColumnDto } from '../../types/api.types'
import { columnSchema, type ColumnFormValues } from '../../validation/column.schema'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'
import { TextField } from '../ui/TextField'

interface RenameColumnModalProps {
  workspaceId: string
  column: WorkspaceColumnDto
  onClose: () => void
}

export function RenameColumnModal({ workspaceId, column, onClose }: RenameColumnModalProps) {
  const renameMutation = useRenameColumn(workspaceId)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ColumnFormValues>({
    resolver: zodResolver(columnSchema),
    defaultValues: { name: column.name },
  })

  const onSubmit = (values: ColumnFormValues) => {
    setServerError(null)
    renameMutation
      .mutateAsync({ columnId: column.id, data: values })
      .then(onClose)
      .catch((error: unknown) => {
        const fieldErrors = getFieldErrors(error)
        if (fieldErrors) {
          for (const [field, message] of Object.entries(fieldErrors)) {
            setError(field as keyof ColumnFormValues, { message })
          }
          return
        }
        setServerError(getErrorMessage(error))
      })
  }

  return (
    <Modal title="Rename column" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert variant="error">{serverError}</Alert>}

        <TextField
          label="Column name"
          autoFocus
          placeholder="e.g. In Review"
          error={errors.name?.message}
          {...register('name')}
        />

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={renameMutation.isPending}>
            Save
          </Button>
        </div>
      </form>
    </Modal>
  )
}
