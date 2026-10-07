import { CheckOutlined, CloseOutlined, MessageOutlined, RollbackOutlined, StarOutlined } from '@ant-design/icons'
import { Button, Card, Flex, Popconfirm, Space, Tag, Typography } from 'antd'
import { Link, useNavigate } from 'react-router-dom'
import { EnumTag } from '../../../components/shared/EnumTag'
import { RatingBadge } from '../../../components/shared/RatingBadge'
import { UserAvatar } from '../../../components/shared/UserAvatar'
import type { ConnectionRequestDto } from '../../../types'
import { formatDate, formatDateTime } from '../../../utils/datetime'
import { resolveFileUrl } from '../../../utils/format'
import { POST_STATUS_LABELS, REQUEST_STATUS_LABELS } from '../../../utils/labels'

export type ConnectionDirection = 'incoming' | 'outgoing'

interface ConnectionCardProps {
  request: ConnectionRequestDto
  direction: ConnectionDirection
  busy: boolean
  onAccept: (id: number) => void
  onReject: (id: number) => void
  onCancel: (id: number) => void
  onReview: (request: ConnectionRequestDto) => void
}

/** Phần đánh giá: nút Đánh giá / "Có thể đánh giá từ ..." / "Đã đánh giá". */
function ReviewState({ request, onReview }: { request: ConnectionRequestDto; onReview: () => void }) {
  if (request.hasReviewed) return <Tag color="green">Đã đánh giá</Tag>
  if (request.canReview) {
    return (
      <Button size="small" icon={<StarOutlined />} onClick={onReview}>
        Đánh giá
      </Button>
    )
  }
  if (request.reviewAvailableAt) {
    return (
      <Typography.Text type="secondary" style={{ fontSize: 13 }}>
        Có thể đánh giá từ {formatDate(request.reviewAvailableAt)}
      </Typography.Text>
    )
  }
  return null
}

export function ConnectionCard({ request, direction, busy, onAccept, onReject, onCancel, onReview }: ConnectionCardProps) {
  const navigate = useNavigate()
  const isIncoming = direction === 'incoming'
  const other = isIncoming
    ? { id: request.senderId, name: request.senderName, avatar: request.senderAvatarUrl }
    : { id: request.receiverId, name: request.receiverName, avatar: request.receiverAvatarUrl }
  const thumb = resolveFileUrl(request.postThumbnailUrl)

  return (
    <Card size="small" style={{ marginBottom: 12 }}>
      <Flex gap={12} wrap="wrap">
        <Link to={`/posts/${request.postId}`} style={{ flex: '0 0 auto' }}>
          <div style={{ width: 96, height: 72, borderRadius: 6, overflow: 'hidden', background: '#f0f2f5' }}>
            {thumb && <img src={thumb} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />}
          </div>
        </Link>
        <div style={{ flex: '1 1 220px', minWidth: 0 }}>
          <Space size={[6, 6]} wrap>
            <EnumTag map={REQUEST_STATUS_LABELS} value={request.status} />
            {request.postStatus !== 'approved' && <EnumTag map={POST_STATUS_LABELS} value={request.postStatus} />}
          </Space>
          <div>
            <Link to={`/posts/${request.postId}`}>
              <Typography.Text strong ellipsis style={{ maxWidth: '100%' }}>
                {request.postTitle}
              </Typography.Text>
            </Link>
          </div>
          <Flex align="center" gap={8} wrap="wrap" style={{ marginTop: 4 }}>
            <Typography.Text type="secondary">{isIncoming ? 'Người gửi:' : 'Chủ tin:'}</Typography.Text>
            <UserAvatar url={other.avatar} name={other.name} size="small" />
            <Link to={`/users/${other.id}`}>{other.name}</Link>
            {isIncoming && <RatingBadge rating={request.senderRating} reviewCount={request.senderReviewCount} />}
          </Flex>
          {request.message && (
            <Typography.Paragraph
              italic
              ellipsis={{ rows: 3, expandable: true, symbol: 'Xem thêm' }}
              style={{ margin: '6px 0 0', background: '#fafafa', padding: '6px 10px', borderRadius: 6 }}
            >
              “{request.message}”
            </Typography.Paragraph>
          )}
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Gửi lúc {formatDateTime(request.createdAt)}
            {request.respondedAt && ` · Phản hồi lúc ${formatDateTime(request.respondedAt)}`}
          </Typography.Text>
        </div>
      </Flex>

      <Flex gap={8} wrap="wrap" justify="end" align="center" style={{ marginTop: 12 }}>
        {request.status === 'pending' && isIncoming && (
          <>
            <Popconfirm
              title="Từ chối yêu cầu này?"
              okText="Từ chối"
              cancelText="Hủy"
              okButtonProps={{ danger: true }}
              onConfirm={() => onReject(request.requestId)}
            >
              <Button size="small" danger icon={<CloseOutlined />} disabled={busy}>
                Từ chối
              </Button>
            </Popconfirm>
            <Popconfirm
              title="Chấp nhận yêu cầu?"
              description="Hai bạn sẽ có thể nhắn tin và thấy số điện thoại của nhau."
              okText="Chấp nhận"
              cancelText="Hủy"
              onConfirm={() => onAccept(request.requestId)}
            >
              <Button size="small" type="primary" icon={<CheckOutlined />} disabled={busy}>
                Chấp nhận
              </Button>
            </Popconfirm>
          </>
        )}
        {request.status === 'pending' && !isIncoming && (
          <Popconfirm title="Rút lại yêu cầu này?" okText="Rút lại" cancelText="Hủy" onConfirm={() => onCancel(request.requestId)}>
            <Button size="small" icon={<RollbackOutlined />} disabled={busy}>
              Rút lại
            </Button>
          </Popconfirm>
        )}
        {request.status === 'accepted' && (
          <>
            <ReviewState request={request} onReview={() => onReview(request)} />
            {request.conversationId !== null && (
              <Button
                size="small"
                type="primary"
                icon={<MessageOutlined />}
                onClick={() => navigate(`/chat/${request.conversationId}`)}
              >
                Nhắn tin
              </Button>
            )}
          </>
        )}
      </Flex>
    </Card>
  )
}
