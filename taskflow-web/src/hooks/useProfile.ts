import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { profileApi } from '../api/profile.api'
import { useAuthStore } from '../store/authStore'
import type { ChangePasswordRequest, UpdateProfileRequest } from '../types/api.types'

const PROFILE_KEY = ['me'] as const

export function useProfile() {
  const qc = useQueryClient()
  const updateUser = useAuthStore((s) => s.updateUser)
  const prevStatus = useRef<string | undefined>(undefined)

  const result = useQuery({
    queryKey: PROFILE_KEY,
    queryFn: async () => {
      const data = await profileApi.get()
      updateUser(data)
      return data
    },
    refetchInterval: (q) => (q.state.data?.avatarStatus === 'Pending' ? 2000 : false),
  })

  // When the worker finishes (Pending → Ready), refresh tasks + members so
  // assignee avatars and the members list update without a page reload.
  useEffect(() => {
    const current = result.data?.avatarStatus
    if (prevStatus.current === 'Pending' && current === 'Ready') {
      qc.invalidateQueries({ queryKey: ['tasks'] })
      qc.invalidateQueries({ queryKey: ['members'] })
    }
    prevStatus.current = current
  }, [result.data?.avatarStatus, qc])

  return result
}

export function useUpdateProfile() {
  const qc = useQueryClient()
  const updateUser = useAuthStore((s) => s.updateUser)
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) => profileApi.update(data),
    onSuccess: (updated) => {
      updateUser(updated)
      qc.setQueryData(PROFILE_KEY, updated)
      qc.invalidateQueries({ queryKey: ['tasks'] })
      qc.invalidateQueries({ queryKey: ['members'] })
    },
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: ChangePasswordRequest) => profileApi.changePassword(data),
  })
}

export function useUploadAvatar() {
  const qc = useQueryClient()
  const updateUser = useAuthStore((s) => s.updateUser)
  return useMutation({
    mutationFn: (file: File) => profileApi.uploadAvatar(file),
    onSuccess: (updated) => {
      updateUser(updated)
      qc.setQueryData(PROFILE_KEY, updated)
      // trigger polling — useProfile's refetchInterval picks it up
      qc.invalidateQueries({ queryKey: PROFILE_KEY })
    },
  })
}

export function useRemoveAvatar() {
  const qc = useQueryClient()
  const updateUser = useAuthStore((s) => s.updateUser)
  return useMutation({
    mutationFn: () => profileApi.removeAvatar(),
    onSuccess: (updated) => {
      updateUser(updated)
      qc.setQueryData(PROFILE_KEY, updated)
      qc.invalidateQueries({ queryKey: ['tasks'] })
      qc.invalidateQueries({ queryKey: ['members'] })
    },
  })
}
