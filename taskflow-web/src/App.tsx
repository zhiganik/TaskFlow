import { useEffect, useState } from 'react'
import { authApi } from './api/auth.api'
import { Spinner } from './components/ui/Spinner'
import { AppRouter } from './router'
import { useAuthStore } from './store/authStore'

function isExpired(expiresAt: string | null) {
  return !expiresAt || new Date(expiresAt).getTime() <= Date.now()
}

// Token still valid (or there's nothing to refresh) — render immediately, no network call.
function isAlreadyReady() {
  const { accessToken, refreshToken, expiresAt } = useAuthStore.getState()
  return !accessToken || !refreshToken || !isExpired(expiresAt)
}

function App() {
  const [ready, setReady] = useState(isAlreadyReady)

  useEffect(() => {
    if (ready) return

    // Persisted access token has expired while the app was closed — refresh silently
    // before rendering so the user doesn't see a flash of the login page.
    const { refreshToken, setAuth, clearAuth } = useAuthStore.getState()
    if (!refreshToken) return // unreachable: isAlreadyReady() already guarantees this is set

    authApi
      .refresh({ refreshToken })
      .then((result) => setAuth(result, result.user))
      .catch(() => clearAuth())
      .finally(() => setReady(true))
  }, [ready])

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-gray-50">
        <Spinner className="h-6 w-6 border-gray-300 border-t-brand-500" />
      </div>
    )
  }

  return <AppRouter />
}

export default App
