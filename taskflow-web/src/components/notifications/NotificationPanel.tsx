import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useNotifications } from '../../hooks/useNotifications'
import type { NotificationDto } from '../../types/api.types'
import { formatDistanceToNow } from '../../lib/dateUtils'

interface Props {
  onClose: () => void
}

export function NotificationPanel({ onClose }: Props) {
  const panelRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const { query, markRead, markAllRead } = useNotifications()

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (panelRef.current && !panelRef.current.contains(e.target as Node)) {
        onClose()
      }
    }
    document.addEventListener('mousedown', handleClick)
    return () => document.removeEventListener('mousedown', handleClick)
  }, [onClose])

  const notifications = query.data?.pages.flatMap((p) => p.items) ?? []

  function handleNotificationClick(n: NotificationDto) {
    if (!n.isRead) {
      markRead.mutate(n.id)
    }
    if (n.workspaceId && n.taskId) {
      navigate(`/workspaces/${n.workspaceId}`)
    } else if (n.workspaceId) {
      navigate(`/workspaces/${n.workspaceId}`)
    }
    onClose()
  }

  return (
    <div
      ref={panelRef}
      className="absolute bottom-16 left-56 z-50 w-80 rounded-lg border border-gray-200 bg-white shadow-lg"
    >
      <div className="flex items-center justify-between border-b border-gray-100 px-4 py-3">
        <span className="text-sm font-semibold text-gray-900">Notifications</span>
        {notifications.some((n) => !n.isRead) && (
          <button
            type="button"
            onClick={() => markAllRead.mutate()}
            className="text-xs text-brand-600 hover:text-brand-700"
          >
            Mark all read
          </button>
        )}
      </div>

      <div className="max-h-96 overflow-y-auto">
        {query.isPending && (
          <div className="px-4 py-6 text-center text-sm text-gray-400">Loading…</div>
        )}

        {!query.isPending && notifications.length === 0 && (
          <div className="px-4 py-6 text-center text-sm text-gray-400">No notifications</div>
        )}

        {notifications.map((n) => (
          <button
            key={n.id}
            type="button"
            onClick={() => handleNotificationClick(n)}
            className={`flex w-full flex-col gap-0.5 px-4 py-3 text-left hover:bg-gray-50 ${
              !n.isRead ? 'bg-brand-50' : ''
            }`}
          >
            <span className="text-xs font-medium text-gray-900 line-clamp-1">{n.title}</span>
            <span className="text-xs text-gray-500 line-clamp-2">{n.body}</span>
            <span className="text-xs text-gray-400">{formatDistanceToNow(n.createdAt)}</span>
          </button>
        ))}

        {query.hasNextPage && (
          <button
            type="button"
            onClick={() => query.fetchNextPage()}
            disabled={query.isFetchingNextPage}
            className="w-full border-t border-gray-100 py-2.5 text-xs text-brand-600 hover:text-brand-700 disabled:opacity-50"
          >
            {query.isFetchingNextPage ? 'Loading…' : 'Load more'}
          </button>
        )}
      </div>
    </div>
  )
}
