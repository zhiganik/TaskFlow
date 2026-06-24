import { NavLink, useParams } from 'react-router-dom'
import { useLogout } from '../../hooks/useAuth'
import { useAuthStore } from '../../store/authStore'
import { WorkspaceSwitcher } from '../workspaces/WorkspaceSwitcher'
import { Button } from '../ui/Button'

function navLinkClassName({ isActive }: { isActive: boolean }) {
  return `block rounded-md px-2.5 py-1.5 text-xs font-medium ${
    isActive ? 'bg-brand-50 text-brand-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
  }`
}

function initials(displayName: string) {
  return displayName
    .split(' ')
    .map((part) => part[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export function Sidebar() {
  const user = useAuthStore((s) => s.user)
  const logoutMutation = useLogout()
  const { workspaceId } = useParams<{ workspaceId: string }>()

  return (
    <aside className="flex w-56 shrink-0 flex-col border-r border-gray-200 bg-white py-4">
      <div className="mb-4 flex items-center gap-2 px-4">
        <div className="flex h-7 w-7 items-center justify-center rounded-md bg-brand-500">
          <svg viewBox="0 0 16 16" className="h-3.5 w-3.5 fill-white">
            <path d="M2 3h12v2H2zM2 7h8v2H2zM2 11h10v2H2z" />
          </svg>
        </div>
        <span className="text-base font-medium text-gray-900">TaskFlow</span>
      </div>

      <WorkspaceSwitcher />

      {workspaceId && (
        <nav className="mt-3 flex flex-col gap-0.5 px-2">
          <NavLink to={`/workspaces/${workspaceId}`} end className={navLinkClassName}>
            Board
          </NavLink>
          <NavLink to={`/workspaces/${workspaceId}/members`} className={navLinkClassName}>
            Members
          </NavLink>
        </nav>
      )}

      <div className="mt-auto flex items-center gap-2 border-t border-gray-100 px-4 pt-3">
        {user && (
          <>
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-brand-50 text-xs font-medium text-brand-700">
              {initials(user.displayName)}
            </div>
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-medium text-gray-900">{user.displayName}</div>
              <div className="truncate text-xs text-gray-500">{user.email}</div>
            </div>
          </>
        )}
        <Button
          type="button"
          variant="secondary"
          className="shrink-0 px-2.5 py-1.5 text-xs"
          loading={logoutMutation.isPending}
          onClick={() => logoutMutation.mutate()}
        >
          Log out
        </Button>
      </div>
    </aside>
  )
}
