import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { getErrorMessage, getFieldErrors } from '../api/errors'
import { AuthLayout } from '../components/layout/AuthLayout'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { Spinner } from '../components/ui/Spinner'
import { TextField } from '../components/ui/TextField'
import { useAcceptInvitation, useInvitationInfo } from '../hooks/useInvitations'
import { useAuthStore } from '../store/authStore'
import { inviteRegisterSchema, type InviteRegisterFormValues } from '../validation/auth.schema'

export function InvitePage() {
  const { token = '' } = useParams<{ token: string }>()
  const navigate = useNavigate()
  const { data: info, isLoading, error } = useInvitationInfo(token)
  const acceptMutation = useAcceptInvitation(token)
  const { accessToken, user, setAuth } = useAuthStore()
  const [serverError, setServerError] = useState<string | null>(null)

  const isLoggedIn = accessToken !== null
  const emailMatchesLoggedInUser = isLoggedIn && user?.email === info?.email

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<InviteRegisterFormValues>({
    resolver: zodResolver(inviteRegisterSchema),
  })

  const handleAccept = (body: { displayName?: string; password?: string } = {}) => {
    setServerError(null)
    acceptMutation.mutate(body, {
      onSuccess: (result) => {
        if (result.auth) {
          setAuth(
            {
              accessToken: result.auth.accessToken,
              refreshToken: result.auth.refreshToken,
              expiresAt: result.auth.expiresAt,
            },
            result.auth.user,
          )
        }
        navigate(`/workspaces/${result.workspaceId}`, { replace: true })
      },
      onError: (err) => {
        const fieldErrors = getFieldErrors(err)
        if (fieldErrors) {
          for (const [field, message] of Object.entries(fieldErrors)) {
            setError(field as keyof InviteRegisterFormValues, { message })
          }
          return
        }
        setServerError(getErrorMessage(err))
      },
    })
  }

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <Spinner className="h-6 w-6 border-gray-300 border-t-brand-500" />
      </div>
    )
  }

  if (error || !info) {
    return (
      <AuthLayout title="Invitation not found">
        <Alert variant="error">
          This invitation link is invalid or has expired.
        </Alert>
        <p className="mt-4 text-center text-sm text-gray-500">
          <Link to="/login" className="font-medium text-brand-700 hover:underline">
            Go to login
          </Link>
        </p>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout
      title={`Join ${info.workspaceName}`}
      subtitle={`You've been invited as ${info.role}`}
    >
      {serverError && <Alert variant="error">{serverError}</Alert>}

      <p className="mb-4 text-center text-sm text-gray-500">
        Invitation for <span className="font-medium text-gray-700">{info.email}</span>
      </p>

      {info.userExists ? (
        emailMatchesLoggedInUser ? (
          <Button
            className="w-full"
            loading={acceptMutation.isPending}
            onClick={() => handleAccept()}
          >
            {acceptMutation.isPending ? 'Joining…' : `Accept and join ${info.workspaceName}`}
          </Button>
        ) : (
          <div className="space-y-3 text-center text-sm text-gray-600">
            <p>This invitation is for an existing account.</p>
            <Link
              to={`/login?returnUrl=/invite/${token}`}
              className="block w-full rounded-lg bg-brand-600 px-4 py-2.5 text-center text-sm font-semibold text-white hover:bg-brand-700"
            >
              Sign in to accept
            </Link>
          </div>
        )
      ) : (
        <form
          onSubmit={handleSubmit((values) =>
            handleAccept({ displayName: values.displayName, password: values.password })
          )}
          className="space-y-4"
          noValidate
        >
          <TextField
            label="Display name"
            autoComplete="name"
            placeholder="Jane Smith"
            error={errors.displayName?.message}
            {...register('displayName')}
          />

          <TextField
            label="Email"
            type="email"
            value={info.email}
            readOnly
            disabled
          />

          <div className="space-y-1.5">
            <TextField
              label="Password"
              type="password"
              autoComplete="new-password"
              placeholder="••••••••"
              error={errors.password?.message}
              {...register('password')}
            />
            {!errors.password && (
              <p className="text-xs text-gray-400">
                At least 8 characters, with one uppercase letter and one number.
              </p>
            )}
          </div>

          <Button type="submit" className="w-full" loading={acceptMutation.isPending}>
            {acceptMutation.isPending ? 'Creating account…' : 'Create account & join'}
          </Button>
        </form>
      )}
    </AuthLayout>
  )
}
