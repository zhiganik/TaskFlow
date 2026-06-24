import { create } from 'zustand'
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
}

// Never persisted to localStorage/sessionStorage — XSS risk. Lost on page reload by design.
export const useAuthStore = create<AuthState>((set) => ({
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
}))

// Plain accessors for use outside React (e.g. the Axios interceptor in api/client.ts).
export const getAccessToken = () => useAuthStore.getState().accessToken
export const getRefreshToken = () => useAuthStore.getState().refreshToken
