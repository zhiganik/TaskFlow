import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notificationsApi } from '../api/notifications.api'
import { useNotificationStore } from '../store/notificationStore'

export const NOTIFICATIONS_KEY = ['notifications'] as const

export interface NotificationsFilter {
  unreadOnly?: boolean
  type?: string
}

export function useNotifications(filter: NotificationsFilter = {}) {
  const setUnreadCount = useNotificationStore((s) => s.setUnreadCount)
  const queryClient = useQueryClient()

  const query = useInfiniteQuery({
    queryKey: [...NOTIFICATIONS_KEY, filter.unreadOnly, filter.type],
    queryFn: ({ pageParam }) =>
      notificationsApi.list(pageParam as string | undefined, 20, filter.unreadOnly, filter.type),
    getNextPageParam: (last) => (last.hasMore ? last.nextCursor : undefined),
    initialPageParam: undefined as string | undefined,
  })

  const markRead = useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY })
      notificationsApi.unreadCount().then((r) => setUnreadCount(r.count)).catch(() => {})
    },
  })

  const markAllRead = useMutation({
    mutationFn: () => notificationsApi.markAllRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: NOTIFICATIONS_KEY })
      setUnreadCount(0)
    },
  })

  return { query, markRead, markAllRead }
}
