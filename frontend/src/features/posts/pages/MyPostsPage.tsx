import { DeleteOutlined, EditOutlined, FieldTimeOutlined, PlusOutlined, StopOutlined, TeamOutlined } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { Alert, Badge, Button, Card, Flex, List, Popconfirm, Space, Tabs, Typography } from 'antd'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { EnumTag } from '../../../components/shared/EnumTag'
import { PageTitle } from '../../../components/shared/PageTitle'
import { EmptyBlock, ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { POST_STATUSES, type PostStatus, type PostSummaryDto } from '../../../types'
import { formatDate } from '../../../utils/datetime'
import { formatMoney, resolveFileUrl } from '../../../utils/format'
import { POST_STATUS_LABELS, POST_TYPE_SHORT_LABELS } from '../../../utils/labels'
import { oneOf } from '../../../utils/query'
import { usePostOwnerActions } from '../hooks/usePostOwnerActions'
import { canClosePost, canEditPost, canExtendPost } from '../postRules'

function MyPostItem({ post }: { post: PostSummaryDto }) {
  const navigate = useNavigate()
  const { extend, close, remove } = usePostOwnerActions()
  const canExtend = canExtendPost(post)
  const canClose = canClosePost(post)
  const canEdit = canEditPost(post)
  const thumb = resolveFileUrl(post.thumbnailUrl)

  return (
    <Card size="small" style={{ marginBottom: 12 }}>
      <Flex gap={12} wrap="wrap">
        <Link to={`/posts/${post.postId}`} style={{ flex: '0 0 auto' }}>
          <div style={{ width: 120, height: 90, borderRadius: 6, overflow: 'hidden', background: '#f0f2f5' }}>
            {thumb && <img src={thumb} alt="" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />}
          </div>
        </Link>
        <div style={{ flex: '1 1 220px', minWidth: 0 }}>
          <Space size={[6, 6]} wrap>
            <EnumTag map={POST_STATUS_LABELS} value={post.status} />
            <Typography.Text type="secondary">{POST_TYPE_SHORT_LABELS[post.postType]}</Typography.Text>
          </Space>
          <div>
            <Link to={`/posts/${post.postId}`}>
              <Typography.Text strong>{post.title}</Typography.Text>
            </Link>
          </div>
          <Typography.Text style={{ color: '#cf1322' }}>{formatMoney(post.price)}</Typography.Text>
          <Typography.Text type="secondary"> · {post.areaName}</Typography.Text>
          <div>
            <Typography.Text type="secondary" style={{ fontSize: 13 }}>
              Đăng {formatDate(post.createdAt)}
              {post.expiredAt && ` · Hết hạn ${formatDate(post.expiredAt)}`} · Gia hạn {post.extendCount} lần · {post.viewCount}{' '}
              lượt xem
            </Typography.Text>
          </div>
          {post.pendingRequestCount > 0 && (
            <Link to="/connections">
              <Badge status="processing" text={`${post.pendingRequestCount} yêu cầu kết nối đang chờ`} />
            </Link>
          )}
          {post.status === 'rejected' && post.rejectReason && (
            <Alert type="error" showIcon message={`Lý do từ chối: ${post.rejectReason}`} style={{ marginTop: 8 }} />
          )}
        </div>
      </Flex>
      <Flex gap={8} wrap="wrap" justify="end" style={{ marginTop: 12 }}>
        {post.pendingRequestCount > 0 && (
          <Button size="small" icon={<TeamOutlined />} onClick={() => navigate('/connections')}>
            Xem yêu cầu
          </Button>
        )}
        {canEdit && (
          <Button size="small" icon={<EditOutlined />} onClick={() => navigate(`/posts/${post.postId}/edit`)}>
            Sửa
          </Button>
        )}
        {canExtend && (
          <Popconfirm title="Gia hạn tin đăng này?" okText="Gia hạn" cancelText="Hủy" onConfirm={() => extend.mutate(post.postId)}>
            <Button size="small" icon={<FieldTimeOutlined />} loading={extend.isPending}>
              Gia hạn
            </Button>
          </Popconfirm>
        )}
        {canClose && (
          <Popconfirm title="Đóng tin đăng?" okText="Đóng tin" cancelText="Hủy" onConfirm={() => close.mutate(post.postId)}>
            <Button size="small" icon={<StopOutlined />} loading={close.isPending}>
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
          <Button size="small" danger icon={<DeleteOutlined />} loading={remove.isPending}>
            Xóa
          </Button>
        </Popconfirm>
      </Flex>
    </Card>
  )
}

export default function MyPostsPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const status = oneOf(POST_STATUSES, searchParams.get('status'))

  const query = useQuery({ queryKey: queryKeys.posts.mine(status), queryFn: () => postsApi.mine(status) })

  const tabs = [
    { key: 'all', label: 'Tất cả' },
    ...POST_STATUSES.map((s) => ({ key: s, label: POST_STATUS_LABELS[s].label })),
  ]

  return (
    <>
      <PageTitle
        title="Tin của tôi"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/posts/new')}>
            Đăng tin mới
          </Button>
        }
      />
      <Tabs
        activeKey={status ?? 'all'}
        items={tabs}
        onChange={(key) => setSearchParams(key === 'all' ? {} : { status: key as PostStatus })}
      />
      {query.isLoading ? (
        <LoadingBlock />
      ) : query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (query.data ?? []).length === 0 ? (
        <EmptyBlock description="Không có tin đăng nào">
          <Button type="primary" onClick={() => navigate('/posts/new')}>
            Đăng tin ngay
          </Button>
        </EmptyBlock>
      ) : (
        <List dataSource={query.data} renderItem={(post) => <MyPostItem key={post.postId} post={post} />} />
      )}
    </>
  )
}
