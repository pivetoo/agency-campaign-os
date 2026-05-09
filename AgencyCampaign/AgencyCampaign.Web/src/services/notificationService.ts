import { httpClient } from 'archon-ui'
import type { Notification } from '../types/notification'

const BASE_URL = '/Notifications'

interface PagedResponse<T> {
  items: T[]
  pagination: { page: number; pageSize: number; totalItems: number; totalPages: number }
}

export const notificationService = {
  async getRecent(unreadOnly = false, pageSize = 20): Promise<Notification[]> {
    const params = new URLSearchParams()
    if (unreadOnly) params.append('unreadOnly', 'true')
    params.append('pageSize', String(pageSize))
    const response = await httpClient.get<PagedResponse<Notification>>(`${BASE_URL}/Get?${params.toString()}`)
    return response.data?.items ?? []
  },

  async getUnreadCount(): Promise<number> {
    const response = await httpClient.get<{ count: number }>(`${BASE_URL}/unread-count`)
    return response.data?.count ?? 0
  },

  markAsRead(id: number) {
    return httpClient.post(`${BASE_URL}/MarkAsRead/${id}/read`, {})
  },

  markAllAsRead() {
    return httpClient.post(`${BASE_URL}/mark-all-read`, {})
  },

  delete(id: number) {
    return httpClient.delete(`${BASE_URL}/Delete/${id}`)
  },

  clearAll() {
    return httpClient.delete(`${BASE_URL}/clear-all`)
  },
}
