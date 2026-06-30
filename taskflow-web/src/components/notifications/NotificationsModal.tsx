import { useEffect, type MouseEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useNotifications, type NotificationsFilter } from '../../hooks/useNotifications'
import { useNotificationStore } from '../../store/notificationStore'
import { formatDistanceToNow } from '../../lib/dateUtils'
import type { NotificationDto, NotificationType } from '../../types/api.types'

const TYPE_LABELS: Record<NotificationType, string> = {
  MentionedInComment: 'Mention',
  TaskAssigned:       'Assigned',
  TaskStatusChanged:  'Status',
  MemberInvited:      'Invited',
}

const ALL_TYPES: NotificationType[] = [
  'MentionedInComment',
  'TaskAssigned',
  'TaskStatusChanged',
  'MemberInvited',
]

interface Props {
  unreadOnly: boolean
  typeFilter: NotificationType | undefined
  onUnreadOnlyChange: (v: boolean) => void
  onTypeFilterChange: (v: NotificationType | undefined) => void
  onClose: () => void
}

export function NotificationsModal({
  unreadOnly,
  typeFilter,
  onUnreadOnlyChange,
  onTypeFilterChange,
  onClose,
}: Props) {
  const navigate = useNavigate()
  const setUnreadCount = useNotificationStore((s) => s.setUnreadCount)

  const filter: NotificationsFilter = { unreadOnly, type: typeFilter }
  const { query, markRead, markAllRead } = useNotifications(filter)

  const notifications = query.data?.pages.flatMap((p) => p.items) ?? []

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose() }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])

  function handleClick(n: NotificationDto) {
    if (!n.isRead) markRead.mutate(n.id)
    if (n.workspaceId) {
      const params = new URLSearchParams()
      if (n.taskId) params.set('taskId', n.taskId)
      if (n.commentId) params.set('commentId', n.commentId)
      const qs = params.size > 0 ? `?${params}` : ''
      navigate(`/workspaces/${n.workspaceId}${qs}`, { replace: false })
    }
    onClose()
  }

  function handleMarkAllRead() {
    markAllRead.mutate(undefined, { onSuccess: () => setUnreadCount(0) })
  }

  const stopPropagation = (e: MouseEvent) => e.stopPropagation()

  return (
    <div
      className="fixed inset-0 z-50 flex flex-col bg-white sm:items-center sm:justify-center sm:bg-black/30 sm:p-4"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Notifications"
        className="flex h-full w-full flex-col bg-white sm:h-auto sm:max-h-[80vh] sm:w-full sm:max-w-lg sm:rounded-xl sm:border sm:border-gray-200 sm:shadow-xl"
        onClick={stopPropagation}
      >
        {/* Header */}
        <div className="flex items-center justify-between border-b border-gray-100 px-5 py-4">
          <span className="text-base font-semibold text-gray-900">Notifications</span>
          <div className="flex items-center gap-3">
            {notifications.some((n) => !n.isRead) && (
              <button
                type="button"
                onClick={handleMarkAllRead}
                className="text-xs text-brand-600 hover:text-brand-700"
              >
                Mark all read
              </button>
            )}
            <button
              type="button"
              onClick={onClose}
              className="flex h-8 w-8 items-center justify-center rounded-md text-gray-400 hover:bg-gray-100 hover:text-gray-600"
              aria-label="Close notifications"
            >
              ✕
            </button>
          </div>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-3 border-b border-gray-100 px-5 py-3">
          {/* All / Unread tabs */}
          <div className="flex gap-1 rounded-lg border border-gray-200 bg-gray-50 p-0.5">
            {(['all', 'unread'] as const).map((tab) => (
              <button
                key={tab}
                type="button"
                onClick={() => onUnreadOnlyChange(tab === 'unread')}
                className={`rounded-md px-3 py-1 text-xs font-medium transition-colors ${
                  (tab === 'unread') === unreadOnly
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-500 hover:text-gray-700'
                }`}
              >
                {tab === 'all' ? 'All' : 'Unread'}
              </button>
            ))}
          </div>

          {/* Type dropdown */}
          <select
            value={typeFilter ?? ''}
            onChange={(e) =>
              onTypeFilterChange(e.target.value ? (e.target.value as NotificationType) : undefined)
            }
            className="h-7 rounded-md border border-gray-200 bg-white px-2 text-xs text-gray-700 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500"
          >
            <option value="">All types</option>
            {ALL_TYPES.map((t) => (
              <option key={t} value={t}>
                {TYPE_LABELS[t]}
              </option>
            ))}
          </select>
        </div>

        {/* List */}
        <div className="overflow-y-auto">
          {query.isPending && (
            <div className="px-5 py-10 text-center text-sm text-gray-400">Loading…</div>
          )}

          {!query.isPending && notifications.length === 0 && (
            <div className="px-5 py-10 text-center text-sm text-gray-400">
              No notifications
            </div>
          )}

          {notifications.map((n, i) => (
            <button
              key={n.id}
              type="button"
              onClick={() => handleClick(n)}
              className={`flex w-full items-start gap-3 px-5 py-3.5 text-left transition-colors hover:bg-gray-50 ${
                i > 0 ? 'border-t border-gray-100' : ''
              } ${!n.isRead ? 'bg-brand-50 hover:bg-brand-50/80' : ''}`}
            >
              <span className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${!n.isRead ? 'bg-brand-500' : 'bg-transparent'}`} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-medium text-gray-900 line-clamp-1">{n.title}</span>
                  <span className="shrink-0 rounded-full bg-gray-100 px-2 py-0.5 text-[10px] font-medium text-gray-500">
                    {TYPE_LABELS[n.type]}
                  </span>
                </div>
                <p className="mt-0.5 text-xs text-gray-500 line-clamp-2">{n.body}</p>
                <p className="mt-1 text-[11px] text-gray-400">{formatDistanceToNow(n.createdAt)}</p>
              </div>
            </button>
          ))}

          {query.hasNextPage && (
            <button
              type="button"
              onClick={() => query.fetchNextPage()}
              disabled={query.isFetchingNextPage}
              className="w-full border-t border-gray-100 py-3 text-xs text-brand-600 hover:text-brand-700 disabled:opacity-50"
            >
              {query.isFetchingNextPage ? 'Loading…' : 'Load more'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
