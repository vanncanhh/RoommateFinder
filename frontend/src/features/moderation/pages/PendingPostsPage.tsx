import { CheckOutlined, CloseOutlined, EyeOutlined } from '@ant-design/icons'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Popconfirm, Select, Space, Table, Typography, type TableColumnsType } from 'antd'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { moderationApi } from '../../../api/moderation'
import { queryKeys } from '../../../api/queryKeys'
import { EnumTag } from '../../../components/shared/EnumTag'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult } from '../../../components/shared/QueryState'
import { POST_STATUSES, type PostStatus, type PostSummaryDto } from '../../../types'
import { formatDateTime } from '../../../utils/datetime'
import { formatMoney } from '../../../utils/format'
import { POST_STATUS_LABELS, POST_TYPE_SHORT_LABELS, toOptions } from '../../../utils/labels'
import { feedback } from '../../../utils/feedback'
import { oneOf, toNumber } from '../../../utils/query'
import { PostReviewDrawer } from '../components/PostReviewDrawer'
import { RejectPostModal } from '../components/RejectPostModal'

const PAGE_SIZE = 10

/** Hàng đợi duyệt tin (mặc định trạng thái pending). */
export default function PendingPostsPage() {
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()
  const status: PostStatus = oneOf(POST_STATUSES, searchParams.get('status')) ?? 'pending'
  const page = Math.max(1, toNumber(searchParams.get('page')) ?? 1)
  const [viewingId, setViewingId] = useState<number | null>(null)
  const [rejecting, setRejecting] = useState<{ id: number; title: string } | null>(null)

  const query = useQuery({
    queryKey: queryKeys.moderation.posts(status, page, PAGE_SIZE),
    queryFn: () => moderationApi.getPosts({ status, page, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  })

  const onDone = (text: string) => {
    feedback.message.success(text)
    setViewingId(null)
    setRejecting(null)
    void queryClient.invalidateQueries({ queryKey: queryKeys.moderation.all })
    void queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })
  }

  const approve = useMutation({ mutationFn: moderationApi.approvePost, onSuccess: () => onDone('Đã duyệt tin') })
  const reject = useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) => moderationApi.rejectPost(id, { reason }),
    onSuccess: () => onDone('Đã từ chối tin'),
  })

  const setParams = (next: { status?: PostStatus; page?: number }) => {
    const params = new URLSearchParams(searchParams)
    if (next.status) params.set('status', next.status)
    if (next.page) params.set('page', String(next.page))
    setSearchParams(params)
  }

  const isPending = status === 'pending'

  const actionButtons = (p: { postId: number; title: string }, size: 'small' | 'middle' = 'small') =>
    isPending && (
      <Space wrap>
        <Popconfirm title="Duyệt tin này?" okText="Duyệt" cancelText="Hủy" onConfirm={() => approve.mutate(p.postId)}>
          <Button size={size} type="primary" icon={<CheckOutlined />} loading={approve.isPending && approve.variables === p.postId}>
            Duyệt
          </Button>
        </Popconfirm>
        <Button size={size} danger icon={<CloseOutlined />} onClick={() => setRejecting({ id: p.postId, title: p.title })}>
          Từ chối
        </Button>
      </Space>
    )

  const columns: TableColumnsType<PostSummaryDto> = [
    {
      title: 'Tin đăng',
      key: 'title',
      render: (_, p) => (
        <Space direction="vertical" size={0}>
          <Typography.Link onClick={() => setViewingId(p.postId)}>{p.title}</Typography.Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {POST_TYPE_SHORT_LABELS[p.postType]} · {p.areaName} · {formatMoney(p.price)}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Người đăng',
      key: 'owner',
      width: 160,
      render: (_, p) => (
        <Link to={`/users/${p.ownerId}`} target="_blank">
          {p.ownerName}
        </Link>
      ),
    },
    { title: 'Ngày gửi', dataIndex: 'createdAt', width: 150, render: (v: string) => formatDateTime(v) },
    { title: 'Trạng thái', dataIndex: 'status', width: 120, render: (v: string) => <EnumTag map={POST_STATUS_LABELS} value={v} /> },
    {
      title: 'Thao tác',
      key: 'actions',
      width: 260,
      render: (_, p) => (
        <Space wrap>
          <Button size="small" icon={<EyeOutlined />} onClick={() => setViewingId(p.postId)}>
            Xem
          </Button>
          {actionButtons(p)}
        </Space>
      ),
    },
  ]

  const viewing = query.data?.items.find((p) => p.postId === viewingId)

  return (
    <>
      <PageTitle
        title="Duyệt tin đăng"
        subTitle={query.data ? `${query.data.totalCount} tin` : undefined}
        extra={
          <Select<PostStatus>
            value={status}
            style={{ width: 180 }}
            options={toOptions(POST_STATUSES, POST_STATUS_LABELS)}
            onChange={(s) => setParams({ status: s, page: 1 })}
          />
        }
      />
      {query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Table<PostSummaryDto>
          rowKey="postId"
          columns={columns}
          dataSource={query.data?.items ?? []}
          loading={query.isFetching}
          scroll={{ x: 860 }}
          locale={{ emptyText: isPending ? 'Không có tin nào chờ duyệt' : 'Không có dữ liệu' }}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: query.data?.totalCount ?? 0,
            showSizeChanger: false,
            onChange: (p) => setParams({ page: p }),
          }}
        />
      )}

      <PostReviewDrawer
        postId={viewingId}
        onClose={() => setViewingId(null)}
        footer={viewing && isPending ? actionButtons(viewing, 'middle') : undefined}
      />
      <RejectPostModal
        open={rejecting !== null}
        postTitle={rejecting?.title}
        loading={reject.isPending}
        onCancel={() => setRejecting(null)}
        onSubmit={(reason) => rejecting && reject.mutate({ id: rejecting.id, reason })}
      />
    </>
  )
}
