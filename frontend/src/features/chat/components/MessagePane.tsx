import { ArrowLeftOutlined, SendOutlined } from '@ant-design/icons'
import { HubConnectionState } from '@microsoft/signalr'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Flex, Input, Spin, Tooltip, Typography } from 'antd'
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { MESSAGE_PAGE_SIZE, conversationsApi } from '../../../api/conversations'
import { queryKeys } from '../../../api/queryKeys'
import { UserAvatar } from '../../../components/shared/UserAvatar'
import type { HubState } from '../../../hooks/useSignalR'
import type { ConversationDto, MessageDto, MessagesReadEvent } from '../../../types'
import { formatChatTime, formatDateTime } from '../../../utils/datetime'
import { getErrorMessage } from '../../../utils/errors'
import { feedback } from '../../../utils/feedback'
import { CHAT_MESSAGE_MAX } from '../../../utils/validation'
import { mergeMessages } from '../messageUtils'

/** Khoảng tải lại dự phòng khi mất kết nối SignalR. */
const FALLBACK_POLL_MS = 10_000

/** Lỗi khi gửi: REST trả `{code, message}`; hub trả HubException dạng "...HubException: <thông điệp>". */
function sendErrorMessage(err: unknown): string {
  if (err instanceof Error && !('isAxiosError' in err)) {
    const idx = err.message.indexOf('HubException: ')
    if (idx >= 0) return err.message.slice(idx + 'HubException: '.length)
  }
  return getErrorMessage(err, 'Không gửi được tin nhắn. Vui lòng thử lại.')
}

interface MessagePaneProps {
  conversation: ConversationDto | undefined
  conversationId: number
  currentUserId: number
  hub: HubState
  showBack: boolean
}

/**
 * Cửa sổ tin nhắn của một hội thoại. Component được gắn `key={conversationId}` nên state reset khi đổi hội thoại.
 * Nguồn tin nhắn: tải lần đầu / tải lại định kỳ (REST) + sự kiện `message` (SignalR) + kết quả gửi → gộp, loại trùng theo messageId.
 */
