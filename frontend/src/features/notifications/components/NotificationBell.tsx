import { BellOutlined } from '@ant-design/icons'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Badge, Button, Flex, List, Popover, Spin, Typography } from 'antd'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { notificationsApi } from '../../../api/notifications'
import { queryKeys } from '../../../api/queryKeys'
import { useAuth } from '../../../hooks/useAuth'
import { useSignalR } from '../../../hooks/useSignalR'
import type { NotificationDto } from '../../../types'
import { fromNow } from '../../../utils/datetime'
import { feedback } from '../../../utils/feedback'
import { NOTIFICATION_TYPE_LABELS, labelOf } from '../../../utils/labels'

const PAGE_SIZE = 10

/** Chuông thông báo: số chưa đọc, danh sách thả xuống, realtime qua /hubs/notifications. */
export function NotificationBell() {
  const { token, refreshUser } = useAuth()
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const { connection, isConnected } = useSignalR('/hubs/notifications', token)

  const unreadQuery = useQuery({
    queryKey: queryKeys.notifications.unreadCount,
    queryFn: notificationsApi.unreadCount,
    // Dự phòng khi mất kết nối realtime: tải lại định kỳ.
    refetchInterval: isConnected ? false : 30_000,
  })

  const listQuery = useQuery({
    queryKey: queryKeys.notifications.list(1, PAGE_SIZE),
    queryFn: () => notificationsApi.list({ page: 1, pageSize: PAGE_SIZE }),
    enabled: open,
  })

  useEffect(() => {
    if (!connection) return undefined
    const handler = (n: NotificationDto) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all })
      feedback.notification.info({
        message: labelOf(NOTIFICATION_TYPE_LABELS, n.type, 'Thông báo mới'),
        description: n.content,
        placement: 'bottomRight',
        onClick: n.link ? () => navigate(n.link ?? '/') : undefined,
        style: n.link ? { cursor: 'pointer' } : undefined,
      })
      // Dữ liệu liên quan có thể đã thay đổi → làm mới.
      if (n.type.startsWith('request_')) void queryClient.invalidateQueries({ queryKey: queryKeys.connections.all })
      if (n.type.startsWith('post_')) void queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })
      if (n.type === 'role_changed') void refreshUser()
    }
    connection.on('notification', handler)
    return () => connection.off('notification', handler)
  }, [connection, queryClient, navigate, refreshUser])

  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all })

  const markRead = useMutation({ mutationFn: notificationsApi.markRead, onSuccess: invalidate })
  const markAllRead = useMutation({ mutationFn: notificationsApi.markAllRead, onSuccess: invalidate })

  const handleClick = (n: NotificationDto) => {
    if (!n.isRead) markRead.mutate(n.notificationId)
    setOpen(false)
    if (n.link) navigate(n.link)
  }

  const unread = unreadQuery.data ?? 0
  const items = listQuery.data?.items ?? []

  const content = (
    <div style={{ width: 'min(360px, calc(100vw - 32px))' }}>
      <Flex justify="space-between" align="center" style={{ marginBottom: 8 }}>
        <Typography.Text strong>Thông báo</Typography.Text>
        <Button
          type="link"
          size="small"
          disabled={unread === 0}
          loading={markAllRead.isPending}
          onClick={() => markAllRead.mutate()}
        >
          Đánh dấu tất cả đã đọc
        </Button>
      </Flex>
      {listQuery.isLoading ? (
        <Flex justify="center" style={{ padding: 24 }}>
          <Spin />
        </Flex>
      ) : (
        <List
          dataSource={items}
          locale={{ emptyText: 'Chưa có thông báo' }}
          style={{ maxHeight: 400, overflowY: 'auto' }}
          renderItem={(n) => (
            <List.Item
              key={n.notificationId}
              onClick={() => handleClick(n)}
              style={{
                cursor: 'pointer',
                padding: '8px',
                borderRadius: 6,
                background: n.isRead ? undefined : '#e6f4ff',
                marginBottom: 2,
              }}
            >
              <List.Item.Meta
                title={
                  <Flex justify="space-between" gap={8}>
                    <Typography.Text strong={!n.isRead} style={{ fontSize: 13 }}>
                      {labelOf(NOTIFICATION_TYPE_LABELS, n.type, 'Thông báo')}
                    </Typography.Text>
                    <Typography.Text type="secondary" style={{ fontSize: 12, whiteSpace: 'nowrap' }}>
                      {fromNow(n.createdAt)}
                    </Typography.Text>
                  </Flex>
                }
                description={<span style={{ color: 'rgba(0,0,0,0.75)' }}>{n.content}</span>}
              />
            </List.Item>
          )}
        />
      )}
    </div>
  )

  return (
    <Popover content={content} trigger="click" open={open} onOpenChange={setOpen} placement="bottomRight" arrow={false}>
      <Badge count={unread} size="small" overflowCount={99}>
        <Button type="text" shape="circle" aria-label="Thông báo" icon={<BellOutlined style={{ fontSize: 18 }} />} />
      </Badge>
    </Popover>
  )
}
