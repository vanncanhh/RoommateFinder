import { useQuery } from '@tanstack/react-query'
import { List, Rate, Space, Typography } from 'antd'
import { useState } from 'react'
import { queryKeys } from '../../../api/queryKeys'
import { usersApi } from '../../../api/users'
import { ErrorResult } from '../../../components/shared/QueryState'
import { UserAvatar } from '../../../components/shared/UserAvatar'
import { formatDate } from '../../../utils/datetime'

const PAGE_SIZE = 5

/** Danh sách đánh giá của một người dùng, phân trang. */
export function ReviewList({ userId }: { userId: number }) {
  const [page, setPage] = useState(1)
  const query = useQuery({
    queryKey: queryKeys.users.reviews(userId, page),
    queryFn: () => usersApi.getReviews(userId, { page, pageSize: PAGE_SIZE }),
  })

  if (query.isError) return <ErrorResult error={query.error} onRetry={() => void query.refetch()} />

  return (
    <List
      loading={query.isLoading}
      dataSource={query.data?.items ?? []}
      locale={{ emptyText: 'Chưa có đánh giá nào' }}
      pagination={
        query.data && query.data.totalCount > PAGE_SIZE
          ? { current: page, pageSize: PAGE_SIZE, total: query.data.totalCount, onChange: setPage, size: 'small' }
          : false
      }
      renderItem={(r) => (
        <List.Item key={r.reviewId}>
          <List.Item.Meta
            avatar={<UserAvatar url={r.reviewerAvatarUrl} name={r.reviewerName} />}
            title={
              <Space wrap size={8}>
                <Typography.Text strong>{r.reviewerName}</Typography.Text>
                <Rate disabled value={r.rating} style={{ fontSize: 14 }} />
              </Space>
            }
            description={
              <>
                {r.comment && (
                  <Typography.Paragraph style={{ marginBottom: 4, color: 'rgba(0,0,0,0.85)' }}>{r.comment}</Typography.Paragraph>
                )}
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {formatDate(r.createdAt)}
                  {r.postTitle && ` · Tin: ${r.postTitle}`}
                </Typography.Text>
              </>
            }
          />
        </List.Item>
      )}
    />
  )
}
