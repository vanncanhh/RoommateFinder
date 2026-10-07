import { MessageOutlined } from '@ant-design/icons'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Card, Col, Empty, Grid, Row } from 'antd'
import { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { conversationsApi } from '../../../api/conversations'
import { queryKeys } from '../../../api/queryKeys'
import { ErrorResult } from '../../../components/shared/QueryState'
import { useAuth } from '../../../hooks/useAuth'
import { useDocumentTitle } from '../../../hooks/useDocumentTitle'
import { useSignalR } from '../../../hooks/useSignalR'
import { toNumber } from '../../../utils/query'
import { ConversationList } from '../components/ConversationList'
import { MessagePane } from '../components/MessagePane'

const FALLBACK_POLL_MS = 10_000

/**
 * Trang tin nhắn: danh sách hội thoại + cửa sổ chat realtime qua /hubs/chat.
 * Kết nối SignalR mở khi vào trang, tự kết nối lại, và đóng khi rời trang (useSignalR cleanup).
 */
export default function ChatPage() {
  useDocumentTitle('Tin nhắn')
  const { conversationId: idParam } = useParams()
  const conversationId = toNumber(idParam)
  const { user, token } = useAuth()
  const queryClient = useQueryClient()
  const screens = Grid.useBreakpoint()
  const isWide = Boolean(screens.md)
  const hub = useSignalR('/hubs/chat', token)
  const { connection, isConnected, connectedVersion } = hub

  const conversations = useQuery({
    queryKey: queryKeys.conversations.list,
    queryFn: conversationsApi.list,
    refetchInterval: isConnected ? false : FALLBACK_POLL_MS,
  })

  // Tin nhắn mới ở bất kỳ hội thoại nào → cập nhật danh sách (tin cuối, số chưa đọc).
  useEffect(() => {
    if (!connection) return undefined
    const onMessage = () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.conversations.list })
    }
    connection.on('message', onMessage)
    return () => connection.off('message', onMessage)
  }, [connection, queryClient])

  // Kết nối lại thành công → tải lại để bù các tin nhắn bị lỡ trong lúc mất kết nối (gộp & loại trùng theo messageId).
  // (version 1 = lần kết nối đầu, dữ liệu vừa được tải nên bỏ qua.)
  useEffect(() => {
    if (connectedVersion <= 1) return
    void queryClient.invalidateQueries({ queryKey: queryKeys.conversations.all })
  }, [connectedVersion, queryClient])

  if (!user) return null
  if (conversations.isError && !conversations.data) {
    return <ErrorResult error={conversations.error} onRetry={() => void conversations.refetch()} />
  }

  const list = conversations.data ?? []
  const active = list.find((c) => c.conversationId === conversationId)
  const showList = isWide || conversationId === undefined
  const showPane = isWide || conversationId !== undefined
  const height = 'calc(100vh - 64px - 32px - 56px)'

  return (
    <Card styles={{ body: { padding: 0 } }}>
      <Row style={{ height, minHeight: 420 }}>
        {showList && (
          <Col
            xs={24}
            md={9}
            lg={8}
            style={{ height: '100%', overflowY: 'auto', borderRight: isWide ? '1px solid #f0f0f0' : undefined, padding: 8 }}
          >
            <ConversationList
              conversations={list}
              activeId={conversationId}
              currentUserId={user.userId}
              loading={conversations.isLoading}
            />
          </Col>
        )}
        {showPane && (
          <Col xs={24} md={15} lg={16} style={{ height: '100%' }}>
            {conversationId !== undefined ? (
              <MessagePane
                key={conversationId}
                conversationId={conversationId}
                conversation={active}
                currentUserId={user.userId}
                hub={hub}
                showBack={!isWide}
              />
            ) : (
              <Empty
                image={<MessageOutlined style={{ fontSize: 64, color: '#bfbfbf' }} />}
                description="Chọn một cuộc trò chuyện để bắt đầu"
                style={{ marginTop: 120 }}
              />
            )}
          </Col>
        )}
      </Row>
    </Card>
  )
}
