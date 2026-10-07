import type { ConversationDto, MessageDto, SendMessageRequest } from '../types'
import { apiClient } from './client'

const BASE = '/api/conversations'

export const MESSAGE_PAGE_SIZE = 30

export const conversationsApi = {
  list: async (): Promise<ConversationDto[]> => (await apiClient.get<ConversationDto[]>(BASE)).data,

  /** Trả về tin nhắn theo thứ tự cũ → mới; `before` = messageId để tải tin cũ hơn. */
  messages: async (id: number, params: { before?: number; limit?: number } = {}): Promise<MessageDto[]> =>
    (
      await apiClient.get<MessageDto[]>(`${BASE}/${id}/messages`, {
        params: { before: params.before, limit: params.limit ?? MESSAGE_PAGE_SIZE },
      })
    ).data,

  send: async (id: number, body: SendMessageRequest): Promise<MessageDto> =>
    (await apiClient.post<MessageDto>(`${BASE}/${id}/messages`, body)).data,

  markRead: async (id: number): Promise<void> => {
    await apiClient.put(`${BASE}/${id}/read`)
  },
}
