import { Badge, Flex, List, Typography } from 'antd'
import { useNavigate } from 'react-router-dom'
import { UserAvatar } from '../../../components/shared/UserAvatar'
import type { ConversationDto } from '../../../types'
import { formatChatTime } from '../../../utils/datetime'

interface ConversationListProps {
  conversations: ConversationDto[]
  activeId?: number
  currentUserId: number
  loading: boolean
}

/** Danh sách hội thoại (mới nhất trước) kèm số tin chưa đọc. */
export function ConversationList({ conversations, activeId, currentUserId, loading }: ConversationListProps) {
  const navigate = useNavigate()
  return (
    <List
      loading={loading}
      dataSource={conversations}
      locale={{ emptyText: 'Chưa có cuộc trò chuyện nào. Hội thoại được tạo khi yêu cầu kết nối được chấp nhận.' }}
      renderItem={(c) => {
        const active = c.conversationId === activeId
        const preview = c.lastMessage
          ? `${c.lastMessageSenderId === currentUserId ? 'Bạn: ' : ''}${c.lastMessage}`
          : 'Chưa có tin nhắn'
        return (
          <List.Item
            key={c.conversationId}
            onClick={() => navigate(`/chat/${c.conversationId}`)}
            style={{
              cursor: 'pointer',
              padding: '10px 12px',
              background: active ? '#e6f4ff' : undefined,
              borderRadius: 8,
            }}
          >
            <Flex gap={10} align="center" style={{ width: '100%', minWidth: 0 }}>
              <Badge count={c.unreadCount} size="small">
                <UserAvatar url={c.otherUserAvatarUrl} name={c.otherUserName} size={40} />
              </Badge>
              <div style={{ flex: 1, minWidth: 0 }}>
                <Flex justify="space-between" gap={8}>
                  <Typography.Text strong ellipsis>
                    {c.otherUserName}
                  </Typography.Text>
                  <Typography.Text type="secondary" style={{ fontSize: 12, whiteSpace: 'nowrap' }}>
                    {formatChatTime(c.lastMessageAt ?? c.createdAt)}
                  </Typography.Text>
                </Flex>
                <Typography.Text type="secondary" ellipsis style={{ fontSize: 12, display: 'block' }}>
                  {c.postTitle}
                </Typography.Text>
                <Typography.Text
                  ellipsis
                  strong={c.unreadCount > 0}
                  type={c.unreadCount > 0 ? undefined : 'secondary'}
                  style={{ display: 'block', fontSize: 13 }}
                >
                  {preview}
                </Typography.Text>
              </div>
            </Flex>
          </List.Item>
        )
      }}
    />
  )
}
