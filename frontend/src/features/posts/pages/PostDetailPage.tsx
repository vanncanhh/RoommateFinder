import {
  CheckOutlined,
  DeleteOutlined,
  EditOutlined,
  EnvironmentOutlined,
  EyeOutlined,
  FieldTimeOutlined,
  FlagOutlined,
  HeartFilled,
  HeartOutlined,
  SendOutlined,
  StopOutlined,
} from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { Alert, Button, Card, Col, Descriptions, Flex, Popconfirm, Row, Space, Tag, Typography } from 'antd'
import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { EnumTag } from '../../../components/shared/EnumTag'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { useAuth } from '../../../hooks/useAuth'
import { useDocumentTitle } from '../../../hooks/useDocumentTitle'
import type { PostDetailDto } from '../../../types'
import { formatDate, formatDateTime } from '../../../utils/datetime'
import { formatMoney } from '../../../utils/format'
import {
  POST_STATUS_LABELS,
  POST_TYPE_LABELS,
  PREFERRED_GENDER_LABELS,
  REQUEST_STATUS_LABELS,
  labelOf,
} from '../../../utils/labels'
import { toNumber } from '../../../utils/query'
import { ReportModal } from '../../reports/components/ReportModal'
import { ConnectRequestModal } from '../components/ConnectRequestModal'
import { ImageGallery } from '../components/ImageGallery'
import { OwnerCard } from '../components/OwnerCard'
import { usePostOwnerActions, useSavePost } from '../hooks/usePostOwnerActions'
import { canClosePost, canEditPost, canExtendPost, isPostPublic } from '../postRules'

/** Nút thao tác cho chủ tin: Sửa / Gia hạn / Đóng / Xóa. */
function OwnerActions({ post }: { post: PostDetailDto }) {
  const navigate = useNavigate()
  const { extend, close, remove } = usePostOwnerActions(() => navigate('/my-posts', { replace: true }))
  const canExtend = canExtendPost(post)
  const canClose = canClosePost(post)
  const canEdit = canEditPost(post)

  return (
    <Card size="small" title="Quản lý tin của bạn">
      <Space direction="vertical" style={{ width: '100%' }}>
        <div>
          Trạng thái: <EnumTag map={POST_STATUS_LABELS} value={post.status} />
        </div>
        {post.status === 'rejected' && post.rejectReason && (
          <Alert type="error" showIcon message="Lý do từ chối" description={post.rejectReason} />
        )}
        {post.status === 'hidden' && (
          <Alert type="warning" showIcon message="Tin đã bị ẩn do vi phạm hoặc bị báo cáo nhiều lần." />
        )}
        {post.expiredAt && (
          <Typography.Text type="secondary">
            Hết hạn: {formatDate(post.expiredAt)} · Đã gia hạn {post.extendCount} lần
          </Typography.Text>
        )}
        <Flex gap={8} wrap="wrap">
          {canEdit && (
            <Button icon={<EditOutlined />} onClick={() => navigate(`/posts/${post.postId}/edit`)}>
              Sửa
            </Button>
          )}
          {canExtend && (
            <Popconfirm title="Gia hạn tin đăng này?" okText="Gia hạn" cancelText="Hủy" onConfirm={() => extend.mutate(post.postId)}>
              <Button icon={<FieldTimeOutlined />} loading={extend.isPending}>
                Gia hạn
              </Button>
            </Popconfirm>
          )}
          {canClose && (
            <Popconfirm
              title="Đóng tin đăng?"
              description="Tin sẽ không còn hiển thị trong kết quả tìm kiếm."
              okText="Đóng tin"
              cancelText="Hủy"
              onConfirm={() => close.mutate(post.postId)}
            >
              <Button icon={<StopOutlined />} loading={close.isPending}>
                Đóng
              </Button>
            </Popconfirm>
          )}
          <Popconfirm
            title="Xóa tin đăng?"
            description="Thao tác này không thể hoàn tác."
            okText="Xóa"
            okButtonProps={{ danger: true }}
            cancelText="Hủy"
            onConfirm={() => remove.mutate(post.postId)}
          >
            <Button danger icon={<DeleteOutlined />} loading={remove.isPending}>
              Xóa
            </Button>
          </Popconfirm>
        </Flex>
      </Space>
    </Card>
  )
}

/** Nút thao tác cho người xem khác: gửi yêu cầu, lưu tin, báo cáo. */
function ViewerActions({ post }: { post: PostDetailDto }) {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const savePost = useSavePost()
  const [connectOpen, setConnectOpen] = useState(false)
  const [reportOpen, setReportOpen] = useState(false)

  if (!isAuthenticated) {
    return (
      <Card size="small">
        <Space direction="vertical" style={{ width: '100%' }}>
          <Typography.Text>Đăng nhập để gửi yêu cầu kết nối, lưu tin hoặc báo cáo.</Typography.Text>
          <Button
            type="primary"
            block
            onClick={() => navigate(`/login?returnUrl=${encodeURIComponent(location.pathname)}`)}
          >
            Đăng nhập
          </Button>
        </Space>
      </Card>
    )
  }

  return (
    <Card size="small">
      <Space direction="vertical" style={{ width: '100%' }}>
        {post.myRequestStatus && (
          <Alert
            type={post.myRequestStatus === 'accepted' ? 'success' : post.myRequestStatus === 'pending' ? 'info' : 'warning'}
            showIcon
            message={
              <span>
                Yêu cầu của bạn: <EnumTag map={REQUEST_STATUS_LABELS} value={post.myRequestStatus} />
                <Link to="/connections?tab=outgoing">Xem kết nối</Link>
              </span>
            }
          />
        )}
        {post.canSendRequest && (
          <Button type="primary" block size="large" icon={<SendOutlined />} onClick={() => setConnectOpen(true)}>
            Gửi yêu cầu kết nối
          </Button>
        )}
        <Flex gap={8}>
          {/* Chỉ lưu được tin công khai; tin đã lưu vẫn cho phép bỏ lưu. */}
          {(post.isSaved || isPostPublic(post)) && (
            <Button
              block
              icon={post.isSaved ? <HeartFilled style={{ color: '#eb2f96' }} /> : <HeartOutlined />}
              loading={savePost.isPending}
              onClick={() => savePost.mutate({ postId: post.postId, save: !post.isSaved })}
            >
              {post.isSaved ? 'Bỏ lưu' : 'Lưu tin'}
            </Button>
          )}
          <Button block danger icon={<FlagOutlined />} onClick={() => setReportOpen(true)}>
            Báo cáo tin
          </Button>
        </Flex>
      </Space>
      <ConnectRequestModal
        open={connectOpen}
        onClose={() => setConnectOpen(false)}
        postId={post.postId}
        postTitle={post.title}
      />
      <ReportModal
        open={reportOpen}
        onClose={() => setReportOpen(false)}
        target={{ postId: post.postId }}
        targetName={post.title}
      />
    </Card>
  )
}

