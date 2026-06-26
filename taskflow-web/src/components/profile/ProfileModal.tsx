import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, type MouseEvent, useState } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useLogout } from '../../hooks/useAuth'
import { useChangePassword, useUpdateProfile } from '../../hooks/useProfile'
import { useAuthStore } from '../../store/authStore'
import {
  changePasswordSchema,
  type ChangePasswordFormValues,
  updateProfileSchema,
  type UpdateProfileFormValues,
} from '../../validation/profile.schema'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { TextField } from '../ui/TextField'

const AVATAR_COLORS = [
  '#818cf8',
  '#60a5fa',
  '#34d399',
  '#fbbf24',
  '#f87171',
  '#f472b6',
  '#a78bfa',
  '#94a3b8',
]

interface ProfileModalProps {
  onClose: () => void
}

function initials(displayName: string) {
  return displayName
    .split(' ')
    .map((part) => part[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export function ProfileModal({ onClose }: ProfileModalProps) {
  const user = useAuthStore((s) => s.user)
  const updateProfileMutation = useUpdateProfile()
  const changePasswordMutation = useChangePassword()
  const logoutMutation = useLogout()

  const [profileSuccess, setProfileSuccess] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [passwordSuccess, setPasswordSuccess] = useState(false)
  const [passwordError, setPasswordError] = useState<string | null>(null)

  const {
    register: registerProfile,
    handleSubmit: handleProfileSubmit,
    setError: setProfileFieldError,
    watch,
    setValue,
    formState: { errors: profileErrors },
  } = useForm<UpdateProfileFormValues>({
    resolver: zodResolver(updateProfileSchema),
    defaultValues: {
      displayName: user?.displayName ?? '',
      email: user?.email ?? '',
      avatarColor: user?.avatarColor ?? '#818cf8',
    },
  })

  const selectedColor = watch('avatarColor')

  const {
    register: registerPassword,
    handleSubmit: handlePasswordSubmit,
    setError: setPasswordFieldError,
    reset: resetPassword,
    formState: { errors: passwordErrors },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
  })

  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  const stopPropagation = (e: MouseEvent) => e.stopPropagation()

  const onProfileSubmit = (values: UpdateProfileFormValues) => {
    setProfileError(null)
    setProfileSuccess(false)
    updateProfileMutation.mutateAsync(values).then(() => {
      setProfileSuccess(true)
    }).catch((error: unknown) => {
      const fieldErrors = getFieldErrors(error)
      if (fieldErrors) {
        for (const [field, message] of Object.entries(fieldErrors)) {
          setProfileFieldError(field as keyof UpdateProfileFormValues, { message })
        }
        return
      }
      setProfileError(getErrorMessage(error))
    })
  }

  const onPasswordSubmit = (values: ChangePasswordFormValues) => {
    setPasswordError(null)
    setPasswordSuccess(false)
    changePasswordMutation.mutateAsync(values).then(() => {
      setPasswordSuccess(true)
      resetPassword()
    }).catch((error: unknown) => {
      const fieldErrors = getFieldErrors(error)
      if (fieldErrors) {
        for (const [field, message] of Object.entries(fieldErrors)) {
          setPasswordFieldError(field as keyof ChangePasswordFormValues, { message })
        }
        return
      }
      setPasswordError(getErrorMessage(error))
    })
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/30 px-4"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Profile settings"
        className="w-full max-w-md rounded-lg border border-gray-200 bg-white shadow-lg"
        onClick={stopPropagation}
      >
        <div className="flex items-center justify-between border-b border-gray-100 px-5 py-4">
          <h2 className="text-base font-semibold text-gray-900">Profile settings</h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
            aria-label="Close"
          >
            <svg viewBox="0 0 16 16" className="h-4 w-4 fill-current">
              <path d="M3.72 3.72a.75.75 0 0 1 1.06 0L8 6.94l3.22-3.22a.75.75 0 1 1 1.06 1.06L9.06 8l3.22 3.22a.75.75 0 1 1-1.06 1.06L8 9.06l-3.22 3.22a.75.75 0 0 1-1.06-1.06L6.94 8 3.72 4.78a.75.75 0 0 1 0-1.06Z" />
            </svg>
          </button>
        </div>

        <div className="divide-y divide-gray-100 overflow-y-auto" style={{ maxHeight: '80vh' }}>
          {/* Profile info section */}
          <form onSubmit={handleProfileSubmit(onProfileSubmit)} className="space-y-4 p-5" noValidate>
            <div className="flex items-center gap-3">
              <div
                className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full text-sm font-medium text-white"
                style={{ backgroundColor: selectedColor }}
              >
                {user ? initials(user.displayName) : '?'}
              </div>
              <div>
                <p className="text-sm font-medium text-gray-900">Avatar color</p>
                <div className="mt-1.5 flex gap-1.5">
                  {AVATAR_COLORS.map((color) => (
                    <button
                      key={color}
                      type="button"
                      onClick={() => setValue('avatarColor', color, { shouldDirty: true })}
                      className="relative h-6 w-6 rounded-full transition-transform hover:scale-110 focus:outline-none focus:ring-2 focus:ring-offset-1"
                      style={{ backgroundColor: color }}
                      aria-label={`Select color ${color}`}
                    >
                      {selectedColor === color && (
                        <svg viewBox="0 0 16 16" className="absolute inset-0 h-full w-full fill-white p-1">
                          <path d="M13.78 4.22a.75.75 0 0 1 0 1.06l-7.25 7.25a.75.75 0 0 1-1.06 0L2.22 9.28a.75.75 0 0 1 1.06-1.06L6 10.94l6.72-6.72a.75.75 0 0 1 1.06 0Z" />
                        </svg>
                      )}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            {profileSuccess && <Alert variant="success">Profile updated.</Alert>}
            {profileError && <Alert variant="error">{profileError}</Alert>}

            <TextField
              label="Display name"
              autoComplete="name"
              error={profileErrors.displayName?.message}
              {...registerProfile('displayName')}
            />
            <TextField
              label="Email"
              type="email"
              autoComplete="email"
              error={profileErrors.email?.message}
              {...registerProfile('email')}
            />

            <div className="flex justify-end">
              <Button type="submit" loading={updateProfileMutation.isPending}>
                Save changes
              </Button>
            </div>
          </form>

          {/* Password section */}
          <form onSubmit={handlePasswordSubmit(onPasswordSubmit)} className="space-y-4 p-5" noValidate>
            <p className="text-sm font-medium text-gray-900">Change password</p>

            {passwordSuccess && <Alert variant="success">Password changed.</Alert>}
            {passwordError && <Alert variant="error">{passwordError}</Alert>}

            <TextField
              label="Current password"
              type="password"
              autoComplete="current-password"
              error={passwordErrors.currentPassword?.message}
              {...registerPassword('currentPassword')}
            />
            <TextField
              label="New password"
              type="password"
              autoComplete="new-password"
              error={passwordErrors.newPassword?.message}
              {...registerPassword('newPassword')}
            />

            <div className="flex justify-end">
              <Button type="submit" loading={changePasswordMutation.isPending}>
                Change password
              </Button>
            </div>
          </form>

          {/* Logout section */}
          <div className="p-5">
            <Button
              type="button"
              variant="danger"
              className="w-full"
              loading={logoutMutation.isPending}
              onClick={() => logoutMutation.mutate()}
            >
              Log out
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
