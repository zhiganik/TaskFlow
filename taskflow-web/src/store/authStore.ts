import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { UserDto } from '../types/api.types'

interface AuthTokens {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: string | null
  user: UserDto | null
  setAuth: (tokens: AuthTokens, user: UserDto) => void
  clearAuth: () => void
  updateUser: (user: UserDto) => void
}

// Persisted to localStorage so a page reload doesn't force a re-login. The access token
// is short-lived and the refresh token rotates on every use (the backend invalidates the
// old one), which bounds how long a token pulled out of storage stays useful.
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,
      setAuth: (tokens, user) =>
        set({
          accessToken: tokens.accessToken,
          refreshToken: tokens.refreshToken,
          expiresAt: tokens.expiresAt,
          user,
        }),
      clearAuth: () => set({ accessToken: null, refreshToken: null, expiresAt: null, user: null }),
      updateUser: (user) => set({ user }),
    }),
    { name: 'taskflow.auth' },
  ),
)

// Plain accessors for use outside React (e.g. the Axios interceptor in api/client.ts).
export const getAccessToken = () => useAuthStore.getState().accessToken
export const getRefreshToken = () => useAuthStore.getState().refreshToken
