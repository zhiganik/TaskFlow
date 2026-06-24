import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { useDeleteWorkspace } from '../../hooks/useWorkspaces'
import { useWorkspaceStore } from '../../store/workspaceStore'
import type { WorkspaceDto } from '../../types/api.types'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'

interface DeleteWorkspaceDialogProps {
  workspace: WorkspaceDto
  onClose: () => void
}

export function DeleteWorkspaceDialog({ workspace, onClose }: DeleteWorkspaceDialogProps) {
  const deleteMutation = useDeleteWorkspace()
  const selectedWorkspaceId = useWorkspaceStore((s) => s.selectedWorkspaceId)
  const selectWorkspace = useWorkspaceStore((s) => s.selectWorkspace)
  const [error, setError] = useState<string | null>(null)

  const onConfirm = () => {
    setError(null)
    deleteMutation.mutate(workspace.id, {
      onSuccess: () => {
        if (selectedWorkspaceId === workspace.id) selectWorkspace(null)
        onClose()
      },
      onError: (err) => setError(getErrorMessage(err)),
    })
  }

  return (
    <Modal title="Delete workspace" onClose={onClose}>
      <div className="space-y-4">
        {error && <Alert variant="error">{error}</Alert>}

        <p className="text-sm text-gray-600">
          Delete <span className="font-medium text-gray-900">{workspace.name}</span>? This cannot
          be undone.
        </p>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="danger"
            loading={deleteMutation.isPending}
            onClick={onConfirm}
          >
            Delete
          </Button>
        </div>
      </div>
    </Modal>
  )
}
