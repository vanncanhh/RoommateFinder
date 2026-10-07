import { useQuery } from '@tanstack/react-query'
import { Alert, Descriptions, Drawer, Grid, Space, Tag, Typography } from 'antd'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { EnumTag } from '../../../components/shared/EnumTag'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { RatingBadge } from '../../../components/shared/RatingBadge'
import { formatDateTime } from '../../../utils/datetime'
import { formatMoney } from '../../../utils/format'
import { POST_STATUS_LABELS, POST_TYPE_LABELS, PREFERRED_GENDER_LABELS, labelOf } from '../../../utils/labels'
import { ImageGallery } from '../../posts/components/ImageGallery'

interface PostReviewDrawerProps {
  postId: number | null
  onClose: () => void
  /** Nút duyệt / từ chối hiển thị ở chân drawer. */
  footer?: ReactNode
}

/** Xem nhanh chi tiết tin (GET /api/posts/{id}) khi kiểm duyệt. */
export function PostReviewDrawer({ postId, onClose, footer }: PostReviewDrawerProps) {
  const screens = Grid.useBreakpoint()
  const query = useQuery({
    queryKey: queryKeys.posts.detail(postId ?? 0),
    queryFn: () => postsApi.get(postId ?? 0),
    enabled: postId !== null,
  })
  const post = query.data

  return (
    <Drawer
      open={postId !== null}
      onClose={onClose}
      width={screens.md ? 720 : '100%'}
      title={post ? post.title : 'Chi tiết tin'}
      footer={footer}
      destroyOnHidden
    >
      {query.isLoading ? (
        <LoadingBlock rows={10} />
      ) : query.isError || !post ? (
        <ErrorResult error={query.error} />
      ) : (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <ImageGallery images={post.images} title={post.title} />
          <Space wrap>
            <EnumTag map={POST_TYPE_LABELS} value={post.postType} />
            <EnumTag map={POST_STATUS_LABELS} value={post.status} />
            <Link to={`/posts/${post.postId}`} target="_blank">
              Mở trang tin ↗
            </Link>
          </Space>
          {post.rejectReason && <Alert type="error" showIcon message={`Lý do từ chối trước: ${post.rejectReason}`} />}
          <Descriptions column={1} size="small" bordered>
            <Descriptions.Item label="Giá / người / tháng">{formatMoney(post.price)}</Descriptions.Item>
            <Descriptions.Item label="Khu vực">
              {[post.address, post.areaName, post.district, post.city].filter(Boolean).join(', ')}
            </Descriptions.Item>
            <Descriptions.Item label="Số người">
              Hiện có {post.currentOccupants} · Cần thêm {post.neededOccupants}
            </Descriptions.Item>
            <Descriptions.Item label="Giới tính mong muốn">
              {labelOf(PREFERRED_GENDER_LABELS, post.preferredGender, 'Không yêu cầu')}
            </Descriptions.Item>
            <Descriptions.Item label="Người đăng">
              <Space wrap>
                <Link to={`/users/${post.ownerId}`} target="_blank">
                  {post.ownerName}
                </Link>
                <RatingBadge rating={post.ownerRating} reviewCount={post.ownerReviewCount} />
                {post.ownerPhone && <Typography.Text copyable>{post.ownerPhone}</Typography.Text>}
              </Space>
            </Descriptions.Item>
            <Descriptions.Item label="Ngày đăng">{formatDateTime(post.createdAt)}</Descriptions.Item>
            <Descriptions.Item label="Tiện ích">
              <Space size={[4, 4]} wrap>
                {post.amenities.length === 0 ? '—' : post.amenities.map((a) => <Tag key={a.amenityId}>{a.name}</Tag>)}
              </Space>
            </Descriptions.Item>
          </Descriptions>
          <div>
            <Typography.Title level={5}>Mô tả</Typography.Title>
            <Typography.Paragraph style={{ whiteSpace: 'pre-line' }}>{post.description || '—'}</Typography.Paragraph>
          </div>
        </Space>
      )}
    </Drawer>
  )
}
