import { useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { getAccessToken } from '../store/authStore'
import { useNotificationStore } from '../store/notificationStore'
import { NOTIFICATIONS_KEY } from './useNotifications'
import type { NotificationDto } from '../types/api.types'

const HUB_URL = '/hubs/notifications'

export function useNotificationHub() {
  const queryClient = useQueryClient()
  const incrementUnread = useNotificationStore((s) => s.incrementUnread)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => getAccessToken() ?? '',
        // skipNegotiation + WebSockets avoids the negotiate POST hitting a different
        // replica than the WebSocket upgrade — connectionToken is server-local, not in Redis.
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('ReceiveNotification', (dto: NotificationDto) => {
      queryClient.setQueryData(NOTIFICATIONS_KEY, (old: unknown) => {
        if (!old) return old
        const data = old as { pages: { items: NotificationDto[] }[] }
        return {
          ...data,
          pages: data.pages.map((page, i) =>
            i === 0 ? { ...page, items: [dto, ...page.items] } : page,
          ),
        }
      })
      incrementUnread()
    })

    connection.start().catch(() => {})

    return () => {
      connection.stop().catch(() => {})
    }
  }, [queryClient, incrementUnread])
}
