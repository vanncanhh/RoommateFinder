import { StarFilled } from '@ant-design/icons'
import { Space, Typography } from 'antd'
import { formatRating } from '../../utils/format'

interface RatingBadgeProps {
  rating: number
  reviewCount?: number
}

/** ★ 4,5 (12 đánh giá) */
export function RatingBadge({ rating, reviewCount }: RatingBadgeProps) {
  const hasReviews = reviewCount === undefined ? rating > 0 : reviewCount > 0
  return (
    <Space size={4} style={{ whiteSpace: 'nowrap' }}>
      <StarFilled style={{ color: hasReviews ? '#faad14' : '#d9d9d9' }} />
      <Typography.Text>{hasReviews ? formatRating(rating) : 'Chưa có'}</Typography.Text>
      {reviewCount !== undefined && reviewCount > 0 && (
        <Typography.Text type="secondary">({reviewCount} đánh giá)</Typography.Text>
      )}
    </Space>
  )
}
