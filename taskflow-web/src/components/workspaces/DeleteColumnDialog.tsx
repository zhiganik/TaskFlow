import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { useDeleteColumn } from '../../hooks/useColumns'
import type { WorkspaceColumnDto } from '../../types/api.types'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'

interface DeleteColumnDialogProps {
  workspaceId: string
  column: WorkspaceColumnDto
  onClose: () => void
}

export function DeleteColumnDialog({ workspaceId, column, onClose }: DeleteColumnDialogProps) {
  const deleteMutation = useDeleteColumn(workspaceId)
  const [error, setError] = useState<string | null>(null)

  const onConfirm = () => {
    setError(null)
    deleteMutation.mutate(column.id, {
      onSuccess: onClose,
      onError: (err) => setError(getErrorMessage(err)),
    })
  }

  return (
    <Modal title="Delete column" onClose={onClose}>
      <div className="space-y-4">
        {error && <Alert variant="error">{error}</Alert>}

        <p className="text-sm text-gray-600">
          Delete column{' '}
          <span className="font-medium text-gray-900">&ldquo;{column.name}&rdquo;</span>? This
          cannot be undone.
        </p>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="button" variant="danger" loading={deleteMutation.isPending} onClick={onConfirm}>
            Delete
          </Button>
        </div>
      </div>
    </Modal>
  )
}
