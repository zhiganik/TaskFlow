import { apiClient } from './client'
import type {
  AuthResponseDto,
  LoginRequest,
  RefreshTokenRequest,
  RegisterRequest,
  UserDto,
} from '../types/api.types'

export const authApi = {
  register: (data: RegisterRequest) =>
    apiClient.post<UserDto>('/auth/register', data).then((r) => r.data),

  login: (data: LoginRequest) =>
    apiClient.post<AuthResponseDto>('/auth/login', data).then((r) => r.data),

  refresh: (data: RefreshTokenRequest) =>
    apiClient.post<AuthResponseDto>('/auth/refresh', data).then((r) => r.data),

  logout: (data: RefreshTokenRequest) =>
    apiClient.post<void>('/auth/logout', data).then((r) => r.data),
}