export default function PostDetailPage() {
  const { id } = useParams()
  const postId = toNumber(id) ?? 0
  const query = useQuery({
    queryKey: queryKeys.posts.detail(postId),
    queryFn: () => postsApi.get(postId),
    enabled: postId > 0,
  })
  const post = query.data
  useDocumentTitle(post?.title ?? 'Chi tiết tin')

  if (postId <= 0) return <ErrorResult error={null} title="Tin đăng không tồn tại" />
  if (query.isLoading) return <LoadingBlock rows={12} />
  if (query.isError || !post) return <ErrorResult error={query.error} onRetry={() => void query.refetch()} />

  const location = [post.address, post.areaName, post.district, post.city].filter(Boolean).join(', ')
  const hasCoords = post.latitude !== null && post.longitude !== null

  return (
    <Row gutter={[16, 16]}>
      <Col xs={24} lg={16}>
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <ImageGallery images={post.images} title={post.title} />
          <Card>
            <Space size={[8, 8]} wrap style={{ marginBottom: 8 }}>
              <EnumTag map={POST_TYPE_LABELS} value={post.postType} />
              {post.status !== 'approved' && <EnumTag map={POST_STATUS_LABELS} value={post.status} />}
            </Space>
            <Typography.Title level={3} style={{ marginTop: 0 }}>
              {post.title}
            </Typography.Title>
            <Typography.Title level={3} style={{ color: '#cf1322', margin: '0 0 8px' }}>
              {formatMoney(post.price)}
              <Typography.Text type="secondary" style={{ fontSize: 16, fontWeight: 400 }}>
                {' '}
                /người/tháng
              </Typography.Text>
            </Typography.Title>
            <Space direction="vertical" size={4}>
              <Typography.Text>
                <EnvironmentOutlined /> {location}
                {hasCoords && (
                  <>
                    {' · '}
                    <a
                      href={`https://www.openstreetmap.org/?mlat=${post.latitude}&mlon=${post.longitude}#map=17/${post.latitude}/${post.longitude}`}
                      target="_blank"
                      rel="noreferrer"
                    >
                      Xem bản đồ
                    </a>
                  </>
                )}
              </Typography.Text>
              <Typography.Text type="secondary">
                Đăng lúc {formatDateTime(post.createdAt)} · <EyeOutlined /> {post.viewCount} lượt xem
              </Typography.Text>
            </Space>
          </Card>

          <Card title="Thông tin chi tiết">
            <Descriptions column={{ xs: 1, sm: 2 }} size="small">
              <Descriptions.Item label={post.postType === 'has_room' ? 'Số người đang ở' : 'Số người trong nhóm'}>
                {post.currentOccupants}
              </Descriptions.Item>
              <Descriptions.Item label="Cần thêm">{post.neededOccupants} người</Descriptions.Item>
              <Descriptions.Item label="Giới tính mong muốn">
                {labelOf(PREFERRED_GENDER_LABELS, post.preferredGender, 'Không yêu cầu')}
              </Descriptions.Item>
              <Descriptions.Item label="Khu vực">{post.areaName}</Descriptions.Item>
              {post.expiredAt && <Descriptions.Item label="Hiển thị đến">{formatDate(post.expiredAt)}</Descriptions.Item>}
              {post.updatedAt && <Descriptions.Item label="Cập nhật">{formatDateTime(post.updatedAt)}</Descriptions.Item>}
            </Descriptions>
            {post.description && (
              <>
                <Typography.Title level={5}>Mô tả</Typography.Title>
                <Typography.Paragraph style={{ whiteSpace: 'pre-line' }}>{post.description}</Typography.Paragraph>
              </>
            )}
          </Card>

          <Card title="Tiện ích">
            {post.amenities.length === 0 ? (
              <Typography.Text type="secondary">Chưa cập nhật tiện ích</Typography.Text>
            ) : (
              <Space size={[8, 8]} wrap>
                {post.amenities.map((a) => (
                  <Tag key={a.amenityId} icon={<CheckOutlined />} color="green">
                    {a.name}
                  </Tag>
                ))}
              </Space>
            )}
          </Card>
        </Space>
      </Col>

      <Col xs={24} lg={8}>
        <Space direction="vertical" size={16} style={{ width: '100%', position: 'sticky', top: 80 }}>
          {post.isOwner ? <OwnerActions post={post} /> : <ViewerActions post={post} />}
          <OwnerCard post={post} />
        </Space>
      </Col>
    </Row>
  )
}
