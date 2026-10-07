import { PhoneOutlined } from '@ant-design/icons'
import { Card, Flex, Space, Typography } from 'antd'
import { Link } from 'react-router-dom'
import { RatingBadge } from '../../../components/shared/RatingBadge'
import { UserAvatar } from '../../../components/shared/UserAvatar'
import type { PostDetailDto } from '../../../types'
import { GENDER_LABELS, OCCUPATION_LABELS, labelOf } from '../../../utils/labels'

/** Thẻ người đăng: ảnh, tên (link hồ sơ công khai), đánh giá; SĐT chỉ hiện khi API trả về. */
export function OwnerCard({ post }: { post: PostDetailDto }) {
  const meta = [
    post.ownerGender ? labelOf(GENDER_LABELS, post.ownerGender) : null,
    post.ownerOccupation ? labelOf(OCCUPATION_LABELS, post.ownerOccupation) : null,
  ].filter(Boolean)

  return (
    <Card size="small" title="Người đăng">
      <Flex gap={12} align="center">
        <UserAvatar url={post.ownerAvatarUrl} name={post.ownerName} size={56} />
        <Space direction="vertical" size={2} style={{ minWidth: 0 }}>
          <Link to={`/users/${post.ownerId}`}>
            <Typography.Text strong ellipsis style={{ maxWidth: 220 }}>
              {post.ownerName}
            </Typography.Text>
          </Link>
          <RatingBadge rating={post.ownerRating} reviewCount={post.ownerReviewCount} />
          {meta.length > 0 && <Typography.Text type="secondary">{meta.join(' · ')}</Typography.Text>}
        </Space>
      </Flex>
      <div style={{ marginTop: 12 }}>
        {post.ownerPhone ? (
          <Typography.Text copyable={{ text: post.ownerPhone }}>
            <PhoneOutlined /> <a href={`tel:${post.ownerPhone}`}>{post.ownerPhone}</a>
          </Typography.Text>
        ) : (
          <Typography.Text type="secondary" style={{ fontSize: 13 }}>
            Số điện thoại hiển thị sau khi yêu cầu kết nối được chấp nhận.
          </Typography.Text>
        )}
      </div>
    </Card>
  )
}
