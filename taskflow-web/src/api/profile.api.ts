import { apiClient } from './client'
import type { ChangePasswordRequest, UpdateProfileRequest, UserDto } from '../types/api.types'

export const profileApi = {
  get: () => apiClient.get<UserDto>('/me').then((r) => r.data),

  update: (data: UpdateProfileRequest) =>
    apiClient.put<UserDto>('/me', data).then((r) => r.data),

  changePassword: (data: ChangePasswordRequest) =>
    apiClient.put<void>('/me/password', data).then((r) => r.data),
}
