import { useQuery } from '@tanstack/react-query'
import { Card, Col, Flex, Row, Tabs, Tag, Typography } from 'antd'
import { Link } from 'react-router-dom'
import { profileApi } from '../../../api/profile'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { RatingBadge } from '../../../components/shared/RatingBadge'
import { UserAvatar } from '../../../components/shared/UserAvatar'
import { formatDate } from '../../../utils/datetime'
import { ROLE_LABELS } from '../../../utils/labels'
import { AvatarUploader } from '../components/AvatarUploader'
import { ChangePasswordForm } from '../components/ChangePasswordForm'
import { ProfileForm } from '../components/ProfileForm'

export default function ProfilePage() {
  const query = useQuery({ queryKey: queryKeys.profile, queryFn: profileApi.get })
  const profile = query.data

  return (
    <>
      <PageTitle title="Hồ sơ của tôi" />
      {query.isLoading ? (
        <LoadingBlock rows={10} />
      ) : query.isError || !profile ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Row gutter={[16, 16]}>
          <Col xs={24} lg={7}>
            <Card>
              <Flex vertical align="center" gap={8} style={{ textAlign: 'center' }}>
                <UserAvatar url={profile.avatarUrl} name={profile.fullName} size={96} />
                <Typography.Title level={4} style={{ margin: 0 }}>
                  {profile.fullName}
                </Typography.Title>
                <Tag color={ROLE_LABELS[profile.role]?.color}>{ROLE_LABELS[profile.role]?.label ?? profile.role}</Tag>
                <RatingBadge rating={profile.avgRating} reviewCount={profile.reviewCount} />
                <Typography.Text type="secondary">Tham gia từ {formatDate(profile.createdAt)}</Typography.Text>
                <AvatarUploader />
                <Link to={`/users/${profile.userId}`}>Xem hồ sơ công khai</Link>
              </Flex>
            </Card>
          </Col>
          <Col xs={24} lg={17}>
            <Card>
              <Tabs
                items={[
                  { key: 'info', label: 'Thông tin cá nhân', children: <ProfileForm profile={profile} /> },
                  { key: 'password', label: 'Đổi mật khẩu', children: <ChangePasswordForm /> },
                ]}
              />
            </Card>
          </Col>
        </Row>
      )}
    </>
  )
}
