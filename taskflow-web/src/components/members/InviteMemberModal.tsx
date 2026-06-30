import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useCreateInvitation } from '../../hooks/useInvitations'
import { inviteMemberSchema, type InviteMemberFormValues } from '../../validation/member.schema'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { Modal } from '../ui/Modal'
import { Select } from '../ui/Select'
import { TextField } from '../ui/TextField'

interface InviteMemberModalProps {
  workspaceId: string
  onClose: () => void
}

export function InviteMemberModal({ workspaceId, onClose }: InviteMemberModalProps) {
  const createInvitationMutation = useCreateInvitation(workspaceId)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<InviteMemberFormValues>({
    resolver: zodResolver(inviteMemberSchema),
    defaultValues: { role: 'Member' },
  })

  const onSubmit = (values: InviteMemberFormValues) => {
    setServerError(null)
    createInvitationMutation.mutate(values, {
      onSuccess: onClose,
      onError: (error) => {
        const fieldErrors = getFieldErrors(error)
        if (fieldErrors) {
          for (const [field, message] of Object.entries(fieldErrors)) {
            setError(field as keyof InviteMemberFormValues, { message })
          }
          return
        }
        setServerError(getErrorMessage(error))
      },
    })
  }

  return (
    <Modal title="Invite member" onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
        {serverError && <Alert variant="error">{serverError}</Alert>}

        <div className="space-y-1.5">
          <TextField
            label="Email"
            type="email"
            autoFocus
            placeholder="teammate@example.com"
            error={errors.email?.message}
            {...register('email')}
          />
          <p className="text-xs text-gray-400">
            We&apos;ll send them an email with a link to join.
          </p>
        </div>

        <Select label="Role" error={errors.role?.message} {...register('role')}>
          <option value="Member">Member</option>
          <option value="Admin">Admin</option>
        </Select>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={createInvitationMutation.isPending}>
            {createInvitationMutation.isPending ? 'Sending…' : 'Send invite'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
