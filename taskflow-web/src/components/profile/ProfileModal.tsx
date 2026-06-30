import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useRef, useState, type MouseEvent } from 'react'
import { useForm } from 'react-hook-form'
import { getErrorMessage, getFieldErrors } from '../../api/errors'
import { useLogout } from '../../hooks/useAuth'
import { useChangePassword, useProfile, useRemoveAvatar, useUpdateProfile, useUploadAvatar } from '../../hooks/useProfile'
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
import { UserAvatar } from '../ui/UserAvatar'
import { AvatarCropModal } from './AvatarCropModal'

interface ProfileModalProps {
  onClose: () => void
}

export function ProfileModal({ onClose }: ProfileModalProps) {
  const user = useAuthStore((s) => s.user)

  // keep profile in sync and poll while avatar is Pending
  useProfile()

  const updateProfileMutation = useUpdateProfile()
  const changePasswordMutation = useChangePassword()
  const uploadAvatarMutation = useUploadAvatar()
  const removeAvatarMutation = useRemoveAvatar()
  const logoutMutation = useLogout()

  const [profileSuccess, setProfileSuccess] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [passwordSuccess, setPasswordSuccess] = useState(false)
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [avatarError, setAvatarError] = useState<string | null>(null)

  // crop modal state
  const [cropSrc, setCropSrc] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const {
    register: registerProfile,
    handleSubmit: handleProfileSubmit,
    setError: setProfileFieldError,
    formState: { errors: profileErrors },
  } = useForm<UpdateProfileFormValues>({
    resolver: zodResolver(updateProfileSchema),
    defaultValues: {
      displayName: user?.displayName ?? '',
      email: user?.email ?? '',
    },
  })

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
    const onKeyDown = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
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
        for (const [field, message] of Object.entries(fieldErrors))
          setProfileFieldError(field as keyof UpdateProfileFormValues, { message })
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
        for (const [field, message] of Object.entries(fieldErrors))
          setPasswordFieldError(field as keyof ChangePasswordFormValues, { message })
        return
      }
      setPasswordError(getErrorMessage(error))
    })
  }

  const handleFileSelected = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = () => setCropSrc(reader.result as string)
    reader.readAsDataURL(file)
    e.target.value = ''
  }

  const handleCropConfirm = (file: File) => {
    setCropSrc(null)
    setAvatarError(null)
    uploadAvatarMutation.mutate(file, {
      onError: (err) => setAvatarError(getErrorMessage(err)),
    })
  }

  const handleRemoveAvatar = () => {
    setAvatarError(null)
    removeAvatarMutation.mutate(undefined, {
      onError: (err) => setAvatarError(getErrorMessage(err)),
    })
  }

  const hasPhoto = user?.avatarPath != null
  const avatarIsPending = user?.avatarStatus === 'Pending'
  const avatarIsFailed = user?.avatarStatus === 'Failed'

  return (
    <>
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
            {/* Avatar section */}
            <div className="p-5">
              <p className="mb-3 text-sm font-medium text-gray-900">Avatar</p>

              <div className="flex items-center gap-4">
                <UserAvatar
                  displayName={user?.displayName ?? '?'}
                  avatarColor={user?.avatarColor ?? '#818cf8'}
                  avatarPath={user?.avatarPath}
                  avatarStatus={user?.avatarStatus}
                  size="lg"
                />

                <div className="min-w-0">
                  <div className="flex flex-wrap gap-2">
                    <button
                      type="button"
                      disabled={uploadAvatarMutation.isPending || avatarIsPending}
                      onClick={() => fileInputRef.current?.click()}
                      className="rounded-md border border-gray-200 bg-white px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
                    >
                      {avatarIsPending ? 'Processing…' : hasPhoto ? 'Change photo' : 'Upload photo'}
                    </button>
                    {hasPhoto && (
                      <button
                        type="button"
                        disabled={removeAvatarMutation.isPending}
                        onClick={handleRemoveAvatar}
                        className="rounded-md border border-gray-200 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
                      >
                        {avatarIsPending ? 'Cancel' : 'Remove'}
                      </button>
                    )}
                  </div>

                  <p className="mt-2 text-[11px] leading-snug text-gray-400">
                    {avatarIsPending
                      ? 'Processing your photo…'
                      : 'JPG, PNG or GIF · Initials show when no photo is set.'}
                  </p>

                  {avatarIsFailed && (
                    <p className="mt-1 text-[11px] text-red-500">Processing failed — try again.</p>
                  )}
                  {avatarError && (
                    <p className="mt-1 text-[11px] text-red-500">{avatarError}</p>
                  )}
                </div>
              </div>

              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="hidden"
                onChange={handleFileSelected}
              />
            </div>

            {/* Profile info section */}
            <form onSubmit={handleProfileSubmit(onProfileSubmit)} className="space-y-4 p-5" noValidate>
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

      {/* crop modal rendered outside the profile modal so z-index stacks correctly */}
      {cropSrc && (
        <AvatarCropModal
          imageSrc={cropSrc}
          onConfirm={handleCropConfirm}
          onCancel={() => setCropSrc(null)}
        />
      )}
    </>
  )
}
