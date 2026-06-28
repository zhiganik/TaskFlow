import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { getAccessToken, getRefreshToken, useAuthStore } from '../store/authStore'
import type { AuthResponseDto } from '../types/api.types'

// In Docker: VITE_API_URL is "" — Nginx proxies /api/* to the API container.
// In local dev (non-Docker): Vite's dev server proxy forwards /api to the API.
const BASE_URL = import.meta.env.VITE_API_URL ?? ''

export const apiClient = axios.create({
  baseURL: `${BASE_URL}/api/v1`,
  headers: { 'Content-Type': 'application/json' },
})

const AUTH_REFRESH_PATH = '/auth/refresh'
const AUTH_PATHS = ['/auth/login', '/auth/register', AUTH_REFRESH_PATH, '/auth/logout']
const isAuthEndpoint = (url?: string) => !!url && AUTH_PATHS.some((path) => url.includes(path))

// Attach the in-memory JWT on every request except the auth endpoints themselves.
apiClient.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token && !isAuthEndpoint(config.url)) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

interface RetryableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean
}

// Shared across concurrent 401s so only one refresh call is in flight at a time —
// the backend invalidates a refresh token as soon as it's used, so a second
// concurrent call with the same token would fail.
let refreshPromise: Promise<string | null> | null = null

function refreshAccessToken(): Promise<string | null> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) return Promise.resolve(null)

  return apiClient
    .post<AuthResponseDto>(AUTH_REFRESH_PATH, { refreshToken })
    .then(({ data }) => {
      useAuthStore.getState().setAuth(data, data.user)
      return data.accessToken
    })
    .catch(() => {
      useAuthStore.getState().clearAuth()
      return null
    })
}

// Translate Axios errors into typed app errors (ProblemDetails/ValidationProblemDetails),
// and transparently retry a request once after a refreshed access token.
apiClient.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const config = error.config as RetryableConfig | undefined

    const shouldRetry =
      error.response?.status === 401 && config && !config._retried && !isAuthEndpoint(config.url)

    if (shouldRetry) {
      config._retried = true
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })

      const accessToken = await refreshPromise
      if (accessToken) {
        config.headers.Authorization = `Bearer ${accessToken}`
        return apiClient(config)
      }
    }

    if (error.response?.status === 413) {
      return Promise.reject({ status: 413, title: 'File too large', detail: 'The file exceeds the maximum upload size allowed by the server.' })
    }

    return Promise.reject(error.response?.data ?? error)
  },
)
