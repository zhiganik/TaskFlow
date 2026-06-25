import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useCreateColumn } from '../../hooks/useColumns'
import { columnSchema, type ColumnFormValues } from '../../validation/column.schema'
import { COLOR_PALETTE } from '../../lib/priority'
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
  const [selectedColor, setSelectedColor] = useState<string | undefined>(undefined)

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
      .mutateAsync({ name: values.name, color: selectedColor })
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

        <div>
          <p className="mb-2 text-sm font-medium text-gray-700">
            Color{' '}
            <span className="font-normal text-gray-400">(optional — random if skipped)</span>
          </p>
          <div className="flex flex-wrap gap-2">
            {COLOR_PALETTE.map((color) => (
              <button
                key={color}
                type="button"
                onClick={() => setSelectedColor(selectedColor === color ? undefined : color)}
                style={{ backgroundColor: color }}
                className={[
                  'h-6 w-6 rounded-full transition-transform',
                  selectedColor === color ? 'ring-2 ring-offset-2 ring-gray-600 scale-110' : 'hover:scale-110',
                ].join(' ')}
                aria-label={color}
              />
            ))}
          </div>
        </div>

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
