import { apiClient } from './client'
import type { ChangePasswordRequest, UpdateProfileRequest, UserDto } from '../types/api.types'

export const profileApi = {
  get: () => apiClient.get<UserDto>('/me').then((r) => r.data),

  update: (data: UpdateProfileRequest) =>
    apiClient.put<UserDto>('/me', data).then((r) => r.data),

  changePassword: (data: ChangePasswordRequest) =>
    apiClient.put<void>('/me/password', data).then((r) => r.data),

  uploadAvatar: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return apiClient
      .post<UserDto>('/me/avatar', form, { headers: { 'Content-Type': 'multipart/form-data' } })
      .then((r) => r.data)
  },

  removeAvatar: () => apiClient.delete<UserDto>('/me/avatar').then((r) => r.data),
}
