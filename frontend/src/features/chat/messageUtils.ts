import type { MessageDto } from '../../types'

/**
 * Gộp tin nhắn mới vào danh sách hiện có, loại trùng theo messageId (tin nhắn có thể đến từ
 * SignalR, từ kết quả SendMessage và từ lần tải lại sau khi kết nối lại) và sắp xếp cũ → mới.
 */
export function mergeMessages(current: MessageDto[], incoming: MessageDto[]): MessageDto[] {
  if (incoming.length === 0) return current
  const byId = new Map<number, MessageDto>()
  for (const m of current) byId.set(m.messageId, m)
  for (const m of incoming) byId.set(m.messageId, { ...byId.get(m.messageId), ...m })
  return [...byId.values()].sort((a, b) => a.messageId - b.messageId)
}
