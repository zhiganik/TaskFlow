import { useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { getAccessToken } from '../store/authStore'
import { useNotificationStore } from '../store/notificationStore'
import { NOTIFICATIONS_KEY } from './useNotifications'
import type { NotificationDto } from '../types/api.types'

const HUB_BASE = import.meta.env.VITE_HUB_URL ?? ''
const HUB_URL  = `${HUB_BASE}/hubs/notifications`

export function useNotificationHub() {
  const queryClient = useQueryClient()
  const incrementUnread = useNotificationStore((s) => s.incrementUnread)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => getAccessToken() ?? '',
        transport: signalR.HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
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

    connection.start()
      .then(() => console.log('[Hub] Connected:', HUB_URL))
      .catch((e) => console.error('[Hub] Connection failed:', e))

    return () => {
      connection.stop().catch(() => {})
    }
  }, [queryClient, incrementUnread])
}
