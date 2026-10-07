import type { CountResponse, NotificationDto, PagedResult, PageQuery } from '../types'
import { apiClient } from './client'

const BASE = '/api/notifications'

export const notificationsApi = {
  list: async (query: PageQuery): Promise<PagedResult<NotificationDto>> =>
    (await apiClient.get<PagedResult<NotificationDto>>(BASE, { params: query })).data,

  unreadCount: async (): Promise<number> => (await apiClient.get<CountResponse>(`${BASE}/unread-count`)).data.count,

  markRead: async (id: number): Promise<void> => {
    await apiClient.put(`${BASE}/${id}/read`)
  },

  markAllRead: async (): Promise<void> => {
    await apiClient.put(`${BASE}/read-all`)
  },
}
