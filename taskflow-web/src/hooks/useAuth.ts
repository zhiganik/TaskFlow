import { useMutation } from '@tanstack/react-query'
import { authApi } from '../api/auth.api'
import { useAuthStore } from '../store/authStore'
import type { LoginRequest, RegisterRequest } from '../types/api.types'

export const useRegister = () =>
  useMutation({
    mutationFn: (data: RegisterRequest) => authApi.register(data),
  })

export const useLogin = () => {
  const setAuth = useAuthStore((s) => s.setAuth)

  return useMutation({
    mutationFn: (data: LoginRequest) => authApi.login(data),
    onSuccess: (result) => setAuth(result, result.user),
  })
}

export const useLogout = () => {
  const refreshToken = useAuthStore((s) => s.refreshToken)
  const clearAuth = useAuthStore((s) => s.clearAuth)

  return useMutation({
    mutationFn: () => (refreshToken ? authApi.logout({ refreshToken }) : Promise.resolve()),
    onSettled: () => clearAuth(),
  })
}
