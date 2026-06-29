import { apiClient } from './client'
import type { NotificationDto, PagedResult, UnreadCountDto } from '../types/api.types'

export const notificationsApi = {
  list: (cursor?: string | null, limit = 20, unreadOnly = false, type?: string) =>
    apiClient
      .get<PagedResult<NotificationDto>>('/notifications', {
        params: { cursor: cursor ?? undefined, limit, unreadOnly: unreadOnly || undefined, type: type || undefined },
      })
      .then((r) => r.data),

  unreadCount: () =>
    apiClient.get<UnreadCountDto>('/notifications/unread-count').then((r) => r.data),

  markRead: (id: string) =>
    apiClient.put<void>(`/notifications/${id}/read`).then((r) => r.data),

  markAllRead: () =>
    apiClient.put<void>('/notifications/read-all').then((r) => r.data),
}
