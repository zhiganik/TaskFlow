import { useEffect, useState } from 'react'
import { NavLink, useParams } from 'react-router-dom'
import { notificationsApi } from '../../api/notifications.api'
import { useProfile } from '../../hooks/useProfile'
import { useNotificationHub } from '../../hooks/useNotificationHub'
import { useAuthStore } from '../../store/authStore'
import { useNotificationStore } from '../../store/notificationStore'
import { ArchiveIcon, BellIcon, BoardIcon, MembersIcon, SettingsIcon } from '../ui/Icons'
import { ProfileModal } from '../profile/ProfileModal'
import { UserAvatar } from '../ui/UserAvatar'
import { WorkspaceSwitcher } from '../workspaces/WorkspaceSwitcher'
import { NotificationsModal } from '../notifications/NotificationsModal'
import type { NotificationType } from '../../types/api.types'

function navLinkClassName({ isActive }: { isActive: boolean }) {
  return `block rounded-md px-2.5 py-1.5 text-xs font-medium ${
    isActive ? 'bg-brand-50 text-brand-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
  }`
}

export function Sidebar() {
  useProfile()
  useNotificationHub()
  const user = useAuthStore((s) => s.user)
  const unreadCount = useNotificationStore((s) => s.unreadCount)
  const setUnreadCount = useNotificationStore((s) => s.setUnreadCount)
  const { workspaceId } = useParams<{ workspaceId: string }>()
  const [profileOpen, setProfileOpen] = useState(false)
  const [notifOpen, setNotifOpen] = useState(false)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [typeFilter, setTypeFilter] = useState<NotificationType | undefined>()

  useEffect(() => {
    notificationsApi.unreadCount().then((r) => setUnreadCount(r.count)).catch(() => {})
  }, [setUnreadCount])

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
          <div className="mt-auto">
            <div className="border-t border-gray-100 px-2 pt-2 pb-1">
              <button
                type="button"
                onClick={() => setNotifOpen(true)}
                className="flex w-full items-center gap-1.5 rounded-md px-2.5 py-1.5 text-xs font-medium text-gray-600 hover:bg-gray-50 hover:text-gray-900"
              >
                <span className="relative flex items-center">
                  <BellIcon className="h-3.5 w-3.5" />
                  {unreadCount > 0 && (
                    <span className="absolute -right-1.5 -top-1.5 flex h-3 w-3 items-center justify-center rounded-full bg-red-500 text-[8px] font-semibold leading-none text-white">
                      {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                  )}
                </span>
                Notifications
              </button>
            </div>
            <button
              type="button"
              onClick={() => setProfileOpen(true)}
              className="flex w-full items-center gap-2 border-t border-gray-100 px-4 py-3 text-left hover:bg-gray-50"
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
          </div>
        )}
      </aside>

      {profileOpen && <ProfileModal onClose={() => setProfileOpen(false)} />}

      {notifOpen && (
        <NotificationsModal
          unreadOnly={unreadOnly}
          typeFilter={typeFilter}
          onUnreadOnlyChange={setUnreadOnly}
          onTypeFilterChange={setTypeFilter}
          onClose={() => setNotifOpen(false)}
        />
      )}
    </>
  )
}
