import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Badge, Button, Space, Table, Tabs, Typography, type TableColumnsType } from 'antd'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { moderationApi } from '../../../api/moderation'
import { queryKeys } from '../../../api/queryKeys'
import { EnumTag } from '../../../components/shared/EnumTag'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult } from '../../../components/shared/QueryState'
import { REPORT_STATUSES, type HandleReportRequest, type ReportDto, type ReportStatus } from '../../../types'
import { formatDateTime } from '../../../utils/datetime'
import { feedback } from '../../../utils/feedback'
import { POST_STATUS_LABELS, REPORT_STATUS_LABELS } from '../../../utils/labels'
import { oneOf, toNumber } from '../../../utils/query'
import { HandleReportModal } from '../components/HandleReportModal'

const PAGE_SIZE = 10

export default function ReportsPage() {
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()
  const status: ReportStatus = oneOf(REPORT_STATUSES, searchParams.get('status')) ?? 'pending'
  const page = Math.max(1, toNumber(searchParams.get('page')) ?? 1)
  const [handling, setHandling] = useState<ReportDto | null>(null)

  const query = useQuery({
    queryKey: queryKeys.moderation.reports(status, page, PAGE_SIZE),
    queryFn: () => moderationApi.getReports({ status, page, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  })

  const handle = useMutation({
    mutationFn: ({ id, body }: { id: number; body: HandleReportRequest }) => moderationApi.handleReport(id, body),
    onSuccess: () => {
      feedback.message.success('Đã xử lý báo cáo')
      setHandling(null)
      void queryClient.invalidateQueries({ queryKey: queryKeys.moderation.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })
    },
  })

  const columns: TableColumnsType<ReportDto> = [
    { title: 'Thời gian', dataIndex: 'createdAt', width: 140, render: (v: string) => formatDateTime(v) },
    {
      title: 'Đối tượng bị báo cáo',
      key: 'target',
      render: (_, r) => (
        <Space direction="vertical" size={0}>
          {r.postId ? (
            <Space size={4} wrap>
              <Typography.Text type="secondary">Tin:</Typography.Text>
              <Link to={`/posts/${r.postId}`} target="_blank">
                {r.postTitle ?? `#${r.postId}`}
              </Link>
              {r.postStatus && <EnumTag map={POST_STATUS_LABELS} value={r.postStatus} />}
            </Space>
          ) : (
            <Typography.Text type="secondary">Người dùng</Typography.Text>
          )}
          <Space size={4}>
            <Typography.Text type="secondary">Chịu trách nhiệm:</Typography.Text>
            <Link to={`/users/${r.targetUserId}`} target="_blank">
              {r.targetUserName}
            </Link>
            <Link to={`/moderator/violations?userId=${r.targetUserId}`} style={{ fontSize: 12 }}>
              (lịch sử)
            </Link>
          </Space>
        </Space>
      ),
    },
    {
      title: 'Lý do',
      key: 'reason',
      render: (_, r) => (
        <Space direction="vertical" size={0}>
          <Typography.Text strong>{r.reasonName}</Typography.Text>
          {r.description && (
            <Typography.Paragraph type="secondary" ellipsis={{ rows: 2, tooltip: r.description }} style={{ margin: 0 }}>
              {r.description}
            </Typography.Paragraph>
          )}
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            Người báo cáo: {r.reporterName}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Chờ xử lý',
      dataIndex: 'pendingReportsOnTarget',
      width: 90,
      align: 'center',
      render: (v: number) => <Badge count={v} showZero color={v >= 3 ? 'red' : 'gold'} />,
    },
    {
      title: status === 'pending' ? 'Thao tác' : 'Kết quả',
      key: 'action',
      width: 220,
      render: (_, r) =>
        r.status === 'pending' ? (
          <Button type="primary" size="small" onClick={() => setHandling(r)}>
            Xử lý
          </Button>
        ) : (
          <Space direction="vertical" size={0}>
            <EnumTag map={REPORT_STATUS_LABELS} value={r.status} />
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {r.handlerName ?? '—'} · {formatDateTime(r.handledAt)}
            </Typography.Text>
            {r.handledNote && <Typography.Text style={{ fontSize: 12 }}>{r.handledNote}</Typography.Text>}
          </Space>
        ),
    },
  ]

  return (
    <>
      <PageTitle title="Báo cáo vi phạm" subTitle={query.data ? `${query.data.totalCount} báo cáo` : undefined} />
      <Tabs
        activeKey={status}
        onChange={(k) => setSearchParams({ status: k })}
        items={REPORT_STATUSES.map((s) => ({ key: s, label: REPORT_STATUS_LABELS[s].label }))}
      />
      {query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Table<ReportDto>
          rowKey="reportId"
          columns={columns}
          dataSource={query.data?.items ?? []}
          loading={query.isFetching}
          scroll={{ x: 900 }}
          locale={{ emptyText: 'Không có báo cáo' }}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: query.data?.totalCount ?? 0,
            showSizeChanger: false,
            onChange: (p) => setSearchParams({ status, page: String(p) }),
          }}
        />
      )}
      <HandleReportModal
        report={handling}
        loading={handle.isPending}
        onCancel={() => setHandling(null)}
        onSubmit={(body) => handling && handle.mutate({ id: handling.reportId, body })}
      />
    </>
  )
}
