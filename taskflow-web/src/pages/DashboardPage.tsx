import { Button } from '../components/ui/Button'
import { useLogout } from '../hooks/useAuth'
import { useAuthStore } from '../store/authStore'

function initials(displayName: string) {
  return displayName
    .split(' ')
    .map((part) => part[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const logoutMutation = useLogout()

  return (
    <div className="flex min-h-screen flex-col bg-gray-50">
      <header className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-3">
        <div className="flex items-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-md bg-brand-500">
            <svg viewBox="0 0 16 16" className="h-3.5 w-3.5 fill-white">
              <path d="M2 3h12v2H2zM2 7h8v2H2zM2 11h10v2H2z" />
            </svg>
          </div>
          <span className="text-base font-medium text-gray-900">TaskFlow</span>
        </div>

        {user && (
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-50 text-xs font-medium text-brand-700">
              {initials(user.displayName)}
            </div>
            <div className="hidden text-right sm:block">
              <div className="text-sm font-medium text-gray-900">{user.displayName}</div>
              <div className="text-xs text-gray-500">{user.email}</div>
            </div>
            <Button
              type="button"
              variant="secondary"
              loading={logoutMutation.isPending}
              onClick={() => logoutMutation.mutate()}
            >
              Log out
            </Button>
          </div>
        )}
      </header>

      <main className="flex flex-1 items-center justify-center px-4">
        <div className="text-center">
          <h1 className="text-lg font-semibold text-gray-900">You&apos;re logged in</h1>
          <p className="mt-1 text-sm text-gray-500">Workspaces, projects and tasks are coming soon.</p>
        </div>
      </main>
    </div>
  )
}
