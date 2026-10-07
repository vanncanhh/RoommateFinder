import { FlagOutlined } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { Button, Card, Col, Flex, Row, Space, Tabs, Typography } from 'antd'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { queryKeys } from '../../../api/queryKeys'
import { usersApi } from '../../../api/users'
import { PostGrid } from '../../../components/shared/PostGrid'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { RatingBadge } from '../../../components/shared/RatingBadge'
import { UserAvatar } from '../../../components/shared/UserAvatar'
import { useAuth } from '../../../hooks/useAuth'
import { useDocumentTitle } from '../../../hooks/useDocumentTitle'
import { formatDate } from '../../../utils/datetime'
import { toNumber } from '../../../utils/query'
import { ReportModal } from '../../reports/components/ReportModal'
import { LifestyleDescriptions } from '../components/LifestyleDescriptions'
import { ReviewList } from '../components/ReviewList'

function UserPosts({ userId }: { userId: number }) {
  const query = useQuery({ queryKey: queryKeys.users.posts(userId), queryFn: () => usersApi.getPosts(userId) })
  if (query.isLoading) return <LoadingBlock />
  if (query.isError) return <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
  return <PostGrid posts={query.data ?? []} emptyText="Người dùng chưa có tin đang hiển thị" />
}

export default function PublicProfilePage() {
  const { id } = useParams()
  const userId = toNumber(id) ?? 0
  const { user } = useAuth()
  const [reportOpen, setReportOpen] = useState(false)

  const query = useQuery({
    queryKey: queryKeys.users.detail(userId),
    queryFn: () => usersApi.getPublicProfile(userId),
    enabled: userId > 0,
  })
  const profile = query.data
  useDocumentTitle(profile?.fullName ?? 'Hồ sơ người dùng')

  if (userId <= 0) return <ErrorResult error={null} title="Người dùng không tồn tại" />
  if (query.isLoading) return <LoadingBlock rows={10} />
  if (query.isError || !profile) return <ErrorResult error={query.error} onRetry={() => void query.refetch()} />

  const canReport = user !== null && user.userId !== profile.userId

  return (
    <Row gutter={[16, 16]}>
      <Col xs={24} lg={8}>
        <Card>
          <Flex vertical align="center" gap={8} style={{ textAlign: 'center' }}>
            <UserAvatar url={profile.avatarUrl} name={profile.fullName} size={96} />
            <Typography.Title level={4} style={{ margin: 0 }}>
              {profile.fullName}
            </Typography.Title>
            <RatingBadge rating={profile.avgRating} reviewCount={profile.reviewCount} />
            <Typography.Text type="secondary">Tham gia từ {formatDate(profile.createdAt)}</Typography.Text>
            {profile.bio && <Typography.Paragraph style={{ whiteSpace: 'pre-line' }}>{profile.bio}</Typography.Paragraph>}
            {canReport && (
              <Button danger icon={<FlagOutlined />} onClick={() => setReportOpen(true)}>
                Báo cáo người dùng
              </Button>
            )}
          </Flex>
        </Card>
      </Col>
      <Col xs={24} lg={16}>
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Card title="Thông tin sinh hoạt">
            <LifestyleDescriptions info={profile} />
          </Card>
          <Card>
            <Tabs
              items={[
                { key: 'posts', label: 'Tin đang đăng', children: <UserPosts userId={userId} /> },
                { key: 'reviews', label: `Đánh giá (${profile.reviewCount})`, children: <ReviewList userId={userId} /> },
              ]}
            />
          </Card>
        </Space>
      </Col>
      {canReport && (
        <ReportModal
          open={reportOpen}
          onClose={() => setReportOpen(false)}
          target={{ reportedUserId: profile.userId }}
          targetName={profile.fullName}
        />
      )}
    </Row>
  )
}
