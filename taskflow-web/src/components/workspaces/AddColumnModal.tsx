import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useCreateColumn } from '../../hooks/useColumns'
import { columnSchema, type ColumnFormValues } from '../../validation/column.schema'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'
import { TextField } from '../ui/TextField'

interface AddColumnModalProps {
  workspaceId: string
  onClose: () => void
}

export function AddColumnModal({ workspaceId, onClose }: AddColumnModalProps) {
  const createMutation = useCreateColumn(workspaceId)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ColumnFormValues>({
    resolver: zodResolver(columnSchema),
    defaultValues: { name: '' },
  })

  const onSubmit = (values: ColumnFormValues) => {
    setServerError(null)
    createMutation
      .mutateAsync(values)
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
    <Modal title="Add column" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert variant="error">{serverError}</Alert>}

        <TextField
          label="Column name"
          autoFocus
          placeholder="e.g. Review"
          error={errors.name?.message}
          {...register('name')}
        />

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={createMutation.isPending}>
            Add
          </Button>
        </div>
      </form>
    </Modal>
  )
}
