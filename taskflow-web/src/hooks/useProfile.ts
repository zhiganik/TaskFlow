import { useMutation } from '@tanstack/react-query'
import { profileApi } from '../api/profile.api'
import { useAuthStore } from '../store/authStore'
import type { ChangePasswordRequest, UpdateProfileRequest } from '../types/api.types'

export function useUpdateProfile() {
  const updateUser = useAuthStore((s) => s.updateUser)
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => profileApi.update(data),
    onSuccess: (updated) => updateUser(updated),
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: ChangePasswordRequest) => profileApi.changePassword(data),
  })
}