export function MessagePane({ conversation, conversationId, currentUserId, hub, showBack }: MessagePaneProps) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { connection, isConnected } = hub
  /** Tin nhắn nhận ngoài lần tải REST mới nhất: từ SignalR, kết quả gửi, các trang cũ hơn. */
  const [extra, setExtra] = useState<MessageDto[]>([])
  const [olderExhausted, setOlderExhausted] = useState(false)
  /** Mốc đã đọc: tin của tôi có id ≤ `mine` đã được người kia xem; tin đến có id ≤ `theirs` tôi đã xem. */
  const [readUpTo, setReadUpTo] = useState({ mine: 0, theirs: 0 })
  const maxIdRef = useRef(0)
  const [loadingOlder, setLoadingOlder] = useState(false)
  const [draft, setDraft] = useState('')
  const [sending, setSending] = useState(false)

  const scrollRef = useRef<HTMLDivElement>(null)
  /** scrollHeight trước khi chèn tin cũ lên đầu — để giữ nguyên vị trí đang đọc. */
  const prependAnchorRef = useRef<number | null>(null)
  const stickToBottomRef = useRef(true)

  // Tin nhắn mới nhất: tải lần đầu, và tải lại mỗi 10s khi mất kết nối realtime.
  const latest = useQuery({
    queryKey: queryKeys.conversations.latestMessages(conversationId),
    queryFn: () => conversationsApi.messages(conversationId),
    refetchInterval: isConnected ? false : FALLBACK_POLL_MS,
    staleTime: 0,
  })

  const latestData = latest.data

  // Danh sách hiển thị = gộp (loại trùng theo messageId) + áp dụng mốc đã đọc.
  const messages = useMemo(
    () =>
      mergeMessages(extra, latestData ?? []).map((m) => {
        const read = m.senderId === currentUserId ? m.messageId <= readUpTo.mine : m.messageId <= readUpTo.theirs
        return read && !m.isRead ? { ...m, isRead: true } : m
      }),
    [extra, latestData, readUpTo, currentUserId],
  )
  useEffect(() => {
    maxIdRef.current = messages.length > 0 ? messages[messages.length - 1].messageId : 0
  }, [messages])

  // Trang mới nhất chưa đủ một trang → không còn tin cũ hơn.
  const hasMore = !olderExhausted && (latestData?.length ?? 0) >= MESSAGE_PAGE_SIZE

  const invalidateConversations = useCallback(
    () => queryClient.invalidateQueries({ queryKey: queryKeys.conversations.list }),
    [queryClient],
  )

  const markRead = useCallback(async () => {
    try {
      if (connection && connection.state === HubConnectionState.Connected) {
        await connection.invoke('MarkRead', conversationId)
      } else {
        await conversationsApi.markRead(conversationId)
      }
    } catch {
      // Không chặn người dùng nếu đánh dấu đã đọc thất bại.
    }
    void invalidateConversations()
  }, [connection, conversationId, invalidateConversations])

  // Sự kiện realtime cho hội thoại này.
  useEffect(() => {
    if (!connection) return undefined
    const onMessage = (m: MessageDto) => {
      if (m.conversationId !== conversationId) return
      setExtra((prev) => mergeMessages(prev, [m]))
    }
    const onRead = (e: MessagesReadEvent) => {
      if (e.conversationId !== conversationId || e.readerId === currentUserId) return
      setReadUpTo((prev) => ({ ...prev, mine: Math.max(prev.mine, maxIdRef.current) }))
    }
    connection.on('message', onMessage)
    connection.on('messagesRead', onRead)
    return () => {
      connection.off('message', onMessage)
      connection.off('messagesRead', onRead)
    }
  }, [connection, conversationId, currentUserId])

  // Có tin chưa đọc từ người kia (khi mở hội thoại hoặc vừa nhận) → đánh dấu đã đọc.
  const lastUnreadIncomingId = messages.reduce<number | null>(
    (acc, m) => (m.senderId !== currentUserId && !m.isRead ? Math.max(acc ?? 0, m.messageId) : acc),
    null,
  )
  useEffect(() => {
    if (lastUnreadIncomingId === null) return
    void markRead().then(() =>
      setReadUpTo((prev) => ({ ...prev, theirs: Math.max(prev.theirs, lastUnreadIncomingId) })),
    )
  }, [lastUnreadIncomingId, markRead])

  // Cuộn: giữ vị trí khi chèn tin cũ; tự cuộn xuống cuối khi đang ở cuối và có tin mới.
  useLayoutEffect(() => {
    const el = scrollRef.current
    if (!el) return
    if (prependAnchorRef.current !== null) {
      el.scrollTop = el.scrollHeight - prependAnchorRef.current
      prependAnchorRef.current = null
    } else if (stickToBottomRef.current) {
      el.scrollTop = el.scrollHeight
    }
  }, [messages])

  const handleScroll = () => {
    const el = scrollRef.current
    if (!el) return
    stickToBottomRef.current = el.scrollHeight - el.scrollTop - el.clientHeight < 80
  }

  const loadOlder = async () => {
    const oldest = messages[0]
    if (!oldest || loadingOlder) return
    setLoadingOlder(true)
    try {
      const older = await conversationsApi.messages(conversationId, { before: oldest.messageId })
      prependAnchorRef.current = scrollRef.current?.scrollHeight ?? null
      if (older.length < MESSAGE_PAGE_SIZE) setOlderExhausted(true)
      setExtra((prev) => mergeMessages(prev, older))
    } catch (err) {
      feedback.message.error(getErrorMessage(err))
    } finally {
      setLoadingOlder(false)
    }
  }

  const send = async () => {
    const content = draft.trim()
    if (!content || sending) return
    if (content.length > CHAT_MESSAGE_MAX) {
      feedback.message.error(`Tin nhắn tối đa ${CHAT_MESSAGE_MAX} ký tự.`)
      return
    }
    setSending(true)
    try {
      const sent =
        connection && connection.state === HubConnectionState.Connected
          ? await connection.invoke<MessageDto>('SendMessage', conversationId, content)
          : await conversationsApi.send(conversationId, { content })
      stickToBottomRef.current = true
      setExtra((prev) => mergeMessages(prev, [sent]))
      setDraft('')
      void invalidateConversations()
    } catch (err) {
      feedback.message.error(sendErrorMessage(err))
    } finally {
      setSending(false)
    }
  }

  return (
    <Flex vertical style={{ height: '100%', minHeight: 0 }}>
      <Flex align="center" gap={10} style={{ padding: '10px 12px', borderBottom: '1px solid #f0f0f0' }}>
        {showBack && (
          <Button type="text" icon={<ArrowLeftOutlined />} aria-label="Quay lại" onClick={() => navigate('/chat')} />
        )}
        {conversation && (
          <>
            <UserAvatar url={conversation.otherUserAvatarUrl} name={conversation.otherUserName} />
            <div style={{ minWidth: 0, flex: 1 }}>
              <Link to={`/users/${conversation.otherUserId}`}>
                <Typography.Text strong ellipsis style={{ display: 'block' }}>
                  {conversation.otherUserName}
                </Typography.Text>
              </Link>
              <Link to={`/posts/${conversation.postId}`}>
                <Typography.Text type="secondary" ellipsis style={{ fontSize: 12, display: 'block' }}>
                  {conversation.postTitle}
                </Typography.Text>
              </Link>
            </div>
          </>
        )}
        <Tooltip title={isConnected ? 'Đang kết nối realtime' : 'Mất kết nối – tự tải lại mỗi 10 giây'}>
          <span
            aria-label={isConnected ? 'Đang kết nối' : 'Mất kết nối'}
            style={{
              width: 10,
              height: 10,
              borderRadius: '50%',
              background: isConnected ? '#52c41a' : '#faad14',
              flexShrink: 0,
              marginLeft: 'auto',
            }}
          />
        </Tooltip>
      </Flex>

      {!isConnected && (
        <Alert
          type="warning"
          banner
          message="Đang kết nối lại máy chủ tin nhắn… Tin nhắn mới sẽ được tải lại mỗi 10 giây."
        />
      )}

      <div
        ref={scrollRef}
        onScroll={handleScroll}
        style={{ flex: 1, overflowY: 'auto', padding: 12, background: '#fafafa', minHeight: 0 }}
      >
        {latest.isLoading ? (
          <Flex justify="center" style={{ padding: 24 }}>
            <Spin />
          </Flex>
        ) : latest.isError && messages.length === 0 ? (
          <Alert type="error" showIcon message={getErrorMessage(latest.error)} />
        ) : (
          <>
            {hasMore && (
              <Flex justify="center" style={{ marginBottom: 8 }}>
                <Button size="small" loading={loadingOlder} onClick={() => void loadOlder()}>
                  Tải tin nhắn cũ hơn
                </Button>
              </Flex>
            )}
            {messages.length === 0 && (
              <Typography.Paragraph type="secondary" style={{ textAlign: 'center', marginTop: 24 }}>
                Hãy gửi lời chào đầu tiên!
              </Typography.Paragraph>
            )}
            {messages.map((m, i) => {
              const mine = m.senderId === currentUserId
              const isLastMine = mine && !messages.slice(i + 1).some((x) => x.senderId === currentUserId)
              return (
                <Flex key={m.messageId} justify={mine ? 'flex-end' : 'flex-start'} style={{ marginBottom: 6 }}>
                  <Tooltip title={formatDateTime(m.sentAt)} placement={mine ? 'left' : 'right'}>
                    <div
                      style={{
                        maxWidth: 'min(75%, 520px)',
                        padding: '6px 12px',
                        borderRadius: 14,
                        background: mine ? '#1677ff' : '#fff',
                        color: mine ? '#fff' : 'rgba(0,0,0,0.88)',
                        border: mine ? 'none' : '1px solid #f0f0f0',
                        whiteSpace: 'pre-wrap',
                        wordBreak: 'break-word',
                      }}
                    >
                      {m.content}
                      <div style={{ fontSize: 11, opacity: 0.7, textAlign: 'right', marginTop: 2 }}>
                        {formatChatTime(m.sentAt)}
                        {isLastMine && (m.isRead ? ' · Đã xem' : ' · Đã gửi')}
                      </div>
                    </div>
                  </Tooltip>
                </Flex>
              )
            })}
          </>
        )}
      </div>

      <Flex gap={8} style={{ padding: 10, borderTop: '1px solid #f0f0f0' }} align="flex-end">
        <Input.TextArea
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Nhập tin nhắn… (Enter để gửi, Shift+Enter xuống dòng)"
          autoSize={{ minRows: 1, maxRows: 4 }}
          maxLength={CHAT_MESSAGE_MAX}
          onPressEnter={(e) => {
            if (!e.shiftKey) {
              e.preventDefault()
              void send()
            }
          }}
        />
        <Button
          type="primary"
          icon={<SendOutlined />}
          loading={sending}
          disabled={!draft.trim()}
          onClick={() => void send()}
          aria-label="Gửi"
        />
      </Flex>
    </Flex>
  )
}
