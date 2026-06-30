import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useCreateInvitation } from '../../hooks/useInvitations'
import type { CreateInvitationResponseDto } from '../../types/api.types'
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
  const [result, setResult] = useState<CreateInvitationResponseDto | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    getValues,
    formState: { errors },
  } = useForm<InviteMemberFormValues>({
    resolver: zodResolver(inviteMemberSchema),
    defaultValues: { role: 'Member' },
  })

  const onSubmit = (values: InviteMemberFormValues) => {
    setServerError(null)
    createInvitationMutation.mutate(values, {
      onSuccess: (data) => setResult(data),
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

  if (result) {
    return (
      <Modal title="Invite member" onClose={onClose}>
        <div className="space-y-4">
          <div className="flex flex-col items-center gap-3 py-4 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-green-100">
              <svg className="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
              </svg>
            </div>
            {result.directlyAdded ? (
              <>
                <p className="text-base font-medium text-gray-900">
                  {result.addedDisplayName} has been added
                </p>
                <p className="text-sm text-gray-500">
                  They now have access to this workspace.
                </p>
              </>
            ) : (
              <>
                <p className="text-base font-medium text-gray-900">Invitation sent</p>
                <p className="text-sm text-gray-500">
                  We sent an invitation link to{' '}
                  <span className="font-medium text-gray-700">{getValues('email')}</span>.
                </p>
              </>
            )}
          </div>
          <div className="flex justify-end">
            <Button onClick={onClose}>Done</Button>
          </div>
        </div>
      </Modal>
    )
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
            If they already have an account they&apos;ll be added directly; otherwise we&apos;ll
            send them an invitation link.
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
            {createInvitationMutation.isPending ? 'Inviting…' : 'Invite'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
