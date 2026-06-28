import { useState } from 'react'
import { NavLink, useParams } from 'react-router-dom'
import { useProfile } from '../../hooks/useProfile'
import { useAuthStore } from '../../store/authStore'
import { ArchiveIcon, BoardIcon, MembersIcon, SettingsIcon } from '../ui/Icons'
import { ProfileModal } from '../profile/ProfileModal'
import { UserAvatar } from '../ui/UserAvatar'
import { WorkspaceSwitcher } from '../workspaces/WorkspaceSwitcher'

function navLinkClassName({ isActive }: { isActive: boolean }) {
  return `block rounded-md px-2.5 py-1.5 text-xs font-medium ${
    isActive ? 'bg-brand-50 text-brand-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
  }`
}

export function Sidebar() {
  // Keep profile in sync for the lifetime of the app session (not just while ProfileModal is open).
  // This is also what drives the Pending → Ready polling and the resulting tasks/members invalidation.
  useProfile()
  const user = useAuthStore((s) => s.user)
  const { workspaceId } = useParams<{ workspaceId: string }>()
  const [profileOpen, setProfileOpen] = useState(false)

  return (
    <>
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
              <span className="flex items-center gap-1.5">
                <BoardIcon className="h-3.5 w-3.5" />
                Board
              </span>
            </NavLink>
            <NavLink to={`/workspaces/${workspaceId}/archive`} className={navLinkClassName}>
              <span className="flex items-center gap-1.5">
                <ArchiveIcon className="h-3.5 w-3.5" />
                Archive
              </span>
            </NavLink>
            <NavLink to={`/workspaces/${workspaceId}/members`} className={navLinkClassName}>
              <span className="flex items-center gap-1.5">
                <MembersIcon className="h-3.5 w-3.5" />
                Members
              </span>
            </NavLink>
            <NavLink to={`/workspaces/${workspaceId}/settings`} className={navLinkClassName}>
              <span className="flex items-center gap-1.5">
                <SettingsIcon className="h-3.5 w-3.5" />
                Settings
              </span>
            </NavLink>
          </nav>
        )}

        {user && (
          <button
            type="button"
            onClick={() => setProfileOpen(true)}
            className="mt-auto flex items-center gap-2 border-t border-gray-100 px-4 pt-3 text-left hover:bg-gray-50"
          >
            <UserAvatar
              displayName={user.displayName}
              avatarColor={user.avatarColor}
              avatarPath={user.avatarPath}
              avatarStatus={user.avatarStatus}
              size="md"
            />
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-medium text-gray-900">{user.displayName}</div>
              <div className="truncate text-xs text-gray-500">{user.email}</div>
            </div>
          </button>
        )}
      </aside>

      {profileOpen && <ProfileModal onClose={() => setProfileOpen(false)} />}
    </>
  )
}
