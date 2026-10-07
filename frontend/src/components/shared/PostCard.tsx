import { EnvironmentOutlined, HomeOutlined, TeamOutlined } from '@ant-design/icons'
import { Card, Flex, Space, Tag, Typography } from 'antd'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { PostSummaryDto } from '../../types'
import { fromNow } from '../../utils/datetime'
import { formatDistance, formatMoney, resolveFileUrl } from '../../utils/format'
import { POST_TYPE_LABELS, POST_TYPE_SHORT_LABELS } from '../../utils/labels'
import { RatingBadge } from './RatingBadge'

interface PostCardProps {
  post: PostSummaryDto
  /** Nút thao tác phụ (ví dụ "Bỏ lưu") hiển thị ở chân thẻ. */
  actions?: ReactNode[]
}

function Thumbnail({ post }: { post: PostSummaryDto }) {
  const src = resolveFileUrl(post.thumbnailUrl)
  return (
    <div style={{ position: 'relative', aspectRatio: '4 / 3', background: '#f0f2f5', overflow: 'hidden' }}>
      {src ? (
        <img
          src={src}
          alt={post.title}
          loading="lazy"
          style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block' }}
        />
      ) : (
        <Flex align="center" justify="center" style={{ height: '100%', color: '#bfbfbf', fontSize: 40 }}>
          <HomeOutlined />
        </Flex>
      )}
      <Tag color={POST_TYPE_LABELS[post.postType]?.color} style={{ position: 'absolute', top: 8, left: 8, margin: 0 }}>
        {POST_TYPE_SHORT_LABELS[post.postType] ?? post.postType}
      </Tag>
    </div>
  )
}

/** Thẻ tin đăng dùng ở trang chủ, tìm kiếm, tin đã lưu, hồ sơ công khai. */
export function PostCard({ post, actions }: PostCardProps) {
  const distance = formatDistance(post.distanceKm)
  const location = [post.areaName, post.district, post.city].filter(Boolean).join(', ')
  return (
    <Card
      hoverable
      cover={
        <Link to={`/posts/${post.postId}`}>
          <Thumbnail post={post} />
        </Link>
      }
      styles={{ body: { padding: 12 } }}
      actions={actions}
      style={{ height: '100%', display: 'flex', flexDirection: 'column' }}
    >
      <Link to={`/posts/${post.postId}`}>
        <Typography.Paragraph strong ellipsis={{ rows: 2 }} style={{ marginBottom: 6, minHeight: 44 }}>
          {post.title}
        </Typography.Paragraph>
      </Link>
      <Typography.Text strong style={{ color: '#cf1322', fontSize: 16 }}>
        {formatMoney(post.price)}
      </Typography.Text>
      <Typography.Text type="secondary"> /người/tháng</Typography.Text>
      <Flex vertical gap={4} style={{ marginTop: 8 }}>
        <Typography.Text type="secondary" ellipsis={{ tooltip: location }}>
          <EnvironmentOutlined /> {location}
        </Typography.Text>
        <Space size={12} wrap>
          <Typography.Text type="secondary">
            <TeamOutlined /> Cần {post.neededOccupants} người
          </Typography.Text>
          {distance && <Tag color="blue">Cách {distance}</Tag>}
        </Space>
        <Flex justify="space-between" align="center" gap={8} wrap="wrap">
          <Space size={4}>
            <Typography.Text ellipsis style={{ maxWidth: 140 }}>
              {post.ownerName}
            </Typography.Text>
            <RatingBadge rating={post.ownerRating} />
          </Space>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {fromNow(post.createdAt)}
          </Typography.Text>
        </Flex>
      </Flex>
    </Card>
  )
}
