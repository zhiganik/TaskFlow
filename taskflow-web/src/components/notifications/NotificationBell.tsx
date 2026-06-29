import { useEffect, useState } from 'react'
import { notificationsApi } from '../../api/notifications.api'
import { BellIcon } from '../ui/Icons'
import { useNotificationStore } from '../../store/notificationStore'
import { NotificationPanel } from './NotificationPanel'

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const unreadCount = useNotificationStore((s) => s.unreadCount)
  const setUnreadCount = useNotificationStore((s) => s.setUnreadCount)

  useEffect(() => {
    notificationsApi.unreadCount().then((r) => setUnreadCount(r.count)).catch(() => {})
  }, [setUnreadCount])

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="relative flex h-8 w-8 items-center justify-center rounded-md text-gray-500 hover:bg-gray-100 hover:text-gray-900"
        aria-label="Notifications"
      >
        <BellIcon className="h-4 w-4" />
        {unreadCount > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[10px] font-semibold text-white">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {open && <NotificationPanel onClose={() => setOpen(false)} />}
    </div>
  )
}
