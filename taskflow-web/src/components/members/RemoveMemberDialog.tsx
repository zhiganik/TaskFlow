import { useState } from 'react'
import { getErrorMessage } from '../../api/errors'
import { useRemoveMember } from '../../hooks/useMembers'
import type { MemberDto } from '../../types/api.types'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'

interface RemoveMemberDialogProps {
  workspaceId: string
  member: MemberDto
  onClose: () => void
}

export function RemoveMemberDialog({ workspaceId, member, onClose }: RemoveMemberDialogProps) {
  const removeMutation = useRemoveMember(workspaceId)
  const [error, setError] = useState<string | null>(null)

  const onConfirm = () => {
    setError(null)
    removeMutation.mutate(member.userId, {
      onSuccess: onClose,
      onError: (err) => setError(getErrorMessage(err)),
    })
  }

  return (
    <Modal title="Remove member" onClose={onClose}>
      <div className="space-y-4">
        {error && <Alert variant="error">{error}</Alert>}

        <p className="text-sm text-gray-600">
          Remove <span className="font-medium text-gray-900">{member.displayName}</span> from this
          workspace?
        </p>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="danger"
            loading={removeMutation.isPending}
            onClick={onConfirm}
          >
            Remove
          </Button>
        </div>
      </div>
    </Modal>
  )
}
